param sitename string
param location string
param sku string = 'Free'
param skucode string = 'Free'
param repositoryUrl string
param branch string = 'main'
@secure()
param serviceBusConnection string
param serviceBusQueueName string = 'queue'
param eventHubName string = 'events'
@secure()
param eventHubConnection string

@secure()
param repositoryToken string
param appLocation string = '/static-app/Client'
param apiLocation string = '/static-app/Api'
param appArtifactLocation string = 'wwwroot'

@secure()
param pubsubConnectionString string = ''


resource staticwebapp 'Microsoft.Web/staticSites@2021-01-15' = {
  name: sitename
  location: location
  properties: {
    buildProperties: {
      appLocation: appLocation
      apiLocation: apiLocation
      appArtifactLocation: appArtifactLocation
    }
  }
  sku: {
    tier: sku
    name: skucode
  }
}


resource name_appsettings 'Microsoft.Web/staticSites/config@2021-01-15' = {
  parent: staticwebapp
  name: 'appsettings'
  properties: {
    ServiceBusConnection: serviceBusConnection
    SERVICEBUS_QUEUE_NAME: serviceBusQueueName
    EventHubConnection: eventHubConnection
    EVENTHUB_NAME: eventHubName
    WebPubSubConnectionString: pubsubConnectionString
  }
}
