param name string
param location string
param containerAppsEnvId string
param acrLoginServer string
param acrName string
param imageTag string

@secure()
param sqlConnectionString string

@secure()
param redisConnectionString string

@secure()
param storageConnectionString string

@secure()
param jwtSecret string

param keyVaultUri string

// Get ACR credentials for image pull
resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: acrName
}

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvId
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        corsPolicy: {
          allowedOrigins: ['*']    // Tightened in production via env var
          allowedMethods: ['*']
          allowedHeaders: ['*']
          allowCredentials: true
        }
      }
      registries: [
        {
          server: acrLoginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        { name: 'acr-password', value: acr.listCredentials().passwords[0].value }
        { name: 'sql-connection', value: sqlConnectionString }
        { name: 'redis-connection', value: redisConnectionString }
        { name: 'storage-connection', value: storageConnectionString }
        { name: 'jwt-secret', value: jwtSecret }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${acrLoginServer}/gitlabclone-api:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
            { name: 'ConnectionStrings__AppDb', secretRef: 'sql-connection' }
            { name: 'Redis__ConnectionString', secretRef: 'redis-connection' }
            { name: 'AzureBlob__ConnectionString', secretRef: 'storage-connection' }
            { name: 'Jwt__Secret', secretRef: 'jwt-secret' }
            { name: 'Jwt__Issuer', value: 'GitLabClone' }
            { name: 'Jwt__Audience', value: 'GitLabClone.Client' }
            { name: 'Jwt__ExpiryHours', value: '8' }
            { name: 'Git__RepoBasePath', value: '/data/repos' }
            { name: 'KeyVault__Uri', value: keyVaultUri }
          ]
          volumeMounts: [
            { volumeName: 'repos', mountPath: '/data/repos' }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 10
            }
          ]
        }
      ]
      volumes: [
        {
          name: 'repos'
          storageType: 'EmptyDir'  // Ephemeral — repos restored from blob on startup
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

output fqdn string = apiApp.properties.configuration.ingress.fqdn
output name string = apiApp.name
