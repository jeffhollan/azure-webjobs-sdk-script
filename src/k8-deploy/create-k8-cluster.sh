#!/bin/bash

export CLUSTER_NAME=funcexpk8
export DNS_PREFIX=funcexpk8
export AGENT_VM_SIZE=Standard_DS4_v2
export AGENT_COUNT=3

echo "Creating enclosing resource group"
az group create --name $RESOURCE_GROUP --location $LOCATION

echo "Creating virtual network and subnets"
az network vnet create --resource-group ${RESOURCE_GROUP} \
    --name ${VNET_NAME} --address-prefixes ${VNET_PREFIXES} 
az network vnet subnet create --resource-group ${RESOURCE_GROUP} \
    --vnet-name ${VNET_NAME} --name master-subnet --address-prefix $VNET_MASTER_PREFIX
az network vnet subnet create --resource-group ${RESOURCE_GROUP} \
    --vnet-name ${VNET_NAME} --name agent-subnet --address-prefix $VNET_AGENT_PREFIX

masterSubnetId=$(az network vnet subnet show --resource-group ${RESOURCE_GROUP} --vnet-name ${VNET_NAME} --name "master-subnet" --query id --output tsv)
agentSubnetId=$(az network vnet subnet show --resource-group ${RESOURCE_GROUP} --vnet-name ${VNET_NAME} --name "agent-subnet" --query id --output tsv)

# Create a key vault
echo "Creating key vault"
az keyvault create --resource-group ${RESOURCE_GROUP} \
    --location ${LOCATION} --sku standard \
    --name ${KEYVAULT_NAME}

# Create and store the jumpbox keys
if [ ! -f ~/.ssh/funcexplore ]; then
    echo "Creating cluster SSH keys"

    # TODO - check to see if the keys exist before regenerating
    ssh-keygen -f ~/.ssh/funcexplore -P ""
fi
export SSH_KEYDATA=`cat ~/.ssh/funcexplore.pub`

az keyvault secret set --vault-name ${KEYVAULT_NAME} \
    --name funcexp-k8-ssh --file  ~/.ssh/funcexplore
az keyvault secret set --vault-name ${KEYVAULT_NAME} \
    --name funcexp-k8-ssh-pub --file  ~/.ssh/funcexplore.pub

echo "Creating container registry"
az acr create --resource-group $RESOURCE_GROUP \
    --name $REGISTRY_NAME --sku Basic --admin-enabled true
# az acr login --name $REGISTRY_NAME
# TODO - auth the acr to the cluster below

echo "Creating k8 cluster"
az acs create --orchestrator-type=kubernetes \
    --resource-group $RESOURCE_GROUP \
    --dns-prefix $DNS_PREFIX \
    --name $CLUSTER_NAME \
    --agent-vm-size $AGENT_VM_SIZE \
    --agent-count $AGENT_COUNT \
    --agent-vnet-subnet-id $agentSubnetId \
    --agent-storage-profile ManagedDisks \
    --agent-osdisk-size 512 \
    --dns-prefix $DNS_PREFIX \
    --master-count 3 \
    --master-vm-size Standard_D2_v2 \
    --master-storage-profile ManagedDisks \
    --master-vnet-subnet-id $masterSubnetId \
    --master-first-consecutive-static-ip 10.1.0.5 \
    --orchestrator-version 1.8.1 \
    --generate-ssh-keys

   #\ --ssh-key-value "$SSH_KEYDATA"  

az acs kubernetes get-credentials --resource-group=$RESOURCE_GROUP --name=$CLUSTER_NAME

echo "Registering ACR registry to k8"
echo "TODO"

echo "Tagging agent node as monitoring"
echo "TODO"

echo "Deploying system services"

  #--generate-ssh-key
#                     [--service-principal SERVICE_PRINCIPAL] [--windows]
#                     [--client-secret CLIENT_SECRET] [--validate]
#                     [--master-first-consecutive-static-ip MASTER_FIRST_CONSECUTIVE_STATIC_IP]
#                     [--location LOCATION] [--agent-vm-size AGENT_VM_SIZE]