// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Bootstrap.Api.Controllers;

/// <summary>
/// Controller for web-based GitHub App setup wizard.
/// Provides an interactive UI for configuring and registering a GitHub App.
/// </summary>
[Route("[controller]")]
public class SetupController : Controller
{
    private readonly ISetupWizardPort _setupWizard;
    private readonly IManifestPersistencePort _manifestPort;
    private readonly IEnvironmentConfigurationPort _envConfig;
    private readonly ILogger<SetupController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetupController"/> class.
    /// </summary>
    /// <param name="setupWizard">The setup wizard port for GitHub App configuration.</param>
    /// <param name="manifestPort">The manifest persistence port for storing app manifests.</param>
    /// <param name="envConfig">The environment configuration port for environment settings.</param>
    /// <param name="logger">The logger for structured logging.</param>
    public SetupController(
        ISetupWizardPort setupWizard,
        IManifestPersistencePort manifestPort,
        IEnvironmentConfigurationPort envConfig,
        ILogger<SetupController> logger)
    {
        this._setupWizard = setupWizard;
        this._manifestPort = manifestPort;
        this._envConfig = envConfig;
        this._logger = logger;
    }

    /// <summary>
    /// GET /setup - Landing page that checks if app is configured.
    /// </summary>
    /// <returns>Setup landing page view.</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Check if already configured
        var manifestResult = await this._manifestPort.GetAsync().ConfigureAwait(false);
        var isConfigured = manifestResult.IsSuccess && !string.IsNullOrWhiteSpace(manifestResult.Value);

        this.ViewData["IsConfigured"] = isConfigured;
        return this.View();
    }

    /// <summary>
    /// GET /setup/new - Configuration form for creating a new app.
    /// </summary>
    /// <returns>Configuration form view.</returns>
    [HttpGet("new")]
    public IActionResult New()
    {
        // Pre-fill with defaults
        this.ViewData["AppName"] = "My ProbotSharp App";
        this.ViewData["Description"] = "Built with ProbotSharp";
        this.ViewData["BaseUrl"] = $"{this.Request.Scheme}://{this.Request.Host}";
        return this.View();
    }

    /// <summary>
    /// POST /setup/manifest - Generate manifest and GitHub registration URL.
    /// </summary>
    /// <param name="request">Manifest creation request.</param>
    /// <returns>Manifest view with JSON and registration URL.</returns>
    [HttpPost("manifest")]
    public async Task<IActionResult> CreateManifest([FromForm] ManifestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AppName))
        {
            this.ModelState.AddModelError("AppName", "App name is required");
            return this.View("New");
        }

        var baseUrl = $"{this.Request.Scheme}://{this.Request.Host}";
        var command = new GetManifestCommand(
            baseUrl,
            request.AppName,
            request.Description,
            request.Homepage,
            request.WebhookProxyUrl,
            request.IsPublic);

        var result = await this._setupWizard.GetManifestAsync(command).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            this._logger.LogError("Failed to create manifest: {Error}", result.Error?.Message);
            this.ModelState.AddModelError(string.Empty, result.Error?.Message ?? "Failed to create manifest");
            return this.View("New");
        }

        var manifestResponse = result.Value!;
        this.ViewData["ManifestJson"] = manifestResponse.ManifestJson;
        this.ViewData["CreateAppUrl"] = manifestResponse.CreateAppUrl.ToString();
        this.ViewData["AppName"] = request.AppName;

        return this.View("Manifest");
    }

    /// <summary>
    /// GET /setup/callback - OAuth callback from GitHub after app registration.
    /// </summary>
    /// <param name="code">OAuth code from GitHub.</param>
    /// <returns>Callback processing view.</returns>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            this._logger.LogError("Callback received without code");
            return this.BadRequest("OAuth code is required");
        }

        this._logger.LogInformation("Processing setup callback with code");

        var command = new CompleteSetupCommand(code, null);
        var result = await this._setupWizard.CompleteSetupAsync(command).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            this._logger.LogError("Failed to complete setup: {Error}", result.Error?.Message);
            this.ViewData["Error"] = result.Error?.Message ?? "Failed to complete setup";
            return this.View("Error");
        }

        this.ViewData["AppUrl"] = result.Value;
        return this.View("Complete");
    }

    /// <summary>
    /// GET /setup/complete - Success page after setup completion.
    /// </summary>
    /// <returns>Success page view.</returns>
    [HttpGet("complete")]
    public IActionResult Complete()
    {
        return this.View();
    }
}

/// <summary>
/// Request model for manifest creation form.
/// </summary>
public class ManifestRequest
{
    /// <summary>
    /// Gets or sets the name of the GitHub App.
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the GitHub App.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the homepage URL for the GitHub App.
    /// </summary>
    public string? Homepage { get; set; }

#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility
    /// <summary>
    /// Gets or sets the webhook proxy URL for local development (e.g., Smee.io).
    /// </summary>
    public string? WebhookProxyUrl { get; set; }
#pragma warning restore CA1056

    /// <summary>
    /// Gets or sets a value indicating whether the GitHub App is public.
    /// </summary>
    public bool IsPublic { get; set; } = true;
}

#pragma warning restore CA1848
