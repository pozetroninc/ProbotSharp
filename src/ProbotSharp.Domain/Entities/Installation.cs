// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Collections.Generic;

using ProbotSharp.Domain.Abstractions;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Entities;

/// <summary>
/// Represents a GitHub App installation on an account.
/// </summary>
public sealed class Installation : Entity<InstallationId>
{
    private readonly List<Repository> _repositories = new();

    private Installation(InstallationId id, string accountLogin)
        : base(id)
    {
        this.AccountLogin = accountLogin;
    }

    /// <summary>
    /// Gets the account login for this installation.
    /// </summary>
    public string AccountLogin { get; private set; }

    /// <summary>
    /// Gets the collection of repositories for this installation.
    /// </summary>
    public IReadOnlyCollection<Repository> Repositories => this._repositories.AsReadOnly();

    /// <summary>
    /// Creates a new installation instance.
    /// </summary>
    /// <param name="id">The installation ID.</param>
    /// <param name="accountLogin">The account login.</param>
    /// <returns>A new installation instance.</returns>
    public static Installation Create(InstallationId id, string accountLogin)
    {
        if (string.IsNullOrWhiteSpace(accountLogin))
        {
            throw new ArgumentException("Account login cannot be null or whitespace.", nameof(accountLogin));
        }

        return new Installation(id, accountLogin.Trim());
    }

    /// <summary>
    /// Restores an installation instance from persistence.
    /// </summary>
    /// <param name="id">The installation ID.</param>
    /// <param name="accountLogin">The account login.</param>
    /// <param name="repositories">The collection of repositories.</param>
    /// <returns>A restored installation instance.</returns>
    internal static Installation Restore(InstallationId id, string accountLogin, IEnumerable<Repository> repositories)
    {
        var installation = new Installation(id, accountLogin);
        foreach (var repository in repositories)
        {
            installation._repositories.Add(repository);
        }

        return installation;
    }

    /// <summary>
    /// Updates the account login for this installation.
    /// </summary>
    /// <param name="accountLogin">The new account login.</param>
    public void UpdateAccountLogin(string accountLogin)
    {
        if (string.IsNullOrWhiteSpace(accountLogin))
        {
            throw new ArgumentException("Account login cannot be null or whitespace.", nameof(accountLogin));
        }

        this.AccountLogin = accountLogin.Trim();
    }

    /// <summary>
    /// Adds a repository to this installation.
    /// </summary>
    /// <param name="repositoryId">The repository ID.</param>
    /// <param name="name">The repository name.</param>
    /// <param name="fullName">The full repository name.</param>
    /// <returns>The newly created repository.</returns>
    public Repository AddRepository(long repositoryId, string name, string fullName)
    {
        if (this._repositories.Any(r => r.Id == repositoryId))
        {
            throw new InvalidOperationException($"Repository {repositoryId} already exists for installation {this.Id}.");
        }

        var repository = Repository.Create(repositoryId, name, fullName);
        this._repositories.Add(repository);
        return repository;
    }

    /// <summary>
    /// Removes a repository from this installation.
    /// </summary>
    /// <param name="repositoryId">The repository ID to remove.</param>
    public void RemoveRepository(long repositoryId)
    {
        var repository = this._repositories.SingleOrDefault(r => r.Id == repositoryId);
        if (repository is null)
        {
            throw new InvalidOperationException($"Repository {repositoryId} not found for installation {this.Id}.");
        }

        this._repositories.Remove(repository);
    }
}
