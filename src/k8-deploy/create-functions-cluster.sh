#!/bin/bash

set_variables() { 
    export RESOURCE_GROUP=mask8test-rg
    export KEYVAULT_NAME=mask8test-kv
    export LOCATION=centralus

    export MGMT_VM_NAME=masmonk8
    export MGMT_DNS_NAME=masmonk8
    export MGMT_VM_SIZE=Standard_DS4_v2
    export MGMT_VM_IMAGE=UbuntuLTS
    export MGMT_USERNAME=masimms

    export K8_NODE_SIZE=Standard_DS4_v2
    export K8_DISK_SIZE=1023
    export K8_NODE_COUNT=3
    export K8_DNS_PREFIX=mask8test-k8
    export K8_CLUSTER_NAME=mask8test-k8

    # Shared Keyvault
    export KEYVAULT_NAME=funcexpk8-kv

    # Container registry
    export REGISTRY_NAME=funcexpk8reg
    export REGISTRY_LOGINSERVER=funcexpk8reg.azurecr.io

    # Shared resources
    export SHARE_NAME=scriptshare
    export STORAGE_NAME=masfunctest
}


deploy_shared() { 
    # Create the basic resource group
    az group create --name $RESOURCE_GROUP --location $LOCATION

    # Create a key vault
    az keyvault create --resource-group ${RESOURCE_GROUP} \
        --location ${LOCATION} --sku standard \
        --name ${KEYVAULT_NAME}

    # Create and store the jumpbox keys
    if [ ! -f ~/.ssh/vnettest-jumpbox ]; then
        echo "Creating jumpbox SSH keys"

        # TODO - check to see if the keys exist before regenerating
        ssh-keygen -f ~/.ssh/vnettest-jumpbox -P ""
    fi
    export SSH_KEYDATA=`cat ~/.ssh/vnettest-jumpbox.pub`

    az keyvault secret set --vault-name ${KEYVAULT_NAME} \
        --name jumpbox-ssh --file  ~/.ssh/vnettest-jumpbox
    az keyvault secret set --vault-name ${KEYVAULT_NAME} \
        --name jumpbox-ssh-pub --file  ~/.ssh/vnettest-jumpbox.pub

    # Create the container registry
    echo "Creating container registry"

    # az acr login --name $REGISTRY_NAME
    # TODO - auth the acr to the cluster below
    az acr create --resource-group $RESOURCE_GROUP \
        --name $REGISTRY_NAME --sku Basic --admin-enabled true

    # az ad sp create-for-rbac --scopes /subscriptions/3e9c25fc-55b3-4837-9bba-02b6eb204331/resourceGroups/mask8test-rg/providers/Microsoft.ContainerRegistry/registries/funcexpk8reg --role Owner --password <password>
}


deploy_monitoring_vm() {
    # Create the monitoring VM
    az vm create --resource-group $RESOURCE_GROUP --name $MGMT_VM_NAME \
        --location $LOCATION --image $MGMT_VM_IMAGE \
        --admin-username $MGMT_USERNAME --ssh-key-value "${SSH_KEYDATA}" \
        --authentication-type ssh \
        --size $MGMT_VM_SIZE \
        --storage-sku Premium_LRS \
        --public-ip-address-dns-name $MGMT_DNS_NAME \
        --custom-data monserver-cloud-init.txt \
        --data-disk-sizes-gb 1024

    # Allow access to monitoring ports
    # TODO - lock down publish from K8 host
    nsgName=$(az network nsg list --resource-group $RESOURCE_GROUP | \
        jq ".[].name|select(startswith(\"$MGMT_VM_NAME\"))" | tr -d '"')
        
    az network nsg rule create --resource-group $RESOURCE_GROUP --nsg-name $nsgName \
        --name allow-monitoring-influx-rule --priority 105 \
        --description "Allow incoming connections to InfluxDB" \
        --protocol tcp --access Allow --direction Inbound \
        --destination-port-ranges 8086

    az network nsg rule create --resource-group $RESOURCE_GROUP --nsg-name $nsgName \
        --name allow-monitoring-grafana-rule --priority 106 \
        --description "Allow incoming connections to Grafana" \
        --protocol tcp --access Allow --direction Inbound \
        --destination-port-ranges 3000

    az network nsg rule create --resource-group $RESOURCE_GROUP --nsg-name $nsgName \
        --name allow-monitoring-elasticsearch-rule --priority 107 \
        --description "Allow incoming connections to ElasticSearch" \
        --protocol tcp --access Allow --direction Inbound \
        --destination-port-ranges 9200

    az network nsg rule create --resource-group $RESOURCE_GROUP --nsg-name $nsgName \
        --name allow-monitoring-kibana-rule --priority 108 \
        --description "Allow incoming connections to Kibana" \
        --protocol tcp --access Allow --direction Inbound \
        --destination-port-ranges 5601

    # TODO - may need to add inbound port allows
}

