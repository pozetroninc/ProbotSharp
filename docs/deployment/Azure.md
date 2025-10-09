# Azure Web Apps Deployment Guide

This guide covers deploying ProbotSharp to Azure using Azure App Service (Web Apps for Containers).

## Architecture Overview

The Azure deployment includes:
- **Azure App Service** - Managed container hosting with built-in scaling
- **Azure Database for PostgreSQL** - Fully managed PostgreSQL database
- **Azure Cache for Redis** - Managed Redis cache service
- **Azure Key Vault** - Secure secrets management
- **Application Insights** - Application performance monitoring
- **Log Analytics** - Centralized logging and diagnostics

## Prerequisites

### Required Tools

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) v2.50 or later
- [Docker](https://www.docker.com/products/docker-desktop)
- Azure subscription with appropriate permissions
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for running migrations)

### Azure Permissions Required

Your account needs permissions for:
- Resource Groups (create/manage)
- App Service Plans and Web Apps
- Azure Database for PostgreSQL
- Azure Cache for Redis
- Key Vault (create/manage secrets)
- Application Insights
- Container Registry (optional, for custom images)

### GitHub App Credentials

Before deploying, you need:
- GitHub App ID
- GitHub App Private Key (PEM format)
- GitHub Webhook Secret

See the [Local Development Guide](../LocalDevelopment.md#2-create-a-github-app) for GitHub App setup instructions.

## Deployment Methods

### Method 1: Azure Bicep (Infrastructure as Code) - Recommended

This method uses Azure Bicep to provision all resources declaratively.

#### Step 1: Login to Azure

```bash
az login
az account set --subscription "Your Subscription Name"
```

#### Step 2: Create Resource Group

```bash
az group create \
  --name probotsharp-production-rg \
  --location eastus
```

#### Step 3: Prepare Secrets

Store your GitHub App private key in a file:

```bash
cat > github-private-key.pem <<'EOF'
-----BEGIN RSA PRIVATE KEY-----
Your private key content here
-----END RSA PRIVATE KEY-----
EOF
```

#### Step 4: Deploy Infrastructure

```bash
az deployment group create \
  --resource-group probotsharp-production-rg \
  --template-file deploy/azure/azuredeploy.bicep \
  --parameters \
    environment=production \
    githubAppId="123456" \
    githubWebhookSecret="your-webhook-secret" \
    githubPrivateKey="$(cat github-private-key.pem)" \
    postgresqlPassword="YourSecurePassword123!"
```

This typically takes 10-15 minutes and creates:
- App Service Plan (B1 tier)
- Web App with system-assigned managed identity
- PostgreSQL Flexible Server
- Redis Cache
- Key Vault with secrets
- Application Insights
- Log Analytics Workspace

#### Step 5: Get Deployment Outputs

```bash
az deployment group show \
  --resource-group probotsharp-production-rg \
  --name azuredeploy \
  --query properties.outputs
```

Note the following outputs:
- `webAppUrl` - Your application URL
- `webAppName` - Web app name for deployment
- `keyVaultName` - Key Vault name
- `postgreSqlServerFqdn` - Database server FQDN

#### Step 6: Build and Deploy Application

**Option A: Deploy from local build**

```bash
# Build and publish
dotnet publish src/ProbotSharp.Bootstrap.Api/ProbotSharp.Bootstrap.Api.csproj \
  --configuration Release \
  --output ./publish

# Create deployment package
cd publish
zip -r ../app.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production \
  --src app.zip
```

**Option B: Deploy Docker container**

```bash
# Enable container deployment
az webapp config container set \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production \
  --docker-custom-image-name your-registry/probotsharp:latest \
  --docker-registry-server-url https://your-registry.azurecr.io \
  --docker-registry-server-user <username> \
  --docker-registry-server-password <password>
```

#### Step 7: Run Database Migrations

```bash
# Get database connection string
CONNECTION_STRING=$(az postgres flexible-server show-connection-string \
  --server-name probotsharp-production-pgsql \
  --database-name probotsharp \
  --admin-user probotsharp \
  --admin-password YourSecurePassword123! \
  --query connectionStrings.psql_cmd \
  --output tsv)

# Run migrations locally
dotnet ef database update \
  --project src/ProbotSharp.Infrastructure \
  --startup-project src/ProbotSharp.Bootstrap.Api \
  --connection "Host=probotsharp-production-pgsql.postgres.database.azure.com;Port=5432;Database=probotsharp;Username=probotsharp;Password=YourSecurePassword123!;SSL Mode=Require"
```

Or run migrations from Azure Cloud Shell or a VM with network access to the database.

#### Step 8: Configure GitHub Webhook URL

1. Get your Web App URL from deployment outputs
2. Go to your GitHub App settings
3. Update **Webhook URL** to `https://<your-app-name>.azurewebsites.net/webhooks`
4. Save changes

#### Step 9: Verify Deployment

```bash
# Health check
curl https://probotsharp-production.azurewebsites.net/health

# Root endpoint
curl https://probotsharp-production.azurewebsites.net/

# View logs
az webapp log tail \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production
```

### Method 2: Azure Portal (Manual Setup)

If you prefer a GUI-based approach:

#### Step 1: Create App Service

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** → **Web App**
3. Fill in:
   - **Subscription**: Your subscription
   - **Resource Group**: Create new "probotsharp-production-rg"
   - **Name**: probotsharp-production
   - **Publish**: Docker Container
   - **Operating System**: Linux
   - **Region**: East US
   - **Linux Plan**: Create new B1 plan
4. Click **Next: Docker**
5. Select **Single Container** and configure your image
6. Click **Review + create** → **Create**

#### Step 2: Create PostgreSQL Database

1. Click **Create a resource** → **Azure Database for PostgreSQL**
2. Select **Flexible server**
3. Fill in:
   - **Resource group**: probotsharp-production-rg
   - **Server name**: probotsharp-production-pgsql
   - **Region**: East US
   - **PostgreSQL version**: 15
   - **Workload type**: Development
   - **Compute + storage**: Burstable B1ms
   - **Admin username**: probotsharp
   - **Password**: Secure password
4. Click **Networking** → Allow public access from Azure services
5. Click **Review + create** → **Create**

#### Step 3: Create Redis Cache

1. Click **Create a resource** → **Azure Cache for Redis**
2. Fill in:
   - **Resource group**: probotsharp-production-rg
   - **DNS name**: probotsharp-production-redis
   - **Location**: East US
   - **Cache type**: Basic C0
3. Click **Review + create** → **Create**

#### Step 4: Create Key Vault

1. Click **Create a resource** → **Key Vault**
2. Fill in:
   - **Resource group**: probotsharp-production-rg
   - **Key vault name**: probotsharp-production-kv
   - **Region**: East US
3. Click **Access policies** → Add access policy for your Web App's managed identity
4. Click **Review + create** → **Create**

#### Step 5: Add Secrets to Key Vault

1. Go to Key Vault → **Secrets**
2. Click **Generate/Import** and add:
   - `github-app-id` - Your GitHub App ID
   - `github-webhook-secret` - Your webhook secret
   - `github-private-key` - Your private key (multiline)

#### Step 6: Configure Web App

1. Go to your Web App → **Configuration**
2. Add application settings:

```
ASPNETCORE_ENVIRONMENT = Production
ConnectionStrings__ProbotSharp = Host=probotsharp-production-pgsql.postgres.database.azure.com;Port=5432;Database=probotsharp;Username=probotsharp;Password=<password>;SSL Mode=Require
ProbotSharp__GitHub__AppId = @Microsoft.KeyVault(VaultName=probotsharp-production-kv;SecretName=github-app-id)
ProbotSharp__GitHub__WebhookSecret = @Microsoft.KeyVault(VaultName=probotsharp-production-kv;SecretName=github-webhook-secret)
ProbotSharp__GitHub__PrivateKey = @Microsoft.KeyVault(VaultName=probotsharp-production-kv;SecretName=github-private-key)
ProbotSharp__Adapters__Cache__Provider = Redis
ProbotSharp__Adapters__Cache__Options__ConnectionString = probotsharp-production-redis.redis.cache.windows.net:6380,password=<redis-key>,ssl=True,abortConnect=False
ProbotSharp__Adapters__Idempotency__Provider = Redis
ProbotSharp__Adapters__Idempotency__Options__ConnectionString = probotsharp-production-redis.redis.cache.windows.net:6380,password=<redis-key>,ssl=True,abortConnect=False
ProbotSharp__Adapters__Persistence__Provider = PostgreSQL
ProbotSharp__Adapters__ReplayQueue__Provider = InMemory
ProbotSharp__Adapters__DeadLetterQueue__Provider = Database
ProbotSharp__Adapters__Metrics__Provider = OpenTelemetry
ProbotSharp__Adapters__Tracing__Provider = OpenTelemetry
```

3. Click **Save**

### Method 3: GitHub Actions (CI/CD)

Automate deployments using GitHub Actions.

#### Step 1: Create Azure Service Principal

```bash
az ad sp create-for-rbac \
  --name probotsharp-github-actions \
  --role contributor \
  --scopes /subscriptions/{subscription-id}/resourceGroups/probotsharp-production-rg \
  --sdk-auth
```

Copy the entire JSON output.

#### Step 2: Configure GitHub Secrets

Add to repository secrets (Settings → Secrets and variables → Actions):

- `AZURE_CREDENTIALS` - JSON output from step 1
- `AZURE_DATABASE_CONNECTION_STRING` - Your database connection string

#### Step 3: Deploy Infrastructure First

Before GitHub Actions can deploy, create the infrastructure using Method 1 (Bicep).

#### Step 4: Push to Main Branch

Every push to `main` triggers automatic deployment via `.github/workflows/deploy-azure.yml`.

The workflow:
1. Checks out code
2. Sets up .NET 8
3. Restores dependencies
4. Builds in Release mode
5. Publishes application
6. Logs in to Azure
7. Deploys to Web App
8. Runs database migrations
9. Logs out

## Configuration

### Environment Variables

Configure via App Service → Configuration → Application settings.

Key settings:
- `ASPNETCORE_ENVIRONMENT` - Production/Staging
- `ConnectionStrings__ProbotSharp` - Database connection
- `ProbotSharp__GitHub__*` - GitHub App credentials (from Key Vault)
- `ProbotSharp__Cache__RedisConnectionString` - Redis connection
- `APPLICATIONINSIGHTS_CONNECTION_STRING` - Auto-populated for monitoring

### Key Vault Integration

Reference secrets from Key Vault using:

```
@Microsoft.KeyVault(VaultName=your-vault;SecretName=secret-name)
```

Or with secret version:

```
@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/secret-name/version)
```

### Scaling Configuration

**Manual Scaling**

App Service → Scale up/Scale out:
- **Scale up** (Vertical): Change App Service Plan tier (B1 → S1 → P1v2, etc.)
- **Scale out** (Horizontal): Add more instances

**Auto-scaling**

Enable auto-scale rules based on:
- CPU percentage
- Memory percentage
- HTTP queue length
- Custom metrics

Example auto-scale rule (Azure CLI):

```bash
az monitor autoscale create \
  --resource-group probotsharp-production-rg \
  --resource probotsharp-production \
  --resource-type Microsoft.Web/serverfarms \
  --name autoscale-probotsharp \
  --min-count 1 \
  --max-count 10 \
  --count 2

az monitor autoscale rule create \
  --resource-group probotsharp-production-rg \
  --autoscale-name autoscale-probotsharp \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1
```

## Monitoring and Logging

### Application Insights

View metrics and logs:

```bash
# Open in portal
az monitor app-insights component show \
  --app probotsharp-production-appins \
  --resource-group probotsharp-production-rg \
  --query appId --output tsv
```

Key metrics:
- Request rate
- Response time
- Failed requests
- Exceptions
- Dependency calls (database, Redis, GitHub API)

### Stream Logs

```bash
# Stream application logs
az webapp log tail \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production

# Download logs
az webapp log download \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production \
  --log-file logs.zip
```

### Enable Diagnostic Logging

```bash
az webapp log config \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production \
  --application-logging filesystem \
  --detailed-error-messages true \
  --failed-request-tracing true \
  --web-server-logging filesystem
```

### Query Logs with Kusto

In Application Insights → Logs, use Kusto Query Language (KQL):

```kql
// Exceptions in last 24 hours
exceptions
| where timestamp > ago(24h)
| summarize count() by problemId, outerMessage
| order by count_ desc

// Slow requests
requests
| where timestamp > ago(1h)
| where duration > 1000
| project timestamp, name, duration, resultCode

// Failed webhook processing
traces
| where message contains "webhook" and message contains "failed"
| project timestamp, message, severityLevel
```

## Database Management

### Connect to PostgreSQL

Using Azure Cloud Shell or local psql:

```bash
psql "host=probotsharp-production-pgsql.postgres.database.azure.com port=5432 dbname=probotsharp user=probotsharp password=<password> sslmode=require"
```

### Backup and Restore

Azure Database for PostgreSQL provides automatic backups.

**Manual backup**

```bash
az postgres flexible-server backup create \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production-pgsql \
  --backup-name manual-backup-$(date +%Y%m%d)
```

**Restore from backup**

```bash
az postgres flexible-server restore \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production-pgsql-restored \
  --source-server probotsharp-production-pgsql \
  --restore-time "2024-01-15T10:00:00Z"
```

### Run Migrations

From local machine:

```bash
dotnet ef database update \
  --project src/ProbotSharp.Infrastructure \
  --startup-project src/ProbotSharp.Bootstrap.Api \
  --connection "Host=probotsharp-production-pgsql.postgres.database.azure.com;Port=5432;Database=probotsharp;Username=probotsharp;Password=<password>;SSL Mode=Require"
```

Or use Azure App Service SSH:

```bash
az webapp ssh \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production

# Inside container
dotnet ef database update
```

## Troubleshooting

### Application Not Starting

Check logs:

```bash
az webapp log tail \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production
```

Common issues:
- **Database connection failure** - Check connection string, firewall rules
- **Key Vault access denied** - Verify managed identity has Key Vault permissions
- **Missing environment variables** - Check Configuration settings

### Database Connection Issues

Enable Azure Services access:

```bash
az postgres flexible-server firewall-rule create \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production-pgsql \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

Test connectivity from App Service:

```bash
az webapp ssh \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production

# Test DNS resolution
nslookup probotsharp-production-pgsql.postgres.database.azure.com

# Test connection
nc -zv probotsharp-production-pgsql.postgres.database.azure.com 5432
```

### High CPU/Memory Usage

1. Check Application Insights for bottlenecks
2. Scale up to higher tier:

```bash
az appservice plan update \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production-plan \
  --sku S1
```

3. Or scale out:

```bash
az appservice plan update \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production-plan \
  --number-of-workers 3
```

### Deployment Slot Swap Failures

If using deployment slots, ensure:
- All configuration marked as "slot settings" where needed
- Database schema compatible between versions
- Health check endpoint responding

## Updating the Application

### Zero-Downtime Deployment with Slots

1. Create staging slot:

```bash
az webapp deployment slot create \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production \
  --slot staging
```

2. Deploy to staging slot:

```bash
az webapp deployment source config-zip \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production \
  --slot staging \
  --src app.zip
```

3. Test staging slot:

```bash
curl https://probotsharp-production-staging.azurewebsites.net/health
```

4. Swap slots:

```bash
az webapp deployment slot swap \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production \
  --slot staging \
  --target-slot production
```

### Rolling Back

If using deployment slots, swap back:

```bash
az webapp deployment slot swap \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production \
  --slot staging \
  --target-slot production
```

Without slots, redeploy previous version.

## Security

### Network Isolation

Use VNet integration for private connectivity:

```bash
az network vnet create \
  --resource-group probotsharp-production-rg \
  --name probotsharp-vnet \
  --address-prefix 10.0.0.0/16 \
  --subnet-name app-subnet \
  --subnet-prefix 10.0.1.0/24

az webapp vnet-integration add \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production \
  --vnet probotsharp-vnet \
  --subnet app-subnet
```

### Managed Identity

Use system-assigned or user-assigned managed identity for accessing Azure resources without credentials.

Grant Key Vault access:

```bash
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group probotsharp-production-rg \
  --name probotsharp-production \
  --query principalId \
  --output tsv)

