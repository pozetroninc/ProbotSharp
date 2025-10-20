// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Configuration;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Application.Services;
using ProbotSharp.Application.UseCases;
using ProbotSharp.Domain.Services;

namespace ProbotSharp.Application.Extensions;

/// <summary>
/// Extension methods for configuring application layer services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Adds application layer services to the specified <see cref="IServiceCollection"/>.
    /// This includes all use cases, domain services, and their mappings to inbound ports.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register domain services as singletons (stateless services)
        services.AddSingleton<WebhookSignatureValidator>();

        // Register application services
        // Note: CommentAttachmentService and MetadataService are not registered in DI
        // because they require ProbotSharpContext, which is created per-request.
        // Users should create these services manually when needed:
        //   var attachments = new CommentAttachmentService(context);
        //   var metadata = new MetadataService(metadataPort, context);
        services.AddScoped<RepositoryConfigurationService>();

        // Register context configurators
        services.AddScoped<IProbotSharpContextConfigurator, RepositoryConfigurationContextConfigurator>();

        // Register event routing infrastructure
        // Note: Use AddProbotHandlers to register handlers from specific assemblies
        services.AddEventRouter();

        // Register use cases and expose them via their inbound ports
        services.AddTransient<IWebhookProcessingPort, ProcessWebhookUseCase>();
        services.AddTransient<IInstallationAuthenticationPort, AuthenticateInstallationUseCase>();
        services.AddSingleton<IAppLifecyclePort, RunAppLifecycleUseCase>();
        services.AddTransient<ISetupWizardPort, RunSetupWizardUseCase>();
        services.AddTransient<IReplayWebhookPort, ReplayWebhookUseCase>();

        return services;
    }
}
