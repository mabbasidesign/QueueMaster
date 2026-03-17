metadata description = 'Application Insights component'

param location string
param appInsightsName string

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
  }
}

@secure()
output connectionString string = appInsights.properties.ConnectionString
output name string = appInsights.name
