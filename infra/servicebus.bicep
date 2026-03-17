metadata description = 'Service Bus namespace and queue'

param location string
param namespaceName string
param queueName string

resource namespace 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: namespaceName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2021-11-01' = {
  parent: namespace
  name: queueName
  properties: {
    maxDeliveryCount: 5
    lockDuration: 'PT5M'
    deadLetteringOnMessageExpiration: true
  }
}

resource authRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2021-11-01' = {
  parent: namespace
  name: 'RootManageSharedAccessKey'
  properties: {
    rights: ['Listen', 'Manage', 'Send']
  }
}

output namespaceName string = namespace.name
output namespaceFqdn string = '${namespace.name}.servicebus.windows.net'
output queueName string = queue.name
@secure()
output connectionString string = authRule.listKeys().primaryConnectionString
