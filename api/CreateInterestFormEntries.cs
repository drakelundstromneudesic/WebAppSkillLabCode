using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Company.Services;
using Company.Models;

namespace Company.Function
{
    public static class CreateInterestFormEntries
    {
        [FunctionName("CreateInterestFormEntries")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "interest-form-entries")] HttpRequest req,
            ILogger log)
        {
            var _loggingService = new LoggingService(log);

            // get secrets to access email service and set up email sender
            var sendingEmailAddress = Environment.GetEnvironmentVariable("sendingEmailAddress");
            var sendingEmailPassword = Environment.GetEnvironmentVariable("sendingEmailPassword");
            var emailSender = new EmailSender(sendingEmailAddress, sendingEmailPassword, _loggingService);

            // get secrets to access cosmos db and set up cosmos db client
            var cosmosConnectionString = Environment.GetEnvironmentVariable("databaseConnectionString");
            var cosmos = new CosmosClient(cosmosConnectionString);
            var emailsContainer = cosmos.GetContainer("EmailForwarding", "ContactInfoAndRequests");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // get request body and log it to cosmos db.  On failure, send email with error message and return 500.
            try
            {
                var requestBodyLog = new RequestBodyLog(requestBody);
                await emailsContainer.CreateItemAsync<RequestBodyLog>(requestBodyLog);
            }
            catch (Exception e)
            {
                _loggingService.LogException(e, requestBody);
                await emailSender.SendEmailAsync(new List<string> { sendingEmailAddress }, "submission unable to be logged to database", $@"error message: {e.Message}.  Submission: {requestBody}");
                return new StatusCodeResult(500);
            }

            // Deserialize request.  On failure, send email with error message and return 500.
            var interestFormSubmissions = new List<InterestFormSubmission>();
            try
            {
                interestFormSubmissions = JsonSerializer.Deserialize<List<InterestFormSubmission>>(requestBody);
            }
            catch (Exception e)
            {
                _loggingService.LogException(e, requestBody);
                await emailSender.SendEmailAsync(new List<string> { sendingEmailAddress }, "Failure to process submission or send email", $@"error message: {e.Message}.  Submission: {requestBody}");
                return new StatusCodeResult(500);
            }

            // Loop through the array of interest form submissions and create a new item in cosmos db for each.  Then forward the emails.  Log on failure
            var response = new InterestFormsResponse();
            foreach (var interestFormSubmission in interestFormSubmissions)
            {
                try
                {
                    // Interest form submission is added to the database before the email is sent.  This is because the email sender is a more fragile part of the system.
                    await emailsContainer.CreateItemAsync<InterestFormSubmission>(interestFormSubmission);
                    // If country is usa or canada, get the districts and forward the emails.  If country is not usa or canada, get the country and forward the emails.
                    if (interestFormSubmission.CountryOfResidence == "usa" || interestFormSubmission.CountryOfResidence == "canada")
                    {
                        // get district(s) by zip code
                        var zipCodeService = new ZipCodeService(emailsContainer);
                        var districts = await zipCodeService.GetDistrictsByZipCode(interestFormSubmission.Zipcode);
                        // if districts exist, get the emails of the district and send an email.
                        if (districts.Any())
                        {
                            // get most recent contacts for district
                            var emails = new List<string>();
                            foreach (var district in districts)
                            {
                                var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.District = @District and c.Type='ContactsForDistrict' ORDER BY c._ts desc").WithParameter("@District", district);
                                var iterator = await emailsContainer.GetItemQueryIterator<ContactsForDistrict>(query).ReadNextAsync();
                                foreach (ContactsForDistrict contactsForDistrict in iterator)
                                {
                                    emails.AddRange(contactsForDistrict.EmailAddresses);
                                }
                            }
                            var errors = await emailSender.SendDistrictEmailAsync(emails, districts, interestFormSubmission);
                            if (errors.Any())
                            {
                                interestFormSubmission.AddErrors(errors);
                            }
                        }
                        else
                        {
                            interestFormSubmission.AddError("District not found by zip code");
                        }

                    }
                    else
                    {
                        // Get most recent contacts for country
                        var emails = new List<string>();
                        var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.Country = @Country and c.Type='ContactsForCountry' ORDER BY c._ts desc").WithParameter("@Country", interestFormSubmission.CountryOfResidence);
                        var iterator = await emailsContainer.GetItemQueryIterator<ContactsForCountry>(query).ReadNextAsync();
                        if (iterator.Any())
                        {
                            foreach (ContactsForCountry contactsForCountry in iterator)
                            {
                                emails.AddRange(contactsForCountry.EmailAddresses);
                            }
                            var errors = await emailSender.SendCountryEmailAsync(emails, interestFormSubmission);
                            if (errors.Any())
                            {
                                interestFormSubmission.AddErrors(errors);
                            }
                        }
                        else
                        {
                            interestFormSubmission.AddError("country not found");
                        }

                    }
                    if (interestFormSubmission.Errors.Any())
                    {
                        response.CountError++;
                        response.ErrorSubmissions.Add(new ErrorSubmission(interestFormSubmission.id, interestFormSubmission.Errors));
                        // Log error and update database interestFormSubmission with errors
                        _loggingService.LogError(interestFormSubmission.Errors, interestFormSubmission.id);
                        await emailsContainer.UpsertItemAsync<InterestFormSubmission>(interestFormSubmission);
                    }
                    else
                    {
                        response.CountSuccess++;
                    }
                }
                catch (Exception e)
                {
                    // Handle error with initial database connection, or uncaught exception.  Log error and send email.
                    response.CountError++;
                    interestFormSubmission.AddError(e.Message);
                    _loggingService.LogException(e, interestFormSubmission.ToString());
                    // try to handle sending email with errors 
                    try
                    {
                        await emailSender.SendEmailAsync(new List<string> { sendingEmailAddress }, "Failure to send to database or process submission", $@"error message: {e.Message}.  Submission: {interestFormSubmission}");
                    }
                    catch (Exception e2)
                    {
                        interestFormSubmission.AddError("failed to send email to notify of failure do to following exception:" + e2.Message);
                        _loggingService.LogException(e2, interestFormSubmission.ToString());
                    }
                    response.ErrorSubmissions.Add(new ErrorSubmission(interestFormSubmission.id, interestFormSubmission.Errors));
                }
            }
            return new OkObjectResult(response);
        }
    }
}
