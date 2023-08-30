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
    public static class GetContactsForDistrict
    {
        [FunctionName("GetContactsForDistrict")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "contacts-for-districts/{id}")] HttpRequest req, string id,
            ILogger log)
        {
            var _loggingService = new LoggingService(log);

            // get secrets to access cosmos db and set up cosmos db client
            var cosmosConnectionString = Environment.GetEnvironmentVariable("databaseConnectionString");
            var cosmos = new CosmosClient(cosmosConnectionString);
            var emailsContainer = cosmos.GetContainer("EmailForwarding", "ContactInfoAndRequests");

            try
            {
                // Get the contacts for the district.  Resturn 404 if none are found.
                var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.District = @District and c.Type='ContactsForDistrict' ORDER BY c._ts desc").WithParameter("@District", id);
                var contactsForDistrict = await emailsContainer.GetItemQueryIterator<ContactsForDistrict>(query).ReadNextAsync();
                if (contactsForDistrict.Count == 0)
                {
                    return new NotFoundResult();
                }
                return new OkObjectResult(contactsForDistrict);
            }
            catch (Exception e)
            {
                // log the exception and return a 500 error to the caller
                _loggingService.LogException(e, id);
                return new StatusCodeResult(500);
            }
        }
    }
}
