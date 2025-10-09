// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Collections.Generic;

using ProbotSharp.Domain.Abstractions;
using ProbotSharp.Domain.Events;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Entities;

public sealed class GitHubApp : AggregateRoot<GitHubAppId>
{
    private readonly List<Installation> _installations = new();

    private GitHubApp(
        GitHubAppId id,
        string name,
        PrivateKeyPem privateKey,
        string webhookSecret)
        : base(id)
    {
        this.Name = name;
        this.PrivateKey = privateKey;
        this.WebhookSecret = webhookSecret;
    }

    public string Name { get; private set; }

    public PrivateKeyPem PrivateKey { get; private set; }

    public string WebhookSecret { get; private set; }

    public IReadOnlyCollection<Installation> Installations => this._installations.AsReadOnly();

    public static GitHubApp Create(
        GitHubAppId id,
        string name,
        PrivateKeyPem privateKey,
        string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("App name cannot be null or whitespace.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new ArgumentException("Webhook secret cannot be null or whitespace.", nameof(webhookSecret));
        }

        var app = new GitHubApp(id, name.Trim(), privateKey, webhookSecret.Trim());
        app.RaiseDomainEvent(new GitHubAppCreatedDomainEvent(id, app.Name));
        return app;
    }

    internal static GitHubApp Restore(
        GitHubAppId id,
        string name,
        PrivateKeyPem privateKey,
        string webhookSecret,
        IEnumerable<Installation> installations)
    {
        var app = new GitHubApp(id, name, privateKey, webhookSecret);
        _ = installations ?? throw new ArgumentNullException(nameof(installations));

        foreach (var installation in installations)
        {
            app._installations.Add(installation);
        }

        return app;
    }

    public Installation AddInstallation(InstallationId installationId, string accountLogin)
    {
        if (this._installations.Any(i => i.Id == installationId))
        {
            throw new InvalidOperationException($"Installation {installationId} already exists.");
        }

        var installation = Installation.Create(installationId, accountLogin);
        this._installations.Add(installation);
        this.RaiseDomainEvent(new InstallationAddedDomainEvent(this.Id, installationId, installation.AccountLogin));

        return installation;
    }

    public void RemoveInstallation(InstallationId installationId)
    {
        var installation = this._installations.SingleOrDefault(i => i.Id == installationId);
        if (installation is null)
        {
            throw new InvalidOperationException($"Installation {installationId} does not exist.");
        }

        this._installations.Remove(installation);
    }

    public void UpdatePrivateKey(PrivateKeyPem privateKey)
    {
        this.PrivateKey = privateKey;
    }

    public void UpdateWebhookSecret(string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new ArgumentException("Webhook secret cannot be null or whitespace.", nameof(webhookSecret));
        }

        this.WebhookSecret = webhookSecret.Trim();
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("App name cannot be null or whitespace.", nameof(name));
        }

        this.Name = name.Trim();
    }
}

