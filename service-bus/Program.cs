using service_bus;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<QueueProcessor>();
    })
    .Build();

await host.RunAsync();
