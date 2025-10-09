// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Reflection;
using ProbotSharp.Application.Abstractions.Commands;

namespace ProbotSharp.Application.Services;

/// <summary>
/// Discovers slash command handlers in assemblies by scanning for classes decorated
/// with <see cref="SlashCommandHandlerAttribute"/>.
/// </summary>
public static class SlashCommandHandlerDiscovery
{
    /// <summary>
    /// Discovers all slash command handlers in a single assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <returns>An enumerable of tuples containing handler types and their command names.</returns>
    public static IEnumerable<(Type HandlerType, string[] Commands)> DiscoverHandlers(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return DiscoverHandlers([assembly]);
    }

    /// <summary>
    /// Discovers all slash command handlers in multiple assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>An enumerable of tuples containing handler types and their command names.</returns>
    public static IEnumerable<(Type HandlerType, string[] Commands)> DiscoverHandlers(Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return DiscoverHandlersIterator(assemblies);
    }

    /// <summary>
    /// Internal iterator method for discovering handlers. Separated to ensure immediate argument validation.
    /// </summary>
    private static IEnumerable<(Type HandlerType, string[] Commands)> DiscoverHandlersIterator(Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var types = GetLoadableTypes(assembly);

            foreach (var type in types)
            {
                // Skip non-class types, abstract classes, and interfaces
                if (!type.IsClass || type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                // Skip types that don't implement ISlashCommandHandler
                if (!typeof(ISlashCommandHandler).IsAssignableFrom(type))
                {
                    continue;
                }

                // Get all SlashCommandHandlerAttribute instances
                var attributes = type.GetCustomAttributes<SlashCommandHandlerAttribute>(inherit: false).ToArray();

                if (attributes.Length > 0)
                {
                    var commandNames = attributes.Select(attr => attr.CommandName).ToArray();
                    yield return (type, commandNames);
                }
            }
        }
    }

    /// <summary>
    /// Gets all loadable types from an assembly, handling ReflectionTypeLoadException gracefully.
    /// </summary>
    /// <param name="assembly">The assembly to get types from.</param>
    /// <returns>An enumerable of types that could be loaded.</returns>
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return only the types that loaded successfully
            return ex.Types.Where(t => t != null)!;
        }
    }
}
