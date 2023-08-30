using System;


namespace Company.Models
{
    public class RequestBodyLog
    {
        public string id { get; set; }
        public string Type { get; set; }
        public string RequestBody { get; set; }

        public RequestBodyLog(string requestBody)
        {
            id = Guid.NewGuid().ToString();
            Type = "RequestBodyLog";
            RequestBody = requestBody;
        }
    }
}
