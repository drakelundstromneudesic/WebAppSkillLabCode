using System;
using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;
using System.Collections.Generic;
using Company.Models;
using System.Globalization;
using Org.BouncyCastle.Crypto.Engines;

namespace Company.Services
{
    public class EmailSender
    {
        private readonly string _sendingAccountAddress;
        private readonly string _sendingAccountPassword;
        private readonly string subjectLine = "Rotary Youth Exchange Form Submission";
        private LoggingService _loggingService { get; set; }

        public EmailSender(string sendingAccountAddress, string sendingAccountPassword, LoggingService loggingService)
        {
            _sendingAccountAddress = sendingAccountAddress;
            _sendingAccountPassword = sendingAccountPassword;
            _loggingService = loggingService;
        }

        // This method generates an email and sends it to the district representative(s).  It return an empty list on success and errors on failure
        public async Task<List<string>> SendDistrictEmailAsync(List<string> districtEmailAddresses, List<string> districts, InterestFormSubmission interestFormSubmission)
        {
            try
            {
                // generate and send email to district representatives
                var districtEmailBody = DistrictEmailBodyGenerator(districts, interestFormSubmission);
                var errors = await SendEmailAsync(districtEmailAddresses, subjectLine, districtEmailBody);
                // generate and send email to form submitter
                var returnEmailBody = ReturnEmailBodyGenerator(false, districts, interestFormSubmission);
                errors.AddRange(await SendEmailAsync(new List<string>() { interestFormSubmission.Email }, subjectLine, returnEmailBody));
                return errors;
            }
            catch (Exception e)
            {
                return new List<string> { e.Message };
            }
        }

        // This method generates an email and sends it to the country representative(s).  It return an empty list on success and errors on failure
        public async Task<List<string>> SendCountryEmailAsync(List<string> countryEmailAddresses, InterestFormSubmission interestFormSubmission)
        {
            try
            {
                // generate and send email to country representatives
                var countryEmailBody = CountryEmailBodyGenerator(interestFormSubmission);
                var errors = await SendEmailAsync(countryEmailAddresses, subjectLine, countryEmailBody);
                // generate and send email to form submitter
                var returnEmailBody = ReturnEmailBodyGenerator(true, new List<string>(), interestFormSubmission);
                errors.AddRange(await SendEmailAsync(new List<string>() { interestFormSubmission.Email }, subjectLine, returnEmailBody));
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
                    newBody += $@"Representatives,</h4>
        <p>We are not sure what districts this student is a part of, so this email is going to all districts present in this zip code.</p>";
                }
            }
            newBody += StudentInformationToHtml(interestFormSubmission);
            newBody += @"<p> They have also been informed of your district number and been told to expect a follow up within a couple of weeks.</p>
        <h4> If you have any questions, advice for the process, to add or remove email addresses for your district, or to get a list of previous submissions, please reach out to StudyAbroadScholarshipsWebsite@outlook.com.</h4>
        ";
            newBody += " <p>Thank you for your support of <a href=\"https://studyabroadscholarships.org/\">studyabroadscholarships.org</a>!</p>";
            return newBody;
        }

        // This method generates the body of the email for the country representative(s).  It returns the body of the email as a string
        private string CountryEmailBodyGenerator(InterestFormSubmission interestFormSubmission)
        {
            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            var newBody = $@"<h4>Hello RYE {ti.ToTitleCase(interestFormSubmission.CountryOfResidence)} Representatives,</h4>";
            newBody += StudentInformationToHtml(interestFormSubmission);
            newBody += @"<p> They have also been informed that their submission was forwarded and been told to expect a follow up within a couple of weeks.</p>
        <h4> If you have any questions, advice for the process, to add or remove email addresses for your country, or to get a list of previous submissions, please reach out to StudyAbroadScholarshipsWebsite@outlook.com.</h4>
        ";
            newBody += " <p>Thank you for your support of <a href=\"https://studyabroadscholarships.org/\">studyabroadscholarships.org</a>!</p>";
            return newBody;
        }

        // This method generates the body of a return email for the person who submitted the form.  It returns the body of the email as a string
        private string ReturnEmailBodyGenerator(bool isDistrict, List<string> Districts, InterestFormSubmission interestFormSubmission)
        {
            var responder = "";
            if (isDistrict)
            {
                if (Districts.Count == 1)
                {
                    responder = "Rotary District " + Districts[0];
                }
                else
                {
                    responder = "Rotary Districts " + string.Join(", ", Districts);
                }
            }
            else
            {
                TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
                responder = ti.ToTitleCase(interestFormSubmission.CountryOfResidence);
            }
            var newBody = $@"<h4>Hello {interestFormSubmission.Name},</h4>
            <div>Thank you for your interest in StudyAbroadScholarships.org.  A representative from rotary youth exchange in {responder} should follow up with you within 2 weeks.</div>
            <div>There is a lot of detail on the website to answer any questions that you may have.  And if you do not hear back from a rotarian within 2 weeks, please reach out to StudyAbroadScholarshipsWebsite@outlook.com.</div>
            <p>We look forward to hearing from you!</p>
            ";
            return newBody;
        }

        // This method converts the submission information into a string
        private string StudentInformationToHtml(InterestFormSubmission interestFormSubmission)
        {
            var goingInterest = interestFormSubmission.IsInterestedOutboundStudent ? "yes" : "no";
            var hostingInterest = interestFormSubmission.IsInterestedInHosting ? "yes" : "no";
            return $@"<h3>Here is the information from the form submission:</h3>
        <div><b>Name:</b> {interestFormSubmission.Name}</div>
        <div><b>Interested in going on exchange:</b> {goingInterest}</div>
        <div><b>Interested in hosting:</b> {hostingInterest}</div>
        <div><b>Age:</b> {interestFormSubmission.Age}</div>
        <div><b>Gender:</b> {interestFormSubmission.Gender}</div>
        <div><b>Email:</b> {interestFormSubmission.Email}</div>
        <div><b>Phone:</b> {interestFormSubmission.Phone}</div>
        <div><b>CountryOfResidence:</b> {interestFormSubmission.CountryOfResidence}</div>
        <div><b>State:</b> {interestFormSubmission.State}</div>
        <div><b>City:</b> {interestFormSubmission.City}</div>
        <div><b>Zipcode:</b> {interestFormSubmission.Zipcode}</div>
        <div><b>CountryChoiceOne:</b> {interestFormSubmission.CountryChoiceOne}</div>
        <div><b>CountryChoiceTwo:</b> {interestFormSubmission.CountryChoiceTwo}</div>
        <div><b>CountryChoiceThree:</b> {interestFormSubmission.CountryChoiceThree}</div>
        <div><b>CountryChoiceFour:</b> {interestFormSubmission.CountryChoiceFour}</div>";
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


