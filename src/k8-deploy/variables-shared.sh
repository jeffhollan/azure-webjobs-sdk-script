#!/bin/bash

# Global settings
export RESOURCE_GROUP=funcexplore-shared-rg
export LOCATION=westus2
 
# Virtual network settings
export VNET_NAME=funcexpk8-vnet
export VNET_PREFIXES=10.1.0.0/16
export VNET_SHARED_PREFIX=10.1.0.0/24
export VNET_MASTER_PREFIX=10.1.1.0/24
export VNET_AGENT_PREFIX=10.1.2.0/24

# Shared Keyvault
export KEYVAULT_NAME=funcexpk8-kv

# Container registry
export REGISTRY_NAME=funcexpk8reg
export REGISTRY_LOGINSERVER=funcexpk8reg.azurecr.io

export MONVM_NAME=funcexplmon
export USERNAME=masimms
export MONVM_IP=10.1.0.10

export VM_SIZE=Standard_DS


--authentication-type ssh \
    --size ${VM_SIZE} --image ${VM_IMAGE} \
    --storage-sku Premium_LRS --location ${LOCATION_NAME} \
    --vnet-name ${VNET_NAME} \
    --subnet shared-subnet \
    --private-ip-address $MONVM_IP