deploy_aks_k8() { 
    echo "Creating k8 cluster"
    az aks create --resource-group ${RESOURCE_GROUP} \
        --name ${K8_CLUSTER_NAME} \
        --admin-username ${MGMT_USERNAME} 
        --location ${LOCATION} \
        --dns-name-prefix ${K8_DNS_PREFIX} \
        --agent-vm-size ${K8_NODE_SIZE} \
        --agent-count $K8_NODE_COUNT \
        --ssh-key-value "${SSH_KEYDATA}" 
}

deploy_acs_k8() { 
    echo "Creating k8 cluster"
  
    subid=$(az account show --query id | tr -d '"')
    az ad sp create-for-rbac --role="Contributor" \
        --scopes="/subscriptions/$subid/resourceGroups/$RESOURCE_GROUP" > adrole.json
    spid=$(cat adrole.json | jq .appId | tr -d '"')
    sppw=$(cat adrole.json | jq .password | tr -d '"')

    az acs create --orchestrator-type=kubernetes \
        --resource-group $RESOURCE_GROUP \
        --dns-prefix $K8_DNS_PREFIX \
        --name $K8_CLUSTER_NAME \
        --agent-vm-size $K8_NODE_SIZE \
        --agent-count $K8_NODE_COUNT \
        --agent-storage-profile ManagedDisks \
        --agent-osdisk-size 1023 \
        --master-count 3 \
        --master-vm-size Standard_D2_v2 \
        --master-storage-profile ManagedDisks \
        --orchestrator-version 1.8.1 \
        --service-principal $spid \
        --client-secret $sppw \
        --ssh-key-value "$SSH_KEYDATA"
        
    az acs kubernetes get-credentials \
        --resource-group=$RESOURCE_GROUP --name=$K8_CLUSTER_NAME \
        --ssh-key-file ~/.ssh/vnettest-jumpbox

    az acs kubernetes browse --resource-group $RESOURCE_GROUP \
        --name $K8_CLUSTER_NAME --ssh-key-file ~/.ssh/vnettest-jumpbox
}

configure_k8() { 

    LABEL='beta.kubernetes.io/fluentd-ds-ready=true'
    NODES=($(kubectl get nodes --selector=kubernetes.io/role=agent -o jsonpath='{.items[*].metadata.name}'))
    for node in "${NODES[@]}"
    do
        echo "adding label:$LABEL to node:$node"
        kubectl label nodes "$node" $LABEL 
    done

    # Create the service account and role bindings - TODO - create custom clusterrole
    kubectl create serviceaccount fluentd-es
    kubectl create clusterrolebinding fluentd-es \
        --clusterrole=system:heapster-with-nanny \
        --serviceaccount=kube-system:fluentd-es

    # Deploy fluentd for moving system logs to ELK
    kubectl create -f fluentd-configmap.yaml
    kubectl create -f fluentd-service.yaml

    # Deploy heapster for logging to influxdb
    kubectl create -f heapster-to-influx.yaml
}

create_shared_resources()
{
    az storage account create --resource-group $RESOURCE_GROUP \
        --name $STORAGE_NAME --location $LOCATION
    az storage share create --account-name $STORAGE_NAME \
        --name $SHARE_NAME

    STORAGE_KEY=$(az storage account keys list \
        --resource-group $RESOURCE_GROUP \
        --account-name $STORAGE_NAME --query "[0].value" -o tsv)

    echo -n $STORAGE_NAME > "storage.txt"
    echo -n $STORAGE_KEY > "storage-key.txt"

    kubectl create secret generic script-azure-file \
        --from-file=azurestorageaccountname=./storage.txt \
        --from-file=azurestorageaccountkey=./storage-key.txt

    rm storage.txt
    rm storage-key.txt
}

main() { 
    set_variables
    deploy_shared
    deploy_monitoring_vm

    deploy_acs_k8
    configure_k8
}

main