// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Domain.Context;

/// <summary>
/// Represents GitHub App installation information extracted from a webhook payload.
/// </summary>
public sealed class InstallationInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InstallationInfo"/> class.
    /// </summary>
    /// <param name="id">The installation ID.</param>
    /// <param name="accountLogin">The account login associated with the installation.</param>
    public InstallationInfo(long id, string accountLogin)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Installation ID must be positive");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(accountLogin);

        this.Id = id;
        this.AccountLogin = accountLogin;
    }

    /// <summary>
    /// Gets the installation ID.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Gets the account login associated with the installation.
    /// </summary>
    public string AccountLogin { get; }
}
