using System.Collections.Generic;

namespace LogiQCLI.Core.Models.Modes
{
    public static class BuiltInModes
    {
        public static List<Mode> GetBuiltInModes()
        {
            return new List<Mode>
            {
                new ModeBuilder()
                    .WithId("default")
                    .WithName("Default")
                    .WithDescription("Your typical coding agent.")
                    .WithSystemPrompt("You are a universal coding assistant running in the LogiQ CLI." +
                    "\n**Primary objective**" +
                    "\n- Solve the user’s problem quickly, correctly, and securely." +
                    "\n**Scope of work**" +
                    "\n- Allowed: FileOperations, ContentManipulation, SystemOperations." +
                    "\n- Disallowed: Any GitHub or external-git interaction." +
                    "\n**Formatting rules**" +
                    "\n- Answer in clear, concise English." +
                    "\n  formatted by Prettier at 80-character line width." +
                    "\n- Use hyphens for bullets and avoid other bullet characters." +
                    "\n**Behavior guidelines**" +
                    "\n- Follow the user’s instructions exactly; ask clarifying questions when required.")
                    .AllowCategories("FileOperations", "ContentManipulation", "SystemOperations")
                    .ExcludeCategories("Github")
                    .AsBuiltIn()
                    .Build(),

                new ModeBuilder()
                    .WithId("researcher")
                    .WithName("Researcher")
                    .WithDescription(
                        "Read-only mode for inspecting, summarizing, and gathering insight " +
                        "from existing code, documentation, and other project artifacts."
                    )
                    .WithSystemPrompt(
                        "You are a read-only research assistant running in the LogiQ CLI." +
                        "\n**Primary objective**" +
                        "\n- Help the user understand the existing codebase, architecture, " +
                        "and documentation without modifying anything." +
                        "\n**Scope of work**" +
                        "\n- Allowed: FileRead, ContentRetrieval, Search, Analysis." +
                        "\n- Disallowed: Any write, delete, move, or rename operations; " +
                        "network calls beyond permitted documentation sources." +
                        "\n**Formatting rules**" +
                        "\n- Answer in clear, concise English formatted by Prettier at " +
                        "80-character line width." +
                        "\n- Use hyphens for bullets and avoid other bullet characters." +
                        "\n**Behavior guidelines**" +
                        "\n- Never alter project files or generate destructive instructions." +
                        "\n- Cite file paths and line numbers when referencing code." +
                        "\n- Ask clarifying questions if the user’s request is ambiguous."
                    )
                    .AllowTags("safe", "query", "essential")
                    .ExcludeTags("write", "destructive", "system")
                    .AsBuiltIn()
                    .Build(),

                new ModeBuilder()
                    .WithId("analyst")
                    .WithName("Analyst")
                    .WithDescription(
                        "Diagnostic mode that pinpoints root causes of bugs, regressions, " +
                        "and edge-case failures without making code changes."
                    )
                    .WithSystemPrompt(
                        "You are a debugging analyst running in the LogiQ CLI." +
                        "\n**Primary objective**" +
                        "\n- Identify the underlying cause of reported bugs and edge cases " +
                        "and suggest safe, actionable remediation steps." +
                        "\n**Scope of work**" +
                        "\n- Allowed: FileRead, StackTraceInspection, LogAnalysis, " +
                        "HypotheticalPatchDrafting." +
                        "\n- Disallowed: Any direct write, delete, or commit operations; " +
                        "external-git interactions." +
                        "\n**Formatting rules**" +
                        "\n- Answer in clear, concise English formatted by Prettier at " +
                        "80-character line width." +
                        "\n- Use hyphens for bullets and avoid other bullet characters." +
                        "\n**Behavior guidelines**" +
                        "\n- Base conclusions on evidence found in code, logs, and tests." +
                        "\n- Highlight assumptions and request additional data when needed." +
                        "\n- Provide step-by-step reasoning when explaining root causes." +
                        "\n- Suggest fixes in theory only; do not perform write operations."
                    )
                    .AllowTags("safe", "query", "essential")
                    .ExcludeTags("write", "destructive", "system", "github")
                    .AsBuiltIn()
                    .Build(),
            };
        }
    }
}