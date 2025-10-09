namespace ProbotSharp.Shared.DTOs;

/// <summary>
/// DTO for GitHub App information from the API.
/// Maps to the response from GET /app endpoint.
/// </summary>
public sealed class GitHubApiAppDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ExternalUrl { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// DTO for GitHub Installation information from the API.
/// Maps to the response from GET /app/installations endpoint.
/// </summary>
public sealed class GitHubApiInstallationDto
{
    public long Id { get; set; }
    public GitHubApiAccountDto? Account { get; set; }
    public string AccessTokensUrl { get; set; } = string.Empty;
    public string RepositoriesUrl { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public long AppId { get; set; }
    public long? TargetId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// DTO for GitHub Account (User or Organization) information from the API.
/// </summary>
public sealed class GitHubApiAccountDto
{
    public long Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? HtmlUrl { get; set; }
}

/// <summary>
/// DTO for GitHub Repository information from the API.
/// Maps to the response from GET /repositories or /installation/repositories endpoints.
/// </summary>
public sealed class GitHubApiRepositoryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public GitHubApiAccountDto? Owner { get; set; }
    public bool Private { get; set; }
    public string? Description { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PushedAt { get; set; }
}

/// <summary>
/// DTO for GitHub Installation Access Token response.
/// Maps to the response from POST /app/installations/{installation_id}/access_tokens.
/// </summary>
public sealed class GitHubApiAccessTokenDto
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public Dictionary<string, string>? Permissions { get; set; }
    public string? RepositorySelection { get; set; }
}
