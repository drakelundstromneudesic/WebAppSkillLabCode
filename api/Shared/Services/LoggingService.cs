using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Company.Services
{
    public class LoggingService
    {
        private ILogger log { get; set; }
        public LoggingService(ILogger Log)
        {
            log = Log;
        }
        public void LogException(Exception e, string requestBody)
        {
            log.LogError($"expection message: {e.Message}.  Submission: {requestBody}");
            log.LogError(e.StackTrace);
            log.LogError(e.InnerException.StackTrace);
            log.LogError(e.InnerException.Message);
            log.LogError(e.InnerException.InnerException.StackTrace);
            log.LogError(e.InnerException.InnerException.Message);
            log.LogError(e.InnerException.InnerException.InnerException.StackTrace);
            log.LogError(e.InnerException.InnerException.InnerException.Message);
            log.LogError(e.InnerException.InnerException.InnerException.InnerException.StackTrace);
            log.LogError(e.InnerException.InnerException.InnerException.InnerException.Message);
            log.LogError(e.InnerException.InnerException.InnerException.InnerException.InnerException.StackTrace);
            log.LogError(e.InnerException.InnerException.InnerException.InnerException.InnerException.Message);
        }
        public void LogError(List<string> errors, string requestBody)
        {
            log.LogError($"expection message: {errors}.  Submission: {requestBody}");
        }
    }
}