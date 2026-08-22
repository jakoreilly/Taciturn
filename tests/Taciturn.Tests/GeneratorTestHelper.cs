using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Taciturn;

namespace Taciturn.Tests;

/// <summary>
/// Drives TaciturnGenerator directly through CSharpGeneratorDriver against a
/// real Compilation, rather than going through the official source-generator
/// testing package - this needs nothing beyond Microsoft.CodeAnalysis.CSharp,
/// which the generator project already pins, so there is no second package's
/// version to keep in step.
/// </summary>
internal static class GeneratorTestHelper
{
    public sealed record Result(ImmutableArray<Diagnostic> GeneratorDiagnostics, string GeneratedSourceOrEmpty, bool CompilesClean, ImmutableArray<Diagnostic> CompileDiagnostics);

    public static Result Run(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

        var references = TrustedPlatformReferences.Value
            .Append(MetadataReference.CreateFromFile(typeof(TaciturnGenerator).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: "TaciturnTests.Generated",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new TaciturnGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        // The generated file is whichever new syntax tree the driver added beyond
        // the attribute's own post-initialization output and the input tree -
        // tests only ever feed one candidate type, so at most one "real" output
        // file exists per run.
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t != syntaxTree && !t.FilePath.EndsWith("TaciturnAttribute.g.cs", StringComparison.Ordinal))
            .ToList();
        string generatedSource = generatedTrees.Count == 1 ? generatedTrees[0].ToString() : "";

        var compileDiagnostics = outputCompilation.GetDiagnostics();
        bool compilesClean = !compileDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

        return new Result(generatorDiagnostics, generatedSource, compilesClean, compileDiagnostics);
    }

    private static readonly Lazy<MetadataReference[]> TrustedPlatformReferences = new(() =>
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToArray();
    });
}
