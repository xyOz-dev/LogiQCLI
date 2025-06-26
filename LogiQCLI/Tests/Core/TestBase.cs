using System;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core.Objects;

namespace LogiQCLI.Tests.Core
{
    public abstract class TestBase
    {
        public abstract string TestName { get; }
        
        public virtual string Category
        {
            get
            {
                var namespaceParts = GetType().Namespace?.Split('.') ?? new string[0];
                if (namespaceParts.Length >= 3)
                {
                    return namespaceParts[2];
                }
                return "General";
            }
        }

        public virtual int Priority { get; } = 100;

        public abstract Task<TestResult> ExecuteAsync();

        protected TestResult CreateSuccessResult(TimeSpan executionTime)
        {
            return TestResult.CreateSuccess(TestName, executionTime);
        }

        protected TestResult CreateFailureResult(string errorMessage, TimeSpan executionTime)
        {
            return TestResult.CreateFailure(TestName, errorMessage, executionTime);
        }

        protected TestResult CreateFailureResult(Exception exception, TimeSpan executionTime)
        {
            return TestResult.CreateFailure(TestName, exception.Message, executionTime);
        }
    }
}
