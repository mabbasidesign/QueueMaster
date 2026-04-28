metadata description = 'Azure API Management resources'

param location string
param environmentName string
param apimName string
param publisherEmail string
param publisherName string
@allowed([
  'Developer'
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Developer'
param skuCapacity int = 1
param logAnalyticsWorkspaceId string
@description('Backend URL for OrderService, e.g. https://orderservice-dev.azurewebsites.net')
param orderServiceBackendUrl string
@description('Backend URL for PaymentService, e.g. https://paymentservice-dev.azurewebsites.net')
param paymentServiceBackendUrl string
@description('Microsoft Entra tenant ID used for JWT validation in APIM policy.')
param entraTenantId string
@description('Expected API audience for JWT validation in APIM policy.')
param jwtAudience string

var entraLoginEndpoint = environment().authentication.loginEndpoint
var openIdConfigUrl = '${entraLoginEndpoint}${entraTenantId}/v2.0/.well-known/openid-configuration'
var v2Issuer = '${entraLoginEndpoint}${entraTenantId}/v2.0'

resource apim 'Microsoft.ApiManagement/service@2023-05-01-preview' = {
  name: apimName
  location: location
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
  }
  tags: {
    environment: environmentName
    'azd-service-name': 'apim'
  }
}

resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diag-${apim.name}'
  scope: apim
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

resource orderApi 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  name: 'order-api'
  parent: apim
  properties: {
    displayName: 'Order API'
    path: 'orders'
    protocols: [
      'https'
    ]
    serviceUrl: orderServiceBackendUrl
    subscriptionRequired: false
  }
}

resource paymentApi 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  name: 'payment-api'
  parent: apim
  properties: {
    displayName: 'Payment API'
    path: 'payments'
    protocols: [
      'https'
    ]
    serviceUrl: paymentServiceBackendUrl
    subscriptionRequired: false
  }
}

resource orderApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2023-05-01-preview' = {
  name: 'policy'
  parent: orderApi
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><cors><allowed-origins><origin>*</origin></allowed-origins><allowed-methods><method>*</method></allowed-methods><allowed-headers><header>*</header></allowed-headers></cors><validate-jwt header-name="Authorization" failed-validation-httpcode="401" require-expiration-time="true"><openid-config url="${openIdConfigUrl}" /><audiences><audience>${jwtAudience}</audience></audiences><issuers><issuer>${v2Issuer}</issuer><issuer>https://sts.windows.net/${entraTenantId}/</issuer></issuers></validate-jwt></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
}

resource paymentApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2023-05-01-preview' = {
  name: 'policy'
  parent: paymentApi
  properties: {
    format: 'rawxml'
    value: '<policies><inbound><base /><cors><allowed-origins><origin>*</origin></allowed-origins><allowed-methods><method>*</method></allowed-methods><allowed-headers><header>*</header></allowed-headers></cors><validate-jwt header-name="Authorization" failed-validation-httpcode="401" require-expiration-time="true"><openid-config url="${openIdConfigUrl}" /><audiences><audience>${jwtAudience}</audience></audiences><issuers><issuer>${v2Issuer}</issuer><issuer>https://sts.windows.net/${entraTenantId}/</issuer></issuers></validate-jwt></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>'
  }
}

output apimName string = apim.name
output gatewayUrl string = 'https://${apim.name}.azure-api.net'
output managementApiUrl string = 'https://${apim.name}.management.azure-api.net'
output developerPortalUrl string = 'https://${apim.name}.developer.azure-api.net'
output resourceId string = apim.id
