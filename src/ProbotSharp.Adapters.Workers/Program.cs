// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ProbotSharp.Adapters.Workers;
using ProbotSharp.Infrastructure.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructure(context.Configuration);
        services.AddHostedService<WebhookReplayWorker>();
    })
    .Build();

await host.RunAsync().ConfigureAwait(false);
