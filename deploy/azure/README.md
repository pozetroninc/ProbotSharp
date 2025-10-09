# Azure Web Apps Deployment Guide

This guide covers deploying ProbotSharp to Microsoft Azure using Web App for Containers.

## Architecture Overview

- **Compute**: Azure Web App for Containers (App Service)
- **Container Registry**: Azure Container Registry (ACR)
- **Database**: Azure Database for PostgreSQL
- **Cache**: Azure Cache for Redis
- **Secrets**: Azure Key Vault
- **Monitoring**: Application Insights
- **Infrastructure**: ARM templates or Bicep (existing files available)

## Prerequisites

Before deploying, ensure you have:

1. **Azure Account** with active subscription
2. **Azure CLI** installed and configured
3. **Azure Resources** (can be created via Bicep/ARM template):
   - Resource Group
   - Azure Container Registry
   - App Service Plan (Linux)
   - Web App for Containers
   - Azure Database for PostgreSQL Flexible Server
   - Azure Cache for Redis
   - Azure Key Vault (recommended)
   - Application Insights (optional)

### Install Azure CLI

```bash
# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# macOS
brew install azure-cli

# Windows
# Download from https://aka.ms/installazurecliwindows

# Verify installation
az --version

# Login to Azure
az login
```

## Quick Start Using Bicep

### 1. Deploy Infrastructure

Use the existing Bicep template to create all required Azure resources:

```bash
# Set variables
RESOURCE_GROUP="probotsharp-rg"
LOCATION="eastus"
DEPLOYMENT_NAME="probotsharp-deployment"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Deploy using Bicep
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --name $DEPLOYMENT_NAME \
  --template-file deploy/azure/azuredeploy.bicep \
  --parameters deploy/azure/azuredeploy.parameters.json \
  --parameters \
    databaseAdminPassword="YourSecurePassword123!" \
    githubAppId="123456" \
    githubWebhookSecret="your-webhook-secret"

# View outputs
az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name $DEPLOYMENT_NAME \
  --query properties.outputs
```

### 2. Configure Secrets in Key Vault

```bash
KEY_VAULT_NAME="probotsharp-kv-$(openssl rand -hex 4)"

# Store GitHub App ID
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name github-app-id \
  --value "123456"

# Store GitHub Webhook Secret
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name github-webhook-secret \
  --value "your-webhook-secret-from-github"

# Store GitHub Private Key
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name github-private-key \
  --file path/to/private-key.pem
```

### 3. Build and Deploy Container

```bash
# Login to ACR
ACR_NAME="probotsharp"
az acr login --name $ACR_NAME

# Build and push image
docker build -f src/ProbotSharp.Bootstrap.Api/Dockerfile -t probotsharp:latest .
docker tag probotsharp:latest $ACR_NAME.azurecr.io/probotsharp:latest
docker push $ACR_NAME.azurecr.io/probotsharp:latest

# Update Web App to use new image
WEBAPP_NAME="probotsharp-production"
az webapp config container set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --docker-custom-image-name $ACR_NAME.azurecr.io/probotsharp:latest \
  --docker-registry-server-url https://$ACR_NAME.azurecr.io

# Restart Web App
az webapp restart --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP
```

## GitHub Actions Deployment

### Setup GitHub Secrets

Configure the following secrets in your GitHub repository settings:

1. **AZURE_CREDENTIALS** - Service Principal credentials for Azure login
2. **AZURE_RESOURCE_GROUP** - Name of the Azure resource group
3. **ACR_USERNAME** - Azure Container Registry username
4. **ACR_PASSWORD** - Azure Container Registry password
5. **AZURE_DATABASE_CONNECTION_STRING** - PostgreSQL connection string

### Create Service Principal

```bash
# Create service principal for GitHub Actions
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

az ad sp create-for-rbac \
  --name "probotsharp-github-actions" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth

# Output is JSON - save this as AZURE_CREDENTIALS secret in GitHub
```

### Get ACR Credentials

