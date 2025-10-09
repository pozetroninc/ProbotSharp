// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;

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

    public SetupController(
        ISetupWizardPort setupWizard,
        IManifestPersistencePort manifestPort,
        IEnvironmentConfigurationPort envConfig,
        ILogger<SetupController> logger)
    {
        _setupWizard = setupWizard;
        _manifestPort = manifestPort;
        _envConfig = envConfig;
        _logger = logger;
    }

    /// <summary>
    /// GET /setup - Landing page that checks if app is configured.
    /// </summary>
    /// <returns>Setup landing page view</returns>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Check if already configured
        var manifestResult = await _manifestPort.GetAsync();
        var isConfigured = manifestResult.IsSuccess && !string.IsNullOrWhiteSpace(manifestResult.Value);

        ViewData["IsConfigured"] = isConfigured;
        return View();
    }

    /// <summary>
    /// GET /setup/new - Configuration form for creating a new app.
    /// </summary>
    /// <returns>Configuration form view</returns>
    [HttpGet("new")]
    public IActionResult New()
    {
        // Pre-fill with defaults
        ViewData["AppName"] = "My ProbotSharp App";
        ViewData["Description"] = "Built with ProbotSharp";
        ViewData["BaseUrl"] = $"{Request.Scheme}://{Request.Host}";
        return View();
    }

    /// <summary>
    /// POST /setup/manifest - Generate manifest and GitHub registration URL.
    /// </summary>
    /// <param name="request">Manifest creation request</param>
    /// <returns>Manifest view with JSON and registration URL</returns>
    [HttpPost("manifest")]
    public async Task<IActionResult> CreateManifest([FromForm] ManifestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AppName))
        {
            ModelState.AddModelError("AppName", "App name is required");
            return View("New");
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var command = new GetManifestCommand(
            baseUrl,
            request.AppName,
            request.Description,
            request.Homepage,
            request.WebhookProxyUrl,
            request.IsPublic);

        var result = await _setupWizard.GetManifestAsync(command);
        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to create manifest: {Error}", result.Error?.Message);
            ModelState.AddModelError(string.Empty, result.Error?.Message ?? "Failed to create manifest");
            return View("New");
        }

        var manifestResponse = result.Value!;
        ViewData["ManifestJson"] = manifestResponse.ManifestJson;
        ViewData["CreateAppUrl"] = manifestResponse.CreateAppUrl.ToString();
        ViewData["AppName"] = request.AppName;

        return View("Manifest");
    }

    /// <summary>
    /// GET /setup/callback - OAuth callback from GitHub after app registration.
    /// </summary>
    /// <param name="code">OAuth code from GitHub</param>
    /// <returns>Callback processing view</returns>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogError("Callback received without code");
            return BadRequest("OAuth code is required");
        }

        _logger.LogInformation("Processing setup callback with code");

        var command = new CompleteSetupCommand(code, null);
        var result = await _setupWizard.CompleteSetupAsync(command);

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to complete setup: {Error}", result.Error?.Message);
            ViewData["Error"] = result.Error?.Message ?? "Failed to complete setup";
            return View("Error");
        }

        ViewData["AppUrl"] = result.Value;
        return View("Complete");
    }

    /// <summary>
    /// GET /setup/complete - Success page after setup completion.
    /// </summary>
    /// <returns>Success page view</returns>
    [HttpGet("complete")]
    public IActionResult Complete()
    {
        return View();
    }
}

/// <summary>
/// Request model for manifest creation form.
/// </summary>
public class ManifestRequest
{
    public string AppName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Homepage { get; set; }
    public string? WebhookProxyUrl { get; set; }
    public bool IsPublic { get; set; } = true;
}
