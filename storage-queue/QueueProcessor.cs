namespace service_bus;

using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.WebPubSub;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

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
        _podName = _configuration.GetValue<string>("CONTAINER_APP_REVISION");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _configuration.GetValue<string>("STORAGE_QUEUE_NAME");
        QueueClient queueClient = new QueueClient(_configuration.GetValue<string>("STORAGE_CONNECTION_STRING"), queueName);


        while (!stoppingToken.IsCancellationRequested)
        {
            QueueMessage[] retrievedMessage = await queueClient.ReceiveMessagesAsync(stoppingToken);
            foreach(var message in retrievedMessage)
            {
                await HandleMessageAsync(message);
                await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
            }
        }

        _logger.LogInformation("Message pump closed : {Time}", DateTimeOffset.UtcNow);
    }
    private Task HandleMessageAsync(QueueMessage msg)
    {
        var message = $"{_podName}: Received Storage message {Encoding.UTF8.GetString(System.Convert.FromBase64String(msg.Body.ToString()))}";
        _logger.LogInformation(message);
        if(_isPubSub)
        {
            _serviceClient.SendToAll(message);
        }
        return Task.CompletedTask;
    }
}
