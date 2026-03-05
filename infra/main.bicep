metadata description = 'QueueMaster infrastructure'

param location string = resourceGroup().location
param environment string = 'dev'

module servicebus 'servicebus.bicep' = {
  params: {
    location: location
    namespaceName: 'sb-queuemaster-${environment}'
    queueName: 'order-created'
  }
}

output namespaceFqdn string = servicebus.outputs.namespaceFqdn
output queueName string = servicebus.outputs.queueName
@secure()
output connectionString string = servicebus.outputs.connectionString
