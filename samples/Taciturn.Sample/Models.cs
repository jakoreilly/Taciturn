using Taciturn;

namespace Taciturn.Sample;

// The motivating example from plan.md: a logged options record whose secrets
// should never reach Seq / the log aggregator via the compiler's own synthesized
// ToString().
[Taciturn]
public sealed partial record StripeOptions(
    string PublishableKey,
    string AccountId,
    string WebhookSecret);
