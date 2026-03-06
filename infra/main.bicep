metadata description = 'QueueMaster infrastructure'

param location string = resourceGroup().location
param environment string = 'dev'

module appinsights 'appinsights.bicep' = {
  params: {
    location: location
    appInsightsName: 'appi-queuemaster-${environment}'
  }
}

module servicebus 'servicebus.bicep' = {
  params: {
    location: location
    namespaceName: 'sb-queuemaster-${environment}'
    queueName: 'order-created'
  }
}

@secure()
output applicationInsightsConnectionString string = appinsights.outputs.connectionString
output namespaceFqdn string = servicebus.outputs.namespaceFqdn
output queueName string = servicebus.outputs.queueName
@secure()
output connectionString string = servicebus.outputs.connectionString
