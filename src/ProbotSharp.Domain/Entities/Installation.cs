// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Collections.Generic;

using ProbotSharp.Domain.Abstractions;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Entities;

public sealed class Installation : Entity<InstallationId>
{
    private readonly List<Repository> _repositories = new();

    private Installation(InstallationId id, string accountLogin)
        : base(id)
    {
        this.AccountLogin = accountLogin;
    }

    public string AccountLogin { get; private set; }

    public IReadOnlyCollection<Repository> Repositories => this._repositories.AsReadOnly();

    public static Installation Create(InstallationId id, string accountLogin)
    {
        if (string.IsNullOrWhiteSpace(accountLogin))
        {
            throw new ArgumentException("Account login cannot be null or whitespace.", nameof(accountLogin));
        }

        return new Installation(id, accountLogin.Trim());
    }

    internal static Installation Restore(InstallationId id, string accountLogin, IEnumerable<Repository> repositories)
    {
        var installation = new Installation(id, accountLogin);
        foreach (var repository in repositories)
        {
            installation._repositories.Add(repository);
        }

        return installation;
    }

    public void UpdateAccountLogin(string accountLogin)
    {
        if (string.IsNullOrWhiteSpace(accountLogin))
        {
            throw new ArgumentException("Account login cannot be null or whitespace.", nameof(accountLogin));
        }

        this.AccountLogin = accountLogin.Trim();
    }

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

