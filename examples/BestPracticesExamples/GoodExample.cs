// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Domain.Context;
using System.Net;
using YamlDotNet.Serialization;

namespace BestPracticesExamples;

/// <summary>
/// GOOD EXAMPLE: Demonstrates best practices for building a ProbotSharp event handler.
/// This handler manages stale issues following all recommended patterns.
/// </summary>
[EventHandler("schedule", "daily")]
public class GoodStaleIssueHandler : IEventHandler
{
    private readonly IConfigService _configService;
    private readonly ILogger<GoodStaleIssueHandler> _logger;

    // ✅ Use dependency injection for services
    public GoodStaleIssueHandler(
        IConfigService configService,
        ILogger<GoodStaleIssueHandler> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // ✅ Always accept and propagate CancellationToken
        ct.ThrowIfCancellationRequested();

        try
        {
            // ✅ Load configuration with defaults
            var config = await _configService.LoadConfigAsync(context, ct);

            // ✅ Default to dry-run for safety
            var dryRun = config?.DryRun ?? true;

            // ✅ Use structured logging with named parameters
            _logger.LogInformation(
                "Starting stale issue scan for {Repository}. Dry run: {DryRun}",
                context.GetRepositoryFullName(),
                dryRun);

            // ✅ Use data from configuration with sensible defaults
            var daysUntilStale = config?.StaleIssueDays ?? 60;
            var staleDate = DateTimeOffset.UtcNow.AddDays(-daysUntilStale);

            // ✅ Use batch operations to minimize API calls
            var issues = await FetchStaleIssuesAsync(context, staleDate, config, ct);

            _logger.LogInformation(
                "Found {Count} stale issues in {Repository}",
                issues.Count,
                context.GetRepositoryFullName());

            // ✅ Process items with cancellation support
            foreach (var issue in issues)
            {
                ct.ThrowIfCancellationRequested();
                await ProcessStaleIssueAsync(context, issue, config, dryRun, ct);
            }

            _logger.LogInformation(
                "Completed stale issue scan for {Repository}",
                context.GetRepositoryFullName());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // ✅ Handle cancellation gracefully
            _logger.LogInformation(
                "Stale issue scan cancelled for {Repository}",
                context.GetRepositoryFullName());
            throw; // Re-throw to propagate cancellation
        }
        catch (Exception ex)
        {
            // ✅ Log errors with context but don't crash the app
            _logger.LogError(
                ex,
                "Failed to process stale issues for {Repository}: {Message}",
                context.GetRepositoryFullName(),
                ex.Message);

            // Don't re-throw - handle gracefully
        }
    }

    private async Task<List<Issue>> FetchStaleIssuesAsync(
        ProbotSharpContext context,
        DateTimeOffset staleDate,
        StaleConfig? config,
        CancellationToken ct)
    {
        var request = new RepositoryIssueRequest
        {
            State = ItemStateFilter.Open,
            SortProperty = IssueSort.Updated,
            SortDirection = SortDirection.Ascending
        };

        var allIssues = await context.GitHub.Issue.GetAllForRepository(
            context.Repository.Owner,
            context.Repository.Name,
            request);

        // ✅ Use LINQ for filtering instead of multiple API calls
        var staleIssues = allIssues
            .Where(i => i.UpdatedAt < staleDate)
            .Where(i => !i.PullRequest.HasValue) // Exclude PRs
            .Where(i => !IsExempt(i, config))
            .ToList();

        return staleIssues;
    }

