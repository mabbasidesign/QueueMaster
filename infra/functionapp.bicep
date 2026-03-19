metadata description = 'NotificationFunction Azure Function App resources'

param location string
param environmentName string
param serviceBusConnectionString string
param appInsightsConnectionString string
@secure()
param notificationConnectionString string = ''
param notificationSenderAddress string = ''
param notificationRecipientAddress string = ''

var resourceToken = uniqueString(subscription().id, resourceGroup().id, location, environmentName)

// Storage account for function app host
resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'azst${take(resourceToken, 18)}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

// User-assigned managed identity for the function app
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'azid${take(resourceToken, 18)}'
  location: location
}

// Consumption hosting plan
resource plan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: 'azasp${take(resourceToken, 17)}'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

// Log Analytics workspace required as a diagnostics data sink
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'azlaw${take(resourceToken, 17)}'
  location: location
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: 'azfn${take(resourceToken, 18)}'
  location: location
  kind: 'functionapp'
  tags: {
    'azd-service-name': 'notification-function'
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v9.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storage.name
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ServiceBusConnection'
          value: serviceBusConnectionString
        }
        {
          name: 'Notification__ConnectionString'
          value: notificationConnectionString
        }
        {
          name: 'Notification__SenderAddress'
          value: notificationSenderAddress
        }
        {
          name: 'Notification__RecipientAddress'
          value: notificationRecipientAddress
        }
      ]
    }
  }
}

// Role: Storage Blob Data Owner
resource roleBlobOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, identity.id, 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b')
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b')
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Role: Storage Blob Data Contributor
resource roleBlobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, identity.id, 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Role: Storage Queue Data Contributor
resource roleQueueContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, identity.id, '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Role: Storage Table Data Contributor
resource roleTableContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, identity.id, '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Role: Monitoring Metrics Publisher
resource roleMetricsPublisher 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(functionApp.id, identity.id, '3913510d-42f4-4e42-8a64-420c390055eb')
  scope: functionApp
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '3913510d-42f4-4e42-8a64-420c390055eb')
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Diagnostic settings — ship function app logs to App Insights via Log Analytics
resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${functionApp.name}'
  scope: functionApp
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'FunctionAppLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

output functionAppName string = functionApp.name
output functionAppHostname string = functionApp.properties.defaultHostName
output identityPrincipalId string = identity.properties.principalId
