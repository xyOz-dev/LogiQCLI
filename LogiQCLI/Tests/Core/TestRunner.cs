using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.Core.Interfaces;
using Spectre.Console;

namespace LogiQCLI.Tests.Core
{
    public class TestRunner
    {
        private readonly TestService _testService;
        private readonly TestDiscoveryService _testDiscoveryService;
        private readonly IServiceContainer _serviceContainer;

        public TestRunner(IServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer;
            _testService = new TestService(serviceContainer);
            _testDiscoveryService = new TestDiscoveryService();
        }

        public async Task<bool> RunAllTestsAsync()
        {
            var headerRule = new Rule("[bold blue]LogiQCLI Test Suite[/]")
            {
                Style = Style.Parse("blue"),
                Justification = Justify.Center
            };
            AnsiConsole.Write(headerRule);
            AnsiConsole.WriteLine();

            await DiscoverAndRegisterTestsAsync();

            var allTests = _testService.GetAllTests();
            if (!allTests.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No tests discovered[/]");
                return false;
            }

            AnsiConsole.MarkupLine($"[cyan]Running {allTests.Count} tests...[/]");
            AnsiConsole.WriteLine();

            var overallStopwatch = Stopwatch.StartNew();
            var summary = await _testService.RunAllTestsAsync();
            overallStopwatch.Stop();

            DisplayTestResults(summary, overallStopwatch.Elapsed);

            return summary.AllTestsPassed;
        }

        public async Task<bool> RunTestsByCategoryAsync(string category)
        {
            AnsiConsole.MarkupLine($"[cyan]Running tests in category: {category}[/]");
            AnsiConsole.WriteLine();

            await DiscoverAndRegisterTestsAsync();

            var testsInCategory = _testService.GetTestsByCategory(category);
            if (!testsInCategory.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No tests found in category '{category}'[/]");
                return false;
            }

            var summary = await _testService.RunTestsAsync(testsInCategory);
            DisplayTestResults(summary, summary.TotalExecutionTime);

            return summary.AllTestsPassed;
        }

        public async Task<bool> RunSpecificTestAsync(string testName)
        {
            AnsiConsole.MarkupLine($"[cyan]Running specific test: {testName}[/]");
            AnsiConsole.WriteLine();

            await DiscoverAndRegisterTestsAsync();

            var test = _testService.GetTest(testName);
            if (test == null)
            {
                AnsiConsole.MarkupLine($"[red]Test '{testName}' not found[/]");
                return false;
            }

            var summary = await _testService.RunTestsAsync(new[] { test });
            DisplayTestResults(summary, summary.TotalExecutionTime);

            return summary.AllTestsPassed;
        }

        private async Task DiscoverAndRegisterTestsAsync()
        {
            await Task.CompletedTask;
            try
            {
                var currentAssembly = Assembly.GetExecutingAssembly();
                var discoveredTests = _testDiscoveryService.DiscoverTests(currentAssembly);

                AnsiConsole.MarkupLine($"[dim]Discovered {discoveredTests.Count} tests[/]");

                foreach (var test in discoveredTests)
                {
                    _testService.RegisterTest(test);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Test discovery failed: {ex.Message}[/]");
                throw;
            }
        }

        private void DisplayTestResults(TestSummary summary, TimeSpan totalTime)
        {
            AnsiConsole.WriteLine();

            var resultsTable = new Table()
                .BorderColor(Color.Grey)
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Test Name[/]")
                .AddColumn("[bold]Status[/]")
                .AddColumn("[bold]Duration[/]")
                .AddColumn("[bold]Details[/]");

            foreach (var result in summary.TestResults.OrderBy(r => r.TestName))
            {
                var statusColor = result.Success ? "green" : "red";
                var statusText = result.Success ? "✓ PASS" : "✗ FAIL";
                var duration = $"{result.ExecutionTime.TotalMilliseconds:F0}ms";
                var details = result.Success ? "Passed" : result.ErrorMessage;

                if (details.Length > 80)
                {
                    details = details.Substring(0, 77) + "...";
                }

                resultsTable.AddRow(
                    result.TestName,
                    $"[{statusColor}]{statusText}[/]",
                    $"[dim]{duration}[/]",
                    $"[dim]{details}[/]"
                );
            }

            AnsiConsole.Write(resultsTable);
            AnsiConsole.WriteLine();

            var summaryPanel = new Panel(
                $"[bold]Test Summary[/]\n\n" +
                $"Total Tests: {summary.TotalTests}\n" +
                $"[green]Passed: {summary.PassedTests}[/]\n" +
                $"[red]Failed: {summary.FailedTests}[/]\n" +
                $"Total Duration: {totalTime.TotalSeconds:F2}s\n" +
                $"Success Rate: {summary.SuccessRate:P1}"
            )
            {
                Border = BoxBorder.Rounded,
                BorderStyle = summary.AllTestsPassed ? Style.Parse("green") : Style.Parse("red"),
                Header = new PanelHeader(summary.AllTestsPassed ? "[green]All Tests Passed ✓[/]" : "[red]Some Tests Failed ✗[/]")
            };

            AnsiConsole.Write(summaryPanel);

            if (!summary.AllTestsPassed)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[red bold]Failed Tests Details:[/]");
                
                foreach (var failedTest in summary.TestResults.Where(r => !r.Success))
                {
                    var errorPanel = new Panel($"[red]{failedTest.ErrorMessage}[/]")
                    {
                        Header = new PanelHeader($"[red]{failedTest.TestName}[/]"),
                        Border = BoxBorder.Rounded,
                        BorderStyle = Style.Parse("red")
                    };
                    AnsiConsole.Write(errorPanel);
                    AnsiConsole.WriteLine();
                }
            }
        }
    }
}
