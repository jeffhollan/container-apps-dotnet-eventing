param location string = resourceGroup().location
param environmentName string = 'event-driven-sample-env'
param swaLocation string = 'centralus'

// Service Bus settings
param serviceBusNamespace string = 'queue-${uniqueString(resourceGroup().id)}'
param serviceBusQueueName string = 'queue'

// Event Hub settings
param eventHubNamespace string = 'eh-${uniqueString(resourceGroup().id)}'
param eventHubName string = 'events'
param eventHubConsumerGroup string = 'aca'
param storageAccountName string = 'stor${uniqueString(resourceGroup().id)}'

// Container Apps settings
param serviceBusImage string
param eventHubImage string
param registry string
param registryUsername string
@secure()
param registryPassword string

// Settings for Static Web App if deploying (by default will not)
param deployDebugSite bool = false
param staticWebAppName string = 'debug-site'
param repositoryUrl string = ''
@secure()
param repositoryToken string = ''

var serviceBusConnectionSecretName = 'service-bus-connection-string'
var eventHubConnectionSecretName = 'event-hub-connection-string'
var storageConnectionSecretName = 'storage-connection-string'
var registryPasswordPropertyName = 'registry-password'
var storageLeaseBlobName = 'aca-leases'
var pubsubConnectionSecretName = 'webpubsub-connection-string'

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

// Deploy Azure Web PubSub if deploying the debug site
module pubsub 'webpubsub.bicep' = {
  name: 'web-pubsub'
  params: {
    location: location
  }
}

// Service Bus Processor Container App
resource sbContainerApp 'Microsoft.Web/containerApps@2021-03-01' = {
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
        {
          name: pubsubConnectionSecretName
          value: pubsub.outputs.pubsubConnectionString
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
            {
              name: 'WEBPUBSUB_CONNECTION_STRING'
              secretref: pubsubConnectionSecretName
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

module eventHub 'eventhub.bicep' = {
  name: 'eventhub'
  params: {
    eventHubNamespaceName: eventHubNamespace
    eventHubName: eventHubName
    consumerGroupName: eventHubConsumerGroup
    storageAccountName: storageAccountName
    storageLeaseBlobName: storageLeaseBlobName
  }
}

resource ehContainerApp 'Microsoft.Web/containerApps@2021-03-01' = {
  name: 'event-hub-app'
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
          name: eventHubConnectionSecretName
          value: eventHub.outputs.eventHubConnectionString
        }
        {
          name: storageConnectionSecretName
          value: eventHub.outputs.storageConnectionString
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
          image: eventHubImage
          name: 'event-hub-app'
          env: [
            {
              name: 'EVENTHUB_CONNECTION_STRING'
              secretref: eventHubConnectionSecretName
            }
            {
              name: 'EVENTHUB_NAME'
              value: eventHubName
            }
            {
              name: 'EVENTHUB_CONSUMER_GROUP'
              value: eventHubConsumerGroup
            }
            {
              name: 'STORAGE_CONNECTION_STRING'
              secretref: storageConnectionSecretName
            }
            {
              name: 'STORAGE_BLOB_NAME'
              value: storageLeaseBlobName
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
              // https://keda.sh/docs/scalers/azure-event-hub/
              type: 'azure-eventhub'
              metadata: {
                consumerGroup: eventHubConsumerGroup
                unprocessedEventThreshold: '64'
                blobContainer: storageLeaseBlobName
                checkpointStrategy: 'blobMetadata'
              }
              auth: [
                {
                  secretRef: eventHubConnectionSecretName
                  // will replace the connectionFromRef KEDA property
                  triggerParameter: 'connection'
                }
                {
                  secretRef: storageConnectionSecretName
                  // will replace the storageConnectionFromEnv KEDA property
                  triggerParameter: 'storageConnection'
                }
              ]
            }
          }
        ]
      }
    }
  }
}


module staticapp 'staticapp.bicep' = if (deployDebugSite) {
  name: 'static-web-app'
  params: {
    sitename: staticWebAppName
    location: swaLocation
    serviceBusConnection: serviceBusQueue.outputs.serviceBusConnectionString
    eventHubConnection: eventHub.outputs.eventHubConnectionString
    serviceBusQueueName: serviceBusQueueName
    eventHubName: eventHubName
    repositoryUrl: repositoryUrl
    repositoryToken: repositoryToken
    pubsubConnectionString: pubsub.outputs.pubsubConnectionString
  }
}
