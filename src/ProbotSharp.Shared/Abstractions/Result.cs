namespace ProbotSharp.Shared.Abstractions;

public readonly record struct Result(bool IsSuccess, Error? Error = null)
{
    public static Result Success() => new(true);

    public static Result Failure(string code, string message, string? details = null)
        => new(false, new Error(code, message, details));

    public static Result Failure(Error error) => new(false, error);

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Error ?? throw new InvalidOperationException("Error should not be null on failure."));
}

public readonly record struct Result<T>(bool IsSuccess, T? Value, Error? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(string code, string message, string? details = null)
        => new(false, default, new Error(code, message, details));

    public static Result<T> Failure(Error error) => new(false, default, error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => IsSuccess && Value is not null
            ? onSuccess(Value)
            : onFailure(Error ?? throw new InvalidOperationException("Error should not be null on failure."));
}

public readonly record struct Error(string Code, string Message, string? Details = null)
{
    public override string ToString()
        => Details is null ? $"{Code}: {Message}" : $"{Code}: {Message} ({Details})";
}

