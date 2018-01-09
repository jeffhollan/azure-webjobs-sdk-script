#!/bin/bash

# Global settings
export RESOURCE_GROUP=funcexplore-shared-rg
export LOCATION=centralus
 
# Virtual network settings
#export MON_VNET_NAME=funcexp-monitoring-vnet
#export MON_VNET_PREFIXES=172.16.1.0/24
#export MON_VNET_MASTER_PREFIX=172.16.1.0/24


# Shared Keyvault
export KEYVAULT_NAME=funcexpk8-kv

# Container registry
export REGISTRY_NAME=funcexpk8reg
export REGISTRY_LOGINSERVER=funcexpk8reg.azurecr.io

# Monitoring server
export VM_SIZE=Standard_DS4_v2
export MONVM_NAME=masfuncmon
export USERNAME=masimms
export MONVM_IMAGE=UbuntuLTS