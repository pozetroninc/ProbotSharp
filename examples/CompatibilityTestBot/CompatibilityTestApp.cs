using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Abstractions.Events;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;

namespace CompatibilityTestBot;

/// <summary>
/// CompatibilityTestApp - A test application for verifying Probot Sharp compatibility with Probot (Node.js).
///
/// This app tracks all webhook events for integration testing purposes.
/// Event handlers are generic and only track events rather than performing actual GitHub operations.
/// </summary>
public class CompatibilityTestApp : IProbotApp
{
    public string Name => "CompatibilityTestBot";
    public string Version => "1.0.0";

    /// <summary>
    /// Configure services for the application.
    /// </summary>
    public Task ConfigureAsync(IServiceCollection services, IConfiguration configuration)
    {
        // Register generic event handlers (scoped to each request)
        services.AddScoped<GenericPushHandler>();
        services.AddScoped<GenericIssuesHandler>();
        services.AddScoped<GenericPullRequestHandler>();
        services.AddScoped<GenericIssueCommentHandler>();
        services.AddScoped<GenericCheckRunHandler>();
        services.AddScoped<GenericCheckSuiteHandler>();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Initialize the application and register event handlers.
    /// </summary>
    public Task InitializeAsync(EventRouter router, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(router);

        // Register handlers for various event types (all actions)
        // Push events (no action)
        router.RegisterHandler("push", null, typeof(GenericPushHandler));

        // Issues events (all actions)
        router.RegisterHandler("issues", "opened", typeof(GenericIssuesHandler));
        router.RegisterHandler("issues", "closed", typeof(GenericIssuesHandler));
        router.RegisterHandler("issues", "reopened", typeof(GenericIssuesHandler));
        router.RegisterHandler("issues", "edited", typeof(GenericIssuesHandler));
        router.RegisterHandler("issues", "assigned", typeof(GenericIssuesHandler));
        router.RegisterHandler("issues", "unassigned", typeof(GenericIssuesHandler));
        router.RegisterHandler("issues", "labeled", typeof(GenericIssuesHandler));
        router.RegisterHandler("issues", "unlabeled", typeof(GenericIssuesHandler));

        // Pull request events (all actions)
        router.RegisterHandler("pull_request", "opened", typeof(GenericPullRequestHandler));
        router.RegisterHandler("pull_request", "closed", typeof(GenericPullRequestHandler));
        router.RegisterHandler("pull_request", "reopened", typeof(GenericPullRequestHandler));
        router.RegisterHandler("pull_request", "edited", typeof(GenericPullRequestHandler));
        router.RegisterHandler("pull_request", "assigned", typeof(GenericPullRequestHandler));
        router.RegisterHandler("pull_request", "unassigned", typeof(GenericPullRequestHandler));
        router.RegisterHandler("pull_request", "review_requested", typeof(GenericPullRequestHandler));
        router.RegisterHandler("pull_request", "review_request_removed", typeof(GenericPullRequestHandler));
        router.RegisterHandler("pull_request", "labeled", typeof(GenericPullRequestHandler));
        router.RegisterHandler("pull_request", "unlabeled", typeof(GenericPullRequestHandler));
        router.RegisterHandler("pull_request", "synchronize", typeof(GenericPullRequestHandler));

        // Issue comment events
        router.RegisterHandler("issue_comment", "created", typeof(GenericIssueCommentHandler));
        router.RegisterHandler("issue_comment", "edited", typeof(GenericIssueCommentHandler));
        router.RegisterHandler("issue_comment", "deleted", typeof(GenericIssueCommentHandler));

        // Check run events
        router.RegisterHandler("check_run", "created", typeof(GenericCheckRunHandler));
        router.RegisterHandler("check_run", "completed", typeof(GenericCheckRunHandler));
        router.RegisterHandler("check_run", "rerequested", typeof(GenericCheckRunHandler));
        router.RegisterHandler("check_run", "requested_action", typeof(GenericCheckRunHandler));

        // Check suite events
        router.RegisterHandler("check_suite", "completed", typeof(GenericCheckSuiteHandler));
        router.RegisterHandler("check_suite", "requested", typeof(GenericCheckSuiteHandler));
        router.RegisterHandler("check_suite", "rerequested", typeof(GenericCheckSuiteHandler));

        return Task.CompletedTask;
    }
}

/// <summary>
/// Generic handler for push events.
/// </summary>
public class GenericPushHandler : IEventHandler
{
    private readonly ILogger<GenericPushHandler> _logger;
    private readonly TestEventTracker _eventTracker;

