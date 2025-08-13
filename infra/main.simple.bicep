targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

// Create resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: environmentName
  location: location
  tags: tags
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: '${abbrs.webServerFarms}${resourceToken}'
  location: location
  tags: tags
  scope: rg
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

// App Service for API
resource api 'Microsoft.Web/sites@2022-03-01' = {
  name: '${abbrs.webSitesAppService}api-${resourceToken}'
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  scope: rg
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|7.0'
      alwaysOn: false
      ftpsState: 'FtpsOnly'
      appSettings: [
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'true'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentity.properties.clientId
        }
      ]
    }
    httpsOnly: true
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
}

// Static Web App for Frontend
resource staticWebApp 'Microsoft.Web/staticSites@2022-03-01' = {
  name: '${abbrs.webStaticSites}${resourceToken}'
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  scope: rg
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    buildProperties: {
      skipGithubActionWorkflowGeneration: true
    }
  }
}

// Storage Account
resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: '${abbrs.storageStorageAccounts}${resourceToken}'
  location: location
  tags: tags
  scope: rg
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// Blob Service
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2022-09-01' = {
  parent: storage
  name: 'default'
}

// Files Container
resource filesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-09-01' = {
  parent: blobService
  name: 'files'
  properties: {
    publicAccess: 'None'
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: '${abbrs.keyVaultVaults}${resourceToken}'
  location: location
  tags: tags
  scope: rg
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        objectId: managedIdentity.properties.principalId
        tenantId: subscription().tenantId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${abbrs.insightsComponents}${resourceToken}'
  location: location
  tags: tags
  scope: rg
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
  location: location
  tags: tags
  scope: rg
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// Managed Identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${abbrs.managedIdentityUserAssignedIdentities}${resourceToken}'
  location: location
  tags: tags
  scope: rg
}

// Storage Account Access for Managed Identity
resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storage
  name: guid(storage.id, managedIdentity.id, 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe') // Storage Blob Data Contributor
    principalType: 'ServicePrincipal'
    principalId: managedIdentity.properties.principalId
  }
}

// Outputs
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = subscription().tenantId
output AZURE_RESOURCE_GROUP string = rg.name

output AZURE_STORAGE_ACCOUNT_NAME string = storage.name
output AZURE_STORAGE_ACCOUNT_ID string = storage.id

output AZURE_KEY_VAULT_NAME string = keyVault.name
output AZURE_KEY_VAULT_URI string = keyVault.properties.vaultUri

output SERVICE_API_IDENTITY_PRINCIPAL_ID string = managedIdentity.properties.principalId
output SERVICE_API_NAME string = api.name
output SERVICE_API_URI string = 'https://${api.properties.defaultHostName}'

output SERVICE_WEB_NAME string = staticWebApp.name
output SERVICE_WEB_URI string = 'https://${staticWebApp.properties.defaultHostname}'

output APPLICATIONINSIGHTS_CONNECTION_STRING string = applicationInsights.properties.ConnectionString
