# Gameproject 

## TODO list
Sound FX : 
- Ninja
    - Footsteps
    - Sound when defense move succeed
- Knight 
    - All

# INFRA

## Build images : 
`docker build --target GameServer -t jausseran/swingballserver .` 
`docker build --target MatchMakingAPI -t jausseran/mmapi .` 

## Run API local: 
`docker run -d jausseran/mmapi -n mmapi` 

## Run gameserver : 

AZURE  : 



Minikube using docker : 

``` 
minikube start --kubernetes-version v1.26.6 -p gamecluster

kubectl create namespace agones-system
kubectl apply --server-side -f https://raw.githubusercontent.com/googleforgames/agones/release-1.35.0/install/yaml/install.yaml
helm install my-release agones/agones --namespace=agones-system --create-namespace --set "agones.featureGates=PlayerTracking=true"

kubectl create namespace agones-system
kubectl create serviceaccount agones-sdk -n gameserver
kubectl create -f https://raw.githubusercontent.com/googleforgames/agones/main/install/yaml/install.yaml
kubectl create namespace gameserver
kubectl create -f https://raw.githubusercontent.com/jausgames/Swingball/main/gameserver.yaml --namespace gameserver

kubectl create -f fleet.yaml --namespace gameserver
kubectl create -f fleetautoscaler.yaml --namespace gameserver

kubectl delete fleet fleet --namespace gameserver
kubectl delete fas gamefleet-autoscaler --namespace gameserver
```