    private async Task ProcessStaleIssueAsync(
        ProbotSharpContext context,
        Issue issue,
        StaleConfig? config,
        bool dryRun,
        CancellationToken ct)
    {
        try
        {
            var staleLabel = config?.StaleLabel ?? "stale";
            var closeComment = config?.CloseComment ??
                "This issue has been automatically closed due to inactivity.";

            if (dryRun)
            {
                // ✅ Provide clear dry-run logging
                _logger.LogInformation(
                    "[DRY RUN] Would add '{Label}' label to issue #{Number}: {Title}",
                    staleLabel,
                    issue.Number,
                    issue.Title);

                _logger.LogInformation(
                    "[DRY RUN] Would post comment: {Comment}",
                    closeComment);

                _logger.LogInformation(
                    "[DRY RUN] Would close issue #{Number}",
                    issue.Number);
            }
            else
            {
                // ✅ Log actual operations
                _logger.LogInformation(
                    "Processing stale issue #{Number}: {Title}",
                    issue.Number,
                    issue.Title);

                // Add stale label
                await context.GitHub.Issue.Labels.AddToIssue(
                    context.Repository.Owner,
                    context.Repository.Name,
                    issue.Number,
                    new[] { staleLabel });

                // Post comment if configured
                if (!string.IsNullOrWhiteSpace(closeComment))
                {
                    await context.GitHub.Issue.Comment.Create(
                        context.Repository.Owner,
                        context.Repository.Name,
                        issue.Number,
                        closeComment);
                }

                // Close issue
                await context.GitHub.Issue.Update(
                    context.Repository.Owner,
                    context.Repository.Name,
                    issue.Number,
                    new IssueUpdate { State = ItemState.Closed });

                _logger.LogInformation(
                    "Closed stale issue #{Number}",
                    issue.Number);
            }
        }
        catch (NotFoundException ex)
        {
            // ✅ Handle specific errors gracefully
            _logger.LogWarning(
                ex,
                "Issue #{Number} not found or was deleted",
                issue.Number);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            // ✅ Provide actionable error messages
            _logger.LogError(
                ex,
                "Insufficient permissions to modify issue #{Number}. " +
                "Ensure the app has 'issues:write' permission",
                issue.Number);
        }
        catch (RateLimitExceededException ex)
        {
            // ✅ Handle rate limits appropriately
            _logger.LogError(
                ex,
                "Rate limit exceeded. Resets at {ResetTime}",
                ex.Reset.ToLocalTime());
            throw; // Re-throw to stop processing
        }
    }

    private bool IsExempt(Issue issue, StaleConfig? config)
    {
        var exemptLabels = config?.ExemptLabels ?? Array.Empty<string>();

        // ✅ Check if issue has any exempt labels
        return issue.Labels.Any(label =>
            exemptLabels.Contains(label.Name, StringComparer.OrdinalIgnoreCase));
    }
}

/// <summary>
/// GOOD EXAMPLE: Configuration service with proper error handling and defaults.
/// </summary>
public interface IConfigService
{
    Task<StaleConfig?> LoadConfigAsync(ProbotSharpContext context, CancellationToken ct);
}

public class ConfigService : IConfigService
{
    private readonly ILogger<ConfigService> _logger;

    public ConfigService(ILogger<ConfigService> logger)
    {
        _logger = logger;
    }

    public async Task<StaleConfig?> LoadConfigAsync(
        ProbotSharpContext context,
        CancellationToken ct)
    {
        try
        {
            // ✅ Use standard configuration path
            var configPath = ".github/stale.yml";
            var defaultBranch = context.Payload["repository"]?["default_branch"]?.ToString() ?? "main";

            var configContent = await context.GitHub.Repository.Content.GetAllContentsByRef(
                context.Repository.Owner,
                context.Repository.Name,
                configPath,
                defaultBranch);

            var yaml = configContent.First().Content;

            // ✅ Decode base64 content
            var decodedYaml = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(yaml));

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize<StaleConfig>(decodedYaml);

            _logger.LogInformation(
                "Loaded configuration from {Path} in {Repository}",
                configPath,
                context.GetRepositoryFullName());

            return config ?? new StaleConfig();
        }
        catch (NotFoundException)
        {
            // ✅ Gracefully handle missing config with defaults
            _logger.LogInformation(
                "No configuration file found at .github/stale.yml in {Repository}, using defaults",
                context.GetRepositoryFullName());

            return new StaleConfig();
        }
        catch (Exception ex)
        {
            // ✅ Log parsing errors and fall back to defaults
            _logger.LogError(
                ex,
                "Failed to parse configuration from .github/stale.yml in {Repository}, using defaults: {Message}",
                context.GetRepositoryFullName(),
                ex.Message);

            return new StaleConfig();
        }
    }
}

