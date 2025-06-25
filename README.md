# LogiQ CLI

![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

# WARNING: THIS PROJECT IS **UNDERDEVELOPMENT**
# WARNING: IT IS **NOT** READY TO BE USED.

**LogiQ CLI** is an intelligent, AI-powered command-line interface that transforms how you interact with code, files, and GitHub repositories. Built with .NET 9.0, it provides an extensive toolkit for developers to perform complex operations through natural language interactions.

## 🌟 Key Features

### 🤖 AI-Powered Assistant
- **Multi-Model Support**: Compatible with top AI models including GPT-4, Claude, Gemini, and more
- **Natural Language Interface**: Communicate with your codebase using plain English
- **Context-Aware**: Maintains conversation history and workspace context
- **Cost Tracking**: Real-time API usage monitoring and cost tracking

### 🎭 Operational Modes
- **Default Mode**: Full-featured coding assistant with file operations and system commands
- **Researcher Mode**: Read-only analysis for understanding codebases and documentation
- **Analyst Mode**: Specialized debugging and root cause analysis without modifications
- **Custom Modes**: Create your own modes with specific tool combinations and behaviors

### 🛠 In-Depth Tool Suite

#### File Operations
- **Smart File Management**: Create, read, write, move, and delete files with intelligent path handling
- **Advanced Search**: Regex and text pattern search across entire codebases
- **Precision Editing**: Apply targeted diffs and search-and-replace operations
- **Line-by-Line Reading**: Efficiently handle large files by reading specific line ranges

#### Content Manipulation
- **Apply Diff Tool**: Surgical code modifications with regex support and preview mode
- **Search & Replace**: Powerful find-and-replace with pattern matching
- **Content Analysis**: Deep file content inspection and manipulation

#### System Integration
- **Command Execution**: Run system commands with persistent terminal sessions
- **Cross-Platform**: Native Windows and Unix shell support
- **Session Management**: Maintain state across multiple command executions

#### GitHub Integration
- **Repository Management**: Full CRUD operations on files, issues, and pull requests
- **Branch Operations**: Create, compare, and merge branches programmatically
- **Issue Tracking**: Create, update, and manage GitHub issues with labels and assignees
- **Code Search**: Search across GitHub repositories using advanced query syntax
- **Pull Request Workflow**: Complete PR lifecycle management including reviews and merging

### Prerequisites
- .NET 9.0 Runtime or SDK
- OpenRouter API key (supports multiple AI providers)
- Optional: GitHub Personal Access Token for repository operations

## 💬 Interactive Commands

### System Commands
- `/clear` - Clear conversation history
- `/exit` or `/quit` - Exit the application
- `/model [model-name]` - Switch AI models or view current model
- `/workspace [path]` - Change workspace directory
- `/settings` - Access configuration settings

### Mode Management
- `/mode` - View current mode information
- `/mode list` - List all available modes
- `/mode set <mode-id>` - Switch to a different mode
- `/mode create` - Create custom modes interactively

### API Key Management
- `/addkey` - Add new API keys
- `/switchkey` - Switch between configured API keys


## 🔧 Configuration

### Models Configuration
Supported AI models include:
- `google/gemini-2.5-pro` (default)
- `anthropic/claude-sonnet-4`
- `anthropic/claude-opus-4`
- `openai/gpt-4.1`
- `mistralai/devstral-small-2505`
- Custom models via OpenRouter

---
