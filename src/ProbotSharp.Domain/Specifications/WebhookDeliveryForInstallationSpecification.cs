// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;

namespace ProbotSharp.Domain.Specifications;

/// <summary>
/// Specification to determine if a WebhookDelivery is associated with a specific Installation.
/// Useful for filtering webhook deliveries by installation ID.
/// </summary>
public sealed class WebhookDeliveryForInstallationSpecification : Specification<WebhookDelivery>
{
    private readonly InstallationId _installationId;

    /// <summary>
    /// Initializes a new instance for the specified installation.
    /// </summary>
    /// <param name="installationId">The installation ID to match.</param>
    public WebhookDeliveryForInstallationSpecification(InstallationId installationId)
    {
        this._installationId = installationId ?? throw new ArgumentNullException(nameof(installationId));
    }

    /// <summary>
    /// Determines whether the specified webhook delivery is associated with the configured installation.
    /// </summary>
    /// <param name="candidate">The webhook delivery to evaluate.</param>
    /// <returns>True if the delivery is for the installation; otherwise, false.</returns>
    public override bool IsSatisfiedBy(WebhookDelivery candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate.InstallationId is not null && candidate.InstallationId.Equals(this._installationId);
    }
}
