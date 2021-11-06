namespace service_bus;

using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

public class QueueProcessor : BackgroundService
{
    private readonly ILogger<QueueProcessor> _logger;
    private readonly IConfiguration _configuration;

    public QueueProcessor(IConfiguration configuration, ILogger<QueueProcessor> logger)
    {
        _configuration = configuration;
        _logger = logger;
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
            _logger.LogInformation("QueueProcessor running at: {time}", DateTimeOffset.Now);
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
        _logger.LogInformation($"Recieved message {Encoding.UTF8.GetString(msg.Message.Body.ToArray())}");
        return Task.CompletedTask;
    }
}
