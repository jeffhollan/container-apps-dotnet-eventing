namespace event_hub;

using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;

public class StreamProcessor : BackgroundService
{
    private readonly ILogger<StreamProcessor> _logger;
    private readonly IConfiguration _configuration;

    public StreamProcessor(IConfiguration configuration, ILogger<StreamProcessor> logger)
    {
        _configuration = configuration;
        _logger = logger;
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
            _logger.LogInformation("StreamProcessor running at: {time}", DateTimeOffset.Now);
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
        _logger.LogInformation($"Recieved message {Encoding.UTF8.GetString(eventArgs.Data.EventBody.ToArray())}");
        await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
    }
}
