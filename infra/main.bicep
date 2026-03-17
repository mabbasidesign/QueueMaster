metadata description = 'QueueMaster infrastructure'

param location string = resourceGroup().location
param environmentName string = 'dev'

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
    queueName: 'order-created'
  }
}

module notificationFunction 'functionapp.bicep' = {
  params: {
    location: location
    environmentName: environmentName
    serviceBusConnectionString: servicebus.outputs.connectionString
    appInsightsConnectionString: appinsights.outputs.connectionString
  }
}

output RESOURCE_GROUP_ID string = resourceGroup().id
@secure()
output applicationInsightsConnectionString string = appinsights.outputs.connectionString
output namespaceFqdn string = servicebus.outputs.namespaceFqdn
output queueName string = servicebus.outputs.queueName
@secure()
output serviceBusConnectionString string = servicebus.outputs.connectionString
output functionAppName string = notificationFunction.outputs.functionAppName
output functionAppHostname string = notificationFunction.outputs.functionAppHostname
