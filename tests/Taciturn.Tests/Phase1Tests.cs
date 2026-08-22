namespace Taciturn.Tests;

/// <summary>
/// Locks in Phase 1 behavior (see plan.md) before Phase 2 starts touching the
/// same Execute()/Render() paths, so a regression in the sealed-deriving-from-
/// object branch shows up as a failing test instead of a manual re-run.
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
        Assert.Contains("\"PublishableKey = \").Append(\"«redacted»\")", result.GeneratedSourceOrEmpty);
        Assert.Contains("\", AccountId = \").Append(\"«redacted»\")", result.GeneratedSourceOrEmpty);
        Assert.Contains("\", WebhookSecret = \").Append(\"«redacted»\")", result.GeneratedSourceOrEmpty);
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
        Assert.Contains("return false;", result.GeneratedSourceOrEmpty);
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

    [Theory]
    [InlineData("""
        [Taciturn]
        public partial record NonSealed(string Secret);
        """)]
    [InlineData("""
        [Taciturn]
        public partial record BaseRec(string A);
        [Taciturn]
        public sealed partial record DerivedRec(string A, string B) : BaseRec(A);
        """)]
    public void Out_of_phase1_scope_shapes_report_TACIT099(string sourceBody)
    {
        var result = GeneratorTestHelper.Run($"""
            using Taciturn;

            {sourceBody}
            """);

        Assert.All(result.GeneratorDiagnostics, d => Assert.Equal("TACIT099", d.Id));
        Assert.NotEmpty(result.GeneratorDiagnostics);
    }
}
