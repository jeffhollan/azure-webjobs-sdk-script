#!/bin/bash

acs-engine upgrade \
    --subscription-id $subid \
    --deployment-dir ./_output/ --
    --location $LOCATION \
    --resource-group $RESOURCE_GROUP \
    --upgrade-version 1.8.4 
    
