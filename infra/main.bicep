metadata description = 'QueueMaster infrastructure'

param location string = resourceGroup().location
@description('Deployment location for Notification Function resources.')
param functionLocation string
param environmentName string
@description('Comma-separated recipient emails for notifications.')
param notificationRecipientAddresses string

@description('Data location for ACS Communication and Email services.')
param notificationDataLocation string

@description('ACS sender username local-part (before @domain).')
param notificationSenderUsername string

@description('Display name used in outgoing notification emails.')
param notificationSenderDisplayName string

module appinsights 'appinsights.bicep' = {
  params: {
    location: location
    appInsightsName: 'appi-queuemaster-${environmentName}'
  }
}

module servicebus 'servicebus.bicep' = {
  params: {
    location: location
    namespaceName: 'sb-queuemaster-${environmentName}'
    topicName: 'order-created-topic'
    paymentSubscriptionName: 'payment'
    notificationSubscriptionName: 'notification'
    // queueName: 'order-created'
  }
}

module communicationEmail 'communication-email.bicep' = {
  params: {
    location: location
    environmentName: environmentName
    dataLocation: notificationDataLocation
    senderUsername: notificationSenderUsername
    senderDisplayName: notificationSenderDisplayName
  }
}

module notificationFunction 'functionapp.bicep' = {
  params: {
    location: functionLocation
    environmentName: environmentName
    serviceBusConnectionString: servicebus.outputs.connectionString
    appInsightsConnectionString: appinsights.outputs.connectionString
    logAnalyticsWorkspaceId: appinsights.outputs.workspaceId
    notificationConnectionString: communicationEmail.outputs.connectionString
    notificationSenderAddress: communicationEmail.outputs.senderAddress
    notificationRecipientAddresses: notificationRecipientAddresses
  }
}

output RESOURCE_GROUP_ID string = resourceGroup().id
@secure()
output applicationInsightsConnectionString string = appinsights.outputs.connectionString
output namespaceFqdn string = servicebus.outputs.namespaceFqdn
// output queueName string = servicebus.outputs.queueName
output topicName string = servicebus.outputs.topicName
output paymentSubscriptionName string = servicebus.outputs.paymentSubscriptionName
output notificationSubscriptionName string = servicebus.outputs.notificationSubscriptionName
@secure()
output serviceBusConnectionString string = servicebus.outputs.connectionString
output communicationServiceName string = communicationEmail.outputs.communicationServiceName
output emailServiceName string = communicationEmail.outputs.emailServiceName
output notificationSenderAddress string = communicationEmail.outputs.senderAddress
output functionAppName string = notificationFunction.outputs.functionAppName
output functionAppHostname string = notificationFunction.outputs.functionAppHostname
