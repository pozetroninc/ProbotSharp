using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Shared.Mapping;

/// <summary>
/// Helper methods for mapping between value objects and primitive types.
/// Useful for serialization, API binding, and data transformation scenarios.
/// </summary>
public static class ValueObjectMappingHelpers
{
    #region GitHubAppId

    /// <summary>
    /// Converts a GitHubAppId value object to its primitive long value.
    /// </summary>
    public static long ToPrimitive(this GitHubAppId appId)
    {
        ArgumentNullException.ThrowIfNull(appId);
        return appId.Value;
    }

    /// <summary>
    /// Creates a GitHubAppId value object from a primitive long value.
    /// </summary>
    public static GitHubAppId ToGitHubAppId(this long value)
    {
        return GitHubAppId.Create(value);
    }

    /// <summary>
    /// Tries to create a GitHubAppId from a nullable long value.
    /// Returns null if the input is null or invalid.
    /// </summary>
    public static GitHubAppId? ToGitHubAppIdOrNull(this long? value)
    {
        return value.HasValue && value.Value > 0 ? GitHubAppId.Create(value.Value) : null;
    }

    #endregion

    #region InstallationId

    /// <summary>
    /// Converts an InstallationId value object to its primitive long value.
    /// </summary>
    public static long ToPrimitive(this InstallationId installationId)
    {
        ArgumentNullException.ThrowIfNull(installationId);
        return installationId.Value;
    }

    /// <summary>
    /// Creates an InstallationId value object from a primitive long value.
    /// </summary>
    public static InstallationId ToInstallationId(this long value)
    {
        return InstallationId.Create(value);
    }

    /// <summary>
    /// Tries to create an InstallationId from a nullable long value.
    /// Returns null if the input is null or invalid.
    /// </summary>
    public static InstallationId? ToInstallationIdOrNull(this long? value)
    {
        return value.HasValue && value.Value > 0 ? InstallationId.Create(value.Value) : null;
    }

    #endregion

    #region DeliveryId

    /// <summary>
    /// Converts a DeliveryId value object to its primitive string value.
    /// </summary>
    public static string ToPrimitive(this DeliveryId deliveryId)
    {
        ArgumentNullException.ThrowIfNull(deliveryId);
        return deliveryId.Value;
    }

    /// <summary>
    /// Creates a DeliveryId value object from a primitive string value.
    /// </summary>
    public static DeliveryId ToDeliveryId(this string value)
    {
        return DeliveryId.Create(value);
    }

    /// <summary>
    /// Tries to create a DeliveryId from a nullable string value.
    /// Returns null if the input is null or whitespace.
    /// </summary>
    public static DeliveryId? ToDeliveryIdOrNull(this string? value)
    {
        return !string.IsNullOrWhiteSpace(value) ? DeliveryId.Create(value) : null;
    }

    #endregion

    #region WebhookEventName

    /// <summary>
    /// Converts a WebhookEventName value object to its primitive string value.
    /// </summary>
    public static string ToPrimitive(this WebhookEventName eventName)
    {
        ArgumentNullException.ThrowIfNull(eventName);
        return eventName.Value;
    }

    /// <summary>
    /// Creates a WebhookEventName value object from a primitive string value.
    /// </summary>
    public static WebhookEventName ToWebhookEventName(this string value)
    {
        return WebhookEventName.Create(value);
    }

    /// <summary>
    /// Tries to create a WebhookEventName from a nullable string value.
    /// Returns null if the input is null or whitespace.
    /// </summary>
    public static WebhookEventName? ToWebhookEventNameOrNull(this string? value)
    {
        return !string.IsNullOrWhiteSpace(value) ? WebhookEventName.Create(value) : null;
    }

    #endregion

    #region WebhookPayload

    /// <summary>
    /// Converts a WebhookPayload value object to its raw JSON string.
    /// </summary>
    public static string ToPrimitive(this WebhookPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return payload.RawBody;
    }

    /// <summary>
    /// Creates a WebhookPayload value object from a JSON string.
    /// </summary>
    public static WebhookPayload ToWebhookPayload(this string jsonBody)
    {
        return WebhookPayload.Create(jsonBody);
    }

    /// <summary>
    /// Tries to create a WebhookPayload from a nullable string value.
    /// Returns null if the input is null or whitespace.
    /// </summary>
    public static WebhookPayload? ToWebhookPayloadOrNull(this string? jsonBody)
    {
        return !string.IsNullOrWhiteSpace(jsonBody) ? WebhookPayload.Create(jsonBody) : null;
    }

    #endregion

    #region WebhookSignature

    /// <summary>
    /// Converts a WebhookSignature value object to its primitive string value.
    /// </summary>
    public static string ToPrimitive(this WebhookSignature signature)
    {
        ArgumentNullException.ThrowIfNull(signature);
        return signature.Value;
    }

    /// <summary>
    /// Creates a WebhookSignature value object from a primitive string value.
    /// </summary>
    public static WebhookSignature ToWebhookSignature(this string value)
    {
        return WebhookSignature.Create(value);
    }

    /// <summary>
    /// Tries to create a WebhookSignature from a nullable string value.
    /// Returns null if the input is null or whitespace.
    /// </summary>
    public static WebhookSignature? ToWebhookSignatureOrNull(this string? value)
    {
        return !string.IsNullOrWhiteSpace(value) ? WebhookSignature.Create(value) : null;
    }

    #endregion

    #region PrivateKeyPem

    /// <summary>
    /// Converts a PrivateKeyPem value object to its primitive string value.
    /// </summary>
    public static string ToPrimitive(this PrivateKeyPem privateKey)
    {
        ArgumentNullException.ThrowIfNull(privateKey);
        return privateKey.Value;
    }

    /// <summary>
    /// Creates a PrivateKeyPem value object from a PEM string.
    /// </summary>
    public static PrivateKeyPem ToPrivateKeyPem(this string pemString)
    {
        return PrivateKeyPem.Create(pemString);
    }

    /// <summary>
    /// Tries to create a PrivateKeyPem from a nullable string value.
    /// Returns null if the input is null or whitespace.
    /// </summary>
    public static PrivateKeyPem? ToPrivateKeyPemOrNull(this string? pemString)
    {
        return !string.IsNullOrWhiteSpace(pemString) ? PrivateKeyPem.Create(pemString) : null;
    }

    #endregion

    #region InstallationAccessToken

    /// <summary>
    /// Converts an InstallationAccessToken value object to its primitive string value.
    /// </summary>
    public static string ToPrimitive(this InstallationAccessToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return token.Value;
    }

    /// <summary>
    /// Creates an InstallationAccessToken value object from a token string.
    /// </summary>
    public static InstallationAccessToken ToInstallationAccessToken(this string tokenString, DateTimeOffset expiresAt)
    {
        return InstallationAccessToken.Create(tokenString, expiresAt);
    }

    #endregion
}
