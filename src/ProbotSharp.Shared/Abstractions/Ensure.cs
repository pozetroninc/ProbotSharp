namespace ProbotSharp.Shared.Abstractions;

/// <summary>
/// Provides validation helper methods for argument checking.
/// </summary>
public static class Ensure
{
    /// <summary>
    /// Ensures that the value is not null.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The non-null value.</returns>
    public static T NotNull<T>(T? value, string name) where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }

        return value;
    }

    /// <summary>
    /// Ensures that the string is not null or whitespace.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The non-null and non-whitespace string.</returns>
    public static string NotNullOrWhiteSpace(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} cannot be null or whitespace.", name);
        }

        return value;
    }

    /// <summary>
    /// Ensures that the integer value is greater than the specified threshold.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="threshold">The threshold value.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The validated value.</returns>
    public static int GreaterThan(int value, int threshold, string name)
    {
        if (value <= threshold)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than {threshold}.");
        }

        return value;
    }

    /// <summary>
    /// Ensures that the long value is greater than the specified threshold.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="threshold">The threshold value.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The validated value.</returns>
    public static long GreaterThan(long value, long threshold, string name)
    {
        if (value <= threshold)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than {threshold}.");
        }

        return value;
    }

    /// <summary>
    /// Ensures that the TimeSpan value is positive.
    /// </summary>
    /// <param name="value">The TimeSpan value to check.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The positive TimeSpan value.</returns>
    public static TimeSpan Positive(TimeSpan value, string name)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be positive.");
        }

        return value;
    }
}
