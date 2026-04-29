metadata description = 'PaymentService deployment to Azure Container Apps'

param location string
param containerAppName string
param containerAppsEnvironmentId string
param containerRegistryServer string
param containerRegistryUsername string
@secure()
param containerRegistryPassword string
param image string
@secure()
param sqlConnectionString string
@secure()
param serviceBusConnectionString string
@secure()
param appInsightsConnectionString string
param authTenantId string
param authAudience string
param serviceBusTopicName string = 'order-created-topic'
param serviceBusSubscriptionName string = 'payment'
param serviceBusEnabled bool = true
param containerCpu string = '0.5'
param containerMemory string = '1Gi'

resource paymentServiceApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: containerRegistryServer
          username: containerRegistryUsername
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: containerRegistryPassword
        }
        {
          name: 'db-connection-string'
          value: sqlConnectionString
        }
        {
          name: 'servicebus-connection-string'
          value: serviceBusConnectionString
        }
        {
          name: 'appinsights-connection-string'
          value: appInsightsConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'paymentservice'
          image: image
          resources: {
            cpu: json(containerCpu)
            memory: containerMemory
          }
          env: [
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__DefaultConnection'
              secretRef: 'db-connection-string'
            }
            {
              name: 'ServiceBus__Enabled'
              value: string(serviceBusEnabled)
            }
            {
              name: 'ServiceBus__ConnectionString'
              secretRef: 'servicebus-connection-string'
            }
            {
              name: 'ServiceBus__UseManagedIdentity'
              value: 'false'
            }
            {
              name: 'ServiceBus__TopicName'
              value: serviceBusTopicName
            }
            {
              name: 'ServiceBus__SubscriptionName'
              value: serviceBusSubscriptionName
            }
            {
              name: 'Authentication__TenantId'
              value: authTenantId
            }
            {
              name: 'Authentication__Audience'
              value: authAudience
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              secretRef: 'appinsights-connection-string'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

output name string = paymentServiceApp.name
output fqdn string = paymentServiceApp.properties.configuration.ingress.fqdn
output url string = 'https://${paymentServiceApp.properties.configuration.ingress.fqdn}'