using ProbotSharp.Shared.Abstractions;

namespace ProbotSharp.Shared.Tests;

public class ResultTests
{
    [Fact]
    public void ResultSuccess_ShouldHaveNoError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ResultFailure_ShouldExposeError()
    {
        var result = Result.Failure("code", "message", "details");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Value.Code.Should().Be("code");
        result.Error!.Value.Message.Should().Be("message");
        result.Error!.Value.Details.Should().Be("details");
    }

    [Fact]
    public void ResultMatch_ShouldChooseCorrectBranch()
    {
        var success = Result.Success();
        var failure = Result.Failure("code", "message");

        success.Match(() => "success", _ => "failure").Should().Be("success");
        failure.Match(() => "success", e => e.Message).Should().Be("message");
    }

    [Fact]
    public void ResultTSuccess_ShouldExposeValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void ResultTFailure_ShouldExposeError()
    {
        var result = Result<int>.Failure("code", "message");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(default);
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public void ResultTMatch_ShouldBranchCorrectly()
    {
        var success = Result<int>.Success(3);
        var failure = Result<int>.Failure("code", "message");

        success.Match(value => value * 2, _ => -1).Should().Be(6);
        failure.Match(value => value * 2, _ => -1).Should().Be(-1);
    }

    [Fact]
    public void Error_ToString_ShouldIncludeDetailsWhenPresent()
    {
        var error = new Error("code", "message", "details");

        error.ToString().Should().Be("code: message (details)");
    }

    [Fact]
    public void Error_ToString_ShouldOmitDetailsWhenNull()
    {
        var error = new Error("code", "message");

        error.ToString().Should().Be("code: message");
    }
}
