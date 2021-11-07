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
    public static class ServiceBusSenderFunction
    {
        [FunctionName("ServiceBusSender")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] ServiceBusRequest req,
            [ServiceBus("%SERVICEBUS_QUEUE_NAME%", Connection = "ServiceBusConnection")] out string message,
            ILogger log)
        {
            message = req.Message;
            return new OkObjectResult(req.Message);
        }
    }
}
