namespace ProbotSharp.Shared.DTOs;

/// <summary>
/// Data transfer object representing a GitHub repository.
/// </summary>
public sealed class RepositoryDto
{
    /// <summary>
    /// The unique identifier of the repository.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The name of the repository.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The full name of the repository (e.g., "owner/repo").
    /// </summary>
    public string FullName { get; set; } = string.Empty;
}