```bash
# Enable admin user on ACR (if not already enabled)
az acr update --name $ACR_NAME --admin-enabled true

# Get credentials
az acr credential show --name $ACR_NAME

# Save username and password as ACR_USERNAME and ACR_PASSWORD in GitHub
```

### Trigger Deployment

```bash
# Push to main branch
git push origin main

# Or manually trigger
gh workflow run deploy-azure.yml
```

## Manual Deployment Steps

### 1. Create Azure Container Registry

```bash
ACR_NAME="probotsharp"
RESOURCE_GROUP="probotsharp-rg"

az acr create \
  --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME \
  --sku Basic \
  --admin-enabled true
```

### 2. Create App Service Plan

```bash
APP_SERVICE_PLAN="probotsharp-plan"

az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --is-linux \
  --sku B1
```

### 3. Create Web App for Containers

```bash
WEBAPP_NAME="probotsharp-production"

az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --name $WEBAPP_NAME \
  --deployment-container-image-name $ACR_NAME.azurecr.io/probotsharp:latest
```

### 4. Configure Web App

```bash
# Configure container settings
az webapp config container set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --docker-custom-image-name $ACR_NAME.azurecr.io/probotsharp:latest \
  --docker-registry-server-url https://$ACR_NAME.azurecr.io \
  --docker-registry-server-user $ACR_NAME \
  --docker-registry-server-password $(az acr credential show --name $ACR_NAME --query "passwords[0].value" -o tsv)

# Configure app settings
az webapp config appsettings set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    WEBSITES_PORT=8080 \
    WEBSITES_ENABLE_APP_SERVICE_STORAGE=false \
    ProbotSharp__Adapters__Cache__Provider=Redis \
    ProbotSharp__Adapters__Idempotency__Provider=Redis \
    ProbotSharp__Adapters__Persistence__Provider=PostgreSQL \
    ProbotSharp__Adapters__ReplayQueue__Provider=InMemory \
    ProbotSharp__Adapters__DeadLetterQueue__Provider=Database \
    ProbotSharp__Adapters__Metrics__Provider=OpenTelemetry \
    ProbotSharp__Adapters__Tracing__Provider=OpenTelemetry

# Enable HTTPS only
az webapp update \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --https-only true

# Configure health check
az webapp config set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --health-check-path /health

# Enable always on (requires Basic tier or higher)
az webapp config set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --always-on true
```

### 5. Create PostgreSQL Database

```bash
DB_SERVER_NAME="probotsharp-db"
DB_ADMIN_USER="probotadmin"
DB_ADMIN_PASSWORD="YourSecurePassword123!"

# Create PostgreSQL Flexible Server
az postgres flexible-server create \
  --resource-group $RESOURCE_GROUP \
  --name $DB_SERVER_NAME \
  --location $LOCATION \
  --admin-user $DB_ADMIN_USER \
  --admin-password $DB_ADMIN_PASSWORD \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --version 14 \
  --storage-size 32 \
  --public-access 0.0.0.0-255.255.255.255

# Create database
az postgres flexible-server db create \
  --resource-group $RESOURCE_GROUP \
  --server-name $DB_SERVER_NAME \
  --database-name probotsharp

# Get connection string
DB_CONNECTION_STRING="Host=$DB_SERVER_NAME.postgres.database.azure.com;Port=5432;Database=probotsharp;Username=$DB_ADMIN_USER;Password=$DB_ADMIN_PASSWORD;Ssl Mode=Require;"

# Configure connection string in Web App
az webapp config connection-string set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --connection-string-type Custom \
  --settings ProbotSharp="$DB_CONNECTION_STRING"
```

### 6. Create Azure Cache for Redis

