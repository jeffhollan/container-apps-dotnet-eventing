namespace service_bus;

using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.WebPubSub;

public class QueueProcessor : BackgroundService
{
    private readonly ILogger<QueueProcessor> _logger;
    private readonly IConfiguration _configuration;
    private readonly WebPubSubServiceClient _serviceClient;
    private readonly bool _isPubSub = false;
    private readonly string _podName;

    public QueueProcessor(IConfiguration configuration, ILogger<QueueProcessor> logger)
    {
        _configuration = configuration;
        _logger = logger;
        var webPubSubConnectionString = _configuration.GetValue<string>("WEBPUBSUB_CONNECTION_STRING");
        if(!string.IsNullOrEmpty(webPubSubConnectionString))
        {
            _serviceClient = new WebPubSubServiceClient(webPubSubConnectionString, "stream");
            _isPubSub = true;
        }
        _podName = _configuration.GetValue<string>("POD_NAME");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _configuration.GetValue<string>("SERVICEBUS_QUEUE_NAME");
        var serviceBusClient = new ServiceBusClient(_configuration.GetValue<string>("SERVICEBUS_CONNECTION_STRING"));
        var messageProcessor = serviceBusClient.CreateProcessor(queueName);
        messageProcessor.ProcessMessageAsync += HandleMessageAsync;
        messageProcessor.ProcessErrorAsync += HandleReceivedExceptionAsync;
        
        _logger.LogInformation($"Starting message pump on queue {queueName} in namespace {messageProcessor.FullyQualifiedNamespace}");
        await messageProcessor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("Message pump started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var message = $"{_podName}: QueueProcessor running at: {DateTimeOffset.Now}";
            _logger.LogInformation(message);
            if(_isPubSub)
            {
                _serviceClient.SendToAll(message);
            }
            await Task.Delay(10000, stoppingToken);
        }

        _logger.LogInformation("Closing message pump");
        await messageProcessor.CloseAsync(cancellationToken: stoppingToken);
        _logger.LogInformation("Message pump closed : {Time}", DateTimeOffset.UtcNow);
    }

    private Task HandleReceivedExceptionAsync(ProcessErrorEventArgs arg)
    {
        throw new NotImplementedException();
    }

    private Task HandleMessageAsync(ProcessMessageEventArgs msg)
    {
        var message = $"{_podName}: Received Service Bus message {Encoding.UTF8.GetString(msg.Message.Body.ToArray())}";
        _logger.LogInformation(message);
        if(_isPubSub)
        {
            _serviceClient.SendToAll(message);
        }
        return Task.CompletedTask;
    }
}
