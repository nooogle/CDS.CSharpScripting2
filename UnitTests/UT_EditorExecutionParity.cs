using AwesomeAssertions;
using CDS.CSharpScript2;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace UnitTests;

/// <summary>
/// Guards against configuration drift between the two compilation paths.
/// <see cref="ScriptAnalyser"/> reports diagnostics from a Roslyn workspace project, while
/// <see cref="ScriptExecutor"/> compiles through the Roslyn scripting API. The two are configured
/// independently, so a feature can work in one and fail in the other — the editor showing
/// squiggles under code that compiles and runs perfectly well.
/// </summary>
/// <remarks>
/// Only error-severity diagnostics are compared. Warnings legitimately differ: the paths reference
/// their assemblies differently, which changes version-unification warnings such as CS1701.
/// </remarks>
[TestClass]
public class UT_EditorExecutionParity
{
    private static readonly string s_loadedScriptPath = CreateLoadedScript();

    /// <summary>A script exercised through both compilation paths.</summary>
    /// <remarks>A plain class rather than a record — the test project also targets net48.</remarks>
    public sealed class ParityCase
    {
        /// <summary>Gets the human-readable case name, used as the test display name.</summary>
        public string Name { get; }

        /// <summary>Gets the script text.</summary>
        public string Script { get; }

        /// <summary>Gets the environment both paths are configured with.</summary>
        public ScriptEnvironment Environment { get; }

        /// <summary>Creates a case that uses <see cref="ScriptEnvironment.Default"/>.</summary>
        public ParityCase(string name, string script)
            : this(name, script, ScriptEnvironment.Default)
        {
        }

        /// <summary>Creates a case that uses the supplied environment.</summary>
        public ParityCase(string name, string script, ScriptEnvironment environment)
        {
            Name = name;
            Script = script;
            Environment = environment;
        }

        /// <inheritdoc/>
        public override string ToString() => Name;
    }

    /// <summary>Globals type used by the globals parity case.</summary>
    public class ParityGlobals
    {
        /// <summary>Gets or sets a value the script can read and write.</summary>
        public string Animal { get; set; } = "Donkey";
    }

    [TestMethod]
    [TestCategory("diagnostics")]
    [DynamicData(nameof(GetParityCases), DynamicDataDisplayName = nameof(GetParityCaseName))]
    public async Task Diagnostics_EditorAndExecutionPaths_ReportTheSameErrors(ParityCase parityCase)
    {
        using var root = await ScriptContext.CreateAsync(parityCase.Environment);
        var context = root.ApplyScript(parityCase.Script);

        var editorDiagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();
        var executable = await new ScriptExecutor(context).CompileAsync();

        var editorErrors = SummariseErrors(editorDiagnostics);
        var executionErrors = SummariseErrors(executable.Diagnostics);

        editorErrors.Should().Be(
            executionErrors,
            "the editor must report the same compile errors as the execution path");
    }

    private static IEnumerable<object[]> GetParityCases()
    {
        // The non-generic overload is required here: Enumerable is a static type, so it cannot be
        // used as a generic type argument.
        var linqEnvironment = ScriptEnvironment.Default
            .WithAdditionalNamespaceName("System.Linq")
            .WithAdditionalReferenceForType(typeof(System.Linq.Enumerable));

        var globalsEnvironment = ScriptEnvironment.Default.WithGlobalType(typeof(ParityGlobals));

        var externalAssemblyPath = typeof(MathNet.Numerics.Distributions.Normal).Assembly.Location;

        var assemblyDirectoryEnvironment = ScriptEnvironment.Default
            .WithBaseDirectory(Path.GetDirectoryName(externalAssemblyPath)!);

        var loadedScriptDirectoryEnvironment = ScriptEnvironment.Default
            .WithBaseDirectory(Path.GetDirectoryName(s_loadedScriptPath)!);

        var cases = new[]
        {
            new ParityCase(
                "Empty script",
                ""),

            new ParityCase(
                "Basic statements",
                "int x = 10; int y = x * 2;"),

            new ParityCase(
                "Unresolved symbol",
                "int x = missing;"),

            new ParityCase(
                "Type mismatch",
                """int x = "text";"""),

            new ParityCase(
                "Reference directive",
                $"""
                #r "{externalAssemblyPath}"
                var distribution = new MathNet.Numerics.Distributions.Normal(0.0, 1.0);
                """),

            new ParityCase(
                // Regression test: a #r directive resolved via environment.MetadataResolver used to
                // pull in the real System.Private.CoreLib, which collided with the reference-assembly
                // facade ScriptContext.CreateCore force-adds for System.Runtime — CS0433 on the editor
                // path only, for any type (Stopwatch here) the facade defines independently rather
                // than forwarding.
                "Reference directive alongside a BCL type",
                $"""
                #r "{externalAssemblyPath}"
                var distribution = new MathNet.Numerics.Distributions.Normal(0.0, 1.0);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                """),

            new ParityCase(
                "Load directive",
                $"""
                #load "{s_loadedScriptPath}"
                var tripled = Triple(4);
                """),

            new ParityCase(
                "Relative reference directive",
                $"""
                #r "{Path.GetFileName(externalAssemblyPath)}"
                var distribution = new MathNet.Numerics.Distributions.Normal(0.0, 1.0);
                """,
                assemblyDirectoryEnvironment),

            new ParityCase(
                "Relative load directive",
                $"""
                #load "{Path.GetFileName(s_loadedScriptPath)}"
                var tripled = Triple(4);
                """,
                loadedScriptDirectoryEnvironment),

            new ParityCase(
                "Top-level return",
                "return 42;"),

            new ParityCase(
                "Await",
                "await System.Threading.Tasks.Task.Delay(1);"),

            new ParityCase(
                "LINQ extension methods",
                "var evens = new[] { 1, 2, 3, 4 }.Where(n => n % 2 == 0).ToList();",
                linqEnvironment),

            new ParityCase(
                "Collection expression",
                "int[] values = [1, 2, 3]; var total = values.Length;"),

            new ParityCase(
                "Raw string literal",
                """"
                var text = """
                    hello
                    """;
                var length = text.Length;
                """"),

            new ParityCase(
                "Record with primary constructor",
                "record Point3(int X, int Y); var point = new Point3(1, 2); var x = point.X;"),

            new ParityCase(
                "Local function",
                "int Double(int value) => value * 2; var doubled = Double(21);"),

            new ParityCase(
                "Nullable annotation",
                "string? maybe = null; var length = maybe?.Length;"),

            new ParityCase(
                "Globals member access",
                """Animal = Animal + " (modified)";""",
                globalsEnvironment),
        };

        return cases.Select(c => new object[] { c });
    }

    public static string GetParityCaseName(MethodInfo methodInfo, object[] data)
        => $"{methodInfo.Name} ({data[0]})";

    /// <summary>
    /// Reduces diagnostics to a stable, comparable summary of distinct error IDs.
    /// IDs rather than messages, so the comparison survives wording and locale differences.
    /// </summary>
    private static string SummariseErrors(IEnumerable<Diagnostic> diagnostics)
    {
        var ids = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id)
            .Distinct()
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        return ids.Count == 0 ? "(no errors)" : string.Join(", ", ids);
    }

    private static string CreateLoadedScript()
    {
        // Fixed filename so repeated runs overwrite rather than litter the temp folder.
        var path = Path.Combine(Path.GetTempPath(), "CDS.CSharpScript2.ParityLoadHelper.csx");
        File.WriteAllText(path, "int Triple(int value) => value * 3;" + System.Environment.NewLine);
        return path;
    }
}
