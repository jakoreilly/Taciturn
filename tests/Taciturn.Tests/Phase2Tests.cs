namespace Taciturn.Tests;

/// <summary>
/// The three shapes that were TACIT099 in Phase 1 now generate correctly
/// instead. Each test both greps the generated text for the right modifiers
/// and, more importantly, actually compiles + runs the redacted ToString() to
/// prove the chaining arithmetic (comma placement, base-then-own ordering) is
/// right — a grep alone can't catch an off-by-one in the separator logic.
/// </summary>
public class Phase2Tests
{
    [Fact]
    public void NonSealed_record_from_object_uses_protected_virtual()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public partial record NonSealed(string Secret);
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Contains("protected virtual bool PrintMembers(System.Text.StringBuilder builder)", result.GeneratedSourceOrEmpty);
        Assert.DoesNotContain("base.PrintMembers", result.GeneratedSourceOrEmpty);
        Assert.Equal("NonSealed { Secret = «redacted» }", result.ToStringOf("NonSealed", "s"));
    }

    [Fact]
    public void RecordStruct_uses_private_and_no_base_chain()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public partial record struct Coords(double Lat, double Lng);
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Contains("partial record struct Coords", result.GeneratedSourceOrEmpty);
        Assert.Contains("private bool PrintMembers(System.Text.StringBuilder builder)", result.GeneratedSourceOrEmpty);
        Assert.DoesNotContain("base.PrintMembers", result.GeneratedSourceOrEmpty);
        Assert.Equal("Coords { Lat = «redacted», Lng = «redacted» }", result.ToStringOf("Coords", 1.0, 2.0));
    }

    [Fact]
    public void Sealed_record_deriving_from_marked_base_chains_and_compiles_correctly()
    {
        // The real end-to-end proof: both records are [Taciturn], the base has
        // one member and the derived type adds one more, and the redacted
        // ToString() must interleave them correctly with exactly one comma.
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public partial record BaseRec(string A);

            [Taciturn]
            public sealed partial record DerivedRec(string A, string B) : BaseRec(A);
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Contains("protected virtual bool PrintMembers", result.GeneratedSourceOrEmpty); // BaseRec: non-sealed, from object
        Assert.Contains("protected override bool PrintMembers", result.GeneratedSourceOrEmpty); // DerivedRec: derives from a record
        Assert.Contains("base.PrintMembers(builder)", result.GeneratedSourceOrEmpty);

        // The comma-placement arithmetic is exactly what a manual re-derivation
        // gets subtly wrong, so this checks the live output, not just that both
        // signatures appear in the generated text.
        Assert.Equal(
            "DerivedRec { A = «redacted», B = «redacted» }",
            result.ToStringOf("DerivedRec", "a-val", "b-val"));
    }

    [Fact]
    public void NonSealed_record_deriving_from_marked_base_also_uses_override()
    {
        // Sealed-ness of the derived type doesn't change the signature once it
        // derives from another record - only whether chaining happens at all.
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public partial record BaseRec(string A);

            [Taciturn]
            public partial record MiddleRec(string A, string B) : BaseRec(A);
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Contains("protected override bool PrintMembers", result.GeneratedSourceOrEmpty);
        Assert.Equal("MiddleRec { A = «redacted», B = «redacted» }", result.ToStringOf("MiddleRec", "a", "b"));
    }

    [Fact]
    public void Derived_record_leaving_base_unmarked_still_compiles_and_chains_to_compiler_synthesized_base()
    {
        // BaseRec here is a plain record (no [Taciturn]) - its PrintMembers is
        // whatever the compiler synthesized, not Taciturn's. DerivedRec's
        // base.PrintMembers call must still resolve and compile against it.
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            public partial record BaseRec(string A);

            [Taciturn]
            public sealed partial record DerivedRec(string A, string B) : BaseRec(A);
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Contains("protected override bool PrintMembers", result.GeneratedSourceOrEmpty);

        // A is real (BaseRec was never marked, so the compiler's own PrintMembers
        // printed it honestly) - only B, DerivedRec's own member, is redacted.
        // This is the exact gap TACIT004 (a later phase) exists to warn about:
        // protection is per-type, not per-hierarchy.
        Assert.Equal("DerivedRec { A = a-val, B = «redacted» }", result.ToStringOf("DerivedRec", "a-val", "b-val"));
    }
}
