using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.DTOs;

namespace ProbotSharp.Shared.Mapping;

/// <summary>
/// Extension methods for mapping GitHub API DTOs to domain entities.
/// These mappers handle the anti-corruption layer between GitHub's API and our domain model.
/// </summary>
public static class GitHubApiMappingExtensions
{
    /// <summary>
    /// Maps a GitHub API Repository DTO to a domain Repository entity.
    /// </summary>
    /// <param name="apiRepo">The GitHub API repository DTO.</param>
    /// <returns>A Repository domain entity.</returns>
    public static Repository ToRepository(this GitHubApiRepositoryDto apiRepo)
    {
        ArgumentNullException.ThrowIfNull(apiRepo);

        return Repository.Create(
            id: apiRepo.Id,
            name: apiRepo.Name,
            fullName: apiRepo.FullName
        );
    }

    /// <summary>
    /// Maps a GitHub API Installation DTO to a domain Installation entity.
    /// </summary>
    /// <param name="apiInstallation">The GitHub API installation DTO.</param>
    /// <returns>An Installation domain entity.</returns>
    public static Installation ToInstallation(this GitHubApiInstallationDto apiInstallation)
    {
        ArgumentNullException.ThrowIfNull(apiInstallation);

        var installationId = InstallationId.Create(apiInstallation.Id);
        var accountLogin = apiInstallation.Account?.Login ?? "unknown";

        return Installation.Create(installationId, accountLogin);
    }

    /// <summary>
    /// Maps a GitHub API Access Token DTO to a domain InstallationAccessToken value object.
    /// </summary>
    /// <param name="apiToken">The GitHub API access token DTO.</param>
    /// <returns>An InstallationAccessToken value object.</returns>
    public static InstallationAccessToken ToInstallationAccessToken(this GitHubApiAccessTokenDto apiToken)
    {
        ArgumentNullException.ThrowIfNull(apiToken);

        return InstallationAccessToken.Create(
            value: apiToken.Token,
            expiresAt: apiToken.ExpiresAt
        );
    }

    /// <summary>
    /// Maps a collection of GitHub API Repository DTOs to domain Repository entities.
    /// </summary>
    /// <param name="apiRepos">The collection of GitHub API repository DTOs.</param>
    /// <returns>A list of Repository domain entities.</returns>
    public static List<Repository> ToRepositories(this IEnumerable<GitHubApiRepositoryDto> apiRepos)
    {
        ArgumentNullException.ThrowIfNull(apiRepos);
        return apiRepos.Select(r => r.ToRepository()).ToList();
    }

    /// <summary>
    /// Maps a collection of GitHub API Installation DTOs to domain Installation entities.
    /// </summary>
    /// <param name="apiInstallations">The collection of GitHub API installation DTOs.</param>
    /// <returns>A list of Installation domain entities.</returns>
    public static List<Installation> ToInstallations(this IEnumerable<GitHubApiInstallationDto> apiInstallations)
    {
        ArgumentNullException.ThrowIfNull(apiInstallations);
        return apiInstallations.Select(i => i.ToInstallation()).ToList();
    }

    /// <summary>
    /// Extracts the installation ID from a GitHub webhook payload.
    /// </summary>
    /// <param name="payload">The webhook payload.</param>
    /// <returns>The installation ID if present, null otherwise.</returns>
    public static InstallationId? ExtractInstallationId(this WebhookPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.RootElement.TryGetProperty("installation", out var installation))
        {
            if (installation.TryGetProperty("id", out var id))
            {
                return InstallationId.Create(id.GetInt64());
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the repository information from a GitHub webhook payload.
    /// </summary>
    /// <param name="payload">The webhook payload.</param>
    /// <returns>The repository if present, null otherwise.</returns>
    public static Repository? ExtractRepository(this WebhookPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.RootElement.TryGetProperty("repository", out var repo))
        {
            if (repo.TryGetProperty("id", out var id) &&
                repo.TryGetProperty("name", out var name) &&
                repo.TryGetProperty("full_name", out var fullName))
            {
                var nameValue = name.GetString();
                var fullNameValue = fullName.GetString();

                // Repository.Create validates that name and fullName cannot be null or empty
                if (string.IsNullOrEmpty(nameValue) || string.IsNullOrEmpty(fullNameValue))
                {
                    return null;
                }

                return Repository.Create(
                    id: id.GetInt64(),
                    name: nameValue,
                    fullName: fullNameValue
                );
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the sender login from a GitHub webhook payload.
    /// </summary>
    /// <param name="payload">The webhook payload.</param>
    /// <returns>The sender login if present, null otherwise.</returns>
    public static string? ExtractSenderLogin(this WebhookPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.RootElement.TryGetProperty("sender", out var sender))
        {
            if (sender.TryGetProperty("login", out var login))
            {
                return login.GetString();
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the action from a GitHub webhook payload.
    /// </summary>
    /// <param name="payload">The webhook payload.</param>
    /// <returns>The action if present, null otherwise.</returns>
    public static string? ExtractAction(this WebhookPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.RootElement.TryGetProperty("action", out var action))
        {
            return action.GetString();
        }

        return null;
    }
}
