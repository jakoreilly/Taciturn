namespace Taciturn.Tests;

/// <summary>
/// [Plain] opts individual members back into the clear. Default stays "every
/// member redacted" - these tests exist to prove the exception, not the rule
/// (Phase1Tests/Phase2Tests already cover the unmarked default extensively).
/// </summary>
public class Phase3Tests
{
    [Fact]
    public void Plain_positional_member_prints_real_value_others_stay_redacted()
    {
        // The exact motivating example from the design spec.
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public sealed partial record StripeOptions(
                string PublishableKey,
                [property: Plain] string AccountId,
                string WebhookSecret);
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Equal(
            "StripeOptions { PublishableKey = «redacted», AccountId = acct_1M2n3, WebhookSecret = «redacted» }",
            result.ToStringOf("StripeOptions", "pk_live_abc", "acct_1M2n3", "whsec_9f2a"));
    }

    [Fact]
    public void Plain_field_also_works_not_just_positional_properties()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public sealed partial record WithField(string Secret)
            {
                [Plain] public int RetryCount = 3;
            }
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Equal(
            "WithField { Secret = «redacted», RetryCount = 3 }",
            result.ToStringOf("WithField", "s"));
    }

    [Fact]
    public void All_members_plain_still_redacts_nothing_and_matches_compiler_shape()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public sealed partial record AllPlain([property: Plain] string A, [property: Plain] string B);
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Equal("AllPlain { A = a, B = b }", result.ToStringOf("AllPlain", "a", "b"));
    }

    [Fact]
    public void Plain_member_on_chained_derived_record_prints_alongside_redacted_base()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public partial record BaseRec(string Secret);

            [Taciturn]
            public sealed partial record DerivedRec(string Secret, [property: Plain] string PublicId) : BaseRec(Secret);
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Equal(
            "DerivedRec { Secret = «redacted», PublicId = pub-1 }",
            result.ToStringOf("DerivedRec", "s", "pub-1"));
    }
}
