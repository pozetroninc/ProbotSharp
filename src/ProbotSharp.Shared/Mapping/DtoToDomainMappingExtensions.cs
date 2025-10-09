using ProbotSharp.Domain.Entities;
using ProbotSharp.Domain.ValueObjects;
using ProbotSharp.Shared.DTOs;

namespace ProbotSharp.Shared.Mapping;

/// <summary>
/// Extension methods for mapping DTOs to domain entities.
/// Note: These methods create new domain entities. For updating existing entities,
/// use the entity's own methods to maintain domain invariants.
/// </summary>
public static class DtoToDomainMappingExtensions
{
    /// <summary>
    /// Maps a RepositoryDto to a Repository domain entity.
    /// </summary>
    /// <param name="dto">The RepositoryDto.</param>
    /// <returns>The mapped Repository entity.</returns>
    public static Repository ToDomain(this RepositoryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return Repository.Create(
            id: dto.Id,
            name: dto.Name,
            fullName: dto.FullName
        );
    }

    /// <summary>
    /// Maps an InstallationDto to an Installation domain entity.
    /// Note: This creates a new Installation without repositories.
    /// Use AddRepository method to add repositories after creation.
    /// </summary>
    /// <param name="dto">The InstallationDto.</param>
    /// <returns>The mapped Installation entity.</returns>
    public static Installation ToDomain(this InstallationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var installationId = InstallationId.Create(dto.Id);
        return Installation.Create(installationId, dto.AccountLogin);
    }

    /// <summary>
    /// Maps a WebhookDeliveryDto to a WebhookDelivery domain entity.
    /// </summary>
    /// <param name="dto">The WebhookDeliveryDto.</param>
    /// <returns>The mapped WebhookDelivery entity.</returns>
    public static WebhookDelivery ToDomain(this WebhookDeliveryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var deliveryId = DeliveryId.Create(dto.DeliveryId);
        var eventName = WebhookEventName.Create(dto.EventName);
        var payload = WebhookPayload.Create(dto.Payload);
        var installationId = dto.InstallationId.HasValue
            ? InstallationId.Create(dto.InstallationId.Value)
            : null;

        return WebhookDelivery.Create(
            id: deliveryId,
            eventName: eventName,
            deliveredAt: dto.DeliveredAt,
            payload: payload,
            installationId: installationId
        );
    }

    /// <summary>
    /// Maps a collection of RepositoryDto objects to Repository entities.
    /// </summary>
    /// <param name="dtos">The collection of RepositoryDto objects.</param>
    /// <returns>A list of Repository entities.</returns>
    public static List<Repository> ToDomainList(this IEnumerable<RepositoryDto> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        return dtos.Select(d => d.ToDomain()).ToList();
    }

    /// <summary>
    /// Maps a collection of InstallationDto objects to Installation entities.
    /// </summary>
    /// <param name="dtos">The collection of InstallationDto objects.</param>
    /// <returns>A list of Installation entities.</returns>
    public static List<Installation> ToDomainList(this IEnumerable<InstallationDto> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        return dtos.Select(d => d.ToDomain()).ToList();
    }

    /// <summary>
    /// Maps a collection of WebhookDeliveryDto objects to WebhookDelivery entities.
    /// </summary>
    /// <param name="dtos">The collection of WebhookDeliveryDto objects.</param>
    /// <returns>A list of WebhookDelivery entities.</returns>
    public static List<WebhookDelivery> ToDomainList(this IEnumerable<WebhookDeliveryDto> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);
        return dtos.Select(d => d.ToDomain()).ToList();
    }
}
