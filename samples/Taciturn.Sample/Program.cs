using Taciturn.Sample;

Console.WriteLine(new StripeOptions("pk_live_abc123", "acct_1M2n3", "whsec_9f2a"));
// Expected: StripeOptions { PublishableKey = «redacted», AccountId = acct_1M2n3, WebhookSecret = «redacted» }
// AccountId is [property: Plain], so it's the one member that prints for real.
