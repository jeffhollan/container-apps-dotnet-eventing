param location string = resourceGroup().location
param environmentName string = 'event-driven-sample-env'
param serviceBusNamespace string = 'queue-${uniqueString(resourceGroup().id)}'
param serviceBusQueueName string = 'queue'
param serviceBusImage string
param registry string
param registryUsername string
@secure()
param registryPassword string

var serviceBusConnectionSecretName = 'service-bus-connection-string'
var registryPasswordPropertyName = 'registry-password'

// Container Apps Environment (environment.bicep)
module environment 'environment.bicep' = {
  name: 'container-app-environment'
  params: {
    environmentName: environmentName
    location: location
  }
}

module serviceBusQueue 'servicebus.bicep' = {
  name: 'service-bus-queue'
  params: {
    serviceBusNamespaceName: serviceBusNamespace
    serviceBusQueueName: serviceBusQueueName
  }
}

// Service Bus Processor Container App
resource containerApp 'Microsoft.Web/containerApps@2021-03-01' = {
  name: 'service-bus-app'
  kind: 'containerapp'
  location: location
  properties: {
    kubeEnvironmentId: environment.outputs.environmentId
    configuration: {
      activeRevisionsMode: 'single'
      secrets: [
        {
          name: registryPasswordPropertyName
          value: registryPassword
        }
        {
          name: serviceBusConnectionSecretName
          value: serviceBusQueue.outputs.serviceBusConnectionString
        }
      ]   
      registries: [
        {
          server: registry
          username: registryUsername
          passwordSecretRef: registryPasswordPropertyName
        }
      ]
    }
    template: {
      containers: [
        {
          image: serviceBusImage
          name: 'service-bus-app'
          env: [
            {
              name: 'SERVICEBUS_CONNECTION_STRING'
              secretref: serviceBusConnectionSecretName
            }
            {
              name: 'SERVICEBUS_QUEUE_NAME'
              value: serviceBusQueueName
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 10
        rules: [
          {
            name: 'sb-keda-scale'
            custom: {
              // https://keda.sh/docs/scalers/azure-service-bus/
              type: 'azure-servicebus'
              metadata: {
                queueName: serviceBusQueueName
                messageCount: '100'
              }
              auth: [
                {
                  secretRef: serviceBusConnectionSecretName
                  // will replace the connectionFromRef KEDA property
                  triggerParameter: 'connection'
                }
              ]
            }
          }
        ]
      }
    }
  }
}
