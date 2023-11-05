# AgonesServerTest

## Build images : 
`docker build --target GameServer -t jausseran/swingballserver .` 
`docker build --target MatchMakingAPI -t jausseran/mmapi .` 

## Run API : 
`docker run -d jausseran/mmapi -n mmapi` 

## Run gameserver : 
### Minikube using docker : 

``` 
minikube start --namespace gameserver --driver docker --kubernetes-version v1.26.3 -p gamecluster 
minikube start --namespace gameserver --driver docker --kubernetes-version v1.26.3
minikube kubectl -- create namespace agones-system
minikube kubectl -- create -f https://raw.githubusercontent.com/googleforgames/agones/main/install/yaml/install.yaml
minikube kubectl -- create namespace gameserver
minikube kubectl -- create -f https://raw.githubusercontent.com/jausgames/Swingball/main/gameserver.yaml --namespace gameserver
```

