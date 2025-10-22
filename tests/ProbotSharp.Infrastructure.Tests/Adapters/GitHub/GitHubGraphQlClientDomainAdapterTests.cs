// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Application.Ports.Outbound;
using ProbotSharp.Domain.Contracts;
using ProbotSharp.Infrastructure.Adapters.GitHub;
using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Infrastructure.Tests.Adapters.GitHub;

public sealed class GitHubGraphQlClientDomainAdapterTests
{
    private sealed class StubPort : IGitHubGraphQlClientPort
    {
        private readonly Result<Dictionary<string, object>> _result;
        public StubPort(Result<Dictionary<string, object>> result) => _result = result;
        public Task<Result<TResponse>> ExecuteAsync<TResponse>(string query, object? variables = null, CancellationToken cancellationToken = default)
        {
            object boxed = _result.IsSuccess ? (object?)_result.Value! : null;
            var err = _result.Error;
            var ret = _result.IsSuccess
                ? Result<TResponse>.Success((TResponse)boxed!)
                : Result<TResponse>.Failure(err!.Value.Code, err.Value.Message);
            return Task.FromResult(ret);
        }
    }

    [Fact]
    public async Task ExecuteAsync_Success_ShouldPassThrough()
    {
        var port = new StubPort(Result<Dictionary<string, object>>.Success(new Dictionary<string, object> { { "ok", true } }));
        var sut = new GitHubGraphQlClientDomainAdapter(port);
        var res = await sut.ExecuteAsync<Dictionary<string, object>>("query {}");
        res.IsSuccess.Should().BeTrue();
        res.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Failure_ShouldMapError()
    {
        var port = new StubPort(Result<Dictionary<string, object>>.Failure("x", "y"));
        var sut = new GitHubGraphQlClientDomainAdapter(port);
        var res = await sut.ExecuteAsync<Dictionary<string, object>>("query {}");
        res.IsSuccess.Should().BeFalse();
        res.ErrorCode.Should().Be("x");
    }
}
