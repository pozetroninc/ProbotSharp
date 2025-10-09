@description('Name prefix for all resources')
param namePrefix string = 'probotsharp'

@description('Environment name')
@allowed([
  'production'
  'staging'
])
param environment string = 'production'

@description('Location for all resources')
param location string = resourceGroup().location

@description('App Service Plan SKU')
param appServicePlanSku string = 'B1'

@description('PostgreSQL Server SKU')
param postgreSqlSku string = 'Standard_B1ms'

@description('GitHub App ID')
@secure()
param githubAppId string

@description('GitHub Webhook Secret')
@secure()
param githubWebhookSecret string

@description('GitHub Private Key')
@secure()
param githubPrivateKey string

@description('PostgreSQL Administrator Password')
@secure()
param postgresqlPassword string

var appName = '${namePrefix}-${environment}'
var appServicePlanName = '${appName}-plan'
var postgreSqlServerName = '${appName}-pgsql'
var redisCacheName = '${appName}-redis'
var keyVaultName = '${appName}-kv'
var logAnalyticsName = '${appName}-logs'
var appInsightsName = '${appName}-appins'

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enabledForDeployment: false
    enabledForTemplateDeployment: true
    enableRbacAuthorization: true
    accessPolicies: []
  }
}

// Key Vault Secrets
resource githubAppIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'github-app-id'
  properties: {
    value: githubAppId
  }
}

resource githubWebhookSecretSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'github-webhook-secret'
  properties: {
    value: githubWebhookSecret
  }
}

resource githubPrivateKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'github-private-key'
  properties: {
    value: githubPrivateKey
  }
}

resource postgresqlPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'postgresql-password'
  properties: {
    value: postgresqlPassword
  }
}

// PostgreSQL Server
resource postgreSqlServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: postgreSqlServerName
  location: location
  sku: {
    name: postgreSqlSku
    tier: 'Burstable'
  }
  properties: {
    version: '15'
    administratorLogin: 'probotsharp'
    administratorLoginPassword: postgresqlPassword
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

// PostgreSQL Database
resource postgreSqlDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgreSqlServer
  name: 'probotsharp'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// PostgreSQL Firewall Rule - Allow Azure Services
resource postgreSqlFirewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
  parent: postgreSqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Redis Cache
resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisCacheName
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisVersion: '6'
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: appName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
      healthCheckPath: '/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'production' ? 'Production' : 'Staging'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ProbotSharp__GitHub__AppId'
          value: '@Microsoft.KeyVault(SecretUri=${githubAppIdSecret.properties.secretUri})'
        }
        {
          name: 'ProbotSharp__GitHub__WebhookSecret'
          value: '@Microsoft.KeyVault(SecretUri=${githubWebhookSecretSecret.properties.secretUri})'
        }
        {
          name: 'ProbotSharp__GitHub__PrivateKey'
          value: '@Microsoft.KeyVault(SecretUri=${githubPrivateKeySecret.properties.secretUri})'
        }
        {
          name: 'ConnectionStrings__ProbotSharp'
          value: 'Host=${postgreSqlServer.properties.fullyQualifiedDomainName};Port=5432;Database=probotsharp;Username=probotsharp;Password=${postgresqlPassword};SSL Mode=Require'
        }
        {
          name: 'ProbotSharp__Adapters__Cache__Provider'
          value: 'Redis'
        }
        {
          name: 'ProbotSharp__Adapters__Cache__Options__ConnectionString'
          value: '${redisCacheName}.redis.cache.windows.net:6380,password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
        }
        {
          name: 'ProbotSharp__Adapters__Idempotency__Provider'
          value: 'Redis'
        }
        {
          name: 'ProbotSharp__Adapters__Idempotency__Options__ConnectionString'
          value: '${redisCacheName}.redis.cache.windows.net:6380,password=${redisCache.listKeys().primaryKey},ssl=True,abortConnect=False'
        }
        {
          name: 'ProbotSharp__Adapters__Persistence__Provider'
          value: 'PostgreSQL'
        }
        {
          name: 'ProbotSharp__Adapters__ReplayQueue__Provider'
          value: 'InMemory'
        }
        {
          name: 'ProbotSharp__Adapters__DeadLetterQueue__Provider'
          value: 'Database'
        }
        {
          name: 'ProbotSharp__Adapters__Metrics__Provider'
          value: 'OpenTelemetry'
        }
        {
          name: 'ProbotSharp__Adapters__Tracing__Provider'
          value: 'OpenTelemetry'
        }
        {
          name: 'ProbotSharp__Metrics__OtlpEndpoint'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
  }
}

// Key Vault Access Policy for Web App
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-02-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: webApp.identity.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

// Outputs
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output webAppName string = webApp.name
output postgreSqlServerFqdn string = postgreSqlServer.properties.fullyQualifiedDomainName
output redisCacheName string = redisCache.name
output keyVaultName string = keyVault.name
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
