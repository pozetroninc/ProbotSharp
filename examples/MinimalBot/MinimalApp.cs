using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;

namespace MinimalBot;

/// <summary>
/// MinimalApp - A simple example bot that demonstrates ProbotSharp with zero infrastructure dependencies.
///
/// This bot automatically labels new issues based on keywords in the title:
/// - "bug" → adds "bug" label
/// - "feature" or "enhancement" → adds "enhancement" label
/// - "question" → adds "question" label
/// - "docs" or "documentation" → adds "documentation" label
/// </summary>
public class MinimalApp : IProbotApp
{
    public string Name => "MinimalBot";
    public string Version => "1.0.0";

    /// <summary>
    /// Configure services for the application.
    /// </summary>
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register event handlers
        services.AddScoped<IssueOpenedHandler>();
        services.AddScoped<IssueCommentHandler>();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initialize the application and register event handlers.
    /// </summary>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(router);

        // Register handlers with the event router
        router.RegisterHandler("issues", "opened", typeof(IssueOpenedHandler));
        router.RegisterHandler("issue_comment", "created", typeof(IssueCommentHandler));

        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for issue opened events.
/// </summary>
public class IssueOpenedHandler : IEventHandler
{
    private readonly ILogger<IssueOpenedHandler> _logger;

    public IssueOpenedHandler(ILogger<IssueOpenedHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = context.Payload;
            var issue = payload["issue"] as JObject;

            if (issue == null || issue["title"] == null)
            {
                _logger.LogWarning("Issue opened event received but issue or title is missing");
                return;
            }

            var title = issue["title"]?.ToString().ToLowerInvariant() ?? string.Empty;
            var labels = new List<string>();

            // Auto-label based on keywords in title
            if (title.Contains("bug"))
            {
                labels.Add("bug");
            }

            if (title.Contains("feature") || title.Contains("enhancement"))
            {
                labels.Add("enhancement");
            }

            if (title.Contains("question"))
            {
                labels.Add("question");
            }

            if (title.Contains("docs") || title.Contains("documentation"))
            {
                labels.Add("documentation");
            }

            if (labels.Count > 0)
            {
                var number = (int?)issue["number"] ?? 0;
                _logger.LogInformation(
                    "Adding labels {Labels} to issue #{Number}: {Title}",
                    string.Join(", ", labels),
                    number,
                    issue["title"]?.ToString());

                var repository = payload["repository"] as JObject;
                var owner = repository?["owner"]?["login"]?.ToString() ?? string.Empty;
                var repo = repository?["name"]?.ToString() ?? string.Empty;

                await context.GitHub.Issue.Labels.AddToIssue(
                    owner,
                    repo,
                    number,
                    labels.ToArray());

                _logger.LogInformation("Labels added successfully to issue #{Number}", number);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling issue opened event");
        }
    }
}

/// <summary>
/// Handler for issue comment events.
/// </summary>
public class IssueCommentHandler : IEventHandler
{
    private readonly ILogger<IssueCommentHandler> _logger;

    public IssueCommentHandler(ILogger<IssueCommentHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = context.Payload;
            var comment = payload["comment"] as JObject;
            var issue = payload["issue"] as JObject;

            if (comment == null || issue == null || comment["body"] == null)
            {
                return;
            }

            var body = comment["body"]?.ToString().Trim() ?? string.Empty;

            // Respond to "/help" command
            if (body.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                var number = (int?)issue["number"] ?? 0;
                _logger.LogInformation("Responding to /help command on issue #{Number}", number);

                var repository = payload["repository"] as JObject;
                var owner = repository?["owner"]?["login"]?.ToString() ?? string.Empty;
                var repo = repository?["name"]?.ToString() ?? string.Empty;

                var helpMessage = @"## MinimalBot Help

I'm a simple auto-labeler bot. I automatically add labels to issues based on keywords in the title:

- **bug** → adds `bug` label
- **feature** or **enhancement** → adds `enhancement` label
- **question** → adds `question` label
- **docs** or **documentation** → adds `documentation` label

**Running in minimal mode** - no databases, no Redis, just pure in-memory operation!

For more info, see the [MinimalBot README](https://github.com/your-org/probot-sharp/tree/main/examples/MinimalBot).
";

                await context.GitHub.Issue.Comment.Create(
                    owner,
                    repo,
                    number,
                    helpMessage);

                _logger.LogInformation("Help message posted to issue #{Number}", number);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling issue comment event");
        }
    }
}
