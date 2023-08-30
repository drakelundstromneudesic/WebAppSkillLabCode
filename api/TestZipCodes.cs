using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Company.Services;
using Company.Models;

namespace Company.Function
{
    public static class TestZipCodes
    {
        [FunctionName("TestZipCodes")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "TestZipCodes/{id}")] HttpRequest req, string id,
            ILogger log)
        {
            var _loggingService = new LoggingService(log);

            // get secrets to access cosmos db and set up cosmos db client
            var cosmosConnectionString = Environment.GetEnvironmentVariable("databaseConnectionString");
            var cosmos = new CosmosClient(cosmosConnectionString);
            var emailsContainer = cosmos.GetContainer("EmailForwarding", "ContactInfoAndRequests");

            var zipCodeService = new ZipCodeService(emailsContainer);

            var districts = await zipCodeService.GetDistrictsByZipCode(id);
            return new OkObjectResult(districts);
        }
    }
}
