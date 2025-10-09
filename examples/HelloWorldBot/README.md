# HelloWorldBot

A simple example ProbotSharp application that demonstrates the basic concepts of building GitHub Apps in C#.

## What it does

This bot automatically posts a friendly greeting comment whenever someone opens a new issue in your repository. It demonstrates:

- Implementing the `IProbotApp` interface
- Creating event handlers with the `[EventHandler]` attribute
- Accessing webhook payloads through `ProbotSharpContext`
- Making GitHub API calls using the authenticated Octokit client
- Proper logging and error handling

## Project Structure

```
HelloWorldBot/
├── HelloWorldBot.csproj    # Project file with dependencies
├── HelloWorldApp.cs        # Main app class implementing IProbotApp
├── IssueGreeter.cs         # Event handler for issues.opened
└── README.md               # This file
```

## How it works

### 1. The App Class (`HelloWorldApp.cs`)

The `HelloWorldApp` class implements `IProbotApp` and defines two key methods:

- **`ConfigureAsync`**: Registers services in the DI container (called during startup)
- **`InitializeAsync`**: Registers event handlers with the EventRouter (called after DI is built)

### 2. The Event Handler (`IssueGreeter.cs`)

The `IssueGreeter` class:
- Implements `IEventHandler`
- Is decorated with `[EventHandler("issues", "opened")]` to subscribe to issue open events
- Receives a `ProbotSharpContext` with the webhook payload and authenticated GitHub client
- Posts a greeting comment using the Octokit GitHub API client

## Using this example

### Option 1: As a library reference

You can reference this example in your own ProbotSharp application:

```text
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ProbotSharp.Application.Extensions;

var services = new ServiceCollection();
var configuration = new ConfigurationBuilder().Build();

// Load HelloWorldBot from its assembly
await services.AddProbotAppsAsync(
    configuration,
    assemblyPath: "path/to/HelloWorldBot.dll");
```

### Option 2: As a template for your own bot

Copy the files and modify them for your own use case:

1. **Create your handler**: Implement `IEventHandler` and add the `[EventHandler]` attribute
2. **Create your app class**: Implement `IProbotApp`
3. **Register services**: Add your handlers in `ConfigureAsync`
4. **Register with router**: Register handlers in `InitializeAsync`

## Example: Extending the bot

Here's how to add a handler for pull request events:

```csharp
[EventHandler("pull_request", "opened")]
public class PullRequestGreeter : IEventHandler
{
    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken)
    {
        var prNumber = context.Payload["pull_request"]?["number"]?.ToObject<int>();
        if (!prNumber.HasValue) return;

        await context.GitHub.Issue.Comment.Create(
            context.Repository.Owner,
            context.Repository.Name,
            prNumber.Value,
            "Thanks for the pull request! We'll review it soon.");
    }
}
```

Then register it in your app:

```text
public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IssueGreeter>();
    services.AddScoped<PullRequestGreeter>();  // Add this line
    return Task.CompletedTask;
}

public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
{
    router.RegisterHandler("issues", "opened", typeof(IssueGreeter));
    router.RegisterHandler("pull_request", "opened", typeof(PullRequestGreeter));  // Add this line
    return Task.CompletedTask;
}
```

## Testing locally

To test this bot locally with the ProbotSharp CLI:

1. Build the example:
   ```bash
   cd examples/HelloWorldBot
   dotnet build
   ```

2. Use the `receive` command to replay a webhook:
   ```bash
   dotnet run --project ../../src/ProbotSharp.Bootstrap.Cli -- \
     receive ./examples/HelloWorldBot \
     -e issues.opened \
     -f ../../fixtures/issues-opened.json \
     --app-id YOUR_APP_ID \
     --private-key path/to/your-key.pem
   ```

## Learn more

- [ProbotSharp Documentation](../../docs/)
- [Event types and payloads](https://docs.github.com/en/webhooks/webhook-events-and-payloads)
- [Octokit.NET documentation](https://octokitnet.readthedocs.io/)
