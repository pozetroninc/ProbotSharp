using ProbotSharp.Shared.DTOs;

namespace ProbotSharp.Shared.Mapping;

/// <summary>
/// Extension methods for mapping domain entities to DTOs.
/// </summary>
public static class DomainToDtoMappingExtensions
{
    /// <summary>
    /// Maps a GitHubApp domain entity to a DTO.
    /// </summary>
    /// <param name="app">The GitHubApp domain entity.</param>
    /// <returns>The mapped GitHubAppDto.</returns>
    public static GitHubAppDto ToDto(this ProbotSharp.Domain.Entities.GitHubApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return new GitHubAppDto
        {
            Id = app.Id.Value,
            Name = app.Name,
            HasPrivateKey = !string.IsNullOrWhiteSpace(app.PrivateKey?.Value),
            HasWebhookSecret = !string.IsNullOrWhiteSpace(app.WebhookSecret),
            Installations = app.Installations.Select(i => i.ToDto()).ToList()
        };
    }

    /// <summary>
    /// Maps an Installation domain entity to a DTO.
    /// </summary>
    /// <param name="installation">The Installation domain entity.</param>
    /// <returns>The mapped InstallationDto.</returns>
    public static InstallationDto ToDto(this ProbotSharp.Domain.Entities.Installation installation)
    {
        ArgumentNullException.ThrowIfNull(installation);

        return new InstallationDto
        {
            Id = installation.Id.Value,
            AccountLogin = installation.AccountLogin,
            Repositories = installation.Repositories.Select(r => r.ToDto()).ToList()
        };
    }

    /// <summary>
    /// Maps a Repository domain entity to a DTO.
    /// </summary>
    /// <param name="repository">The Repository domain entity.</param>
    /// <returns>The mapped RepositoryDto.</returns>
    public static RepositoryDto ToDto(this ProbotSharp.Domain.Entities.Repository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        return new RepositoryDto
        {
            Id = repository.Id,
            Name = repository.Name,
            FullName = repository.FullName
        };
    }

    /// <summary>
    /// Maps a WebhookDelivery domain entity to a DTO.
    /// </summary>
    /// <param name="delivery">The WebhookDelivery domain entity.</param>
    /// <returns>The mapped WebhookDeliveryDto.</returns>
    public static WebhookDeliveryDto ToDto(this ProbotSharp.Domain.Entities.WebhookDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        return new WebhookDeliveryDto
        {
            DeliveryId = delivery.Id.Value,
            EventName = delivery.EventName.Value,
            DeliveredAt = delivery.DeliveredAt,
            Payload = delivery.Payload.RawBody,
            InstallationId = delivery.InstallationId?.Value
        };
    }

    /// <summary>
    /// Maps a collection of GitHubApp entities to DTOs.
    /// </summary>
    /// <param name="apps">The collection of GitHubApp entities.</param>
    /// <returns>A list of GitHubAppDto objects.</returns>
    public static List<GitHubAppDto> ToDtoList(this IEnumerable<ProbotSharp.Domain.Entities.GitHubApp> apps)
    {
        ArgumentNullException.ThrowIfNull(apps);
        return apps.Select(a => a.ToDto()).ToList();
    }

    /// <summary>
    /// Maps a collection of Installation entities to DTOs.
    /// </summary>
    /// <param name="installations">The collection of Installation entities.</param>
    /// <returns>A list of InstallationDto objects.</returns>
    public static List<InstallationDto> ToDtoList(this IEnumerable<ProbotSharp.Domain.Entities.Installation> installations)
    {
        ArgumentNullException.ThrowIfNull(installations);
        return installations.Select(i => i.ToDto()).ToList();
    }

    /// <summary>
    /// Maps a collection of Repository entities to DTOs.
    /// </summary>
    /// <param name="repositories">The collection of Repository entities.</param>
    /// <returns>A list of RepositoryDto objects.</returns>
    public static List<RepositoryDto> ToDtoList(this IEnumerable<ProbotSharp.Domain.Entities.Repository> repositories)
    {
        ArgumentNullException.ThrowIfNull(repositories);
        return repositories.Select(r => r.ToDto()).ToList();
    }

    /// <summary>
    /// Maps a collection of WebhookDelivery entities to DTOs.
    /// </summary>
    /// <param name="deliveries">The collection of WebhookDelivery entities.</param>
    /// <returns>A list of WebhookDeliveryDto objects.</returns>
    public static List<WebhookDeliveryDto> ToDtoList(this IEnumerable<ProbotSharp.Domain.Entities.WebhookDelivery> deliveries)
    {
        ArgumentNullException.ThrowIfNull(deliveries);
        return deliveries.Select(d => d.ToDto()).ToList();
    }
}
