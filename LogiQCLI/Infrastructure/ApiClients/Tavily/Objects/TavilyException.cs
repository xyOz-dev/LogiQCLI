using System;

namespace LogiQCLI.Infrastructure.ApiClients.Tavily.Objects
{
    public class TavilyException : Exception
    {
        public TavilyException(string message) : base(message)
        {
        }

        public TavilyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
} 