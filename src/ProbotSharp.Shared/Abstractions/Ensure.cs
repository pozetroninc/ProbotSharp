namespace ProbotSharp.Shared.Abstractions;

public static class Ensure
{
    public static T NotNull<T>(T? value, string name) where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }

        return value;
    }

    public static string NotNullOrWhiteSpace(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} cannot be null or whitespace.", name);
        }

        return value;
    }

    public static int GreaterThan(int value, int threshold, string name)
    {
        if (value <= threshold)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than {threshold}.");
        }

        return value;
    }

    public static long GreaterThan(long value, long threshold, string name)
    {
        if (value <= threshold)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than {threshold}.");
        }

        return value;
    }

    public static TimeSpan Positive(TimeSpan value, string name)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be positive.");
        }

        return value;
    }
}