```bash
REDIS_NAME="probotsharp-cache"

# Create Redis cache
az redis create \
  --resource-group $RESOURCE_GROUP \
  --name $REDIS_NAME \
  --location $LOCATION \
  --sku Basic \
  --vm-size c0 \
  --enable-non-ssl-port false

# Get Redis connection string
REDIS_KEY=$(az redis list-keys --resource-group $RESOURCE_GROUP --name $REDIS_NAME --query primaryKey -o tsv)
REDIS_CONNECTION_STRING="$REDIS_NAME.redis.cache.windows.net:6380,password=$REDIS_KEY,ssl=True,abortConnect=False"

# Add to Web App settings
az webapp config appsettings set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings ProbotSharp__Cache__RedisConnectionString="$REDIS_CONNECTION_STRING"
```

### 7. Run Database Migrations

```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Run migrations
dotnet ef database update \
  --project src/ProbotSharp.Infrastructure \
  --startup-project src/ProbotSharp.Bootstrap.Api \
  --connection "$DB_CONNECTION_STRING"
```

## Azure Key Vault Integration

### Create Key Vault

```bash
KEY_VAULT_NAME="probotsharp-kv-$(openssl rand -hex 4)"

az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enabled-for-deployment true \
  --enabled-for-template-deployment true
```

### Enable Managed Identity

```bash
# Enable system-assigned managed identity for Web App
az webapp identity assign \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Grant Web App access to Key Vault
az keyvault set-policy \
  --name $KEY_VAULT_NAME \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### Reference Key Vault Secrets

```bash
# Store secrets in Key Vault
az keyvault secret set --vault-name $KEY_VAULT_NAME --name github-app-id --value "123456"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name github-webhook-secret --value "your-secret"
az keyvault secret set --vault-name $KEY_VAULT_NAME --name github-private-key --file private-key.pem

# Reference in Web App settings
az webapp config appsettings set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ProbotSharp__GitHub__AppId="@Microsoft.KeyVault(VaultName=$KEY_VAULT_NAME;SecretName=github-app-id)" \
    ProbotSharp__GitHub__WebhookSecret="@Microsoft.KeyVault(VaultName=$KEY_VAULT_NAME;SecretName=github-webhook-secret)" \
    ProbotSharp__GitHub__PrivateKey="@Microsoft.KeyVault(VaultName=$KEY_VAULT_NAME;SecretName=github-private-key)"
```

## Monitoring and Logging

### Enable Application Insights

```bash
APP_INSIGHTS_NAME="probotsharp-insights"

# Create Application Insights
az monitor app-insights component create \
  --app $APP_INSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP \
  --application-type web

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey -o tsv)

# Configure in Web App
az webapp config appsettings set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

### View Logs

```bash
# Stream logs
az webapp log tail \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP

# Download logs
az webapp log download \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --log-file webapp-logs.zip

# Enable detailed logging
az webapp log config \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --application-logging filesystem \
  --detailed-error-messages true \
  --failed-request-tracing true \
  --web-server-logging filesystem
```

### View Metrics

```bash
# View CPU usage
az monitor metrics list \
  --resource "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$WEBAPP_NAME" \
  --metric "CpuPercentage" \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-02T00:00:00Z

# View memory usage
az monitor metrics list \
  --resource "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$WEBAPP_NAME" \
  --metric "MemoryPercentage" \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-02T00:00:00Z
```

## Scaling

### Manual Scaling

```bash
# Scale up (change instance size)
az appservice plan update \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku P1V2

# Scale out (add instances)
az appservice plan update \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --number-of-workers 3
```

### Auto-scaling

```bash
# Enable auto-scale
az monitor autoscale create \
  --resource-group $RESOURCE_GROUP \
  --resource "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/serverfarms/$APP_SERVICE_PLAN" \
  --name probotsharp-autoscale \
  --min-count 2 \
  --max-count 10 \
  --count 2

# Add CPU-based rule
az monitor autoscale rule create \
  --resource-group $RESOURCE_GROUP \
  --autoscale-name probotsharp-autoscale \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1

az monitor autoscale rule create \
  --resource-group $RESOURCE_GROUP \
  --autoscale-name probotsharp-autoscale \
  --condition "Percentage CPU < 30 avg 5m" \
  --scale in 1
```

## Troubleshooting

### Container Not Starting

