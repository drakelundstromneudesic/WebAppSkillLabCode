using System.Collections.Generic;

namespace Company.Models
{
    public class InterestFormsResponse
    {
        public InterestFormsResponse()
        {
            CountSuccess = 0;
            CountError = 0;
            ErrorSubmissions = new List<ErrorSubmission>();
        }
        public int CountSuccess { get; set; }
        public int CountError { get; set; }
        public List<ErrorSubmission> ErrorSubmissions { get; set; }
    }

    public class ErrorSubmission
    {
        public string Id { get; }
        public List<string> Errors { get; }
        public ErrorSubmission(string id, List<string> errors)
        {
            this.Id = id;
            this.Errors = errors;
        }
    }
}
