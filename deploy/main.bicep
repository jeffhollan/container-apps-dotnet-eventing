param location string = resourceGroup().location
param environmentName string = 'sample-env'
param eventHubImage string
param eventHubPort int
param service-bus-Image string
param service-bus-Port int
param registry string
param registryUsername string
@secure()
param registryPassword string

// Container Apps Environment (environment.bicep)
module environment 'environment.bicep' = {
  name: 'container-app-environment'
  params: {
    environmentName: environmentName
    location: location
  }
}


// Container-2-service-bus- (container-app.bicep)
// We deploy it first so we can call it from the service-bus-app
module service-bus-app 'container-app.bicep' = {
  name: 'service-bus-app'
  params: {
    containerAppName: 'service-bus-app'
    location: location
    environmentId: environment.outputs.environmentId
    containerImage: service-bus-Image
    containerPort: service-bus-Port
    containerRegistry: registry
    containerRegistryUsername: registryUsername
    containerRegistryPassword: registryPassword
    isExternalIngress: false
  }
}


// Container-1-Node (container-app.bicep)
module event-hub-app 'container-app.bicep' = {
  name: 'event-hub-app'
  params: {
    containerAppName: 'event-hub-app'
    location: location
    environmentId: environment.outputs.environmentId
    containerImage: eventHubImage
    containerPort: eventHubPort
    containerRegistry: registry
    containerRegistryUsername: registryUsername
    containerRegistryPassword: registryPassword
    isExternalIngress: true
    // set an environment var for the service-bus-FQDN to call
    environmentVars: [
      {
        name: 'service-bus-_FQDN'
        value: service-bus-app.outputs.fqdn
      }
    ]
  }
}
