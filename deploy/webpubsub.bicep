param pubsubName string = 'pubsub-${uniqueString(resourceGroup().id)}'
param location string = resourceGroup().location
param sku string = 'Free_F1'

resource pubsub 'Microsoft.SignalRService/WebPubSub@2021-04-01-preview' = {
  name: pubsubName
  location: location

  sku: {
    name: sku
    capacity: 1
  }
  
  properties: {
  }
}

output pubsubConnectionString string = 'Endpoint=https://${pubsubName}.webpubsub.azure.com;AccessKey=${listkeys(pubsub.id, pubsub.apiVersion).primaryKey};Version=1.0;'
