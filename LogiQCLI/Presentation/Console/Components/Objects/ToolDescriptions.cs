using System.Collections.Generic;

namespace LogiQCLI.Presentation.Console.Components.Objects
{
    internal static class ToolDescriptions
    {
        public static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
        {
            { "read_file", "Read contents of files" },
            { "read_file_by_line_count", "Read specific lines from files" },
            { "list_files", "List directory contents" },
            { "write_file", "Create or overwrite files" },
            { "apply_diff", "Apply targeted modifications to files" },
            { "append_file", "Append content to files" },
            { "create_file", "Create new files" },
            { "delete_file", "Delete files" },
            { "move_file", "Move or rename files" },
            { "search_and_replace", "Find and replace text in files" },
            { "search_files", "Search for patterns across files" },
            { "execute_command", "Execute system commands" },
            { "create_github_issue", "Create new GitHub issues with title, body, labels, and assignees" },
            { "list_github_issues", "List GitHub issues with optional filtering by state, labels, and assignee" },
            { "comment_on_github_issue", "Add comments to existing GitHub issues" },
            { "update_github_issue", "Update GitHub issue title, body, labels, assignees, or state" },
            { "create_github_pull_request", "Create new GitHub pull requests with title, body, and branch info" },
            { "list_github_pull_requests", "List GitHub pull requests with optional filtering and sorting" },
            { "merge_github_pull_request", "Merge GitHub pull requests using specified merge strategy" },
            { "get_github_repository_info", "Get detailed information about GitHub repositories" },
            { "create_github_branch", "Create new GitHub branches from existing branches or commits" }
        };
    }
}