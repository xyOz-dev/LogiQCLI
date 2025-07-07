# LogiQ CLI

![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
[![Discord](https://img.shields.io/badge/Discord-Join%20Chat-5865F2?logo=discord&logoColor=white)](https://discord.gg/d8tNc9Kf8v)

LogiQ CLI is a powerful, extensible, and intelligent command-line interface designed to streamline your development workflow. It integrates with various AI providers to bring intelligent code generation, conversation, and automation directly to your terminal.

## Getting Started

1.  **Download:** Grab the latest version from the [GitHub Releases](https://github.com/your-repo/logiq-cli/releases) page.
2.  **Extract:** Unzip the downloaded file to a location of your choice.
3.  **Run:** Open your terminal, navigate to the extracted directory, and run the executable.

## Initial Configuration

Before you can start using the AI features, you need to add an API key for your chosen provider. Run the following command and follow the prompts:

```bash
/addkey
```

## Supported Providers

LogiQ CLI supports a variety of AI providers, allowing you to choose the best models for your needs.

*   LMStudio
*   OpenAI
*   OpenRouter
*   Requesty

## Commands

Here is a list of all available commands and their functions:

| Command                               | Description                                                                                                                           |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `/addkey`                             | Add a new API key with a nickname for easy switching.                                                                                 |
| `/model`                              | View or change the current AI model.                                                                                                  |
| `/models`                             | Manage the available model list.                                                                                                      |
| `/settings` or `/settings interactive`| Display current application settings. Use the interactive flag for a guided configuration experience.                                   |
| `/switchkey`                          | Switch between available API keys.                                                                                                    |
| `/workspace`                          | View or change the current workspace directory.                                                                                       |
| `/help` (or `h`)                      | Show help information for commands.                                                                                                   |
| `/mode`                               | Manage modes. Usage: /mode [list|current|switch <mode-id>|info <mode-id>|create]                                                     |
| `/clear`                              | Clear the chat history and reset the display.                                                                                         |
| `/compress`                           | Compress chat history, keeping the first and last three messages.                                                                     |
| `/exit` (or `quit`)                   | Exit the application.                                                                                                                 |

## Key Features

*   **Extensible Agent Framework:** Easily create and customize AI agents for specific tasks.
*   **Mode-Based Interaction:** Switch between different operational modes like 'Code', 'Architect', and 'Debug' to tailor the AI's behavior.
*   **Interactive Configuration:** A user-friendly interface for setting up models, API keys, and other preferences.
*   **Session Management:** Clear, compress, and manage your chat history with simple commands.
*   **Cross-Platform:** Runs on Windows, macOS, and Linux.

---
## Tools

**apply_diff**
This tool applies precise content replacements within a file. It is advantageous for making targeted edits with confidence, as it requires an exact match of the content to be changed, preventing accidental modifications.
***
**search_and_replace**
This tool performs a global search and replace for text or regex patterns across an entire file, updating all occurrences. It is ideal for refactoring variable names or updating recurring text efficiently.
***
**search_files**
This tool searches for text or regex patterns across multiple files, returning the matching lines with their location. It is highly effective for locating code, configuration, or specific mentions throughout a project.
***
**backup_commands**
This tool manages file backups within the workspace, allowing you to list, restore, compare, and clean up previous versions. It provides a safety net for file modifications, ensuring you can revert changes if needed.
***
**get_library_docs**
This tool fetches current documentation for a specified software library. It is invaluable for getting up-to-date information and code examples directly related to the library you are working with.
***
**resolve_library_id**
This tool finds the correct identifier for a software library based on its name. It is essential for ensuring you can retrieve the correct documentation using the `get_library_docs` tool.
***
**append_file**
This tool adds content to the end of a file, creating the file if it doesn't exist. This is useful for logging or adding new entries to configuration files without altering existing data.
***
**create_file**
This tool creates a new file, optionally with initial content, and prevents accidental overwrites. It's the standard way to safely add new files to your project.
***
**delete_file**
This tool permanently deletes files or directories, with safeguards to prevent accidental removal of critical project files. It is a direct way to clean up unneeded components from your workspace.
***
**move_file**
This tool renames or moves files and directories within your project. Its intelligent handling of destinations makes reorganizing your project structure simple and effective.
***
**read_file_by_line_count**
This tool reads a specified number of lines from the beginning of a file. It's highly efficient for quickly previewing large files without loading the entire content into memory.
***
**read_file**
This tool reads the entire content of a specified file. It is the fundamental tool for examining file contents to understand code or configuration before making changes.
***
**write_file**
This tool writes content to a file, either creating a new one or completely overwriting an existing one. It is best used for generating new files or replacing file content in its entirety.
***
**comment_on_github_issue**
This tool adds a comment to a specified GitHub issue. It's useful for providing updates, asking questions, or contributing to discussions on repository issues.
***
**comment_on_github_pull_request**
This tool posts a comment on a GitHub pull request. It is ideal for code reviews, providing feedback, or discussing changes within the pull request workflow.
***
**compare_github_branches**
This tool compares two branches in a GitHub repository to show the differences in commits and files. It's perfect for understanding what changes a branch will introduce before creating a pull request.
***
**create_github_branch**
This tool creates a new branch in a GitHub repository from an existing branch or commit. It is the first step in starting work on a new feature or fix without disrupting the main codebase.
***
**create_github_file**
This tool creates a new file directly in a GitHub repository with a specified commit message. It allows for adding new files to a remote repository without a local clone.
***
**create_github_issue**
This tool creates a new issue in a GitHub repository. It's the standard way to report bugs, request features, or track tasks within a project.
***
**create_github_pull_request**
This tool creates a pull request in a GitHub repository to propose and collaborate on changes. This is the central part of the GitHub workflow for reviewing and merging code.
***
**delete_github_file**
This tool deletes a file from a GitHub repository with a commit message, requiring the file's SHA to prevent conflicts. It's a secure way to remove files directly from the remote repository.
***
**get_github_commit_diff**
This tool retrieves the full diff for a specific commit in a GitHub repository. It's excellent for reviewing the exact changes made in a single commit.
***
**get_github_file_content**
This tool retrieves the content of a file from a GitHub repository at a specific branch or commit. It is essential for examining remote files without cloning the repository.
***
**get_github_issue**
This tool retrieves detailed information about a single GitHub issue. It's useful for getting the full context of a task, bug, or feature request.
***
**get_github_pull_request_diff**
This tool fetches the complete diff of a pull request, showing all proposed changes. It is invaluable for conducting detailed code reviews.
***
**get_github_pull_request_files**
This tool lists all files that have been changed in a pull request along with statistics on additions and deletions. It provides a quick overview of the scope of changes in a PR.
***
**get_github_pull_request_reviews**
This tool fetches all reviews for a pull request, including comments and approval statuses. It's key to understanding the review history and current state of a PR.
***
**get_github_pull_request**
This tool retrieves detailed information about a specific pull request. It provides a complete overview, including metadata, status, and associated branches.
***
**get_github_repository_info**
This tool retrieves comprehensive information about a GitHub repository. It's useful for getting a high-level understanding of a project's settings, statistics, and configuration.
***
**list_github_branches**
This tool lists all branches in a GitHub repository, including their protection status. It provides a quick way to see all active lines of development.
***
**list_github_commits**
This tool lists commits for a GitHub repository, with powerful filtering options. It allows you to track project history, see changes by a specific author, or analyze the commit history of a file.
***
**list_github_issue_comments**
This tool lists all comments on a GitHub issue, showing the full discussion history. It's essential for catching up on the conversation around a bug or feature.
***
**list_github_issues**
This tool lists issues from a GitHub repository with various filtering options. It is perfect for getting an overview of project tasks, bugs, or features based on specific criteria.
***
**list_github_labels**
This tool lists all available labels in a GitHub repository. It helps in understanding the classification system used for issues and pull requests in a project.
***
**list_github_notifications**
This tool lists your GitHub notifications, with options to filter them. It helps you stay updated on activities you are involved in or watching across repositories.
***
**list_github_pull_requests**
This tool lists pull requests from a GitHub repository with several filtering and sorting options. It allows for a customized view of ongoing or historical code reviews.
***
**mark_all_github_notifications_as_read**
This tool marks all of your GitHub notifications as read. It's a quick way to clear your notification inbox once you're caught up.
***
**mark_github_notification_as_read**
This tool marks a single GitHub notification as read. It allows for managing notifications one by one for better focus.
***
**merge_github_pull_request**
This tool merges an open pull request into its base branch using a selected strategy. This is the final step for incorporating approved changes into the codebase.
***
**search_github_code**
This tool searches for code across GitHub repositories using advanced query syntax. It is extremely powerful for finding code examples, tracking down function calls, or analyzing code patterns across a wide scope.
***
**update_github_file**
This tool updates an existing file in a GitHub repository with new content and a commit message. It requires the file's SHA to ensure safe, conflict-free updates.
***
**update_github_issue**
This tool modifies an existing GitHub issue's attributes like title, body, or labels. It is useful for keeping issue information current as work progresses.
***
**update_github_pull_request**
This tool modifies an existing pull request's details, such as its title, body, or state. It's helpful for refining a PR's description or changing its status.
***
**execute_command**
This tool executes shell commands on the local system, with support for persistent sessions to maintain state. It is essential for running build scripts, system utilities, or any command-line operation.
***
**list_files**
This tool lists files and directories within the project, helping you explore the codebase structure. Its output is relative, making it easy to pipe into other file-based tools.
***
**tavily_search**
This tool performs a comprehensive web search optimized for developers and technical queries. It is advantageous for quickly finding factual answers, code snippets, and relevant articles to solve technical problems.
