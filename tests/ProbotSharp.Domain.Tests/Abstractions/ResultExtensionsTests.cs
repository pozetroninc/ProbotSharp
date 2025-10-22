// Copyright (c) 2024 Pozitron Inc. All rights reserved.

namespace ProbotSharp.Domain.Tests.Abstractions;

using FluentAssertions;
using ProbotSharp.Shared.Abstractions;
using Xunit;

public class ResultExtensionsTests
{
    [Fact]
    public async Task BindAsync_WithSuccessResultAndSuccessBinder_ShouldReturnBinderResult()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var expected = "42";

        // Act
        var output = await result.BindAsync(async x =>
        {
            await Task.CompletedTask;
            return Result<string>.Success(x.ToString());
        });

        // Assert
        output.IsSuccess.Should().BeTrue();
        output.Value.Should().Be(expected);
    }

    [Fact]
    public async Task BindAsync_WithFailureResult_ShouldShortCircuitAndNotCallBinder()
    {
        // Arrange
        var error = new Error("test.error", "Test error message");
        var result = Result<int>.Failure(error);
        var binderCalled = false;

        // Act
        var output = await result.BindAsync(async x =>
        {
            binderCalled = true;
            await Task.CompletedTask;
            return Result<string>.Success(x.ToString());
        });

        // Assert
        binderCalled.Should().BeFalse();
        output.IsSuccess.Should().BeFalse();
        output.Error.Should().NotBeNull();
        output.Error!.Value.Code.Should().Be("test.error");
        output.Error!.Value.Message.Should().Be("Test error message");
    }

    [Fact]
    public async Task BindAsync_WithSuccessResultAndFailingBinder_ShouldReturnBinderFailure()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var binderError = new Error("binder.error", "Binder failed");

        // Act
        var output = await result.BindAsync(async x =>
        {
            await Task.CompletedTask;
            return Result<string>.Failure(binderError);
        });

        // Assert
        output.IsSuccess.Should().BeFalse();
        output.Error.Should().NotBeNull();
        output.Error!.Value.Code.Should().Be("binder.error");
    }

    [Fact]
    public async Task BindAsync_WithNullSuccessValue_ShouldReturnFailure()
    {
        // Arrange
        var result = Result<string?>.Success(null);

        // Act
        var output = await result.BindAsync(async x =>
        {
            await Task.CompletedTask;
            return Result<int>.Success(42);
        });

        // Assert
        output.IsSuccess.Should().BeFalse();
        output.Error.Should().NotBeNull();
        output.Error!.Value.Code.Should().Be("null_success_value");
    }

    [Fact]
    public async Task BindAsync_TaskResult_WithSuccessResult_ShouldCallBinder()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        var expected = "42";

        // Act
        var output = await resultTask.BindAsync(async x =>
        {
            await Task.CompletedTask;
            return Result<string>.Success(x.ToString());
        });

        // Assert
        output.IsSuccess.Should().BeTrue();
        output.Value.Should().Be(expected);
    }

    [Fact]
    public async Task BindAsync_TaskResult_WithFailure_ShouldShortCircuit()
    {
        // Arrange
        var error = new Error("test.error", "Test error");
        var resultTask = Task.FromResult(Result<int>.Failure(error));
        var binderCalled = false;

        // Act
        var output = await resultTask.BindAsync(async x =>
        {
            binderCalled = true;
            await Task.CompletedTask;
            return Result<string>.Success(x.ToString());
        });

        // Assert
        binderCalled.Should().BeFalse();
        output.IsSuccess.Should().BeFalse();
        output.Error!.Value.Code.Should().Be("test.error");
    }

    [Fact]
    public async Task TapSuccessAsync_WithSuccessResult_ShouldExecuteActionAndReturnOriginalResult()
    {
        // Arrange
        var result = Task.FromResult(Result<int>.Success(42));
        var actionExecuted = false;
        var capturedValue = 0;

        // Act
        var output = await result.TapSuccessAsync(async x =>
        {
            actionExecuted = true;
            capturedValue = x;
            await Task.CompletedTask;
        });

        // Assert
        actionExecuted.Should().BeTrue();
        capturedValue.Should().Be(42);
        output.IsSuccess.Should().BeTrue();
        output.Value.Should().Be(42);
    }

    [Fact]
    public async Task TapSuccessAsync_WithFailureResult_ShouldNotExecuteActionAndReturnOriginalResult()
    {
        // Arrange
        var error = new Error("test.error", "Test error");
        var result = Task.FromResult(Result<int>.Failure(error));
        var actionExecuted = false;

        // Act
        var output = await result.TapSuccessAsync(async x =>
        {
            actionExecuted = true;
            await Task.CompletedTask;
        });

        // Assert
        actionExecuted.Should().BeFalse();
        output.IsSuccess.Should().BeFalse();
        output.Error!.Value.Code.Should().Be("test.error");
    }

    [Fact]
    public async Task TapFailureAsync_WithFailureResult_ShouldExecuteActionAndReturnOriginalResult()
    {
        // Arrange
        var error = new Error("test.error", "Test error message");
        var result = Task.FromResult(Result<int>.Failure(error));
        var actionExecuted = false;
        var capturedError = default(Error);

        // Act
        var output = await result.TapFailureAsync(async err =>
        {
            actionExecuted = true;
            capturedError = err;
            await Task.CompletedTask;
        });

        // Assert
        actionExecuted.Should().BeTrue();
        capturedError.Code.Should().Be("test.error");
        capturedError.Message.Should().Be("Test error message");
        output.IsSuccess.Should().BeFalse();
        output.Error!.Value.Code.Should().Be("test.error");
    }

    [Fact]
    public async Task TapFailureAsync_WithSuccessResult_ShouldNotExecuteActionAndReturnOriginalResult()
    {
        // Arrange
        var result = Task.FromResult(Result<int>.Success(42));
        var actionExecuted = false;

        // Act
        var output = await result.TapFailureAsync(async err =>
        {
            actionExecuted = true;
            await Task.CompletedTask;
        });

        // Assert
        actionExecuted.Should().BeFalse();
        output.IsSuccess.Should().BeTrue();
        output.Value.Should().Be(42);
    }

    [Fact]
    public void Map_WithSuccessResult_ShouldTransformValue()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var output = result.Map(x => x.ToString());

        // Assert
        output.IsSuccess.Should().BeTrue();
        output.Value.Should().Be("42");
    }

    [Fact]
    public void Map_WithFailureResult_ShouldPropagateFailure()
    {
        // Arrange
        var error = new Error("test.error", "Test error");
        var result = Result<int>.Failure(error);

        // Act
        var output = result.Map(x => x.ToString());

        // Assert
        output.IsSuccess.Should().BeFalse();
        output.Error!.Value.Code.Should().Be("test.error");
    }

    [Fact]
    public void Map_WithNullSuccessValue_ShouldReturnFailure()
    {
        // Arrange
        var result = Result<string?>.Success(null);

        // Act
        var output = result.Map(x => x!.Length);

        // Assert
        output.IsSuccess.Should().BeFalse();
        output.Error!.Value.Code.Should().Be("null_success_value");
    }
}
