using System.Collections.Generic;
using System;

namespace Company.Models
{
    public class ContactsForCountry
    {
        public string id { get; set; }
        public string Type { get; set; }
        public string Country { get; set; }
        public List<string> EmailAddresses { get; set; }

        public ContactsForCountry(string country, List<string> emailAddresses)
        {
            this.id = Guid.NewGuid().ToString();
            this.Type = "ContactsForCountry";
            this.Country = country;
            this.EmailAddresses = emailAddresses;
        }
    }
}
