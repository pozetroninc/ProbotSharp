// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Reflection;

using ProbotSharp.Application.Abstractions.Events;

namespace ProbotSharp.Application.Services;

/// <summary>
/// Discovers event handlers in assemblies by scanning for types that implement
/// <see cref="IEventHandler"/> and are decorated with <see cref="EventHandlerAttribute"/>.
/// </summary>
public static class EventHandlerDiscovery
{
    /// <summary>
    /// Discovers all event handlers in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>A collection of handler types with their associated attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when assembly is null.</exception>
    public static IEnumerable<(Type HandlerType, EventHandlerAttribute[] Attributes)> DiscoverHandlers(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return DiscoverHandlersIterator(assembly);
    }

    /// <summary>
    /// Internal iterator method for discovering handlers. Separated to ensure immediate argument validation.
    /// </summary>
    private static IEnumerable<(Type HandlerType, EventHandlerAttribute[] Attributes)> DiscoverHandlersIterator(Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => typeof(IEventHandler).IsAssignableFrom(type)
                        && type.IsClass
                        && !type.IsAbstract
                        && (type.IsPublic || type.IsNestedPublic));

        foreach (var handlerType in handlerTypes)
        {
            var attributes = handlerType.GetCustomAttributes<EventHandlerAttribute>(inherit: false).ToArray();

            // Only return handlers that have at least one EventHandlerAttribute
            if (attributes.Length > 0)
            {
                yield return (handlerType, attributes);
            }
        }
    }

    /// <summary>
    /// Discovers all event handlers in multiple assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>A collection of handler types with their associated attributes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when assemblies is null.</exception>
    public static IEnumerable<(Type HandlerType, EventHandlerAttribute[] Attributes)> DiscoverHandlers(
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            foreach (var handler in DiscoverHandlers(assembly))
            {
                yield return handler;
            }
        }
    }
}
