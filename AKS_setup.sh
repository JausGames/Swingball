# Declare necessary variables, modify them according to your needs
AKS_RESOURCE_GROUP=Swingball_RG     # Name of the resource group your AKS cluster will be created in
AKS_NAME=gamecluster                 # Name of your AKS cluster
AKS_LOCATION=francecentral          # Azure region in which you'll deploy your AKS cluster

# Create the Resource Group where your AKS resource will be installed
az group create --name $AKS_RESOURCE_GROUP --location $AKS_LOCATION

# Create the AKS cluster - this might take some time. Type 'az aks create -h' to see all available options

# The following command will create a four Node AKS cluster. Node size is Standard A1 v1 and Kubernetes version is 1.27.1. Plus, SSH keys will be generated for you, use --ssh-key-value to provide your values
az aks create --resource-group $AKS_RESOURCE_GROUP --name $AKS_NAME --node-count 4 --generate-ssh-keys --node-vm-size Standard_A4_v2 --kubernetes-version 1.27.1 --enable-node-public-ip

# Install kubectl
sudo az aks install-cli

# Get credentials for your new AKS cluster
az aks get-credentials --resource-group $AKS_RESOURCE_GROUP --name $AKS_NAME

###     Create cluster
# Declare necessary variables, modify them according to your needs
AKS_RESOURCE_GROUP=Swingball_RG     # Name of the resource group your AKS cluster will be created in
AKS_NAME=gamecluster                 # Name of your AKS cluster
AKS_LOCATION=francecentral          # Azure region in which you'll deploy your AKS cluster
AKS_SECUGROUP=aks-agentpool-12539702-nsg
AKS_NAMESPACE=gameserver          # Azure namespace
RESOURCE_GROUP_WITH_AKS_RESOURCES=MC_Swingball_RG_gamecluster_francecentral        # Azure namespace

# Create the Resource Group where your AKS resource will be installed
az group create --name $AKS_RESOURCE_GROUP --location $AKS_LOCATION

# Create the AKS cluster - this might take some time. Type 'az aks create -h' to see all available options

# The following command will create a four Node AKS cluster. Node size is Standard A1 v1 and Kubernetes version is 1.24.6. Plus, SSH keys will be generated for you, use --ssh-key-value to provide your values
az aks create --resource-group $AKS_RESOURCE_GROUP --name $AKS_NAME --node-count 1 --generate-ssh-keys --node-vm-size standard_f4s_v2 --kubernetes-version 1.24.6 --enable-node-public-ip 

# Install kubectl
sudo az aks install-cli

# Get credentials for your new AKS cluster
az aks get-credentials --resource-group $AKS_RESOURCE_GROUP --name $AKS_NAME




###   Security group 
az network nsg rule create \
  --resource-group $RESOURCE_GROUP_WITH_AKS_RESOURCES \
  --nsg-name $AKS_SECUGROUP \
  --name AgonesUDP \
  --access Allow \
  --protocol Udp \
  --direction Inbound \
  --priority 520 \
  --source-port-range "*" \
  --destination-port-range 7000-8000



###   Installing agones :
helm repo add agones https://agones.dev/chart/stable
helm repo update

helm install my-release agones/agones --set "gameservers.namespaces={default,gameserver}" --namespace agones-system

kubectl create namespace gameserver
kubectl create namespace agones-system



#   test api :
https://management.azure.com/subscriptions/subid1/resourceGroups