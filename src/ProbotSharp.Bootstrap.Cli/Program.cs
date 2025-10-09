using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Spectre.Console.Cli;

using ProbotSharp.Adapters.Cli;
using ProbotSharp.Adapters.Cli.Commands;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Ports.Inbound;
using ProbotSharp.Infrastructure.Extensions;

// Build the host for dependency injection
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.IncludeScopes = false;
    });
    logging.SetMinimumLevel(LogLevel.Information);
});

// Register application and infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register CLI adapter services
builder.Services.AddSingleton<ICliCommandPort, CliCommandService>();

// Register CLI commands
builder.Services.AddSingleton<RunCommand>();
builder.Services.AddSingleton<ReceiveCommand>();
builder.Services.AddSingleton<SetupCommand>();
builder.Services.AddSingleton<VersionCommand>();
builder.Services.AddSingleton<HelpCommand>();

// Build the host
using var host = builder.Build();

// Create and configure the CommandApp
var app = new CommandApp(new TypeRegistrar(host.Services));

app.Configure(config =>
{
    config.SetApplicationName("probot-sharp");
    config.SetApplicationVersion(
        typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0");

    config.ValidateExamples();

    // Add commands
    config.AddCommand<RunCommand>("run")
        .WithDescription("Start the ProbotSharp development server")
        .WithExample(new[] { "run", "./app" })
        .WithExample(new[] { "run", "./app", "--port", "3000" })
        .WithExample(new[] { "run", "./app", "--webhook-proxy", "https://smee.io/abc123" });

    config.AddCommand<ReceiveCommand>("receive")
        .WithDescription("Simulate receiving a webhook event for local testing")
        .WithExample(new[] { "receive", "./app", "--event", "issues.opened", "--file", "payload.json" })
        .WithExample(new[] { "receive", "./app", "-e", "pull_request", "-f", "pr.json" });

    config.AddCommand<SetupCommand>("setup")
        .WithDescription("Interactive setup wizard for creating a GitHub App")
        .WithExample(new[] { "setup" })
        .WithExample(new[] { "setup", "--port", "3000" });

    config.AddCommand<VersionCommand>("version")
        .WithDescription("Display version information");

    config.AddCommand<HelpCommand>("help")
        .WithDescription("Display help information")
        .WithExample(new[] { "help", "run" });
});

// Run the application
return await app.RunAsync(args);

/// <summary>
/// Type registrar for integrating Spectre.Console.Cli with Microsoft.Extensions.DependencyInjection.
/// </summary>
internal sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceProvider _provider;

    public TypeRegistrar(IServiceProvider provider)
    {
        _provider = provider;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_provider);
    }

    public void Register(Type service, Type implementation)
    {
        // Not used - we configure services via Host.CreateApplicationBuilder
    }

    public void RegisterInstance(Type service, object implementation)
    {
        // Not used - we configure services via Host.CreateApplicationBuilder
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        // Not used - we configure services via Host.CreateApplicationBuilder
    }
}

/// <summary>
/// Type resolver for integrating Spectre.Console.Cli with Microsoft.Extensions.DependencyInjection.
/// </summary>
internal sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type)
    {
        return type == null ? null : _provider.GetService(type);
    }
}
