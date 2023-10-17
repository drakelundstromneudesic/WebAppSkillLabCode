using System;
using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;
using System.Collections.Generic;
using Company.Models;
using System.Globalization;
using System.Text.Json;

namespace Company.Services
{
    public class EmailSender
    {
        private readonly string _sendingAccountAddress;
        private readonly string _sendingAccountPassword;
        private readonly string forwardingSubjectLine = "Rotary Youth Exchange Form Submission From ";
        private readonly string submitterSubjectLine = "Study Abroad Scholarships with Rotary form submission ";

        private LoggingService _loggingService { get; set; }

        public EmailSender(string sendingAccountAddress, string sendingAccountPassword, LoggingService loggingService)
        {
            _sendingAccountAddress = sendingAccountAddress;
            _sendingAccountPassword = sendingAccountPassword;
            _loggingService = loggingService;
        }

        // This method generates an email and sends it to the district representative(s) and the form submitter.  It return an empty list on success and errors on failure
        public async Task<List<string>> SendDistrictEmailAsync(List<string> districtEmailAddresses, List<string> districts, InterestFormSubmission interestFormSubmission)
        {
            try
            {
                // generate and send email to district representatives
                var districtEmailBody = DistrictEmailBodyGenerator(districts, interestFormSubmission);
                var errors = await SendEmailAsync(districtEmailAddresses, forwardingSubjectLine + interestFormSubmission.Email, districtEmailBody);
                // generate and send email to form submitter
                var returnEmailBody = ReturnEmailBodyGenerator(true, true, districts, interestFormSubmission);
                errors.AddRange(await SendEmailAsync(new List<string>() { interestFormSubmission.Email }, submitterSubjectLine, returnEmailBody));
                return errors;
            }
            catch (Exception e)
            {
                return new List<string> { e.Message };
            }
        }

        // This method generates an email and sends it to the country representative(s) and the form submitter.  It return an empty list on success and errors on failure
        public async Task<List<string>> SendCountryEmailAsync(List<string> countryEmailAddresses, InterestFormSubmission interestFormSubmission)
        {
            try
            {
                // generate and send email to country representatives
                var countryEmailBody = CountryEmailBodyGenerator(interestFormSubmission);
                var errors = await SendEmailAsync(countryEmailAddresses, forwardingSubjectLine + interestFormSubmission.Email, countryEmailBody);
                // generate and send email to form submitter
                var returnEmailBody = ReturnEmailBodyGenerator(true, false, new List<string>(), interestFormSubmission);
                errors.AddRange(await SendEmailAsync(new List<string>() { interestFormSubmission.Email }, submitterSubjectLine, returnEmailBody));
                return errors;
            }
            catch (Exception e)
            {
                return new List<string> { e.Message };
            }
        }

        // This method generates an email and sends it to the the form submitter if their district is not certified for youth exchange.  It return an empty list on success and errors on failure
        public async Task<List<string>> SendDistrictNotCertifiedEmailAsync(InterestFormSubmission interestFormSubmission)
        {
            try
            {
                // generate and send email to form submitter
                var returnEmailBody = DistrictNotCertifiedEmailBodyGenerator(interestFormSubmission);
                var errors = await SendEmailAsync(new List<string>() { interestFormSubmission.Email }, submitterSubjectLine, returnEmailBody);
                return errors;
            }
            catch (Exception e)
            {
                return new List<string> { e.Message };
            }
        }

