using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LogiQCLI.Tests.Core;
using LogiQCLI.Tests.Core.Objects;
using LogiQCLI.Tools.Core;
using LogiQCLI.Tools.Core.Objects;

namespace LogiQCLI.Tests.Core
{
    public class BackupCommandsToolTests : TestBase
    {
        public override string TestName => "BackupCommandsTool_Tests";

        public override async Task<TestResult> ExecuteAsync()
        {
            var tool = new BackupCommandsTool();
            var testFileSystem = new TestFileSystem();

            var originalDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(testFileSystem.TempDirectory);

            try
            {
                await TestListNoBackups(tool);
                await TestBackupLifecycle(tool, testFileSystem);

                return CreateSuccessResult(TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                return CreateFailureResult(ex, TimeSpan.Zero);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
                testFileSystem.Dispose();
            }
        }

        private async Task TestListNoBackups(BackupCommandsTool tool)
        {
            var listArgs = new BackupCommandArguments
            {
                Action = "list"
            };

            var json = JsonSerializer.Serialize(listArgs);
            var result = await tool.Execute(json);

            if (!result.Contains("No backups found"))
            {
                throw new Exception($"Expected no backups message, got: {result}");
            }
        }

        private async Task TestBackupLifecycle(BackupCommandsTool tool, TestFileSystem fs)
        {
            var manager = new LogiqBackupManager(fs.TempDirectory);

            var originalContent = "Line 1\nLine 2\nLine 3";
            var testFilePath = fs.CreateTempFile(originalContent, "backup_test.txt");

            var backupId = await manager.CreateBackupAsync(testFilePath, originalContent, "UnitTest", "create", "Initial backup");

            await AssertListContainsBackup(tool, backupId, testFilePath);

            var modifiedContent = "Line 1 modified\nLine 2\nLine 3";
            File.WriteAllText(testFilePath, modifiedContent);

            await AssertDiffShowsChanges(tool, backupId);

            await AssertRestoreRevertsChanges(tool, backupId, testFilePath, originalContent);

            await AssertCleanupRemovesOldBackups(tool, fs.TempDirectory);

            await AssertStatusOutputs(tool);
        }

        private async Task AssertListContainsBackup(BackupCommandsTool tool, string backupId, string filePath)
        {
            var listArgs = new BackupCommandArguments
            {
                Action = "list"
            };

            var json = JsonSerializer.Serialize(listArgs);
            var result = await tool.Execute(json);

            if (!result.Contains(backupId))
            {
                throw new Exception("List action should include created backup ID");
            }

            if (!result.Contains(Path.GetFileName(filePath)))
            {
                throw new Exception("List action should include original file name");
            }
        }

        private async Task AssertDiffShowsChanges(BackupCommandsTool tool, string backupId)
        {
            var diffArgs = new BackupCommandArguments
            {
                Action = "diff",
                BackupId = backupId
            };

            var json = JsonSerializer.Serialize(diffArgs);
            var result = await tool.Execute(json);

            if (!result.Contains("Line 1:"))
            {
                throw new Exception("Diff output should show differing lines");
            }
        }

        private async Task AssertRestoreRevertsChanges(BackupCommandsTool tool, string backupId, string filePath, string expectedContent)
        {
            var restoreArgs = new BackupCommandArguments
            {
                Action = "restore",
                BackupId = backupId
            };

            var json = JsonSerializer.Serialize(restoreArgs);
            var result = await tool.Execute(json);

            if (!result.Contains("Successfully restored"))
            {
                throw new Exception($"Restore should succeed. Got: {result}");
            }

            var actualContent = await File.ReadAllTextAsync(filePath);
            if (actualContent != expectedContent)
            {
                throw new Exception("File content should match original after restore");
            }
        }

        private async Task AssertCleanupRemovesOldBackups(BackupCommandsTool tool, string workspacePath)
        {
            var managerPre = new LogiqBackupManager(workspacePath);
            var beforeCount = managerPre.ListBackups().Count;

            var cleanupArgs = new BackupCommandArguments
            {
                Action = "cleanup",
                RetentionDays = 0
            };

            var json = JsonSerializer.Serialize(cleanupArgs);
            await tool.Execute(json);

            var managerPost = new LogiqBackupManager(workspacePath);
            var afterCount = managerPost.ListBackups().Count;

            if (afterCount >= beforeCount)
            {
                throw new Exception("Cleanup should remove at least one backup");
            }
        }

        private async Task AssertStatusOutputs(BackupCommandsTool tool)
        {
            var statusArgs = new BackupCommandArguments
            {
                Action = "status"
            };

            var json = JsonSerializer.Serialize(statusArgs);
            var result = await tool.Execute(json);

            if (!result.Contains("Backup System Status"))
            {
                throw new Exception("Status output should include system status header");
            }

            if (!result.Contains("Total Backups:"))
            {
                throw new Exception("Status output should include backup count");
            }
        }
    }
} 