// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ProbotSharp.Domain.Attachments;

/// <summary>
/// Represents a key-value field within a comment attachment.
/// Fields are typically rendered as bold key-value pairs.
/// </summary>
public class AttachmentField
{
    /// <summary>
    /// Gets or sets the title (key) of the field.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the field.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this field should be displayed in a compact format.
    /// Short fields can be displayed side-by-side in some rendering contexts.
    /// </summary>
    public bool Short { get; set; }
}