        // This method generates an email and sends it to the default email address and the form submitter.  It return an empty list on success and errors on failure. 
        public async Task<List<string>> SendCountryOrDistrictNotFoundEmailAsync(InterestFormSubmission interestFormSubmission)
        {
            try
            {
                // generate and send email to country representatives
                var notFoundEmailBody = $@"<h3>There was not a district or country found for this submission.</h3>
                <p>Please reach out to the student within a week.  If the database is missing needed information for a zip code or country, please update it.  If there was an error in filling out the form, please resumbit it correctly.</p>
                {StudentInformationToHtml(interestFormSubmission)}
                <h4>Here is the raw data from the submissions:</h4>
                <p>{JsonSerializer.Serialize(interestFormSubmission)}</p>
               ";
                var errors = await SendEmailAsync(new List<string>() { _sendingAccountAddress }, "District or Country Not Found", notFoundEmailBody);
                // generate and send email to form submitter
                var returnEmailBody = ReturnEmailBodyGenerator(false, false, new List<string>(), interestFormSubmission);
                errors.AddRange(await SendEmailAsync(new List<string>() { interestFormSubmission.Email }, submitterSubjectLine, returnEmailBody));
                return errors;
            }
            catch (Exception e)
            {
                return new List<string> { e.Message };
            }
        }

        // This method generates the body of the email for the district representative(s).  It returns the body of the email as a string
        private string DistrictEmailBodyGenerator(List<string> Districts, InterestFormSubmission interestFormSubmission)
        {
            var newBody = "";

            if (Districts.Count == 1)
            {
                newBody += $@"<h4>Hello RYE District {Districts[0]} Representatives,</h4>";
            }
            else
            {
                newBody += $@"<h4>Hello RYE District ";
                for (int i = 0; i < Districts.Count; i++)
                {
                    if (i == Districts.Count - 1)
                    {
                        newBody += $@"and {Districts[i]} ";
                    }
                    else
                    {
                        newBody += $@"{Districts[i]}, ";
                    }
                }
                newBody += $@"Representatives,</h4>
                    <p>We are not sure what districts this student is a part of, so this email is going to all districts present in this zip code.</p>";
            }
            newBody += "<p>An interested person in your district has submitted a Rotary Youth Exchange contact form at <a href=\"https://studyabroadscholarships.org/\">studyabroadscholarships.org</a>. They have been informed of your district number and been told to expect a follow up within a couple of weeks.</p>";
            newBody += StudentInformationToHtml(interestFormSubmission);
            newBody += @"<h4> If you have any questions, advice for the process, to add or remove email addresses for your district, or to get a list of previous submissions, please reach out to StudyAbroadScholarshipsWebsite@outlook.com.</h4>
        ";
            newBody += " <p>Thank you for your support of <a href=\"https://studyabroadscholarships.org/\">studyabroadscholarships.org</a>!</p>";
            return newBody;
        }

        // This method generates the body of the email for the country representative(s).  It returns the body of the email as a string
        private string CountryEmailBodyGenerator(InterestFormSubmission interestFormSubmission)
        {
            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            var newBody = $@"<h4>Hello RYE {ti.ToTitleCase(interestFormSubmission.CountryOfResidence)} Representatives,</h4>";
            newBody += "<p>An interested person in your country has submitted a Rotary Youth Exchange contact form at <a href=\"https://studyabroadscholarships.org/\">studyabroadscholarships.org</a>. They have been told to expect a follow up within a couple of weeks.</p>";
            newBody += StudentInformationToHtml(interestFormSubmission);
            newBody += @"<h4> If you have any questions, advice for the process, to add or remove email addresses for your country, or to get a list of previous submissions, please reach out to StudyAbroadScholarshipsWebsite@outlook.com.</h4>
        ";
            newBody += " <p>Thank you for your support of <a href=\"https://studyabroadscholarships.org/\">studyabroadscholarships.org</a>!</p>";
            return newBody;
        }

        // This method generates the body of a return email for the person who submitted the form.  It returns the body of the email as a string
        private string ReturnEmailBodyGenerator(bool isDistrictFound, bool isDistrict, List<string> Districts, InterestFormSubmission interestFormSubmission)
        {
            var responder = "";
            if (isDistrictFound)
            {
                if (isDistrict)
                {
                    if (Districts.Count == 1)
                    {
                        responder = " from Rotary District " + Districts[0];
                    }
                    else
                    {
                        responder = " from Rotary Districts " + string.Join(", ", Districts);
                    }
                }
                else
                {
                    TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
                    responder = " from " + ti.ToTitleCase(interestFormSubmission.CountryOfResidence);
                }
            }
            var newBody = $@"<h4>Hello {interestFormSubmission.Name},</h4>
            <div>Thank you for your interest in StudyAbroadScholarships.org.  A representative from Study Abroad Scholarships (aka Rotary Youth Exchange){responder} will follow up with you within 2 weeks.</div>
            <div>If you do not hear back from a Rotarian within 2 weeks, please reach out to StudyAbroadScholarshipsWebsite@outlook.com.</div>
            <div>There is a lot of detail on the website to answer many questions that you may have.</div>
            {StudentInformationToHtml(interestFormSubmission)}
            <p>We look forward to hearing from you!</p>
            ";
            return newBody;
        }

