using System.Collections.Generic;

namespace Company.Models
{
    public class InterestFormsResponse
    {
        public int CountSuccess { get; set; }
        public int CountError { get; set; }
        public List<ErrorSubmission> ErrorSubmissions { get; set; }
    }

    public class ErrorSubmission
    {
        public string id { get; set; }
        public List<string> errors { get; set; }
        public ErrorSubmission(string id, List<string> errors)
        {
            this.id = id;
            this.errors = errors;
        }
    }
}
