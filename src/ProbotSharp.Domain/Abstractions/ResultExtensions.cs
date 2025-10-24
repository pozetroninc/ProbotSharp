// Copyright (c) ProbotSharp Contributors.

namespace ProbotSharp.Domain.Abstractions;

/// <summary>
/// Railway-Oriented Programming extensions for Result&lt;T&gt; monad.
/// Enables functional composition of operations that can fail.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Binds a synchronous Result to an async Result-producing function.
    /// Short-circuits on failure (Railway-Oriented Programming).
    /// </summary>
    /// <typeparam name="TIn">Input result type.</typeparam>
    /// <typeparam name="TOut">Output result type.</typeparam>
    /// <param name="result">The input result.</param>
    /// <param name="binder">Function to bind if result is success.</param>
    /// <returns>Result from binder if success, or propagated failure.</returns>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> binder)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (binder == null)
        {
            throw new ArgumentNullException(nameof(binder));
        }

        if (!result.IsSuccess)
        {
            return Result<TOut>.Failure(
                result.Error ?? new Error("unknown_failure", "An unknown failure occurred"));
        }

        if (result.Value == null)
        {
            return Result<TOut>.Failure(
                new Error("null_success_value", "Success result contained a null value"));
        }

        return await binder(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Binds an async Result to another async Result-producing function.
    /// Short-circuits on failure (Railway-Oriented Programming).
    /// </summary>
    /// <typeparam name="TIn">Input result type.</typeparam>
    /// <typeparam name="TOut">Output result type.</typeparam>
    /// <param name="resultTask">The input result task.</param>
    /// <param name="binder">Function to bind if result is success.</param>
    /// <returns>Result from binder if success, or propagated failure.</returns>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<Result<TOut>>> binder)
    {
        if (resultTask == null)
        {
            throw new ArgumentNullException(nameof(resultTask));
        }

        if (binder == null)
        {
            throw new ArgumentNullException(nameof(binder));
        }

        var result = await resultTask.ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return Result<TOut>.Failure(
                result.Error ?? new Error("unknown_failure", "An unknown failure occurred"));
        }

        if (result.Value == null)
        {
            return Result<TOut>.Failure(
                new Error("null_success_value", "Success result contained a null value"));
        }

        return await binder(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a side-effect action on success without changing the Result.
    /// Used for tracing, logging, metrics.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="action">Action to execute on success.</param>
    /// <returns>Original result unchanged.</returns>
    public static async Task<Result<T>> TapSuccessAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> action)
    {
        if (resultTask == null)
        {
            throw new ArgumentNullException(nameof(resultTask));
        }

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var result = await resultTask.ConfigureAwait(false);

        if (result.IsSuccess && result.Value != null)
        {
            await action(result.Value).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Executes a side-effect action on failure without changing the Result.
    /// Used for error logging, alerting.
    /// </summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="action">Action to execute on failure.</param>
    /// <returns>Original result unchanged.</returns>
    public static async Task<Result<T>> TapFailureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Task> action)
    {
        if (resultTask == null)
        {
            throw new ArgumentNullException(nameof(resultTask));
        }

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var result = await resultTask.ConfigureAwait(false);

        if (!result.IsSuccess && result.Error.HasValue)
        {
            await action(result.Error.Value).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Maps a success value to a new value without changing failure state.
    /// </summary>
    /// <typeparam name="TIn">Input type.</typeparam>
    /// <typeparam name="TOut">Output type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="mapper">Function to transform success value.</param>
    /// <returns>Mapped result if success, or propagated failure.</returns>
    public static Result<TOut> Map<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapper)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (mapper == null)
        {
            throw new ArgumentNullException(nameof(mapper));
        }

        if (!result.IsSuccess)
        {
            return Result<TOut>.Failure(
                result.Error ?? new Error("unknown_failure", "An unknown failure occurred"));
        }

        if (result.Value == null)
        {
            return Result<TOut>.Failure(
                new Error("null_success_value", "Success result contained a null value"));
        }

        var mappedValue = mapper(result.Value);
        return Result<TOut>.Success(mappedValue);
    }
}
