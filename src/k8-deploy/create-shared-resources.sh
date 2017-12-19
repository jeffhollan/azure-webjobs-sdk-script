#!/bin/bash

echo "Loading shared variables"
source ./variables-shared.sh

echo "Creating enclosing resource group"
az group create --name $RESOURCE_GROUP --location $LOCATION

echo "Creating virtual network and subnets"
az network vnet create --resource-group ${RESOURCE_GROUP} \
    --name ${MON_VNET_NAME} --address-prefixes ${MON_VNET_PREFIXES} 

az network vnet subnet create --resource-group ${RESOURCE_GROUP} \
    --vnet-name ${MON_VNET_NAME} --name mgmt-subnet --address-prefix $MON_VNET_MASTER_PREFIX

# Create a key vault
az keyvault create --resource-group ${RESOURCE_GROUP} \
    --location ${LOCATION} --sku standard \
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
    --admin-username ${USERNAME} \
    --authentication-type ssh \
    --image $MONVM_IMAGE \
    --size ${VM_SIZE} \
    --storage-sku Premium_LRS --location ${LOCATION} \
    --vnet-name ${MON_VNET_NAME} \
    --subnet mgmt-subnet \
    --private-ip-address $MONVM_IP \
    --custom-data monserver-cloud-init.txt \
    --data-disk-sizes-gb 1024 \
    --ssh-key-value "${SSH_KEYDATA}"

# Tunnel open the monitoring ports (TEMP)


# Create the container registry
echo "Creating container registry"
# az acr login --name $REGISTRY_NAME
# TODO - auth the acr to the cluster below

az acr create --resource-group $RESOURCE_GROUP \
    --name $REGISTRY_NAME --sku Basic --admin-enabled true
