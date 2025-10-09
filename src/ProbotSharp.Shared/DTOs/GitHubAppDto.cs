namespace ProbotSharp.Shared.DTOs;

/// <summary>
/// Data transfer object representing a GitHub App.
/// </summary>
public sealed class GitHubAppDto
{
    /// <summary>
    /// The unique identifier of the GitHub App.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The name of the GitHub App.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the private key is configured.
    /// Note: The actual private key value is not exposed for security reasons.
    /// </summary>
    public bool HasPrivateKey { get; set; }

    /// <summary>
    /// Indicates whether the webhook secret is configured.
    /// Note: The actual secret value is not exposed for security reasons.
    /// </summary>
    public bool HasWebhookSecret { get; set; }

    /// <summary>
    /// The installations associated with this GitHub App.
    /// </summary>
    public List<InstallationDto> Installations { get; set; } = new();
}
