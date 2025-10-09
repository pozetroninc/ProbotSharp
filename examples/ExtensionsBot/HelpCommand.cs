// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using ProbotSharp.Application.Abstractions.Commands;
using ProbotSharp.Domain.Commands;
using ProbotSharp.Domain.Context;

namespace ExtensionsBot;

/// <summary>
/// Slash command handler that displays help information about available commands.
/// Usage: /help
/// </summary>
[SlashCommandHandler("help")]
public class HelpCommand : ISlashCommandHandler
{
    private readonly ILogger<HelpCommand> _logger;

    public HelpCommand(ILogger<HelpCommand> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        ProbotSharpContext context,
        SlashCommand command,
        CancellationToken cancellationToken = default)
    {
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>();
        if (!issueNumber.HasValue || context.Repository == null)
        {
            _logger.LogWarning("Could not extract issue number or repository from payload");
            return;
        }

        var helpText = @"## Available Commands

### Slash Commands
- `/help` - Show this help message
- `/status` - Display issue status and metadata
- `/label <labels>` - Add comma-separated labels (e.g., `/label bug, enhancement`)
- `/track <metric> <value>` - Track a metric (e.g., `/track progress 50%`)

### How to Use
Type any of these commands in a comment on this issue or PR. The bot will respond automatically!

### Examples
```
/status
/label bug, priority:high
/track progress 75%
```

For more information, see the [Extensions documentation](https://github.com/your-org/probot-sharp/docs/Extensions.md).
";

        await context.GitHub.Issue.Comment.Create(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber.Value,
            helpText);

        _logger.LogInformation(
            "Displayed help message on issue #{IssueNumber}",
            issueNumber.Value);
    }
}
