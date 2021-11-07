using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using BlazorApp.Shared;

namespace BlazorApp.Api
{
    public static class EventHubSenderFunction
    {
        [FunctionName("EventHubSender")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] EventHubRequest req,
            [EventHub("%EVENTHUB_NAME%", Connection = "EventHubConnection")]out string message,
            ILogger log)
        {
            message = req.Message;
            return new OkObjectResult(req.Message);
        }
    }
}
