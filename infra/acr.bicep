metadata description = 'Azure Container Registry for QueueMaster container images'

param location string
param acrName string
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Basic'
param adminUserEnabled bool = true

resource registry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: acrName
  location: location
  sku: {
    name: skuName
  }
  properties: {
    adminUserEnabled: adminUserEnabled
    publicNetworkAccess: 'Enabled'
  }
}

var registryCredentials = listCredentials(registry.id, registry.apiVersion)

output name string = registry.name
output id string = registry.id
output loginServer string = registry.properties.loginServer
output adminUsername string = registryCredentials.username
@secure()
output adminPassword string = registryCredentials.passwords[0].value