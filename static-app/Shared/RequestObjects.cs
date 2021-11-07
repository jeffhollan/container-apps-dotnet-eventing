using System;

namespace BlazorApp.Shared
{
    public class ServiceBusRequest
    {
        public string Message { get; set; }
    }

    public class EventHubRequest
    {
        public string Message { get; set; }
    }
}