    public GenericPushHandler(ILogger<GenericPushHandler> logger, TestEventTracker eventTracker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));
    }

    public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "GenericPushHandler invoked for push event (delivery: {DeliveryId})",
            context.Id);

        // Track the event
        var trackedEvent = new TrackedEvent
        {
            EventName = context.EventName,
            Action = null, // push events don't have actions
            DeliveryId = context.Id,
            Payload = context.Payload,
            ReceivedAt = DateTime.UtcNow
        };

        _eventTracker.AddEvent(trackedEvent);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Generic handler for issues events (all actions).
/// </summary>
public class GenericIssuesHandler : IEventHandler
{
    private readonly ILogger<GenericIssuesHandler> _logger;
    private readonly TestEventTracker _eventTracker;

    public GenericIssuesHandler(ILogger<GenericIssuesHandler> logger, TestEventTracker eventTracker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));
    }

    public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        var action = context.Payload["action"]?.ToString();
        _logger.LogInformation(
            "GenericIssuesHandler invoked for issues.{Action} event (delivery: {DeliveryId})",
            action,
            context.Id);

        // Track the event
        var trackedEvent = new TrackedEvent
        {
            EventName = context.EventName,
            Action = action,
            DeliveryId = context.Id,
            Payload = context.Payload,
            ReceivedAt = DateTime.UtcNow
        };

        _eventTracker.AddEvent(trackedEvent);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Generic handler for pull_request events (all actions).
/// </summary>
public class GenericPullRequestHandler : IEventHandler
{
    private readonly ILogger<GenericPullRequestHandler> _logger;
    private readonly TestEventTracker _eventTracker;

    public GenericPullRequestHandler(ILogger<GenericPullRequestHandler> logger, TestEventTracker eventTracker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));
    }

    public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        var action = context.Payload["action"]?.ToString();
        _logger.LogInformation(
            "GenericPullRequestHandler invoked for pull_request.{Action} event (delivery: {DeliveryId})",
            action,
            context.Id);

        // Track the event
        var trackedEvent = new TrackedEvent
        {
            EventName = context.EventName,
            Action = action,
            DeliveryId = context.Id,
            Payload = context.Payload,
            ReceivedAt = DateTime.UtcNow
        };

        _eventTracker.AddEvent(trackedEvent);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Generic handler for issue_comment events.
/// </summary>
public class GenericIssueCommentHandler : IEventHandler
{
    private readonly ILogger<GenericIssueCommentHandler> _logger;
    private readonly TestEventTracker _eventTracker;

    public GenericIssueCommentHandler(ILogger<GenericIssueCommentHandler> logger, TestEventTracker eventTracker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));
    }

    public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        var action = context.Payload["action"]?.ToString();
        _logger.LogInformation(
            "GenericIssueCommentHandler invoked for issue_comment.{Action} event (delivery: {DeliveryId})",
            action,
            context.Id);

        // Track the event
        var trackedEvent = new TrackedEvent
        {
            EventName = context.EventName,
            Action = action,
            DeliveryId = context.Id,
            Payload = context.Payload,
            ReceivedAt = DateTime.UtcNow
        };

        _eventTracker.AddEvent(trackedEvent);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Generic handler for check_run events.
/// </summary>
public class GenericCheckRunHandler : IEventHandler
{
    private readonly ILogger<GenericCheckRunHandler> _logger;
    private readonly TestEventTracker _eventTracker;

    public GenericCheckRunHandler(ILogger<GenericCheckRunHandler> logger, TestEventTracker eventTracker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));
    }

    public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        var action = context.Payload["action"]?.ToString();
        _logger.LogInformation(
            "GenericCheckRunHandler invoked for check_run.{Action} event (delivery: {DeliveryId})",
            action,
            context.Id);

        // Track the event
        var trackedEvent = new TrackedEvent
        {
            EventName = context.EventName,
            Action = action,
            DeliveryId = context.Id,
            Payload = context.Payload,
            ReceivedAt = DateTime.UtcNow
        };

        _eventTracker.AddEvent(trackedEvent);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Generic handler for check_suite events.
/// </summary>
public class GenericCheckSuiteHandler : IEventHandler
{
    private readonly ILogger<GenericCheckSuiteHandler> _logger;
    private readonly TestEventTracker _eventTracker;

    public GenericCheckSuiteHandler(ILogger<GenericCheckSuiteHandler> logger, TestEventTracker eventTracker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));
    }

    public Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        var action = context.Payload["action"]?.ToString();
        _logger.LogInformation(
            "GenericCheckSuiteHandler invoked for check_suite.{Action} event (delivery: {DeliveryId})",
            action,
            context.Id);

        // Track the event
        var trackedEvent = new TrackedEvent
        {
            EventName = context.EventName,
            Action = action,
            DeliveryId = context.Id,
            Payload = context.Payload,
            ReceivedAt = DateTime.UtcNow
        };

        _eventTracker.AddEvent(trackedEvent);

        return Task.CompletedTask;
    }
}
