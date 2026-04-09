// ─── GitLabClone Infrastructure ─────────────────────────────────────────────
// Deploys: Resource Group scoped — SQL Server, Redis, Storage, Key Vault,
//          Container Registry, Log Analytics, Container Apps Environment,
//          and the API + Frontend Container Apps.
//
// Usage:
//   az deployment sub create \
//     --location eastus2 \
//     --template-file infra/main.bicep \
//     --parameters infra/main.parameters.json
// ─────────────────────────────────────────────────────────────────────────────

targetScope = 'subscription'

@description('Base name for all resources (e.g. "gitlabclone")')
param appName string = 'gitlabclone'

@description('Azure region for deployment')
param location string = 'eastus2'

@description('SQL Server admin password')
@secure()
param sqlAdminPassword string

@description('JWT signing secret (min 64 chars)')
@secure()
param jwtSecret string

@description('Container image tag for the API')
param apiImageTag string = 'latest'

@description('Container image tag for the Frontend')
param frontendImageTag string = 'latest'

// ─── Resource Group ─────────────────────────────────────────────────────────
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${appName}'
  location: location
}

// ─── Modules ────────────────────────────────────────────────────────────────
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'logAnalytics'
  scope: rg
  params: {
    name: 'log-${appName}'
    location: location
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVault'
  scope: rg
  params: {
    name: 'kv-${appName}'
    location: location
    sqlAdminPassword: sqlAdminPassword
    jwtSecret: jwtSecret
  }
}

module sql 'modules/sql-server.bicep' = {
  name: 'sqlServer'
  scope: rg
  params: {
    serverName: 'sql-${appName}'
    databaseName: 'GitLabClone'
    location: location
    adminPassword: sqlAdminPassword
  }
}

module redis 'modules/redis.bicep' = {
  name: 'redis'
  scope: rg
  params: {
    name: 'redis-${appName}'
    location: location
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    name: replace('st${appName}', '-', '')
    location: location
  }
}

module acr 'modules/container-registry.bicep' = {
  name: 'acr'
  scope: rg
  params: {
    name: replace('acr${appName}', '-', '')
    location: location
  }
}

module containerAppsEnv 'modules/container-apps-env.bicep' = {
  name: 'containerAppsEnv'
  scope: rg
  params: {
    name: 'cae-${appName}'
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
    logAnalyticsSharedKey: logAnalytics.outputs.sharedKey
  }
}

module apiApp 'modules/container-app-api.bicep' = {
  name: 'apiApp'
  scope: rg
  params: {
    name: 'ca-${appName}-api'
    location: location
    containerAppsEnvId: containerAppsEnv.outputs.envId
    acrLoginServer: acr.outputs.loginServer
    acrName: acr.outputs.name
    imageTag: apiImageTag
    sqlConnectionString: sql.outputs.connectionString
    redisConnectionString: redis.outputs.connectionString
    storageConnectionString: storage.outputs.connectionString
    jwtSecret: jwtSecret
    keyVaultUri: keyVault.outputs.vaultUri
  }
}

module frontendApp 'modules/container-app-frontend.bicep' = {
  name: 'frontendApp'
  scope: rg
  params: {
    name: 'ca-${appName}-web'
    location: location
    containerAppsEnvId: containerAppsEnv.outputs.envId
    acrLoginServer: acr.outputs.loginServer
    acrName: acr.outputs.name
    imageTag: frontendImageTag
    apiBaseUrl: 'https://${apiApp.outputs.fqdn}'
  }
}

// ─── Outputs ────────────────────────────────────────────────────────────────
output resourceGroupName string = rg.name
output apiUrl string = 'https://${apiApp.outputs.fqdn}'
output frontendUrl string = 'https://${frontendApp.outputs.fqdn}'
output acrLoginServer string = acr.outputs.loginServer
output keyVaultUri string = keyVault.outputs.vaultUri