1. **Check container logs**:
   ```bash
   az webapp log tail --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP
   ```

2. **Verify container settings**:
   ```bash
   az webapp config container show --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP
   ```

3. **Check WEBSITES_PORT**:
   ```bash
   az webapp config appsettings list --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP --query "[?name=='WEBSITES_PORT']"
   ```

### Database Connection Issues

1. **Test connectivity**:
   ```bash
   # From Web App console
   az webapp ssh --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP
   nc -zv $DB_SERVER_NAME.postgres.database.azure.com 5432
   ```

2. **Check firewall rules**:
   ```bash
   az postgres flexible-server firewall-rule list \
     --resource-group $RESOURCE_GROUP \
     --server-name $DB_SERVER_NAME
   ```

3. **Add Web App to firewall**:
   ```bash
   az postgres flexible-server firewall-rule create \
     --resource-group $RESOURCE_GROUP \
     --server-name $DB_SERVER_NAME \
     --name AllowWebApp \
     --start-ip-address 0.0.0.0 \
     --end-ip-address 255.255.255.255
   ```

### Key Vault Access Issues

1. **Verify managed identity**:
   ```bash
   az webapp identity show --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP
   ```

2. **Check access policies**:
   ```bash
   az keyvault show --name $KEY_VAULT_NAME --resource-group $RESOURCE_GROUP --query properties.accessPolicies
   ```

3. **Test secret access**:
   ```bash
   az keyvault secret show --vault-name $KEY_VAULT_NAME --name github-app-id
   ```

### High Memory/CPU Usage

1. **Check metrics**:
   ```bash
   az monitor metrics list \
     --resource "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Web/sites/$WEBAPP_NAME" \
     --metric "MemoryPercentage" \
     --interval PT1M
   ```

2. **Scale up if needed**:
   ```bash
   az appservice plan update --name $APP_SERVICE_PLAN --resource-group $RESOURCE_GROUP --sku P2V2
   ```

### Deployment Slot Issues

Use deployment slots for zero-downtime deployments:

```bash
# Create staging slot
az webapp deployment slot create \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --slot staging

# Deploy to staging
az webapp config container set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --slot staging \
  --docker-custom-image-name $ACR_NAME.azurecr.io/probotsharp:latest

# Swap staging to production
az webapp deployment slot swap \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --slot staging
```

## Security Best Practices

1. **Enable HTTPS Only**: Already configured in deployment
2. **Use Managed Identities**: Avoid storing credentials in app settings
3. **Rotate Secrets**: Use Key Vault with rotation policies
4. **Enable WAF**: Use Application Gateway with WAF for public endpoints
5. **Network Isolation**: Use VNet integration and Private Endpoints
6. **Enable Defender**: Azure Defender for App Service
7. **Audit Logs**: Enable diagnostic settings
8. **Least Privilege**: Grant minimal required permissions

### Enable VNet Integration

```bash
# Create VNet
VNET_NAME="probotsharp-vnet"
az network vnet create \
  --resource-group $RESOURCE_GROUP \
  --name $VNET_NAME \
  --address-prefix 10.0.0.0/16 \
  --subnet-name webapp-subnet \
  --subnet-prefix 10.0.1.0/24

# Integrate Web App with VNet
az webapp vnet-integration add \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --vnet $VNET_NAME \
  --subnet webapp-subnet
```

## Cost Optimization

1. **Choose appropriate SKU**: Start with Basic, scale up as needed
2. **Use auto-scaling**: Scale down during low usage
3. **Reserved instances**: Save up to 72% with 1-year or 3-year commitments
4. **Azure Hybrid Benefit**: Use existing licenses
5. **Monitor spending**: Set up budget alerts
6. **Stop dev/test environments**: When not in use

## Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/)
- [Azure Database for PostgreSQL](https://docs.microsoft.com/en-us/azure/postgresql/)
- [Azure Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Azure Cache for Redis](https://docs.microsoft.com/en-us/azure/azure-cache-for-redis/)
- [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
