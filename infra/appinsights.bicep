metadata description = 'Application Insights component'

param location string
param appInsightsName string

// Single Log Analytics workspace — shared by App Insights and function app diagnostics
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${appInsightsName}-law'
  location: location
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

// Workspace-based App Insights (modern mode)
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
    WorkspaceResourceId: logAnalytics.id
  }
}

@secure()
output connectionString string = appInsights.properties.ConnectionString
output name string = appInsights.name
output workspaceId string = logAnalytics.id
