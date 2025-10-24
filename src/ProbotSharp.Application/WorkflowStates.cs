// Copyright (c) ProbotSharp Contributors.

namespace ProbotSharp.Application.WorkflowStates;

using ProbotSharp.Application.Models;
using ProbotSharp.Domain.Entities;

/// <summary>
/// Initial state: webhook payload received but not yet validated.
/// Security: Signature not verified, origin untrusted.
/// </summary>
public sealed class UntrustedWebhook
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UntrustedWebhook"/> class.
    /// </summary>
    /// <param name="command">The webhook processing command.</param>
    /// <exception cref="ArgumentNullException">Thrown when command is null.</exception>
    internal UntrustedWebhook(ProcessWebhookCommand command)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
    }

    /// <summary>
    /// Gets the webhook processing command.
    /// </summary>
    public ProcessWebhookCommand Command { get; }
}

/// <summary>
/// State after signature validation: webhook authenticity verified.
/// Security: Signature verified, origin authenticated as GitHub.
/// </summary>
public sealed class ValidatedWebhook
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidatedWebhook"/> class.
    /// </summary>
    /// <param name="untrusted">The untrusted webhook that passed validation.</param>
    /// <exception cref="ArgumentNullException">Thrown when untrusted is null.</exception>
    internal ValidatedWebhook(UntrustedWebhook untrusted)
    {
        if (untrusted == null)
        {
            throw new ArgumentNullException(nameof(untrusted));
        }

        Command = untrusted.Command;
    }

    /// <summary>
    /// Gets the webhook processing command.
    /// </summary>
    public ProcessWebhookCommand Command { get; }
}

/// <summary>
/// State after idempotency check: confirmed not a duplicate delivery.
/// Invariant: Delivery ID has not been processed before.
/// </summary>
public sealed class VerifiedUniqueWebhook
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VerifiedUniqueWebhook"/> class.
    /// </summary>
    /// <param name="validated">The validated webhook that passed idempotency check.</param>
    /// <exception cref="ArgumentNullException">Thrown when validated is null.</exception>
    internal VerifiedUniqueWebhook(ValidatedWebhook validated)
    {
        if (validated == null)
        {
            throw new ArgumentNullException(nameof(validated));
        }

        Command = validated.Command;
    }

    /// <summary>
    /// Gets the webhook processing command.
    /// </summary>
    public ProcessWebhookCommand Command { get; }
}

/// <summary>
/// Final state: webhook persisted and ready for routing.
/// Invariant: WebhookDelivery entity saved, idempotency key set.
/// </summary>
public sealed class PersistedWebhook
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistedWebhook"/> class.
    /// </summary>
    /// <param name="unique">The verified unique webhook.</param>
    /// <param name="delivery">The persisted delivery entity.</param>
    /// <exception cref="ArgumentNullException">Thrown when unique or delivery is null.</exception>
    internal PersistedWebhook(VerifiedUniqueWebhook unique, WebhookDelivery delivery)
    {
        if (unique == null)
        {
            throw new ArgumentNullException(nameof(unique));
        }

        Command = unique.Command;
        Delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
    }

    /// <summary>
    /// Gets the webhook processing command.
    /// </summary>
    public ProcessWebhookCommand Command { get; }

    /// <summary>
    /// Gets the persisted webhook delivery entity.
    /// </summary>
    public WebhookDelivery Delivery { get; }
}
