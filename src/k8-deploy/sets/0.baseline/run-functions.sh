#!/bin/bash

# Copy the function set to the share
# TODO

# Delete any outstanding functions host
echo "Deleting functions worker deployment"
kubectl delete -f baseline-functions-worker.yaml

# Set the deployment configuration
echo "Deleting functions worker configuration"
kubectl delete configmap deployment-config 

echo "Creating functions worker configuration"
kubectl create configmap deployment-config \
    --from-literal="FUNCTION_DEPLOYMENT=azureFiles|baseline-simple|1" \
    --from-literal="FUNCTION_STORAGE_PROVIDER=azureFiles" \
    --from-literal="FUNCTION_SAMPLE_SET=baseline-simple" \
    --from-literal="FUNCTION_PATH=/scripts/"

# Create the storage mount with the target file set

# Deploy the function
echo "Creating functions worker deployment"
kubectl apply -f baseline-functions-worker.yaml

# Deploy the internal test agent
# TODO
