using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Application.Ports.Outbound;

/// <summary>
/// Outbound port for fetching repository file content from GitHub.
/// Abstraction over GitHub REST/GraphQL APIs for repository content access.
/// </summary>
public interface IRepositoryContentPort
{
    /// <summary>
    /// Gets the raw content of a file from a repository.
    /// </summary>
    /// <param name="path">Configuration path specifying owner/repo/file.</param>
    /// <param name="installationId">GitHub App installation ID for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Success with RepositoryConfigData if file exists,
    /// Failure with NotFound error if file doesn't exist,
    /// Failure with other errors for API failures.
    /// </returns>
    Task<Result<RepositoryConfigData>> GetFileContentAsync(
        RepositoryConfigPath path,
        long installationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in a repository without fetching content.
    /// </summary>
    /// <param name="path">Configuration path specifying owner/repo/file.</param>
    /// <param name="installationId">GitHub App installation ID for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if file exists, false otherwise.</returns>
    Task<bool> FileExistsAsync(
        RepositoryConfigPath path,
        long installationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default branch for a repository.
    /// Useful for resolving configuration when no ref is specified.
    /// </summary>
    /// <param name="owner">Repository owner.</param>
    /// <param name="repository">Repository name.</param>
    /// <param name="installationId">GitHub App installation ID for authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Default branch name (e.g., "main", "master").</returns>
    Task<Result<string>> GetDefaultBranchAsync(
        string owner,
        string repository,
        long installationId,
        CancellationToken cancellationToken = default);
}
