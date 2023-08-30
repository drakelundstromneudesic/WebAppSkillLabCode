using System.Collections.Generic;
using System;

namespace Company.Models
{
    public class ContactsForDistrict
    {
        public string id { get; set; }
        public string Type { get; set; }
        public string Country { get; set; }
        public string District { get; set; }
        public List<string> EmailAddresses { get; set; }
        public List<string> ZipCodes { get; set; }

        public ContactsForDistrict(string country, string district, List<string> emailAddresses, List<string> zipCodes)
        {
            id = Guid.NewGuid().ToString();
            this.Type = "ContactsForDistrict";
            this.Country = country;
            this.District = district;
            this.EmailAddresses = emailAddresses;
            this.ZipCodes = zipCodes;
        }
    }
}
