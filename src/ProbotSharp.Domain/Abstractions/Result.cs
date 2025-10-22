namespace ProbotSharp.Domain.Abstractions;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
/// <param name="IsSuccess">Indicates whether the operation succeeded.</param>
/// <param name="Error">The error details if the operation failed.</param>
public readonly record struct Result(bool IsSuccess, Error? Error = null)
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with the specified error details.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional additional error details.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(string code, string message, string? details = null)
        => new(false, new Error(code, message, details));

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error details.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Matches the result to one of two functions based on success or failure.
    /// </summary>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <param name="onSuccess">Function to execute on success.</param>
    /// <param name="onFailure">Function to execute on failure.</param>
    /// <returns>The result of the matched function.</returns>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
        => this.IsSuccess ? onSuccess() : onFailure(this.Error ?? throw new InvalidOperationException("Error should not be null on failure."));
}

/// <summary>
/// Represents the result of an operation that can succeed with a value or fail.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
/// <param name="IsSuccess">Indicates whether the operation succeeded.</param>
/// <param name="Value">The success value if the operation succeeded.</param>
/// <param name="Error">The error details if the operation failed.</param>
public readonly record struct Result<T>(bool IsSuccess, T? Value, Error? Error)
{
#pragma warning disable CA1000 // Static factory methods on generic types provide ergonomic API for result pattern
    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful result.</returns>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result with the specified error details.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional additional error details.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(string code, string message, string? details = null)
        => new(false, default, new Error(code, message, details));

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error details.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Failure(Error error) => new(false, default, error);
#pragma warning restore CA1000

    /// <summary>
    /// Matches the result to one of two functions based on success or failure.
    /// </summary>
    /// <typeparam name="TResult">The output type.</typeparam>
    /// <param name="onSuccess">Function to execute on success.</param>
    /// <param name="onFailure">Function to execute on failure.</param>
    /// <returns>The result of the matched function.</returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => this.IsSuccess && this.Value is not null
            ? onSuccess(this.Value)
            : onFailure(this.Error ?? throw new InvalidOperationException("Error should not be null on failure."));
}

/// <summary>
/// Represents an error with a code, message, and optional details.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
/// <param name="Details">Optional additional error details.</param>
public readonly record struct Error(string Code, string Message, string? Details = null)
{
    /// <summary>
    /// Returns a string representation of the error.
    /// </summary>
    /// <returns>A formatted string containing the error code, message, and details.</returns>
    public override string ToString()
        => this.Details is null ? $"{this.Code}: {this.Message}" : $"{this.Code}: {this.Message} ({this.Details})";
}
