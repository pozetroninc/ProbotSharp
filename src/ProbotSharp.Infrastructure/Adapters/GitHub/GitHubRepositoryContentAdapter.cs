using System.Text;

using Microsoft.Extensions.Logging;

using Octokit;

using ProbotSharp.Application.Models;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Abstractions;

#pragma warning disable CA1848 // Performance: LoggerMessage delegates - not performance-critical for this codebase

namespace ProbotSharp.Infrastructure.Adapters.GitHub;

/// <summary>
/// GitHub adapter for fetching repository file content.
/// Uses Octokit with installation token authentication.
/// </summary>
public sealed class GitHubRepositoryContentAdapter : IRepositoryContentPort
{
    private readonly IInstallationAuthenticationPort _installationAuth;
    private readonly ILogger<GitHubRepositoryContentAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubRepositoryContentAdapter"/> class.
    /// </summary>
    /// <param name="installationAuth">The installation authentication port for obtaining GitHub access tokens.</param>
    /// <param name="logger">The logger for recording operations and errors.</param>
    public GitHubRepositoryContentAdapter(
        IInstallationAuthenticationPort installationAuth,
        ILogger<GitHubRepositoryContentAdapter> logger)
    {
        this._installationAuth = installationAuth;
        this._logger = logger;
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
            var authResult = await this._installationAuth.AuthenticateAsync(authCommand, cancellationToken).ConfigureAwait(false);

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

            this._logger.LogDebug(
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
                        path.Ref).ConfigureAwait(false);
                }
                else
                {
                    contents = await client.Repository.Content.GetAllContents(
                        path.Owner,
                        path.Repository,
                        path.Path).ConfigureAwait(false);
                }
            }
            catch (NotFoundException)
            {
                this._logger.LogDebug(
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
                fileContent = await httpClient.GetStringAsync(content.DownloadUrl, cancellationToken).ConfigureAwait(false);
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

            this._logger.LogDebug(
                "Successfully fetched content: {Owner}/{Repository}/{Path} (SHA: {Sha})",
                path.Owner, path.Repository, path.Path, content.Sha);

            return Result<RepositoryConfigData>.Success(configData);
        }
        catch (ApiException ex)
        {
            this._logger.LogError(
                ex,
                "GitHub API error fetching content: {Owner}/{Repository}/{Path}",
                path.Owner, path.Repository, path.Path);

            return Result<RepositoryConfigData>.Failure(
                "External", $"GitHub API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            this._logger.LogError(
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
            var authResult = await this._installationAuth.AuthenticateAsync(authCommand, cancellationToken).ConfigureAwait(false);

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
                    path.Ref).ConfigureAwait(false);
            }
            else
            {
                await client.Repository.Content.GetAllContents(
                    path.Owner,
                    path.Repository,
                    path.Path).ConfigureAwait(false);
            }

            return true;
        }
        catch (NotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(
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
            var authResult = await this._installationAuth.AuthenticateAsync(authCommand, cancellationToken).ConfigureAwait(false);

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

            this._logger.LogDebug(
                "Fetching default branch for {Owner}/{Repository}",
                owner, repository);

            var repo = await client.Repository.Get(owner, repository).ConfigureAwait(false);

            if (string.IsNullOrEmpty(repo.DefaultBranch))
            {
                return Result<string>.Failure(
                    "Validation", $"Repository {owner}/{repository} has no default branch");
            }

            this._logger.LogDebug(
                "Default branch for {Owner}/{Repository}: {Branch}",
                owner, repository, repo.DefaultBranch);

            return Result<string>.Success(repo.DefaultBranch);
        }
        catch (ApiException ex)
        {
            this._logger.LogError(
                ex,
                "GitHub API error fetching default branch: {Owner}/{Repository}",
                owner, repository);

            return Result<string>.Failure(
                "External", $"GitHub API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Unexpected error fetching default branch: {Owner}/{Repository}",
                owner, repository);

            return Result<string>.Failure(
                "Internal", $"Failed to fetch default branch: {ex.Message}");
        }
    }
}

#pragma warning restore CA1848
