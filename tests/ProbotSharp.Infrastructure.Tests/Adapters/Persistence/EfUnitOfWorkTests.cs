// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ProbotSharp.Infrastructure.Adapters.Persistence;
using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Infrastructure.Tests.Adapters.Persistence;

public sealed class EfUnitOfWorkTests
{
    private readonly ProbotSharpDbContext _dbContext;
    private readonly EfUnitOfWork _sut;
    private readonly ILogger<EfUnitOfWork> _logger = Substitute.For<ILogger<EfUnitOfWork>>();

    public EfUnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<ProbotSharpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ProbotSharpDbContext(options);
        _sut = new EfUnitOfWork(_dbContext, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCommit_WhenOperationSucceeds()
    {
        var result = await _sut.ExecuteAsync(_ => Task.FromResult(Result.Success()));

        result.IsSuccess.Should().BeTrue();
        await _dbContext.SaveChangesAsync(); // should not throw
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateFailure()
    {
        var failure = Result.Failure("test", "failed");

        var result = await _sut.ExecuteAsync(_ => Task.FromResult(failure));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(failure.Error);
    }
}

