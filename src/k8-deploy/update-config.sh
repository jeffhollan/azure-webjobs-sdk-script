#!/bin/bash

kubectl create configmap functions-config \
    --from-file=appsettings.json \
    --from-file=host.json \
    -o yaml 


