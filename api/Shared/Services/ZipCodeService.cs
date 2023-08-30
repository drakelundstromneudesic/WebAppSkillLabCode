using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading.Tasks;
using Company.Models;

namespace Company.Services
{
    public class ZipCodeService
    {
        private Container _container { get; set; }
        public ZipCodeService(Container container)
        {
            _container = container;
        }

        // This method gets all the zip codes for a given district.  If the district is not found, it returns an empty list.
        public async Task<List<string>> GetDistrictsByZipCode(string zipCode)
        {
            var query = new QueryDefinition("SELECT c.District FROM c WHERE c.Type='ContactsForDistrict' and ARRAY_CONTAINS(c.ZipCodes, @ZipCode)  GROUP BY c.District")
            .WithParameter("@ZipCode", zipCode);
            var districts = new List<string>();
            var iterator = _container.GetItemQueryIterator<ContactsForDistrict>(query);
            while (iterator.HasMoreResults)
            {
                var result = await iterator.ReadNextAsync();
                foreach (ContactsForDistrict contractsForDistrict in result)
                {
                    districts.Add(contractsForDistrict.District);
                }
            }
            return districts;
        }
    }
}