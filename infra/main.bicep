targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

// Storage account for file storage
resource storage 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: '${abbrs.storageStorageAccounts}${resourceToken}'
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// Blob service for the storage account
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2022-05-01' = {
  parent: storage
  name: 'default'
}

// Container for files
resource filesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-05-01' = {
  parent: blobService
  name: 'files'
  properties: {
    publicAccess: 'None'
  }
}

// Note: Using existing App Service 'RemoteFile' in resource group 'RemoteFile_group'
// The App Service will be configured separately through azd deployment

// Static Web App for frontend
resource web 'Microsoft.Web/staticSites@2021-03-01' = {
  name: '${abbrs.webStaticSites}${resourceToken}'
  location: 'eastus2'  // Static Web Apps require specific regions
  tags: union(tags, { 'azd-service-name': 'web' })
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

// Key Vault for secrets
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: '${abbrs.keyVaultVaults}${resourceToken}'
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        objectId: principalId
        tenantId: subscription().tenantId
        permissions: {
          secrets: ['get', 'list']
        }
      }
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
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
  location: location
  tags: tags
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

// User-assigned managed identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${abbrs.managedIdentityUserAssignedIdentities}${resourceToken}'
  location: location
  tags: tags
}

// Role assignment for managed identity to access storage
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

output AZURE_STORAGE_ACCOUNT_NAME string = storage.name
output AZURE_STORAGE_ACCOUNT_ID string = storage.id
output AZURE_STORAGE_CONTAINER_NAME string = filesContainer.name

output AZURE_KEY_VAULT_NAME string = keyVault.name
output AZURE_KEY_VAULT_URI string = keyVault.properties.vaultUri

output SERVICE_API_IDENTITY_PRINCIPAL_ID string = managedIdentity.properties.principalId
output SERVICE_API_NAME string = 'RemoteFile'
output SERVICE_API_URI string = 'https://remotefile.azurewebsites.net'

output SERVICE_WEB_NAME string = web.name
output SERVICE_WEB_URI string = 'https://${web.properties.defaultHostname}'

output APPLICATIONINSIGHTS_CONNECTION_STRING string = applicationInsights.properties.ConnectionString
