#!/bin/bash

echo "Loading shared variables"
source ./variables-shared.sh

# Create a jumpbox on a separate VNET
echo "Creating overall resource group"
az group create --name ${RESOURCE_GROUP} \
    --location ${LOCATION_NAME}

echo "Creating enclosing resource group"
az group create --name $RESOURCE_GROUP --location $LOCATION

echo "Creating virtual network and subnets"
az network vnet create --resource-group ${RESOURCE_GROUP} \
    --name ${VNET_NAME} --address-prefixes ${VNET_PREFIXES} 

az network vnet subnet create --resource-group ${RESOURCE_GROUP} \
    --vnet-name ${VNET_NAME} --name shared-subnet --address-prefix $VNET_SHARED_PREFIX
az network vnet subnet create --resource-group ${RESOURCE_GROUP} \
    --vnet-name ${VNET_NAME} --name master-subnet --address-prefix $VNET_MASTER_PREFIX
az network vnet subnet create --resource-group ${RESOURCE_GROUP} \
    --vnet-name ${VNET_NAME} --name agent-subnet --address-prefix $VNET_AGENT_PREFIX

# Create a key vault
az keyvault create --resource-group ${RESOURCE_GROUP} \
    --location ${LOCATION_NAME} --sku standard \
    --name ${KEYVAULT_NAME}

# Create and store the ssh keys
if [ ! -f ~/.ssh/funcexpl ]; then
    echo "Creating jumpbox SSH keys"

    # TODO - check to see if the keys exist before regenerating
    ssh-keygen -f ~/.ssh/funcexpl -P ""
fi

az keyvault secret set --vault-name ${KEYVAULT_NAME} \
    --name funcexpl-ssh --file  ~/.ssh/funcexpl
az keyvault secret set --vault-name ${KEYVAULT_NAME} \
    --name funcexpl-ssh-pub --file  ~/.ssh/funcexpl.pub
SSH_KEYDATA=`cat ~/.ssh/funcexpl.pub`

# Create the monitoring VM
az vm create --resource-group ${RESOURCE_GROUP} --name ${MONVM_NAME} \
    --admin-username ${USERNAME} --ssh-key-value "${SSH_KEYDATA}" \
    --authentication-type ssh \
    --size ${VM_SIZE} --image ${VM_IMAGE} \
    --storage-sku Premium_LRS --location ${LOCATION_NAME} \
    --vnet-name ${VNET_NAME} \
    --subnet shared-subnet \
    --private-ip-address $MONVM_IP

