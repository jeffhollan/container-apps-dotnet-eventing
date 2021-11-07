using event_hub;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostContext, builder) =>
    {
        if (hostContext.HostingEnvironment.IsDevelopment())
        {
            builder.AddUserSecrets<Program>();
        }
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<StreamProcessor>();
    })
    .Build();

await host.RunAsync();
