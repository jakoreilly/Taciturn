using System.Reflection;

namespace Taciturn.Tests;

/// <summary>
/// TACIT004 (unmarked record deriving from a marked one) and the generated
/// DebuggerTypeProxy - the final phase from plan.md.
/// </summary>
public class Phase4Tests
{
    [Fact]
    public void Unmarked_record_deriving_from_marked_base_reports_TACIT004()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public partial record BaseRec(string Secret);

            public sealed partial record UnmarkedDerived(string Secret, string Extra) : BaseRec(Secret);
            """);

        var diag = Assert.Single(result.GeneratorDiagnostics, d => d.Id == "TACIT004");
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, diag.Severity);
        Assert.Contains("UnmarkedDerived", diag.GetMessage());
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
    }

    [Fact]
    public void Marked_derived_record_does_not_report_TACIT004()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public partial record BaseRec(string Secret);

            [Taciturn]
            public sealed partial record MarkedDerived(string Secret, string Extra) : BaseRec(Secret);
            """);

        Assert.DoesNotContain(result.GeneratorDiagnostics, d => d.Id == "TACIT004");
    }

    [Fact]
    public void Record_unrelated_to_any_Taciturn_type_does_not_report_TACIT004()
    {
        var result = GeneratorTestHelper.Run("""
            public partial record BaseRec(string A);
            public sealed partial record Derived(string A, string B) : BaseRec(A);
            """);

        Assert.DoesNotContain(result.GeneratorDiagnostics, d => d.Id == "TACIT004");
    }

    [Fact]
    public void DebuggerTypeProxy_view_exposes_same_redacted_split_as_ToString()
    {
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
        Assert.Contains("[System.Diagnostics.DebuggerTypeProxy(typeof(StripeOptions.StripeOptionsDebugView))]", result.GeneratedSourceOrEmpty);

        var type = result.EmittedAssembly!.GetType("StripeOptions")!;
        var instance = Activator.CreateInstance(type, "pk_123", "acct_1M2n3", "whsec_9f2a")!;
        var proxyAttr = type.GetCustomAttribute<System.Diagnostics.DebuggerTypeProxyAttribute>();
        Assert.NotNull(proxyAttr);

        var debugViewType = type.GetNestedType("StripeOptionsDebugView", BindingFlags.NonPublic)!;
        var debugView = Activator.CreateInstance(debugViewType, instance)!;

        Assert.Equal("«redacted»", debugViewType.GetProperty("PublishableKey")!.GetValue(debugView));
        Assert.Equal("acct_1M2n3", debugViewType.GetProperty("AccountId")!.GetValue(debugView));
        Assert.Equal("«redacted»", debugViewType.GetProperty("WebhookSecret")!.GetValue(debugView));
    }

    [Fact]
    public void DebuggerTypeProxy_on_generic_record_uses_correct_unbound_generic_typeof()
    {
        var result = GeneratorTestHelper.Run("""
            using Taciturn;

            [Taciturn]
            public sealed partial record Wrapper<T>(T Value, [property: Plain] string Tag);
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.True(result.CompilesClean, string.Join("\n", result.CompileDiagnostics));
        Assert.Contains("typeof(Wrapper<>.WrapperDebugView)", result.GeneratedSourceOrEmpty);

        var openType = result.EmittedAssembly!.GetType("Wrapper`1")!;
        var closedType = openType.MakeGenericType(typeof(int));
        var instance = Activator.CreateInstance(closedType, 42, "tag-1")!;
        Assert.Equal("Wrapper { Value = «redacted», Tag = tag-1 }", instance.ToString());
    }
}
