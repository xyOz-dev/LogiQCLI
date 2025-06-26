using System.Collections.Generic;

namespace LogiQCLI.Presentation.Console.Components.Objects
{
    internal static class ToolDescriptions
    {
        public static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
        {
            { "read_file", "Read complete file contents as text" },
            { "read_file_by_line_count", "Read specific line ranges from files" },
            { "list_files", "List directory contents with optional filtering" },
            { "write_file", "Create or completely overwrite file content" },
            { "apply_diff", "Apply precise content replacements with exact matching" },
            { "append_file", "Add content to end of existing files" },
            { "create_file", "Create new files with optional content" },
            { "delete_file", "Delete files or directories" },
            { "move_file", "Move, rename, or relocate files and directories" },
            { "search_and_replace", "Global find-replace operations across entire files" },
            { "search_files", "Search for text patterns across multiple files" },
            { "execute_command", "Execute system commands with session persistence" },
            { "create_github_issue", "Create GitHub issues with title, body, labels, assignees" },
            { "list_github_issues", "List repository issues with filtering options" },
            { "comment_on_github_issue", "Add comments to existing GitHub issues" },
            { "update_github_issue", "Update issue title, body, labels, assignees, or state" },
            { "create_github_pull_request", "Create pull requests with title, body, and branch info" },
            { "list_github_pull_requests", "List repository pull requests with filtering" },
            { "merge_github_pull_request", "Merge pull requests using specified strategy" },
            { "get_github_repository_info", "Get detailed repository information and metadata" },
            { "create_github_branch", "Create new branches from existing branches or commits" }
        };
    }
}
