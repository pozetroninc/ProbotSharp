# Slash Commands Bot

A ProbotSharp example demonstrating slash command functionality for GitHub issues and pull requests.

## Overview

This bot shows how to use the Slash Commands extension in ProbotSharp to handle commands typed in issue and PR comments. Slash commands provide an intuitive way for users to interact with your bot using a simple `/command arguments` syntax.

## Features

- **Label Command** (`/label`): Add labels to issues and pull requests
- Automatic command parsing from comments
- Support for multiple commands in a single comment
- Comprehensive error handling and user feedback

## Usage

### Label Command

Add one or more labels to an issue or pull request:

```
/label bug, enhancement
```

This will add the `bug` and `enhancement` labels to the issue/PR. The bot will:
- Parse the comma-separated label names
- Add all labels to the issue/PR
- Post a confirmation comment
- Handle errors if labels don't exist

**Examples:**

```
/label bug
/label help wanted, good first issue
/label documentation, priority:high
```

## How It Works

### Command Parsing

The slash command parser looks for lines that start with `/` followed by a command name:

```
/command-name arguments go here
```

- Command names can contain alphanumeric characters, hyphens, and underscores
- Arguments are everything after the first space
- Multiple commands can be placed in a single comment (one per line)

### Command Handling

1. User posts a comment with a slash command
2. `SlashCommandEventHandler` intercepts the `issue_comment.created` event
3. `SlashCommandParser` parses the comment to find commands
4. `SlashCommandRouter` dispatches each command to registered handlers
5. Handlers execute and interact with the GitHub API

## Implementation

### Creating a Command Handler

Command handlers implement `ISlashCommandHandler` and are decorated with `[SlashCommandHandler]`:

```csharp
[SlashCommandHandler("label")]
public class LabelCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        var issueNumber = context.Payload["issue"]?["number"]?.ToObject<int>() ?? 0;

        // Parse arguments
        var labels = command.Arguments.Split(',').Select(l => l.Trim()).ToArray();

        // Interact with GitHub API
        await context.GitHub.Issue.Labels.AddToIssue(
            context.Repository.Owner,
            context.Repository.Name,
            issueNumber,
            labels);
    }
}
```

### Registering Handlers

In your ProbotSharp app's `ConfigureAsync` method:

```text
public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
{
    // Discover and register all slash command handlers in this assembly
    services.AddSlashCommands(typeof(SlashCommandsApp).Assembly);

    return Task.CompletedTask;
}
```

### Multiple Commands per Handler

A single handler can respond to multiple commands:

```csharp
[SlashCommandHandler("label")]
[SlashCommandHandler("tag")]
public class LabelCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        // Handle both /label and /tag commands
        // Check command.Name to differentiate if needed
    }
}
```

## Architecture

This example follows ProbotSharp's architecture:

- **Domain Layer** (`ProbotSharp.Domain`): `SlashCommand` and `SlashCommandParser`
- **Application Layer** (`ProbotSharp.Application`):
  - `ISlashCommandHandler` interface
  - `SlashCommandRouter` service
  - `SlashCommandEventHandler` event handler
  - `SlashCommandHandlerDiscovery` for auto-discovery
- **Example Layer** (this project): Command implementations

## Testing

To test this bot locally:

1. Set up a test GitHub repository
2. Install your ProbotSharp app on the repository
3. Create an issue or pull request
4. Post a comment with `/label bug, test`
5. Observe the bot adding labels and posting confirmation

## Best Practices

1. **Validate Arguments**: Always check that required arguments are provided
2. **User Feedback**: Post comments to confirm success or explain errors
3. **Error Handling**: Catch API exceptions and provide helpful messages
4. **Avoid Loops**: The `SlashCommandEventHandler` automatically ignores bot comments
5. **Documentation**: Provide help text when users don't supply required arguments

## Extending This Example

Add more commands by creating additional handler classes:

```csharp
[SlashCommandHandler("assign")]
public class AssignCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        // Parse assignees from command.Arguments
        // Call GitHub API to assign issue
    }
}

[SlashCommandHandler("close")]
public class CloseCommand : ISlashCommandHandler
{
    public async Task HandleAsync(ProbotSharpContext context, SlashCommand command, CancellationToken ct)
    {
        // Close the issue with an optional reason from command.Arguments
    }
}
```

## Node.js Probot Parity

This implementation matches the functionality of the Node.js `probot-commands` extension:

- ✅ Parse `/command arguments` from comments
- ✅ Route commands to registered handlers
- ✅ Support multiple handlers per command
- ✅ Automatic discovery via attributes
- ✅ Works with both issue comments and PR review comments

## Resources

- [ProbotSharp Documentation](../../docs/)
- [GitHub API - Issues](https://docs.github.com/en/rest/issues)
- [Octokit.NET](https://github.com/octokit/octokit.net)

## License

MIT License - see [LICENSE](../../LICENSE) for details
