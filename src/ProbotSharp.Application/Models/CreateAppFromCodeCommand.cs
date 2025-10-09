// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

namespace ProbotSharp.Application.Models;

public sealed record class CreateAppFromCodeCommand(
    string Code,
    string? GitHubEnterpriseHost = null)
{
    public string Code { get; } = !string.IsNullOrWhiteSpace(Code)
        ? Code
        : throw new ArgumentException("Code cannot be null or whitespace.", nameof(Code));
}
