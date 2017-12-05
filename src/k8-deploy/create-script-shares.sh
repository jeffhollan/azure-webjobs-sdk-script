#!/bin/bash

# Variables
RESOURCE_GROUP=masfuncresources-rg
STORAGE_NAME=masfunctest
LOCATION=eastus
SHARE_NAME=scriptshare

# Create the resources
az group create --name $RESOURCE_GROUP --location $LOCATION
az storage account create --resource-group $RESOURCE_GROUP \
    --name $STORAGE_NAME --location $LOCATION
az storage share create --account-name $STORAGE_NAME \
    --name $SHARE_NAME

STORAGE_KEY=$(az storage account keys list --resource-group $RESOURCE_GROUP --account-name $STORAGE_NAME --query "[0].value" -o tsv)
echo -n $STORAGE_NAME > "storage.txt"
echo -n $STORAGE_KEY > "storage-key.txt"

kubectl create secret generic script-azure-file \
    --from-file=azurestorageaccountname=./storage.txt \
    --from-file=azurestorageaccountkey=./storage-key.txt

rm storage.txt
rm storage-key.txt