/// <summary>
/// GOOD EXAMPLE: Configuration class with sensible defaults.
/// </summary>
public class StaleConfig
{
    // ✅ All properties have sensible defaults
    public bool DryRun { get; set; } = true; // Safe default
    public int StaleIssueDays { get; set; } = 60;
    public int DaysUntilClose { get; set; } = 7;
    public string StaleLabel { get; set; } = "stale";
    public string[] ExemptLabels { get; set; } = new[] { "pinned", "security" };
    public string CloseComment { get; set; } =
        "This issue has been automatically closed due to inactivity. " +
        "Please reopen if you believe this was closed in error.";
}

/// <summary>
/// GOOD EXAMPLE: Event handler that responds to issue comments with proper validation.
/// </summary>
[EventHandler("issue_comment", "created")]
public class GoodCommandHandler : IEventHandler
{
    private readonly ILogger<GoodCommandHandler> _logger;

    public GoodCommandHandler(ILogger<GoodCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken ct)
    {
        // ✅ Check if sender is a bot to prevent loops
        if (context.IsBot())
        {
            _logger.LogDebug(
                "Ignoring comment from bot {BotName}",
                context.Payload["sender"]?["login"]?.ToString());
            return;
        }

        // ✅ Validate required context
        if (context.Repository == null)
        {
            _logger.LogWarning(
                "No repository information in {EventName} event",
                context.EventName);
            return;
        }

        // ✅ Safely extract and validate payload data
        var comment = context.Payload["comment"]?["body"]?.ToString();
        var issueNumber = context.Payload["issue"]?["number"]?.Value<int>();
        var authorLogin = context.Payload["comment"]?["user"]?["login"]?.ToString();

        if (string.IsNullOrWhiteSpace(comment) || !issueNumber.HasValue)
        {
            _logger.LogDebug("Invalid comment or missing issue number");
            return;
        }

        // ✅ Validate input length to prevent abuse
        if (comment.Length > 10_000)
        {
            _logger.LogWarning(
                "Comment exceeds maximum length ({Length} > 10000), ignoring",
                comment.Length);
            return;
        }

        // ✅ Use regex with timeout to prevent ReDoS attacks
        if (!System.Text.RegularExpressions.Regex.IsMatch(
            comment,
            @"^/label\s+(\w+)$",
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromMilliseconds(100)))
        {
            return; // Not a label command
        }

        var labelName = comment.Split(' ')[1];

        // ✅ Be concise and factual in bot responses
        var response = $"Adding label '{labelName}' to this issue.";

        try
        {
            await context.GitHub.Issue.Labels.AddToIssue(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                new[] { labelName });

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                response);

            _logger.LogInformation(
                "Added label '{Label}' to issue #{Number} by request from @{Author}",
                labelName,
                issueNumber.Value,
                authorLogin);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            // ✅ Handle label not found gracefully
            var errorResponse = $"Label '{labelName}' does not exist in this repository. " +
                               "Please create it first or use an existing label.";

            await context.GitHub.Issue.Comment.Create(
                context.Repository.Owner,
                context.Repository.Name,
                issueNumber.Value,
                errorResponse);

            _logger.LogWarning(
                "Label '{Label}' not found in {Repository}",
                labelName,
                context.GetRepositoryFullName());
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add label '{Label}' to issue #{Number}",
                labelName,
                issueNumber.Value);
        }
    }
}
