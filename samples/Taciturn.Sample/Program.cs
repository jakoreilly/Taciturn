using Taciturn.Sample;

Console.WriteLine(new StripeOptions("pk_live_abc123", "acct_1M2n3", "whsec_9f2a"));
// Expected: StripeOptions { PublishableKey = «redacted», AccountId = «redacted», WebhookSecret = «redacted» }
// (Phase 1 has no [Plain] yet, so AccountId is redacted too — that's the known,
// documented limitation of this milestone, not a bug.)
