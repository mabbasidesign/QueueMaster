metadata description = 'Azure Communication Services Email resources for QueueMaster notifications'

param location string
param environmentName string
param dataLocation string = 'United States'
param senderUsername string = 'donotreply'
param senderDisplayName string = 'QueueMaster Notifications'
param logAnalyticsWorkspaceId string

var resourceToken = uniqueString(subscription().id, resourceGroup().id, location, environmentName)
var communicationServiceName = 'acsqm${take(resourceToken, 16)}'
var emailServiceName = 'acsemailqm${take(resourceToken, 13)}'

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: emailServiceName
  location: 'global'
  properties: {
    dataLocation: dataLocation
  }
}

resource managedDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  name: 'AzureManagedDomain'
  parent: emailService
  location: 'global'
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource sender 'Microsoft.Communication/emailServices/domains/senderUsernames@2023-04-01' = {
  name: senderUsername
  parent: managedDomain
  properties: {
    displayName: senderDisplayName
    username: senderUsername
  }
}

// communicationService is declared after managedDomain so linkedDomains can reference its id
resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: communicationServiceName
  location: 'global'
  properties: {
    dataLocation: dataLocation
    linkedDomains: [managedDomain.id]
  }
}

resource emailServiceDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${emailService.name}'
  scope: emailService
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
  }
}

resource communicationServiceDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${communicationService.name}'
  scope: communicationService
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
  }
}

@secure()
output connectionString string = communicationService.listKeys().primaryConnectionString
output senderAddress string = '${senderUsername}@${managedDomain.properties.mailFromSenderDomain}'
output communicationServiceName string = communicationService.name
output emailServiceName string = emailService.name
