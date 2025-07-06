using System;

namespace LogiQCLI.Infrastructure.ApiClients.OpenRouter.Objects
{
    public class OpenRouterException : Exception
    {
        public string ErrorCode { get; }
        
        public OpenRouterException(string errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
        
        public OpenRouterException(string errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    public class OpenRouterApiException : OpenRouterException
    {
        public int StatusCode { get; }
        public string ResponseBody { get; }
        
        public OpenRouterApiException(int statusCode, string responseBody, string message) 
            : base("API_ERROR", message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }

    public class OpenRouterRateLimitException : OpenRouterException
    {
        public TimeSpan? RetryAfter { get; }
        
        public OpenRouterRateLimitException(string message, TimeSpan? retryAfter = null) 
            : base("RATE_LIMIT", message)
        {
            RetryAfter = retryAfter;
        }
    }

    public class OpenRouterConfigurationException : OpenRouterException
    {
        public OpenRouterConfigurationException(string message) 
            : base("CONFIGURATION_ERROR", message)
        {
        }
    }
} 