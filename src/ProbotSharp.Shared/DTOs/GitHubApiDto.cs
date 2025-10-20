namespace ProbotSharp.Shared.DTOs;

/// <summary>
/// DTO for GitHub App information from the API.
/// Maps to the response from GET /app endpoint.
/// </summary>
public sealed class GitHubApiAppDto
{
    /// <summary>
    /// Gets or sets the GitHub App ID.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the GitHub App name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub App description.
    /// </summary>
    public string? Description { get; set; }

#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility
    /// <summary>
    /// Gets or sets the external URL for the GitHub App.
    /// </summary>
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// Gets or sets the HTML URL for the GitHub App.
    /// </summary>
    public string HtmlUrl { get; set; } = string.Empty;
#pragma warning restore CA1056

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// DTO for GitHub Installation information from the API.
/// Maps to the response from GET /app/installations endpoint.
/// </summary>
public sealed class GitHubApiInstallationDto
{
    /// <summary>
    /// Gets or sets the installation ID.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the account that owns this installation.
    /// </summary>
    public GitHubApiAccountDto? Account { get; set; }

#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility
    /// <summary>
    /// Gets or sets the URL for accessing tokens.
    /// </summary>
    public string AccessTokensUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL for accessing repositories.
    /// </summary>
    public string RepositoriesUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTML URL for the installation.
    /// </summary>
    public string HtmlUrl { get; set; } = string.Empty;
#pragma warning restore CA1056

    /// <summary>
    /// Gets or sets the GitHub App ID.
    /// </summary>
    public long AppId { get; set; }

    /// <summary>
    /// Gets or sets the target ID (user or organization).
    /// </summary>
    public long? TargetId { get; set; }

    /// <summary>
    /// Gets or sets the target type (User or Organization).
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// DTO for GitHub Account (User or Organization) information from the API.
/// </summary>
public sealed class GitHubApiAccountDto
{
    /// <summary>
    /// Gets or sets the account ID.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the account login (username).
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account type (User or Organization).
    /// </summary>
    public string Type { get; set; } = string.Empty;

#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility
    /// <summary>
    /// Gets or sets the avatar URL.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the HTML URL for the account.
    /// </summary>
    public string? HtmlUrl { get; set; }
#pragma warning restore CA1056
}

/// <summary>
/// DTO for GitHub Repository information from the API.
/// Maps to the response from GET /repositories or /installation/repositories endpoints.
/// </summary>
public sealed class GitHubApiRepositoryDto
{
    /// <summary>
    /// Gets or sets the repository ID.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full repository name (owner/repo).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the repository owner.
    /// </summary>
    public GitHubApiAccountDto? Owner { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the repository is private.
    /// </summary>
    public bool Private { get; set; }

    /// <summary>
    /// Gets or sets the repository description.
    /// </summary>
    public string? Description { get; set; }

#pragma warning disable CA1056 // URI properties should be strings for JSON serialization compatibility
    /// <summary>
    /// Gets or sets the HTML URL for the repository.
    /// </summary>
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API URL for the repository.
    /// </summary>
    public string Url { get; set; } = string.Empty;
#pragma warning restore CA1056

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last push timestamp.
    /// </summary>
    public DateTimeOffset? PushedAt { get; set; }
}

/// <summary>
/// DTO for GitHub Installation Access Token response.
/// Maps to the response from POST /app/installations/{installation_id}/access_tokens.
/// </summary>
public sealed class GitHubApiAccessTokenDto
{
    /// <summary>
    /// Gets or sets the access token string.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration timestamp.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

#pragma warning disable CA2227 // Required for JSON deserialization
    /// <summary>
    /// Gets or sets the permissions granted to this token.
    /// </summary>
    public Dictionary<string, string>? Permissions { get; set; }
#pragma warning restore CA2227

    /// <summary>
    /// Gets or sets the repository selection (all or selected).
    /// </summary>
    public string? RepositorySelection { get; set; }
}
