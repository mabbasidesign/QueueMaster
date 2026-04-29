metadata description = 'Azure Container Apps managed environment for QueueMaster APIs'

param location string
param containerAppsEnvironmentName string

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {}
}

output name string = environment.name
output id string = environment.id
