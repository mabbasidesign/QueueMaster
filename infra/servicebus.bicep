metadata description = 'Service Bus namespace, topic, and subscriptions'

param location string
param namespaceName string
param topicName string
param paymentSubscriptionName string
param notificationSubscriptionName string
param logAnalyticsWorkspaceId string

// param queueName string

resource namespace 'Microsoft.ServiceBus/namespaces@2021-11-01' = {
  name: namespaceName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

// resource queue 'Microsoft.ServiceBus/namespaces/queues@2021-11-01' = {
//   parent: namespace
//   name: queueName
//   properties: {
//     maxDeliveryCount: 5
//     lockDuration: 'PT5M'
//     deadLetteringOnMessageExpiration: true
//   }
// }

resource topic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: namespace
  name: topicName
  properties: {
    defaultMessageTimeToLive: 'P14D'
    enableBatchedOperations: true
    requiresDuplicateDetection: false
  }
}

resource paymentSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: topic
  name: paymentSubscriptionName
  properties: {
    maxDeliveryCount: 5
    lockDuration: 'PT5M'
    deadLetteringOnMessageExpiration: true
  }
}

resource notificationSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2021-11-01' = {
  parent: topic
  name: notificationSubscriptionName
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

resource namespaceDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${namespace.name}'
  scope: namespace
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
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

output namespaceName string = namespace.name
output namespaceFqdn string = '${namespace.name}.servicebus.windows.net'
// output queueName string = queue.name
output topicName string = topic.name
output paymentSubscriptionName string = paymentSubscription.name
output notificationSubscriptionName string = notificationSubscription.name
@secure()
output connectionString string = authRule.listKeys().primaryConnectionString
