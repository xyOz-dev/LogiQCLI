using System;

namespace LogiQCLI.Infrastructure.ApiClients.GitHub.Objects
{
    public class GitHubClientException : Exception
    {
        public GitHubClientException(string message) : base(message) { }
        public GitHubClientException(string message, Exception innerException) : base(message, innerException) { }
    }
}
