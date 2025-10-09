using System.Text;
using Microsoft.Extensions.Logging;
using Octokit;
using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Adapters.GitHub;

/// <summary>
/// GitHub adapter for fetching repository file content.
/// Uses Octokit with installation token authentication.
/// </summary>
public sealed class GitHubRepositoryContentAdapter : IRepositoryContentPort
{
    private readonly IInstallationAuthenticationPort _installationAuth;
    private readonly ILogger<GitHubRepositoryContentAdapter> _logger;

    public GitHubRepositoryContentAdapter(
        IInstallationAuthenticationPort installationAuth,
        ILogger<GitHubRepositoryContentAdapter> logger)
    {
        _installationAuth = installationAuth;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<RepositoryConfigData>> GetFileContentAsync(
        RepositoryConfigPath path,
        long installationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Authenticate installation
            var authCommand = new AuthenticateInstallationCommand(Domain.ValueObjects.InstallationId.Create(installationId));
            var authResult = await _installationAuth.AuthenticateAsync(authCommand, cancellationToken);

            if (!authResult.IsSuccess || authResult.Value == null)
            {
                var errorMessage = authResult.Error?.Message ?? "Failed to authenticate installation";
                return Result<RepositoryConfigData>.Failure("Authentication", errorMessage);
            }

            var token = authResult.Value;

            // Create GitHub client with installation token
            var client = new GitHubClient(new ProductHeaderValue("ProbotSharp"))
            {
                Credentials = new Credentials(token.Value),
            };

            _logger.LogDebug(
                "Fetching repository content: {Owner}/{Repository}/{Path} (ref: {Ref})",
                path.Owner, path.Repository, path.Path, path.Ref ?? "default");

            // Get file content
            IReadOnlyList<RepositoryContent> contents;

            try
            {
                if (path.Ref != null)
                {
                    contents = await client.Repository.Content.GetAllContentsByRef(
                        path.Owner,
                        path.Repository,
                        path.Path,
                        path.Ref);
                }
                else
                {
                    contents = await client.Repository.Content.GetAllContents(
                        path.Owner,
                        path.Repository,
                        path.Path);
                }
            }
            catch (NotFoundException)
            {
                _logger.LogDebug(
                    "File not found: {Owner}/{Repository}/{Path}",
                    path.Owner, path.Repository, path.Path);

                return Result<RepositoryConfigData>.Failure(
                    "NotFound", $"File not found: {path.GetFullPath()}");
            }

            if (contents.Count == 0)
            {
                return Result<RepositoryConfigData>.Failure(
                    "NotFound", $"No content returned for: {path.GetFullPath()}");
            }

            var content = contents[0];

            // Ensure it's a file, not a directory
            if (content.Type != ContentType.File)
            {
                return Result<RepositoryConfigData>.Failure(
                    "Validation", $"Path is not a file: {path.GetFullPath()}");
            }

            // Decode content
            string fileContent;
            if (!string.IsNullOrEmpty(content.Content))
            {
                // Content is base64 encoded
                fileContent = content.Content;
            }
            else if (!string.IsNullOrEmpty(content.DownloadUrl))
            {
                // For large files, download via URL
                using var httpClient = new HttpClient();
                fileContent = await httpClient.GetStringAsync(content.DownloadUrl, cancellationToken);
            }
            else
            {
                return Result<RepositoryConfigData>.Failure(
                    "Validation", $"No content available for: {path.GetFullPath()}");
            }

            var configData = RepositoryConfigData.Create(
                fileContent,
                content.Sha,
                path);

            _logger.LogDebug(
                "Successfully fetched content: {Owner}/{Repository}/{Path} (SHA: {Sha})",
                path.Owner, path.Repository, path.Path, content.Sha);

            return Result<RepositoryConfigData>.Success(configData);
        }
        catch (ApiException ex)
        {
            _logger.LogError(
                ex,
                "GitHub API error fetching content: {Owner}/{Repository}/{Path}",
                path.Owner, path.Repository, path.Path);

            return Result<RepositoryConfigData>.Failure(
                "External", $"GitHub API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error fetching content: {Owner}/{Repository}/{Path}",
                path.Owner, path.Repository, path.Path);

            return Result<RepositoryConfigData>.Failure(
                "Internal", $"Failed to fetch content: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(
        RepositoryConfigPath path,
        long installationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Authenticate installation
            var authCommand = new AuthenticateInstallationCommand(Domain.ValueObjects.InstallationId.Create(installationId));
            var authResult = await _installationAuth.AuthenticateAsync(authCommand, cancellationToken);

            if (!authResult.IsSuccess || authResult.Value == null)
            {
                return false;
            }

            var token = authResult.Value;

            // Create GitHub client with installation token
            var client = new GitHubClient(new ProductHeaderValue("ProbotSharp"))
            {
                Credentials = new Credentials(token.Value),
            };

            if (path.Ref != null)
            {
                await client.Repository.Content.GetAllContentsByRef(
                    path.Owner,
                    path.Repository,
                    path.Path,
                    path.Ref);
            }
            else
            {
                await client.Repository.Content.GetAllContents(
                    path.Owner,
                    path.Repository,
                    path.Path);
            }

            return true;
        }
        catch (NotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error checking file existence: {Owner}/{Repository}/{Path}",
                path.Owner, path.Repository, path.Path);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetDefaultBranchAsync(
        string owner,
        string repository,
        long installationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Authenticate installation
            var authCommand = new AuthenticateInstallationCommand(Domain.ValueObjects.InstallationId.Create(installationId));
            var authResult = await _installationAuth.AuthenticateAsync(authCommand, cancellationToken);

            if (!authResult.IsSuccess || authResult.Value == null)
            {
                var errorMessage = authResult.Error?.Message ?? "Failed to authenticate installation";
                return Result<string>.Failure("Authentication", errorMessage);
            }

            var token = authResult.Value;

            // Create GitHub client with installation token
            var client = new GitHubClient(new ProductHeaderValue("ProbotSharp"))
            {
                Credentials = new Credentials(token.Value),
            };

            _logger.LogDebug(
                "Fetching default branch for {Owner}/{Repository}",
                owner, repository);

            var repo = await client.Repository.Get(owner, repository);

            if (string.IsNullOrEmpty(repo.DefaultBranch))
            {
                return Result<string>.Failure(
                    "Validation", $"Repository {owner}/{repository} has no default branch");
            }

            _logger.LogDebug(
                "Default branch for {Owner}/{Repository}: {Branch}",
                owner, repository, repo.DefaultBranch);

            return Result<string>.Success(repo.DefaultBranch);
        }
        catch (ApiException ex)
        {
            _logger.LogError(
                ex,
                "GitHub API error fetching default branch: {Owner}/{Repository}",
                owner, repository);

            return Result<string>.Failure(
                "External", $"GitHub API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error fetching default branch: {Owner}/{Repository}",
                owner, repository);

            return Result<string>.Failure(
                "Internal", $"Failed to fetch default branch: {ex.Message}");
        }
    }
}