az keyvault set-policy \
  --name probotsharp-production-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

## Costs

Estimated monthly costs (East US region):
- **App Service (B1)** - ~$13
- **PostgreSQL Flexible Server (B1ms)** - ~$12
- **Redis Cache (Basic C0)** - ~$16
- **Key Vault** - ~$0.03 (secrets operations)
- **Application Insights** - ~$0-5 (first 5GB free)
- **Bandwidth** - ~$0-10 (depends on traffic)

**Total: ~$41-56/month**

Costs increase with:
- Higher App Service tiers (S1, P1v2, etc.)
- More instances (scale out)
- Larger database/Redis instances
- More Application Insights data

## Cleanup

Delete all resources:

```bash
az group delete \
  --name probotsharp-production-rg \
  --yes \
  --no-wait
```

## Next Steps

- Set up [Azure Front Door](https://azure.microsoft.com/en-us/services/frontdoor/) for global load balancing and CDN
- Configure [Azure Application Gateway](https://azure.microsoft.com/en-us/services/application-gateway/) with WAF for security
- Implement [Azure Monitor Alerts](https://docs.microsoft.com/en-us/azure/azure-monitor/alerts/alerts-overview) for critical issues
- Use [Azure DevOps Pipelines](https://azure.microsoft.com/en-us/services/devops/pipelines/) for advanced CI/CD
- Enable [Azure Backup](https://azure.microsoft.com/en-us/services/backup/) for additional data protection
