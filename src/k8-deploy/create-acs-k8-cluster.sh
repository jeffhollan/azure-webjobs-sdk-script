#!/bin/bash

source ./variables-shared.sh

export K8_RESOURCE_GROUP=masfunck8-baseline-rg
export CLUSTER_NAME=funcexpk8p2
export DNS_PREFIX=funcexpk8p2
export AGENT_VM_SIZE=Standard_DS4_v2
export AGENT_COUNT=3

# Virtual network settings
export K8_VNET_NAME=funcexp-k8base-vnet
export K8_VNET_PREFIXES=10.1.0.0/16
export K8_VNET_MASTER_PREFIX=10.1.1.0/24
export K8_VNET_AGENT_PREFIX=10.1.2.0/24

echo "Creating enclosing resource group"
az group create --name $K8_RESOURCE_GROUP --location $LOCATION

echo "Creating virtual network and subnets"
az network vnet create --resource-group ${K8_RESOURCE_GROUP} \
    --name ${K8_VNET_NAME} --address-prefixes ${VNET_PREFIXES} 

az network vnet subnet create --resource-group ${K8_RESOURCE_GROUP} \
    --vnet-name ${K8_VNET_NAME} --name master-subnet --address-prefix $K8_VNET_MASTER_PREFIX
az network vnet subnet create --resource-group ${K8_RESOURCE_GROUP} \
    --vnet-name ${K8_VNET_NAME} --name agent-subnet --address-prefix $K8_VNET_AGENT_PREFIX

echo "Looking up subnets"
masterSubnetId=$(az network vnet subnet show --resource-group ${K8_RESOURCE_GROUP} --vnet-name ${K8_VNET_NAME} --name "master-subnet" --query id --output tsv)
agentSubnetId=$(az network vnet subnet show --resource-group ${K8_RESOURCE_GROUP} --vnet-name ${K8_VNET_NAME} --name "agent-subnet" --query id --output tsv)
 
# Create and store the jumpbox keys
export SSH_KEYDATA=`cat ~/.ssh/funcexpl.pub`

echo "Creating k8 cluster"
az acs create --orchestrator-type=kubernetes \
    --resource-group $K8_RESOURCE_GROUP \
    --dns-prefix $DNS_PREFIX \
    --name $CLUSTER_NAME \
    --agent-vm-size $AGENT_VM_SIZE \
    --agent-count $AGENT_COUNT \
    --agent-storage-profile ManagedDisks \
    --agent-vnet-subnet-id $agentSubnetId \
    --agent-osdisk-size 512 \    
    --master-count 3 \
    --master-vm-size Standard_D2_v2 \
    --master-storage-profile ManagedDisks \
    --master-vnet-subnet-id $masterSubnetId \
    --master-first-consecutive-static-ip 10.1.1.5 \
    --orchestrator-version 1.8.1 \
    --ssh-key-value "$SSH_KEYDATA" \
    --debug

   #\ --ssh-key-value "$SSH_KEYDATA"  



az acs kubernetes get-credentials \
    --resource-group=$K8_RESOURCE_GROUP --name=$CLUSTER_NAME

az acs kubernetes browse --resource-group $K8_RESOURCE_GROUP \
    --name $CLUSTER_NAME

# Label one node for monitoring and update to enable ELK
hostname=${CLUSTER_NAME}mgmt.${LOCATION}.cloudapp.azure.com
username=azureuser
agentName=$(kubectl get nodes | grep agentpool | head -1 | cut -d' ' -f1)

scp -o StrictHostKeyChecking=no -i ~/.ssh/id_rsa \
    ~/.ssh/id_rsa.pub ${username}@${hostname}:.ssh/id_rsa.pub
ssh ${username}@${hostname} chmod 0600 .ssh/id_rsa.pub

scp -o StrictHostKeyChecking=no \
    -o ProxyCommand="ssh -o StrictHostKeyChecking=no -W %h:%p ${username}@${hostname}" \
    60-elk.conf ${username}@${agentName}:60-elk.conf

# TODO - figure out why this is auto-blocked
#ssh -o StrictHostKeyChecking=no \
    #-o ProxyCommand="ssh -o StrictHostKeyChecking=no -W %h:%p ${username}@${hostname}" \
    #sudo cp 60-elk.conf /etc/sysctl.d/60-elk.conf 
# These commands need to be run interactively:
# ssh ${username}@${hostname} 
# ssh $agentName
# sudo cp 60-elk.conf /etc/sysctl.d/
# sudo service procps restart

kubectl label nodes $agentName nodetype=monitoring



echo "Registering ACR registry to k8"
echo "TODO"

echo "Deploying system services"

  #--generate-ssh-key
#                     [--service-principal SERVICE_PRINCIPAL] [--windows]
#                     [--client-secret CLIENT_SECRET] [--validate]
#                     [--master-first-consecutive-static-ip MASTER_FIRST_CONSECUTIVE_STATIC_IP]
#                     [--location LOCATION] [--agent-vm-size AGENT_VM_SIZE]