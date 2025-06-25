using System;
using System.IO;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.ContentManipulation;
using LogiQCLI.Tools.ContentManipulation.Objects;
using System.Text.Json;

namespace LogiQCLI.Tests.ContentManipulation
{
    public class ApplyDiffContentTests : TestBase
    {
        public override string TestName => "ApplyDiffTool_ContentTests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new ApplyDiffTool();
            var testFileSystem = new TestFileSystem();
            
            try
            {
                await TestStringReplacementBasic(tool, testFileSystem);
                await TestStringReplacementMultiple(tool, testFileSystem);
                await TestStringReplacementCaseSensitive(tool, testFileSystem);
                await TestStringReplacementUnicode(tool, testFileSystem);
                await TestStringReplacementEmpty(tool, testFileSystem);
                await TestStringReplacementNoMatches(tool, testFileSystem);
                await TestRegexReplacementBasic(tool, testFileSystem);
                await TestRegexReplacementCaptureGroups(tool, testFileSystem);
                await TestRegexReplacementMultiline(tool, testFileSystem);
                await TestMaxReplacementsLimit(tool, testFileSystem);
                await TestEncodingSupport(tool, testFileSystem);
                await TestJsonAndRawOutputModes(tool, testFileSystem);
                await TestPreviewMode(tool, testFileSystem);
                return CreateSuccessResult(TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, TimeSpan.Zero);
            }
            finally
            {
                testFileSystem.Dispose();
            }
        }

        // Existing test methods ...
        // [Insert all previous methods here: TestStringReplacementBasic, etc.]

        private async Task TestEncodingSupport(ApplyDiffTool tool, TestFileSystem testFileSystem)
        {
            // Test UTF-16 encoding
            var utf16File = testFileSystem.CreateTempFile("Hello encoding!");
            File.WriteAllText(utf16File, "Hello encoding! \u0016TestUTF16\u0016", System.Text.Encoding.Unicode);

            var args = new ApplyDiffArguments
            {
                Path = utf16File,
                Original = "TestUTF16",
                Replacement = "Success",
                Encoding = "utf-16"
            };
            var json = JsonSerializer.Serialize(args);
            await tool.Execute(json);
            var modifiedContent = File.ReadAllText(utf16File, System.Text.Encoding.Unicode);
            if (!modifiedContent.Contains("Success"))
                throw new Exception("UTF-16 encoding support failed");

            // Test ASCII encoding
            var asciiFile = testFileSystem.CreateTempFile("ASCII test input: foo");
            File.WriteAllText(asciiFile, "ASCII test input: foo", System.Text.Encoding.ASCII);
            args = new ApplyDiffArguments
            {
                Path = asciiFile,
                Original = "foo",
                Replacement = "bar",
                Encoding = "ascii"
            };
            json = JsonSerializer.Serialize(args);
            await tool.Execute(json);
            modifiedContent = File.ReadAllText(asciiFile, System.Text.Encoding.ASCII);
            if (!modifiedContent.Contains("bar"))
                throw new Exception("ASCII encoding support failed");

            // Test invalid encoding exception
            args = new ApplyDiffArguments
            {
                Path = asciiFile,
                Original = "bar",
                Replacement = "baz",
                Encoding = "invalid-encoding-name"
            };
            json = JsonSerializer.Serialize(args);
            var result = await tool.Execute(json);
            if (!result.Contains("Invalid encoding"))
                throw new Exception("Invalid encoding error message missing");
        }

        private async Task TestJsonAndRawOutputModes(ApplyDiffTool tool, TestFileSystem testFileSystem)
        {
            var testFile = testFileSystem.CreateTempFile("Raw/JSON output test");
            // JSON output (default)
            var args = new ApplyDiffArguments
            {
                Path = testFile,
                Original = "output",
                Replacement = "RESULT",
                RawOutput = false
            };
            var result = await tool.Execute(JsonSerializer.Serialize(args));
            if (!result.TrimStart().StartsWith("{"))
                throw new Exception("Expected structured JSON output but did not get it.");
            // Raw output
            args.RawOutput = true;
            result = await tool.Execute(JsonSerializer.Serialize(args));
            if (result.TrimStart().StartsWith("{"))
                throw new Exception("Expected raw output but got JSON");
            if (!result.Contains("SUCCESS") && !result.Contains("FAILED"))
                throw new Exception("Raw output missing SUCCESS/FAILED message");
        }

        private async Task TestPreviewMode(ApplyDiffTool tool, TestFileSystem testFileSystem)
        {
            var testFile = testFileSystem.CreateTempFile("Line1\nLine2abc\nLine3\nLine4\nLine5abc\nLine6\n");
            var args = new ApplyDiffArguments
            {
                Path = testFile,
                Original = "abc",
                Replacement = "XYZ",
                Preview = true,
                PreviewLines = 1 // Should only show first preview group
            };
            var result = await tool.Execute(JsonSerializer.Serialize(args));
            if (!result.Contains("Preview mode") && !result.Contains("previewDiff"))
                throw new Exception("Preview mode did not return expected preview output");
            // Check that multiple previewLines reveals more groups
            args.PreviewLines = 2;
            result = await tool.Execute(JsonSerializer.Serialize(args));
            if (!result.Contains("Preview mode") || !result.Contains("Changes preview"))
                throw new Exception("PreviewLines in preview mode did not return detailed diff snippet");
        }
    }
}
