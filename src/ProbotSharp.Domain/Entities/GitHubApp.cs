// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Collections.Generic;

using ProbotSharp.Domain.Abstractions;
using ProbotSharp.Domain.Events;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Entities;

/// <summary>
/// Represents a GitHub App entity.
/// </summary>
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

    /// <summary>
    /// Gets the name of the GitHub App.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the private key used for authentication.
    /// </summary>
    public PrivateKeyPem PrivateKey { get; private set; }

    /// <summary>
    /// Gets the webhook secret used for signature validation.
    /// </summary>
    public string WebhookSecret { get; private set; }

    /// <summary>
    /// Gets the collection of installations for this app.
    /// </summary>
    public IReadOnlyCollection<Installation> Installations => this._installations.AsReadOnly();

    /// <summary>
    /// Creates a new GitHub App instance.
    /// </summary>
    /// <param name="id">The GitHub App ID.</param>
    /// <param name="name">The name of the app.</param>
    /// <param name="privateKey">The private key for authentication.</param>
    /// <param name="webhookSecret">The webhook secret for signature validation.</param>
    /// <returns>A new GitHub App instance.</returns>
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

    /// <summary>
    /// Restores a GitHub App instance from persistence.
    /// </summary>
    /// <param name="id">The GitHub App ID.</param>
    /// <param name="name">The name of the app.</param>
    /// <param name="privateKey">The private key for authentication.</param>
    /// <param name="webhookSecret">The webhook secret for signature validation.</param>
    /// <param name="installations">The collection of installations.</param>
    /// <returns>A restored GitHub App instance.</returns>
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

    /// <summary>
    /// Adds a new installation to this GitHub App.
    /// </summary>
    /// <param name="installationId">The installation ID.</param>
    /// <param name="accountLogin">The account login.</param>
    /// <returns>The newly created installation.</returns>
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

    /// <summary>
    /// Removes an installation from this GitHub App.
    /// </summary>
    /// <param name="installationId">The installation ID to remove.</param>
    public void RemoveInstallation(InstallationId installationId)
    {
        var installation = this._installations.SingleOrDefault(i => i.Id == installationId);
        if (installation is null)
        {
            throw new InvalidOperationException($"Installation {installationId} does not exist.");
        }

        this._installations.Remove(installation);
    }

    /// <summary>
    /// Updates the private key for this GitHub App.
    /// </summary>
    /// <param name="privateKey">The new private key.</param>
    public void UpdatePrivateKey(PrivateKeyPem privateKey)
    {
        this.PrivateKey = privateKey;
    }

    /// <summary>
    /// Updates the webhook secret for this GitHub App.
    /// </summary>
    /// <param name="webhookSecret">The new webhook secret.</param>
    public void UpdateWebhookSecret(string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new ArgumentException("Webhook secret cannot be null or whitespace.", nameof(webhookSecret));
        }

        this.WebhookSecret = webhookSecret.Trim();
    }

    /// <summary>
    /// Renames this GitHub App.
    /// </summary>
    /// <param name="name">The new name.</param>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("App name cannot be null or whitespace.", nameof(name));
        }

        this.Name = name.Trim();
    }
}
