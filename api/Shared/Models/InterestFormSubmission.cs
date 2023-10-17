using System.Collections.Generic;
using System;

namespace Company.Models
{
    public class InterestFormSubmission
    {
        public string id { get; set; }
        public string Type { get; set; }
        public string IsInterestedOutboundStudent { get; set; }
        public string IsInterestedInHosting { get; set; }
        public string SubmissionQuestion { get; set; }
        public string Name { get; set; }
        public string Age { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string CountryOfResidence { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Zipcode { get; set; }
        public string CountryChoiceOne { get; set; }
        public string CountryChoiceTwo { get; set; }
        public string CountryChoiceThree { get; set; }
        public string CountryChoiceFour { get; set; }
        public List<string> Errors { get; set; }

        public void AddId()
        {
            id = Guid.NewGuid().ToString();
        }
        public void AddError(string errorMessage)
        {
            Errors.Add(errorMessage);
        }
        public void AddErrors(List<string> errorMessage)
        {
            Errors.AddRange(errorMessage);
        }
        public InterestFormSubmission()
        { }
        public InterestFormSubmission(string isInterestedOutboundStudent, string isInterestedInHosting, string submissionQuestion, string name, string age, string gender, string email, string phone, string countryOfResidence, string state, string city, string zipcode, string countryChoiceOne, string countryChoiceTwo, string countryChoiceThree, string countryChoiceFour)
        {
            this.Type = "InterestFormSubmission";
            this.IsInterestedOutboundStudent = isInterestedOutboundStudent.Trim().ToLower();
            this.IsInterestedInHosting = isInterestedInHosting.Trim().ToLower();
            this.SubmissionQuestion = submissionQuestion.Trim();
            this.Name = name.Trim();
            this.Age = age.Trim();
            this.Gender = gender.Trim();
            this.Email = email.Trim();
            this.Phone = phone.Trim();
            this.State = state.Trim();
            this.City = city.Trim();
            this.CountryChoiceOne = countryChoiceOne.Trim();
            this.CountryChoiceTwo = countryChoiceTwo.Trim();
            this.CountryChoiceThree = countryChoiceThree.Trim();
            this.CountryChoiceFour = countryChoiceFour.Trim();
            this.Errors = new List<string>();

            this.AddId();
            // This section does form validation for country names to amke sure they are consitent in the database for search purposes
            var countryName = countryOfResidence.Trim().ToLower().Replace("the ", string.Empty).Replace(".", string.Empty).Replace(" ", string.Empty);
            if (countryName.Equals("unitedstatesofamerica") || countryName.Equals("us") || countryName.Equals("unitedstates") || countryName.Equals("america"))
            {
                countryName = "usa";
            }
            else if (countryName.Equals("britian") || countryName.Equals("unitedkingdom") || countryName.Equals("england"))
            {
                countryName = "uk";
            }
            this.CountryOfResidence = countryName;
            // If the zip code is from the usa, get the first 5 digits of the zip code.  If the zip code is from the canada, get the first 3 digits of the zip code.
            var newZipCode = zipcode.Trim().ToUpper();
            if (this.CountryOfResidence.Equals("usa"))
            {
                this.Zipcode = newZipCode.Length > 5 ? newZipCode.Substring(0, 5) : newZipCode;
            }
            else if (this.CountryOfResidence.Equals("canada"))
            {
                this.Zipcode = newZipCode.Length > 3 ? newZipCode.Substring(0, 3) : newZipCode;

            }
            else
            {
                this.Zipcode = newZipCode;
            }

        }
    }
}