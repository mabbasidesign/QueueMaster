metadata description = 'Azure Key Vault for application secrets'

@description('Azure region for Key Vault.')
param location string
@description('Globally unique Key Vault name.')
param keyVaultName string
@description('Microsoft Entra tenant ID that owns this Key Vault.')
param tenantId string = subscription().tenantId
@description('Key Vault pricing tier.')
param skuName string = 'standard'
@description('Use Azure RBAC for data-plane access instead of access policies.')
param enableRbacAuthorization bool = true
@description('Create OrderService SQL secrets in Key Vault.')
param createOrderServiceSqlSecrets bool = false
@secure()
@description('OrderService SQL admin password secret value.')
param orderServiceSqlAdminPassword string = ''
@secure()
@description('OrderService SQL connection string secret value.')
param orderServiceSqlConnectionString string = ''

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: skuName
    }
    enableRbacAuthorization: enableRbacAuthorization
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
    publicNetworkAccess: 'Enabled'
    enablePurgeProtection: true
  }
}

resource orderServiceSqlAdminPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (createOrderServiceSqlSecrets && !empty(orderServiceSqlAdminPassword)) {
  name: 'orderservice-sql-admin-password'
  parent: keyVault
  properties: {
    value: orderServiceSqlAdminPassword
    contentType: 'password'
  }
}

resource orderServiceSqlConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (createOrderServiceSqlSecrets && !empty(orderServiceSqlConnectionString)) {
  name: 'orderservice-sql-connection-string'
  parent: keyVault
  properties: {
    value: orderServiceSqlConnectionString
    contentType: 'connection-string'
  }
}

output keyVaultName string = keyVault.name
output keyVaultId string = keyVault.id
output vaultUri string = keyVault.properties.vaultUri
