// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Tests.Entities;

public class GitHubAppTests
{
    private static GitHubApp CreateApp()
    {
        var id = GitHubAppId.Create(1);
        var pem = PrivateKeyPem.Create(GeneratePem());
        return GitHubApp.Create(id, "My App", pem, "secret");
    }

    [Fact]
    public void Create_WithValidParameters_ShouldInitializeProperties()
    {
        var pem = PrivateKeyPem.Create(GeneratePem());

        var app = GitHubApp.Create(GitHubAppId.Create(123), "Test App", pem, "secret");

        app.Name.Should().Be("Test App");
        app.PrivateKey.Should().Be(pem);
        app.WebhookSecret.Should().Be("secret");
    }

    [Fact]
    public void AddInstallation_WithNewInstallation_ShouldAddToCollection()
    {
        var app = CreateApp();

        var installation = app.AddInstallation(InstallationId.Create(10), "octocat");

        installation.AccountLogin.Should().Be("octocat");
        app.Installations.Should().Contain(installation);
    }

    [Fact]
    public void AddInstallation_WithExistingInstallation_ShouldThrow()
    {
        var app = CreateApp();
        app.AddInstallation(InstallationId.Create(10), "octocat");

        var act = () => app.AddInstallation(InstallationId.Create(10), "octocat");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveInstallation_WhenExists_ShouldRemove()
    {
        var app = CreateApp();
        var installation = app.AddInstallation(InstallationId.Create(10), "octocat");

        app.RemoveInstallation(installation.Id);

        app.Installations.Should().BeEmpty();
    }

    [Fact]
    public void RemoveInstallation_WhenMissing_ShouldThrow()
    {
        var app = CreateApp();

        var act = () => app.RemoveInstallation(InstallationId.Create(10));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rename_WithValidName_ShouldUpdateName()
    {
        var app = CreateApp();

        app.Rename("New Name");

        app.Name.Should().Be("New Name");
    }

    [Fact]
    public void UpdateWebhookSecret_WithValidSecret_ShouldUpdate()
    {
        var app = CreateApp();

        app.UpdateWebhookSecret("new-secret");

        app.WebhookSecret.Should().Be("new-secret");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrow(string name)
    {
        var pem = PrivateKeyPem.Create(GeneratePem());

        var act = () => GitHubApp.Create(GitHubAppId.Create(123), name!, pem, "secret");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidWebhookSecret_ShouldThrow(string webhookSecret)
    {
        var pem = PrivateKeyPem.Create(GeneratePem());

        var act = () => GitHubApp.Create(GitHubAppId.Create(123), "Test App", pem, webhookSecret!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*webhook secret*");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldTrim()
    {
        var pem = PrivateKeyPem.Create(GeneratePem());

        var app = GitHubApp.Create(GitHubAppId.Create(123), "  Test App  ", pem, "  secret  ");

        app.Name.Should().Be("Test App");
        app.WebhookSecret.Should().Be("secret");
    }

    [Fact]
    public void Create_ShouldRaiseDomainEvent()
    {
        var pem = PrivateKeyPem.Create(GeneratePem());
        var id = GitHubAppId.Create(123);

        var app = GitHubApp.Create(id, "Test App", pem, "secret");

        app.DomainEvents.Should().HaveCount(1);
        app.DomainEvents.First().Should().BeOfType<ProbotSharp.Domain.Events.GitHubAppCreatedDomainEvent>();
    }

    [Fact]
    public void AddInstallation_ShouldRaiseDomainEvent()
    {
        var app = CreateApp();
        app.ClearDomainEvents();

        app.AddInstallation(InstallationId.Create(10), "octocat");

        app.DomainEvents.Should().HaveCount(1);
        app.DomainEvents.First().Should().BeOfType<ProbotSharp.Domain.Events.InstallationAddedDomainEvent>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithInvalidName_ShouldThrow(string name)
    {
        var app = CreateApp();

        var act = () => app.Rename(name!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void Rename_WithWhitespace_ShouldTrim()
    {
        var app = CreateApp();

        app.Rename("  New Name  ");

        app.Name.Should().Be("New Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateWebhookSecret_WithInvalidSecret_ShouldThrow(string webhookSecret)
    {
        var app = CreateApp();

        var act = () => app.UpdateWebhookSecret(webhookSecret!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*webhook secret*");
    }

    [Fact]
    public void UpdateWebhookSecret_WithWhitespace_ShouldTrim()
    {
        var app = CreateApp();

        app.UpdateWebhookSecret("  new-secret  ");

        app.WebhookSecret.Should().Be("new-secret");
    }

    [Fact]
    public void UpdatePrivateKey_ShouldUpdateKey()
    {
        var app = CreateApp();
        var newPem = PrivateKeyPem.Create(GeneratePem());

        app.UpdatePrivateKey(newPem);

        app.PrivateKey.Should().Be(newPem);
    }

    [Fact]
    public void Installations_ShouldBeReadOnly()
    {
        var app = CreateApp();

        app.Installations.Should().BeAssignableTo<IReadOnlyCollection<Installation>>();
    }

    private static string GeneratePem()
    {
        using var rsa = RSA.Create(2048);
        var pkcs8 = rsa.ExportPkcs8PrivateKey();
        var base64 = Convert.ToBase64String(pkcs8, Base64FormattingOptions.InsertLineBreaks);
        return $@"-----BEGIN PRIVATE KEY-----
{base64}
-----END PRIVATE KEY-----";
    }
}