        // This method generates the body of a return email for the person who submitted the form if the district is found but not certified.  It returns the body of the email as a string
        private string DistrictNotCertifiedEmailBodyGenerator(InterestFormSubmission interestFormSubmission)
        {
            var newBody = $@"<h4>Hello {interestFormSubmission.Name},</h4>
            <div>Thank you for your interest in StudyAbroadScholarships.org.  Unfortunately, your Rotary district is not certified for youth exchange, so you are not eligible for this scholarship.</div>
            <div>Please reach out to StudyAbroadScholarshipsWebsite@outlook.com if you have any questions or if you believe that our system made a mistake.</div>
            {StudentInformationToHtml(interestFormSubmission)}
            <p>Thank you for your time.</p>
            ";
            return newBody;
        }

        // This method converts the submission information into a string
        private string StudentInformationToHtml(InterestFormSubmission interestFormSubmission)
        {
            return $@"<h3>Here is the information from the form submission:</h3>
                <div><b>Name:</b> {interestFormSubmission.Name}</div>
                <div><b>Question:</b> {interestFormSubmission.SubmissionQuestion}</div>
                <div><b>Interested in going on exchange:</b> {interestFormSubmission.IsInterestedOutboundStudent}</div>
                <div><b>Interested in hosting:</b> {interestFormSubmission.IsInterestedInHosting}</div>
                <div><b>Age:</b> {interestFormSubmission.Age}</div>
                <div><b>Gender:</b> {interestFormSubmission.Gender}</div>
                <div><b>Email:</b> {interestFormSubmission.Email}</div>
                <div><b>Phone:</b> {interestFormSubmission.Phone}</div>
                <div><b>Country of residence:</b> {interestFormSubmission.CountryOfResidence.ToUpper()}</div>
                <div><b>State:</b> {interestFormSubmission.State}</div>
                <div><b>City:</b> {interestFormSubmission.City}</div>
                <div><b>Zipcode:</b> {interestFormSubmission.Zipcode}</div>
                <div><b>Country choice one:</b> {interestFormSubmission.CountryChoiceOne}</div>
                <div><b>Country choice two:</b> {interestFormSubmission.CountryChoiceTwo}</div>
                <div><b>Country choice three:</b> {interestFormSubmission.CountryChoiceThree}</div>
                <div><b>Country choice four:</b> {interestFormSubmission.CountryChoiceFour}</div>";
        }




        // This method sends an email and returns an empty list on success and an error message on failure
        public async Task<List<string>> SendEmailAsync(List<string> emailRecipients, string subject, string body)
        {
            try
            {
                var emailToSend = new MimeMessage();
                emailToSend.From.Add(MailboxAddress.Parse(_sendingAccountAddress));
                for (int i = 0; i < emailRecipients.Count; i++)
                {
                    emailToSend.To.Add(MailboxAddress.Parse(emailRecipients[i]));
                }
                emailToSend.Subject = subject;
                emailToSend.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = body
                };

                using (var emailClient = new SmtpClient())
                {
                    emailClient.Connect("smtp-mail.outlook.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    emailClient.Authenticate(_sendingAccountAddress, _sendingAccountPassword);
                    emailClient.Send(emailToSend);
                    emailClient.Disconnect(true);
                }
                return new List<string>();
            }
            catch (Exception e)
            {
                _loggingService.LogException(e, body);
                return new List<string>() { e.Message };
            }
        }
    }
}


