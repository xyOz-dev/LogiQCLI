using System;
using System.Collections.Generic;
using System.Linq;

namespace LogiQCLI.Tests.Core.Objects
{
    public class TestSummary
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public TimeSpan TotalExecutionTime { get; set; }
        public List<TestResult> TestResults { get; set; } = new List<TestResult>();

        public double PassRate => TotalTests > 0 ? (double)PassedTests / TotalTests : 0.0;
        public double SuccessRate => PassRate;
        public bool AllTestsPassed => FailedTests == 0 && TotalTests > 0;

        public static TestSummary CreateFromResults(List<TestResult> results)
        {
            var summary = new TestSummary
            {
                TestResults = results,
                TotalTests = results.Count,
                PassedTests = results.Count(r => r.Success),
                FailedTests = results.Count(r => !r.Success),
                TotalExecutionTime = TimeSpan.FromTicks(results.Sum(r => r.ExecutionTime.Ticks))
            };

            return summary;
        }

        public List<TestResult> GetFailedTests()
        {
            return TestResults.Where(r => !r.Success).ToList();
        }

        public List<TestResult> GetPassedTests()
        {
            return TestResults.Where(r => r.Success).ToList();
        }

        public Dictionary<string, List<TestResult>> GetResultsByCategory()
        {
            return TestResults
                .GroupBy(r => r.Metadata.GetValueOrDefault("Category", "General").ToString())
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}
