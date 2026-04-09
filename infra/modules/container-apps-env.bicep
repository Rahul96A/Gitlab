param name string
param location string
param logAnalyticsWorkspaceId string

@secure()
param logAnalyticsSharedKey string

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2023-09-01').customerId
        sharedKey: logAnalyticsSharedKey
      }
    }
    zoneRedundant: false
  }
}

output envId string = env.id
output defaultDomain string = env.properties.defaultDomain
