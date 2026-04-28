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

resource orderHealthOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'health-check'
  parent: orderApi
  properties: {
    displayName: 'Order Service Health'
    method: 'GET'
    urlTemplate: '/health'
  }
}

resource orderListOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'list-orders'
  parent: orderApi
  properties: {
    displayName: 'List Orders'
    method: 'GET'
    urlTemplate: '/api/orders'
  }
}

resource orderGetByIdOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'get-order-by-id'
  parent: orderApi
  properties: {
    displayName: 'Get Order By Id'
    method: 'GET'
    urlTemplate: '/api/orders/{id}'
    templateParameters: [
      {
        name: 'id'
        required: true
        type: 'number'
      }
    ]
  }
}

resource orderCreateOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'create-order'
  parent: orderApi
  properties: {
    displayName: 'Create Order'
    method: 'POST'
    urlTemplate: '/api/orders'
  }
}

resource orderUpdateOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'update-order'
  parent: orderApi
  properties: {
    displayName: 'Update Order'
    method: 'PUT'
    urlTemplate: '/api/orders/{id}'
    templateParameters: [
      {
        name: 'id'
        required: true
        type: 'number'
      }
    ]
  }
}

resource orderDeleteOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'delete-order'
  parent: orderApi
  properties: {
    displayName: 'Delete Order'
    method: 'DELETE'
    urlTemplate: '/api/orders/{id}'
    templateParameters: [
      {
        name: 'id'
        required: true
        type: 'number'
      }
    ]
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

resource paymentHealthOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'health-check'
  parent: paymentApi
  properties: {
    displayName: 'Payment Service Health'
    method: 'GET'
    urlTemplate: '/health'
  }
}

resource paymentListOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'list-payments'
  parent: paymentApi
  properties: {
    displayName: 'List Payments'
    method: 'GET'
    urlTemplate: '/api/payments'
  }
}

resource paymentGetByTransactionOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'get-payment-by-transaction-id'
  parent: paymentApi
  properties: {
    displayName: 'Get Payment By Transaction Id'
    method: 'GET'
    urlTemplate: '/api/payments/{transactionId}'
    templateParameters: [
      {
        name: 'transactionId'
        required: true
        type: 'string'
      }
    ]
  }
}

resource paymentGetByOrderIdOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'get-payments-by-order-id'
  parent: paymentApi
  properties: {
    displayName: 'Get Payments By Order Id'
    method: 'GET'
    urlTemplate: '/api/payments/order/{orderId}'
    templateParameters: [
      {
        name: 'orderId'
        required: true
        type: 'number'
      }
    ]
  }
}

resource paymentCreateOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'create-payment'
  parent: paymentApi
  properties: {
    displayName: 'Create Payment'
    method: 'POST'
    urlTemplate: '/api/payments'
  }
}

resource paymentUpdateOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'update-payment'
  parent: paymentApi
  properties: {
    displayName: 'Update Payment'
    method: 'PUT'
    urlTemplate: '/api/payments/{transactionId}'
    templateParameters: [
      {
        name: 'transactionId'
        required: true
        type: 'string'
      }
    ]
  }
}

resource paymentDeleteOperation 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  name: 'delete-payment'
  parent: paymentApi
  properties: {
    displayName: 'Delete Payment'
    method: 'DELETE'
    urlTemplate: '/api/payments/{transactionId}'
    templateParameters: [
      {
        name: 'transactionId'
        required: true
        type: 'string'
      }
    ]
  }
}

output apimName string = apim.name
output gatewayUrl string = 'https://${apim.name}.azure-api.net'
output managementApiUrl string = 'https://${apim.name}.management.azure-api.net'
output developerPortalUrl string = 'https://${apim.name}.developer.azure-api.net'
output resourceId string = apim.id
