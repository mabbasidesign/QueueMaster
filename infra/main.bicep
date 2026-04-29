metadata description = 'QueueMaster infrastructure'

param location string = resourceGroup().location
@description('Deployment location for Notification Function resources.')
param functionLocation string
param environmentName string
@description('Comma-separated recipient emails for notifications.')
param notificationRecipientAddresses string
@description('Publisher email required by API Management.')
param apimPublisherEmail string
@description('Publisher display name required by API Management.')
param apimPublisherName string
@description('APIM SKU tier.')
param apimSkuName string = 'Developer'
@description('OrderService backend URL for APIM forwarding.')
param orderServiceBackendUrl string
@description('PaymentService backend URL for APIM forwarding.')
param paymentServiceBackendUrl string
@description('Enable ACR and Container Apps environment deployment.')
param deployContainerAppsBase bool = false
@description('Deploy OrderService to Azure Container Apps.')
param deployOrderServiceContainerApp bool = false
@description('Deploy PaymentService to Azure Container Apps.')
param deployPaymentServiceContainerApp bool = false
@description('Location for Container Apps and ACR resources.')
param containerAppsLocation string = location
@description('Name of Azure Container Registry.')
param containerRegistryName string
@description('SKU for Azure Container Registry.')
param containerRegistrySku string = 'Basic'
@description('Name of Container Apps managed environment.')
param containerAppsEnvironmentName string
@description('OrderService image tag in ACR, e.g. orderservice:v1.')
param orderServiceImageTag string = 'orderservice:v1'
@description('PaymentService image tag in ACR, e.g. paymentservice:v1.')
param paymentServiceImageTag string = 'paymentservice:v1'
@description('OrderService Container App name.')
param orderServiceContainerAppName string = 'ca-orderservice-${environmentName}'
@description('PaymentService Container App name.')
param paymentServiceContainerAppName string = 'ca-paymentservice-${environmentName}'
@description('Microsoft Entra tenant ID for API authentication.')
param authTenantId string
@description('Expected audience for API authentication.')
param authAudience string
@secure()
@description('SQL connection string for OrderService in cloud.')
param orderServiceSqlConnectionString string = ''
@secure()
@description('SQL connection string for PaymentService in cloud.')
param paymentServiceSqlConnectionString string = ''
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
    logAnalyticsWorkspaceId: appinsights.outputs.workspaceId
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
    logAnalyticsWorkspaceId: appinsights.outputs.workspaceId
  }
}

module apim 'apim.bicep' = {
  params: {
    location: location
    environmentName: environmentName
    apimName: 'apim-queuemaster-${environmentName}'
    publisherEmail: apimPublisherEmail
    publisherName: apimPublisherName
    skuName: apimSkuName
    logAnalyticsWorkspaceId: appinsights.outputs.workspaceId
    orderServiceBackendUrl: orderServiceBackendUrl
    paymentServiceBackendUrl: paymentServiceBackendUrl
  }
}

module acr 'acr.bicep' = if (deployContainerAppsBase) {
  params: {
    location: containerAppsLocation
    acrName: containerRegistryName
    skuName: containerRegistrySku
    adminUserEnabled: true
  }
}

module containerAppsEnv 'containerapps-env.bicep' = if (deployContainerAppsBase) {
  params: {
    location: containerAppsLocation
    containerAppsEnvironmentName: containerAppsEnvironmentName
  }
}

module orderServiceContainerApp 'orderservice-aca.bicep' = if (deployOrderServiceContainerApp) {
  params: {
    location: containerAppsLocation
    containerAppName: orderServiceContainerAppName
    containerAppsEnvironmentId: containerAppsEnv.outputs.id
    containerRegistryServer: acr.outputs.loginServer
    containerRegistryUsername: acr.outputs.adminUsername
    containerRegistryPassword: acr.outputs.adminPassword
    image: '${acr.outputs.loginServer}/${orderServiceImageTag}'
    sqlConnectionString: orderServiceSqlConnectionString
    serviceBusConnectionString: servicebus.outputs.connectionString
    appInsightsConnectionString: appinsights.outputs.connectionString
    authTenantId: authTenantId
    authAudience: authAudience
  }
}

module paymentServiceContainerApp 'paymentservice-aca.bicep' = if (deployPaymentServiceContainerApp) {
  params: {
    location: containerAppsLocation
    containerAppName: paymentServiceContainerAppName
    containerAppsEnvironmentId: containerAppsEnv.outputs.id
    containerRegistryServer: acr.outputs.loginServer
    containerRegistryUsername: acr.outputs.adminUsername
    containerRegistryPassword: acr.outputs.adminPassword
    image: '${acr.outputs.loginServer}/${paymentServiceImageTag}'
    sqlConnectionString: paymentServiceSqlConnectionString
    serviceBusConnectionString: servicebus.outputs.connectionString
    appInsightsConnectionString: appinsights.outputs.connectionString
    authTenantId: authTenantId
    authAudience: authAudience
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
output apimName string = apim.outputs.apimName
output apimGatewayUrl string = apim.outputs.gatewayUrl
output apimDeveloperPortalUrl string = apim.outputs.developerPortalUrl
output functionAppName string = notificationFunction.outputs.functionAppName
output functionAppHostname string = notificationFunction.outputs.functionAppHostname
output containerRegistryLoginServer string = deployContainerAppsBase ? acr.outputs.loginServer : ''
output containerAppsEnvironmentResourceId string = deployContainerAppsBase ? containerAppsEnv.outputs.id : ''
output orderServiceContainerAppUrl string = deployOrderServiceContainerApp ? orderServiceContainerApp.outputs.url : ''
output paymentServiceContainerAppUrl string = deployPaymentServiceContainerApp ? paymentServiceContainerApp.outputs.url : ''
