// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using FluentAssertions;

using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.Mapping;

using Xunit;

namespace ProbotSharp.Shared.Tests.Mapping;

public sealed class ValueObjectMappingHelpersTests
{
    #region GitHubAppId Tests

    [Fact]
    public void GitHubAppId_ToPrimitive_WithValidAppId_ShouldReturnValue()
    {
        // Arrange
        var appId = GitHubAppId.Create(12345);

        // Act
        var result = appId.ToPrimitive();

        // Assert
        result.Should().Be(12345);
    }

    [Fact]
    public void GitHubAppId_ToPrimitive_WithNullAppId_ShouldThrowArgumentNullException()
    {
        // Arrange
        GitHubAppId? appId = null;

        // Act
        var act = () => appId!.ToPrimitive();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GitHubAppId_ToGitHubAppId_WithValidLong_ShouldCreateAppId()
    {
        // Arrange
        long value = 54321;

        // Act
        var result = value.ToGitHubAppId();

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(54321);
    }

    [Fact]
    public void GitHubAppId_ToGitHubAppIdOrNull_WithValidLong_ShouldCreateAppId()
    {
        // Arrange
        long? value = 999;

        // Act
        var result = value.ToGitHubAppIdOrNull();

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(999);
    }

    [Fact]
    public void GitHubAppId_ToGitHubAppIdOrNull_WithNull_ShouldReturnNull()
    {
        // Arrange
        long? value = null;

        // Act
        var result = value.ToGitHubAppIdOrNull();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GitHubAppId_ToGitHubAppIdOrNull_WithZero_ShouldReturnNull()
    {
        // Arrange
        long? value = 0;

        // Act
        var result = value.ToGitHubAppIdOrNull();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GitHubAppId_ToGitHubAppIdOrNull_WithNegative_ShouldReturnNull()
    {
        // Arrange
        long? value = -1;

        // Act
        var result = value.ToGitHubAppIdOrNull();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region InstallationId Tests

    [Fact]
    public void InstallationId_ToPrimitive_WithValidInstallationId_ShouldReturnValue()
    {
        // Arrange
        var installationId = InstallationId.Create(67890);

        // Act
        var result = installationId.ToPrimitive();

        // Assert
        result.Should().Be(67890);
    }

    [Fact]
    public void InstallationId_ToPrimitive_WithNullInstallationId_ShouldThrowArgumentNullException()
    {
        // Arrange
        InstallationId? installationId = null;

        // Act
        var act = () => installationId!.ToPrimitive();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void InstallationId_ToInstallationId_WithValidLong_ShouldCreateInstallationId()
    {
        // Arrange
        long value = 11111;

        // Act
        var result = value.ToInstallationId();

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(11111);
    }

    [Fact]
    public void InstallationId_ToInstallationIdOrNull_WithValidLong_ShouldCreateInstallationId()
    {
        // Arrange
        long? value = 22222;

        // Act
        var result = value.ToInstallationIdOrNull();

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(22222);
    }

    [Fact]
    public void InstallationId_ToInstallationIdOrNull_WithNull_ShouldReturnNull()
    {
        // Arrange
        long? value = null;

        // Act
        var result = value.ToInstallationIdOrNull();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void InstallationId_ToInstallationIdOrNull_WithZero_ShouldReturnNull()
    {
        // Arrange
        long? value = 0;

        // Act
        var result = value.ToInstallationIdOrNull();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeliveryId Tests

    [Fact]
    public void DeliveryId_ToPrimitive_WithValidDeliveryId_ShouldReturnValue()
    {
        // Arrange
        var deliveryId = DeliveryId.Create("abc-123-def");

        // Act
        var result = deliveryId.ToPrimitive();

        // Assert
        result.Should().Be("abc-123-def");
    }

    [Fact]
    public void DeliveryId_ToPrimitive_WithNullDeliveryId_ShouldThrowArgumentNullException()
    {
        // Arrange
        DeliveryId? deliveryId = null;

        // Act
        var act = () => deliveryId!.ToPrimitive();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DeliveryId_ToDeliveryId_WithValidString_ShouldCreateDeliveryId()
    {
        // Arrange
        var value = "delivery-456";

        // Act
        var result = value.ToDeliveryId();

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be("delivery-456");
    }

    [Fact]
    public void DeliveryId_ToDeliveryIdOrNull_WithValidString_ShouldCreateDeliveryId()
    {
        // Arrange
        string? value = "delivery-789";

        // Act
        var result = value.ToDeliveryIdOrNull();

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("delivery-789");
    }

    [Fact]
    public void DeliveryId_ToDeliveryIdOrNull_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.ToDeliveryIdOrNull();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DeliveryId_ToDeliveryIdOrNull_WithEmptyString_ShouldReturnNull()
    {
        // Arrange
        string? value = string.Empty;

        // Act
        var result = value.ToDeliveryIdOrNull();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DeliveryId_ToDeliveryIdOrNull_WithWhitespace_ShouldReturnNull()
    {
        // Arrange
        string? value = "   ";

        // Act
        var result = value.ToDeliveryIdOrNull();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region WebhookEventName Tests

    [Fact]
    public void WebhookEventName_ToPrimitive_WithValidEventName_ShouldReturnValue()
    {
        // Arrange
        var eventName = WebhookEventName.Create("push");

        // Act
        var result = eventName.ToPrimitive();

        // Assert
        result.Should().Be("push");
    }

    [Fact]
    public void WebhookEventName_ToPrimitive_WithNullEventName_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebhookEventName? eventName = null;

        // Act
        var act = () => eventName!.ToPrimitive();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WebhookEventName_ToWebhookEventName_WithValidString_ShouldCreateEventName()
    {
        // Arrange
        var value = "pull_request";

        // Act
        var result = value.ToWebhookEventName();

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be("pull_request");
    }

    [Fact]
    public void WebhookEventName_ToWebhookEventNameOrNull_WithValidString_ShouldCreateEventName()
    {
        // Arrange
        string? value = "issues";

        // Act
        var result = value.ToWebhookEventNameOrNull();

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("issues");
    }

    [Fact]
    public void WebhookEventName_ToWebhookEventNameOrNull_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.ToWebhookEventNameOrNull();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WebhookEventName_ToWebhookEventNameOrNull_WithWhitespace_ShouldReturnNull()
    {
        // Arrange
        string? value = "  ";

        // Act
        var result = value.ToWebhookEventNameOrNull();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region WebhookPayload Tests

    [Fact]
    public void WebhookPayload_ToPrimitive_WithValidPayload_ShouldReturnRawBody()
    {
        // Arrange
        var jsonBody = "{\"action\":\"opened\"}";
        var payload = WebhookPayload.Create(jsonBody);

        // Act
        var result = payload.ToPrimitive();

        // Assert
        result.Should().Be(jsonBody);
    }

    [Fact]
    public void WebhookPayload_ToPrimitive_WithNullPayload_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebhookPayload? payload = null;

        // Act
        var act = () => payload!.ToPrimitive();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WebhookPayload_ToWebhookPayload_WithValidJson_ShouldCreatePayload()
    {
        // Arrange
        var jsonBody = "{\"event\":\"test\"}";

        // Act
        var result = jsonBody.ToWebhookPayload();

        // Assert
        result.Should().NotBeNull();
        result.RawBody.Should().Be(jsonBody);
    }

    [Fact]
    public void WebhookPayload_ToWebhookPayloadOrNull_WithValidJson_ShouldCreatePayload()
    {
        // Arrange
        string? jsonBody = "{\"data\":\"value\"}";

        // Act
        var result = jsonBody.ToWebhookPayloadOrNull();

        // Assert
        result.Should().NotBeNull();
        result!.RawBody.Should().Be(jsonBody);
    }

    [Fact]
    public void WebhookPayload_ToWebhookPayloadOrNull_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? jsonBody = null;

        // Act
        var result = jsonBody.ToWebhookPayloadOrNull();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WebhookPayload_ToWebhookPayloadOrNull_WithEmptyString_ShouldReturnNull()
    {
        // Arrange
        string? jsonBody = string.Empty;

        // Act
        var result = jsonBody.ToWebhookPayloadOrNull();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region WebhookSignature Tests

    [Fact]
    public void WebhookSignature_ToPrimitive_WithValidSignature_ShouldReturnValue()
    {
        // Arrange
        var validSignature = "sha256=" + new string('a', 64); // 64 hex chars
        var signature = WebhookSignature.Create(validSignature);

        // Act
        var result = signature.ToPrimitive();

        // Assert
        result.Should().Be(validSignature);
    }

    [Fact]
    public void WebhookSignature_ToPrimitive_WithNullSignature_ShouldThrowArgumentNullException()
    {
        // Arrange
        WebhookSignature? signature = null;

        // Act
        var act = () => signature!.ToPrimitive();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WebhookSignature_ToWebhookSignature_WithValidString_ShouldCreateSignature()
    {
        // Arrange
        var value = "sha256=" + new string('b', 64); // 64 hex chars

        // Act
        var result = value.ToWebhookSignature();

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void WebhookSignature_ToWebhookSignatureOrNull_WithValidString_ShouldCreateSignature()
    {
        // Arrange
        string? value = "sha256=" + new string('c', 64); // 64 hex chars

        // Act
        var result = value.ToWebhookSignatureOrNull();

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(value);
    }

    [Fact]
    public void WebhookSignature_ToWebhookSignatureOrNull_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? value = null;

        // Act
        var result = value.ToWebhookSignatureOrNull();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WebhookSignature_ToWebhookSignatureOrNull_WithWhitespace_ShouldReturnNull()
    {
        // Arrange
        string? value = "   ";

        // Act
        var result = value.ToWebhookSignatureOrNull();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PrivateKeyPem Tests

    [Fact]
    public void PrivateKeyPem_ToPrimitive_WithValidKey_ShouldReturnValue()
    {
        // Arrange
        var pemString = GenerateTestPkcs8Pem();
        var privateKey = PrivateKeyPem.Create(pemString);

        // Act
        var result = privateKey.ToPrimitive();

        // Assert
        result.Should().Be(pemString.Trim());
    }

    [Fact]
    public void PrivateKeyPem_ToPrimitive_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        PrivateKeyPem? privateKey = null;

        // Act
        var act = () => privateKey!.ToPrimitive();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PrivateKeyPem_ToPrivateKeyPem_WithValidString_ShouldCreateKey()
    {
        // Arrange
        var pemString = GenerateTestPkcs8Pem();

        // Act
        var result = pemString.ToPrivateKeyPem();

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(pemString.Trim());
    }

    [Fact]
    public void PrivateKeyPem_ToPrivateKeyPemOrNull_WithValidString_ShouldCreateKey()
    {
        // Arrange
        string? pemString = GenerateTestPkcs8Pem();

        // Act
        var result = pemString.ToPrivateKeyPemOrNull();

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(pemString.Trim());
    }

    [Fact]
    public void PrivateKeyPem_ToPrivateKeyPemOrNull_WithNull_ShouldReturnNull()
    {
        // Arrange
        string? pemString = null;

        // Act
        var result = pemString.ToPrivateKeyPemOrNull();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void PrivateKeyPem_ToPrivateKeyPemOrNull_WithEmptyString_ShouldReturnNull()
    {
        // Arrange
        string? pemString = string.Empty;

        // Act
        var result = pemString.ToPrivateKeyPemOrNull();

        // Assert
        result.Should().BeNull();
    }

    private static string GenerateTestPkcs8Pem()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var pkcs8Bytes = rsa.ExportPkcs8PrivateKey();
        var base64 = Convert.ToBase64String(pkcs8Bytes, Base64FormattingOptions.InsertLineBreaks);
        return $@"-----BEGIN PRIVATE KEY-----
{base64}
-----END PRIVATE KEY-----";
    }

    #endregion

    #region InstallationAccessToken Tests

    [Fact]
    public void InstallationAccessToken_ToPrimitive_WithValidToken_ShouldReturnValue()
    {
        // Arrange
        var tokenString = "ghs_test_token_123";
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var token = InstallationAccessToken.Create(tokenString, expiresAt);

        // Act
        var result = token.ToPrimitive();

        // Assert
        result.Should().Be(tokenString);
    }

    [Fact]
    public void InstallationAccessToken_ToPrimitive_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        InstallationAccessToken? token = null;

        // Act
        var act = () => token!.ToPrimitive();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void InstallationAccessToken_ToInstallationAccessToken_WithValidString_ShouldCreateToken()
    {
        // Arrange
        var tokenString = "ghs_test_token_456";
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        var result = tokenString.ToInstallationAccessToken(expiresAt);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(tokenString);
        result.ExpiresAt.Should().Be(expiresAt);
    }

    #endregion
}
