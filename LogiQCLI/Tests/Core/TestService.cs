using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;

namespace LogiQCLI.Tests.Core
{
    public class TestService
    {
        private readonly ConcurrentDictionary<string, TestBase> _tests = new ConcurrentDictionary<string, TestBase>(StringComparer.OrdinalIgnoreCase);
        private readonly IServiceContainer _serviceContainer;

        public TestService(IServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer;
        }

        public void RegisterTest(TestBase test)
        {
            if (test == null) throw new ArgumentNullException(nameof(test));
            
            _tests[test.TestName] = test;
        }

        public TestBase? GetTest(string name)
        {
            if (_tests.TryGetValue(name, out var test))
            {
                return test;
            }
            return null;
        }

        public List<TestBase> GetAllTests()
        {
            return _tests.Values.ToList();
        }

        public List<TestBase> GetTestsByCategory(string category)
        {
            return _tests.Values
                .Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public List<TestBase> QueryTests(Func<TestBase, bool> predicate)
        {
            return _tests.Values
                .Where(predicate)
                .ToList();
        }

        public bool IsTestRegistered(string name)
        {
            return _tests.ContainsKey(name);
        }

        public async Task<TestSummary> RunAllTestsAsync()
        {
            var tests = GetAllTests().OrderBy(t => t.Priority).ThenBy(t => t.TestName);
            return await RunTestsAsync(tests);
        }

        public async Task<TestSummary> RunTestsAsync(IEnumerable<TestBase> tests)
        {
            var results = new List<TestResult>();
            
            foreach (var test in tests)
            {
                var result = await ExecuteTestAsync(test);
                results.Add(result);
            }

            return TestSummary.CreateFromResults(results);
        }

        public async Task<TestSummary> RunTestsAsync(Func<TestBase, bool> filter)
        {
            var filteredTests = QueryTests(filter).OrderBy(t => t.Priority).ThenBy(t => t.TestName);
            return await RunTestsAsync(filteredTests);
        }

        private async Task<TestResult> ExecuteTestAsync(TestBase test)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var result = await test.ExecuteAsync();
                stopwatch.Stop();
                
                result.ExecutionTime = stopwatch.Elapsed;
                result.Metadata["Category"] = test.Category;
                result.Metadata["Priority"] = test.Priority;
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return TestResult.CreateFailure(test.TestName, ex.Message, stopwatch.Elapsed);
            }
        }
    }
}