namespace event_hub;

using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Azure.Messaging.WebPubSub;

public class StreamProcessor : BackgroundService
{
    private readonly ILogger<StreamProcessor> _logger;
    private readonly IConfiguration _configuration;
    private readonly WebPubSubServiceClient _serviceClient;
    private readonly bool _isPubSub = false;
    private readonly string _podName;

    public StreamProcessor(IConfiguration configuration, ILogger<StreamProcessor> logger)
    {
        _configuration = configuration;
        _logger = logger;
        var webPubSubConnectionString = _configuration.GetValue<string>("WEBPUBSUB_CONNECTION_STRING");
        if(!string.IsNullOrEmpty(webPubSubConnectionString))
        {
            _serviceClient = new WebPubSubServiceClient(webPubSubConnectionString, "stream");
            _isPubSub = true;
        }
        _podName = _configuration.GetValue<string>("CONTAINER_APP_REVISION");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var storageClient = new BlobContainerClient(_configuration.GetValue<string>("STORAGE_CONNECTION_STRING"), _configuration.GetValue<string>("STORAGE_BLOB_NAME"));
        var processor = new EventProcessorClient(storageClient, _configuration.GetValue<string>("EVENTHUB_CONSUMER_GROUP"), _configuration.GetValue<string>("EVENTHUB_CONNECTION_STRING"), _configuration.GetValue<string>("EVENTHUB_NAME"));

        processor.ProcessEventAsync += ProcessEventHandler;
        processor.ProcessErrorAsync += ProcessErrorHandler;

        // Start the processing
        await processor.StartProcessingAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            var message = $"{_podName}: StreamProcessor running at: {DateTimeOffset.Now}.";
            _logger.LogInformation(message);
            if(_isPubSub)
            {
                _serviceClient.SendToAll(message);
            }
            await Task.Delay(10000, stoppingToken);
        }

        _logger.LogInformation("Closing message pump");
        processor.ProcessEventAsync -= ProcessEventHandler;
        processor.ProcessErrorAsync -= ProcessErrorHandler;
        // Stop the processing
        await processor.StopProcessingAsync();

        _logger.LogInformation("Message pump closed : {Time}", DateTimeOffset.UtcNow);
    }

    private Task ProcessErrorHandler(ProcessErrorEventArgs arg)
    {
        throw new NotImplementedException();
    }

    private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
    {
        var message = $"{_podName}: Received Event Hub message {Encoding.UTF8.GetString(eventArgs.Data.EventBody.ToArray())}";
        _logger.LogInformation(message);
        if(_isPubSub)
        {
            _serviceClient.SendToAll(message);
        }
        await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
    }
}
