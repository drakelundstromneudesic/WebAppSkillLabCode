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
using System.Web.Http;

namespace Company.Function
{
    public static class CreateInterestFormEntry
    {
        [FunctionName("CreateInterestFormEntry")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "interest-form-entry")] HttpRequest req,
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
            var interestFormSubmission = new InterestFormSubmission();
            try
            {
                var reqSubmission = JsonSerializer.Deserialize<InterestFormSubmission>(requestBody);
                interestFormSubmission = new InterestFormSubmission(reqSubmission.IsInterestedOutboundStudent, reqSubmission.IsInterestedInHosting, reqSubmission.SubmissionQuestion, reqSubmission.Name, reqSubmission.Age, reqSubmission.Gender, reqSubmission.Email, reqSubmission.Phone, reqSubmission.CountryOfResidence, reqSubmission.State, reqSubmission.City, reqSubmission.Zipcode, reqSubmission.CountryChoiceOne, reqSubmission.CountryChoiceTwo, reqSubmission.CountryChoiceThree, reqSubmission.CountryChoiceFour);
            }
            catch (Exception e)
            {
                _loggingService.LogException(e, requestBody);
                await emailSender.SendEmailAsync(new List<string> { sendingEmailAddress }, "Failure to process submission or send email", $@"error message: {e.Message}.  Submission: {requestBody}");
                return new StatusCodeResult(500);
            }

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
                        var errors = await emailSender.SendCountryOrDistrictNotFoundEmailAsync(interestFormSubmission);
                        if (errors.Any())
                        {
                            interestFormSubmission.AddErrors(errors);
                        }
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
                            var errors = new List<string>();
                            if (contactsForCountry.IsCertified)
                            {
                                emails.AddRange(contactsForCountry.EmailAddresses);
                                errors.AddRange(await emailSender.SendCountryEmailAsync(emails, interestFormSubmission));
                            }
                            else
                            {
                                errors.AddRange(await emailSender.SendDistrictNotCertifiedEmailAsync(interestFormSubmission));
                            }
                            if (errors.Any())
                            {
                                interestFormSubmission.AddErrors(errors);
                            }
                        }
                    }
                    else
                    {
                        interestFormSubmission.AddError("country not found");
                        var errors = await emailSender.SendCountryOrDistrictNotFoundEmailAsync(interestFormSubmission);
                        if (errors.Any())
                        {
                            interestFormSubmission.AddErrors(errors);
                        }
                    }

                }
                if (interestFormSubmission.Errors.Any())
                {
                    // Log error and update database interestFormSubmission with errors
                    _loggingService.LogError(interestFormSubmission.Errors, interestFormSubmission.id);
                    await emailsContainer.UpsertItemAsync<InterestFormSubmission>(interestFormSubmission);
                    return new BadRequestErrorMessageResult(@$"Error during submission.  Submission ID: {interestFormSubmission.id}.  Error message: {interestFormSubmission.Errors}");
                }
                else
                {
                    return new OkObjectResult(interestFormSubmission);
                }
            }
            catch (Exception e)
            {
                // Handle error with initial database connection, or uncaught exception.  Log error and send email.
                interestFormSubmission.AddError(e.Message);
                _loggingService.LogException(e, JsonSerializer.Serialize(interestFormSubmission));
                // try to handle sending email with errors 
                try
                {
                    await emailSender.SendEmailAsync(new List<string> { sendingEmailAddress }, "Failure to send to database or process submission", $@"error message: {e.Message}.  Submission: {interestFormSubmission}");
                }
                catch (Exception e2)
                {
                    interestFormSubmission.AddError("failed to send email to notify of failure do to following exception:" + e2.Message);
                    _loggingService.LogException(e2, JsonSerializer.Serialize(interestFormSubmission));
                }
                return new BadRequestErrorMessageResult(@$"Error during submission.  Submission ID: {interestFormSubmission.id}.  Error message: {interestFormSubmission.Errors}");
            }
        }
    }
}
