param name string
param location string

@secure()
param sqlAdminPassword string

@secure()
param jwtSecret string

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 30
    enabledForDeployment: true
    enabledForTemplateDeployment: true
  }
}

resource sqlPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'SqlAdminPassword'
  properties: {
    value: sqlAdminPassword
  }
}

resource jwtSecretSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'JwtSecret'
  properties: {
    value: jwtSecret
  }
}

output vaultUri string = vault.properties.vaultUri
output vaultId string = vault.id
