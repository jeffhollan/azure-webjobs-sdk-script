#!/bin/bash

export RESOURCE_GROUP=masfuncexplore-rg
export LOCATION=westus2

export CLUSTER_NAME=funcexpk8
export REGISTRY_NAME=funcexpk8reg
export REGISTRY_LOGINSERVER=funcexpk8reg.azurecr.io
export DNS_PREFIX=funcexpk8
export VNET_NAME=funcexpk8-vnet
export VNET_PREFIXES=10.1.0.0/16
export VNET_MASTER_PREFIX=10.1.0.0/24
export VNET_AGENT_PREFIX=10.1.1.0/24
export KEYVAULT_NAME=funcexpk8-kv
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
echo az acs create --orchestrator-type=kubernetes \
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
    --master-vm-size Standard_DS1_v2 \
    --master-storage-profile ManagedDisks \
    --master-vnet-subnet-id $masterSubnetId \
    --orchestrator-version 1.8.1 \
    --ssh-key-value "$SSH_KEYDATA"  

  #--generate-ssh-key
#                     [--service-principal SERVICE_PRINCIPAL] [--windows]
#                     [--client-secret CLIENT_SECRET] [--validate]
#                     [--master-first-consecutive-static-ip MASTER_FIRST_CONSECUTIVE_STATIC_IP]
#                     [--location LOCATION] [--agent-vm-size AGENT_VM_SIZE]