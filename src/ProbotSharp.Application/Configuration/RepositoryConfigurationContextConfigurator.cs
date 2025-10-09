// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Abstractions;
using ProbotSharp.Application.Extensions;
using ProbotSharp.Application.Services;
using ProbotSharp.Domain.Context;
using ProbotSharp.Domain.Models;

namespace ProbotSharp.Application.Configuration;

/// <summary>
/// Configures repository configuration service on ProbotSharpContext instances.
/// </summary>
internal sealed class RepositoryConfigurationContextConfigurator : IProbotSharpContextConfigurator
{
    private readonly RepositoryConfigurationService _configService;

    public RepositoryConfigurationContextConfigurator(RepositoryConfigurationService configService)
    {
        _configService = configService;
    }

    public void Configure(ProbotSharpContext context)
    {
        context.SetConfigurationService(_configService, RepositoryConfigurationOptions.Default);
    }
}
