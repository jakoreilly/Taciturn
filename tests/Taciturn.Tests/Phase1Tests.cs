namespace Taciturn.Tests;

/// <summary>
/// Locks in the shape-agnostic behavior (see plan.md): the not-partial/not-a-
/// record/already-declared guards, and redaction correctness for the simplest
/// shape (sealed record deriving from object). The four PrintMembers signature
/// branches themselves are Phase2Tests' job.
/// </summary>
public class Phase1Tests
{
    [Fact]
    public void Sealed_record_from_object_redacts_every_public_member()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public sealed partial record StripeOptions(
                string PublishableKey,
                string AccountId,
                string WebhookSecret);
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Contains("private bool PrintMembers(System.Text.StringBuilder builder)", result.GeneratedSourceOrEmpty);

        // The real proof: actually construct one and read its live ToString(),
        // not just grep the generated source for the right-looking fragments.
        Assert.Equal(
            "StripeOptions { PublishableKey = «redacted», AccountId = «redacted», WebhookSecret = «redacted» }",
            result.ToStringOf("StripeOptions", "pk_live_abc123", "acct_1M2n3", "whsec_9f2a"));
    }

    [Fact]
    public void Record_with_no_printable_members_returns_false()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public sealed partial record Empty();
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Contains("bool printedAny = false;", result.GeneratedSourceOrEmpty);
        Assert.Contains("return printedAny;", result.GeneratedSourceOrEmpty);
        Assert.DoesNotContain("«redacted»", result.GeneratedSourceOrEmpty);
    }

    [Fact]
    public void Non_partial_record_reports_TACIT001_and_generates_nothing()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public sealed record NotPartial(string Secret);
            """);

        var diag = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("TACIT001", diag.Id);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, diag.Severity);
        Assert.Equal("", result.GeneratedSourceOrEmpty);
    }

    [Fact]
    public void Non_record_type_reports_TACIT002()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public sealed partial class NotARecord
            {
                public string Secret = "";
            }
            """);

        var diag = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("TACIT002", diag.Id);
    }

    [Fact]
    public void Hand_written_ToString_reports_TACIT003_and_stands_down()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public sealed partial record HasOwnToString(string Secret)
            {
                public override string ToString() => "custom";
            }
            """);

        var diag = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("TACIT003", diag.Id);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, diag.Severity);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
    }
}
