namespace ProbotSharp.Shared.DTOs;

/// <summary>
/// Data transfer object representing a GitHub App installation.
/// </summary>
public sealed class InstallationDto
{
    /// <summary>
    /// The unique identifier of the installation.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The account login (username or organization name) where the app is installed.
    /// </summary>
    public string AccountLogin { get; set; } = string.Empty;

    /// <summary>
    /// The repositories associated with this installation.
    /// </summary>
    public List<RepositoryDto> Repositories { get; set; } = new();
}
