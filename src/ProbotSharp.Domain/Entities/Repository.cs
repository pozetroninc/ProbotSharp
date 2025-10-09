// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using ProbotSharp.Domain.Abstractions;

namespace ProbotSharp.Domain.Entities;

public sealed class Repository : Entity<long>
{
    private Repository(long id, string name, string fullName)
        : base(id)
    {
        this.Name = name;
        this.FullName = fullName;
    }

    public string Name { get; private set; }

    public string FullName { get; private set; }

    public static Repository Create(long id, string name, string fullName)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Repository ID must be positive.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Repository name cannot be null or whitespace.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Repository full name cannot be null or whitespace.", nameof(fullName));
        }

        return new Repository(id, name.Trim(), fullName.Trim());
    }

    internal static Repository Restore(long id, string name, string fullName)
        => new(id, name, fullName);

    public void Rename(string name, string fullName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Repository name cannot be null or whitespace.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Repository full name cannot be null or whitespace.", nameof(fullName));
        }

        this.Name = name.Trim();
        this.FullName = fullName.Trim();
    }
}

