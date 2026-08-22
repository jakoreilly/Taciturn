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
    public sealed record Result(
        ImmutableArray<Diagnostic> GeneratorDiagnostics,
        string GeneratedSourceOrEmpty,
        bool CompilesClean,
        ImmutableArray<Diagnostic> CompileDiagnostics,
        System.Reflection.Assembly? EmittedAssembly)
    {
        /// <summary>
        /// Constructs an instance of a type from the emitted assembly with the
        /// given constructor arguments and returns its real, live ToString() -
        /// the actual proof that redaction works, not an inference from source
        /// text. Fails loudly (not null) if the type or a matching constructor
        /// isn't found, since a silent null return would make a broken test
        /// look like a passing one.
        /// </summary>
        public string ToStringOf(string typeName, params object[] ctorArgs)
        {
            var assembly = EmittedAssembly ?? throw new InvalidOperationException("Compilation did not emit an assembly - check CompilesClean/CompileDiagnostics first.");
            var type = assembly.GetType(typeName) ?? throw new InvalidOperationException($"Type '{typeName}' not found in emitted assembly.");
            var instance = Activator.CreateInstance(type, ctorArgs) ?? throw new InvalidOperationException($"Activator.CreateInstance returned null for '{typeName}'.");
            return instance.ToString() ?? throw new InvalidOperationException($"'{typeName}'.ToString() returned null.");
        }
    }

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

        // A test source can declare more than one [Taciturn] candidate (e.g. a
        // base + derived pair to exercise chaining), so this concatenates every
        // generated file rather than assuming exactly one - callers grep the
        // combined text rather than needing to know how many types were declared.
        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(t => t != syntaxTree && !t.FilePath.EndsWith("TaciturnAttribute.g.cs", StringComparison.Ordinal))
            .ToList();
        string generatedSource = string.Join("\n// ---\n", generatedTrees.Select(t => t.ToString()));

        var compileDiagnostics = outputCompilation.GetDiagnostics();
        bool compilesClean = !compileDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

        System.Reflection.Assembly? emittedAssembly = null;
        if (compilesClean)
        {
            using var ms = new MemoryStream();
            var emitResult = outputCompilation.Emit(ms);
            if (emitResult.Success)
            {
                emittedAssembly = System.Reflection.Assembly.Load(ms.ToArray());
            }
        }

        return new Result(generatorDiagnostics, generatedSource, compilesClean, compileDiagnostics, emittedAssembly);
    }

    private static readonly Lazy<MetadataReference[]> TrustedPlatformReferences = new(() =>
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToArray();
    });
}
