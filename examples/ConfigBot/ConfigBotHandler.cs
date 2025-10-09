using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Domain.Context;

namespace ConfigBot;

/// <summary>
/// Example handler demonstrating context.config() usage.
/// Loads repository-specific configuration and uses it to customize behavior.
/// </summary>
public class ConfigBotHandler : IEventHandler
{
    public string EventName => "issues";
    public string? EventAction => "opened";

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        // Load configuration from .github/configbot.yml
        // Falls back to default settings if file doesn't exist
        var config = await context.GetConfigAsync<ConfigBotSettings>(
            "configbot.yml",
            new ConfigBotSettings(),
            cancellationToken);

        if (config == null)
        {
            context.Logger.LogWarning("Failed to load configuration, using defaults");
            config = new ConfigBotSettings();
        }

        var issue = context.Payload["issue"];
        var issueNumber = (int?)issue?["number"] ?? 0;
        var issueTitle = (string?)issue?["title"] ?? "";

        context.Logger.LogInformation(
            "Processing issue #{Number}: {Title} with config: WelcomeMessage={Message}, EnableAutoLabel={AutoLabel}",
            issueNumber, issueTitle, config.WelcomeMessage, config.EnableAutoLabel);

        // Post welcome comment using configured message
        var repo = context.Repository;
        if (repo != null)
        {
            await context.GitHub.Issue.Comment.Create(
                repo.Owner,
                repo.Name,
                issueNumber,
                config.WelcomeMessage);

            context.Logger.LogInformation("Posted welcome comment to issue #{Number}", issueNumber);
        }

        // Add labels if enabled in config
        if (config.EnableAutoLabel && config.DefaultLabels.Count > 0 && repo != null)
        {
            await context.GitHub.Issue.Labels.AddToIssue(
                repo.Owner,
                repo.Name,
                issueNumber,
                config.DefaultLabels.ToArray());

            context.Logger.LogInformation(
                "Added {Count} labels to issue #{Number}: {Labels}",
                config.DefaultLabels.Count,
                issueNumber,
                string.Join(", ", config.DefaultLabels));
        }
    }
}
