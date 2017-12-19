#!/bin/bash

source ./variables-shared.sh

AKS_RESOURCE_GROUP=functions-test-baseline-rg
AKS_NAME=functest-baseline-aks
AKS_VM_SIZE=Standard_DS4_v2
AKS_AGENT_COUNT=3
SSH_KEYDATA=`cat ~/.ssh/funcexpl.pub`

echo "Creating k8 cluster overall resource group"
az group create --name ${AKS_RESOURCE_GROUP} \
    --location ${LOCATION}

az aks create --resource-group ${AKS_RESOURCE_GROUP} --name ${AKS_NAME} \
   --admin-username ${USERNAME} 
   --location ${LOCATION} --dns-name-prefix ${AKS_NAME} \
   --agent-vm-size ${AKS_VM_SIZE} \
   --agent-count 3 \
   --ssh-key-value "${SSH_KEYDATA}" 