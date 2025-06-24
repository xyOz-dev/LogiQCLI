using System;
using System.Collections.Generic;

namespace LogiQCLI.Tests.Core.Objects
{
    public class TestResult
    {
        public bool Success { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public static TestResult CreateSuccess(string testName, TimeSpan executionTime)
        {
            return new TestResult
            {
                Success = true,
                TestName = testName,
                ExecutionTime = executionTime,
                ErrorMessage = string.Empty
            };
        }

        public static TestResult CreateFailure(string testName, string errorMessage, TimeSpan executionTime)
        {
            return new TestResult
            {
                Success = false,
                TestName = testName,
                ErrorMessage = errorMessage,
                ExecutionTime = executionTime
            };
        }
    }
}