using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using CDS.CSharpScript2;
using CDS.CSharpScript2.Editors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ConsoleTest.Demos;

/// <summary>
/// Measures how editor responsiveness scales with script size and with <c>#r</c>/<c>#load</c>
/// directives, and writes the results to a CSV file for offline review.
/// </summary>
/// <remarks>
/// Written up as an open question in optimise.md §8 ("reproduce with a realistic hand-written
/// script... to confirm the §2.3 size scaling holds") and as a documented gap in §12 ("not
/// covered, deliberately" - the original findings came from throwaway probes, not anything
/// checked in). This is that measurement, made repeatable.
/// <para>
/// Reports raw engine cost per call (like the optimise.md §2 probes) rather than simulating the
/// Scintilla editor's debounce policy - the point here is comparing how cost scales with size and
/// directives, not reproducing perceived UX. See <see cref="Completions.TypingSessionDemo"/> for
/// the debounce-aware version of that question.
/// </para>
/// </remarks>
class ResponsivenessBenchmark
{
    public static string Name => "Responsiveness benchmark (CSV)";
    public static string Description => "Measures colour-coding and completion latency across script sizes and #r/#load directives; writes a CSV for review.";

    private const int IterationsPerConfiguration = 3;
    private static readonly int[] LineCounts = [10, 100, 1000];

    public static void Run() => new ResponsivenessBenchmark().RunAsync().Wait();

    public async Task RunAsync()
    {
        Console.Clear();
        Console.WriteLine("Responsiveness benchmark");
        Console.WriteLine("=========================\n");
        Console.WriteLine("Measures ApplySyntacticPassAsync (tier 1 colouring), ApplyScript (diagnostics + semantic");
        Console.WriteLine("colouring), and a member-access completion request, at several script sizes, with and");
        Console.WriteLine("without a #r / #load directive. Raw engine cost per call - no UI debounce simulated.\n");

        var workDirectory = Path.Combine(Path.GetTempPath(), "cds_responsiveness_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(workDirectory);

        try
        {
            var fixtureDll = Path.Combine(workDirectory, "Fixture.dll");
            var fixtureScript = Path.Combine(workDirectory, "Fixture.csx");
            EmitFixtureAssembly(fixtureDll);
            File.WriteAllText(fixtureScript, "public static class BenchmarkLoadedHelper { public static int Answer => 42; }");

            var rows = new List<BenchmarkRow>();

            foreach (var lineCount in LineCounts)
            {
                foreach (var directive in Enum.GetValues<DirectiveKind>())
                {
                    Console.WriteLine($"Running: {lineCount,5} lines, directive = {directive}...");
                    rows.Add(await MeasureAsync(lineCount, directive, workDirectory));
                }
            }

            var csvPath = Path.Combine(GetOutputDirectory(), $"responsiveness-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
            WriteCsv(csvPath, rows);

            Console.WriteLine();
            PrintSummaryTable(rows);
            Console.WriteLine($"\nWrote {rows.Count} rows to {csvPath}");
            Console.WriteLine();
            Console.WriteLine("Caveat: all configurations ran in this one process, in the fixed order above.");
            Console.WriteLine("Per-config warm-up covers JIT/workspace cost for that config alone; it does not");
            Console.WriteLine("isolate a config from one-off process-wide effects (tiered JIT promotion, heap");
            Console.WriteLine("growth) left behind by whichever earlier config first touched that code path or");
            Console.WriteLine("script size. Treat differences of a few ms as noise; for a rigorous cross-config");
            Console.WriteLine("comparison, run each configuration in its own process.");
        }
        finally
        {
            try { Directory.Delete(workDirectory, recursive: true); } catch (IOException) { }
        }

        Console.WriteLine("\nDone - press any key to return to the menu.");
        Console.ReadKey(intercept: true);
    }

    private enum DirectiveKind
    {
        None,
        Reference,
        Load,
    }

    private sealed record BenchmarkRow(
        int LineCount,
        DirectiveKind Directive,
        double SyntacticMeanMs,
        double SyntacticMaxMs,
        double FullPassMeanMs,
        double FullPassMaxMs,
        double CompletionMeanMs,
        double CompletionMaxMs,
        int DiagnosticCount,
        int ClassificationCount,
        int CompletionCount);

    private static async Task<BenchmarkRow> MeasureAsync(int lineCount, DirectiveKind directive, string workDirectory)
    {
        var environment = ScriptEnvironment.Default.WithBaseDirectory(workDirectory);
        var script = GenerateScript(lineCount, directive);
        var probeScript = script + "\nConsole.";
        var probeCaret = probeScript.Length;

        using var manager = new EditorManager(environment);

        // Warm-up: builds the workspace and JITs the analysis/completion paths. Excluded below.
        // This only covers per-config JIT/workspace cost - it does not fully isolate a config from
        // process-wide effects (tiered JIT promotion, heap growth for larger scripts) left behind by
        // whichever earlier config first exercised that code path or size. See the caveat printed
        // at the end of the run, and optimise.md's §10 Step 1 measurement note for the same pitfall.
        await manager.ApplyScript(script);
        await manager.ApplySyntacticPassAsync(script, CancellationToken.None);
        await manager.UpdateScriptDocumentAsync(probeScript);
        await manager.GetAutoCompletions(probeCaret);

        var syntacticMs = new List<double>();
        var fullPassMs = new List<double>();
        var completionMs = new List<double>();
        var diagnosticCount = 0;
        var classificationCount = 0;
        var completionCount = 0;

        for (var i = 0; i < IterationsPerConfiguration; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await manager.ApplySyntacticPassAsync(script, CancellationToken.None);
            syntacticMs.Add(stopwatch.Elapsed.TotalMilliseconds);

            stopwatch.Restart();
            await manager.ApplyScript(script);
            fullPassMs.Add(stopwatch.Elapsed.TotalMilliseconds);
            diagnosticCount = manager.LastDiagnostics.Length;
            classificationCount = manager.LastClassifications.Count;

            stopwatch.Restart();
            await manager.UpdateScriptDocumentAsync(probeScript);
            var completions = (await manager.GetAutoCompletions(probeCaret)).ToList();
            completionMs.Add(stopwatch.Elapsed.TotalMilliseconds);
            completionCount = completions.Count;
        }

        return new BenchmarkRow(
            lineCount,
            directive,
            syntacticMs.Average(), syntacticMs.Max(),
            fullPassMs.Average(), fullPassMs.Max(),
            completionMs.Average(), completionMs.Max(),
            diagnosticCount, classificationCount, completionCount);
    }

    /// <summary>Generates a syntactically valid script of roughly <paramref name="targetLineCount"/> lines.</summary>
    private static string GenerateScript(int targetLineCount, DirectiveKind directive)
    {
        var builder = new StringBuilder();

        switch (directive)
        {
            case DirectiveKind.Reference:
                builder.AppendLine("#r \"Fixture.dll\"");
                break;
            case DirectiveKind.Load:
                builder.AppendLine("#load \"Fixture.csx\"");
                break;
        }

        var lines = 0;
        var methodIndex = 0;

        while (lines < targetLineCount)
        {
            builder.AppendLine($"int Compute{methodIndex}(int x)");
            builder.AppendLine("{");
            builder.AppendLine($"    var value{methodIndex} = x * {methodIndex} + 1;");
            builder.AppendLine($"    if (value{methodIndex} % 2 == 0)");
            builder.AppendLine("    {");
            builder.AppendLine($"        value{methodIndex} += 1;");
            builder.AppendLine("    }");
            builder.AppendLine($"    return value{methodIndex};");
            builder.AppendLine("}");
            lines += 9;
            methodIndex++;
        }

        builder.AppendLine("var total = 0;");
        builder.AppendLine($"for (var j = 0; j < {methodIndex}; j++)");
        builder.AppendLine("{");
        builder.AppendLine("    total += j;");
        builder.AppendLine("}");
        builder.AppendLine("Console.WriteLine($\"total={total}\");");

        return builder.ToString();
    }

    private static void EmitFixtureAssembly(string path)
    {
        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(path),
            [CSharpSyntaxTree.ParseText("public static class BenchmarkFixture { public static int Answer => 42; }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        var result = compilation.Emit(stream);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                "Failed to emit the benchmark fixture assembly: " +
                string.Join("; ", result.Diagnostics.Select(d => d.ToString())));
        }
    }

    /// <summary>Resolves to <c>ConsoleTest/BenchmarkResults</c> regardless of the process's working directory.</summary>
    private static string GetOutputDirectory([CallerFilePath] string sourceFile = "")
    {
        var demosDirectory = Path.GetDirectoryName(sourceFile)!;
        var projectDirectory = Path.GetDirectoryName(demosDirectory)!;
        var outputDirectory = Path.Combine(projectDirectory, "BenchmarkResults");
        Directory.CreateDirectory(outputDirectory);
        return outputDirectory;
    }

    private static void WriteCsv(string path, IReadOnlyList<BenchmarkRow> rows)
    {
        using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
        writer.WriteLine("LineCount,Directive,SyntacticMeanMs,SyntacticMaxMs,FullPassMeanMs,FullPassMaxMs,CompletionMeanMs,CompletionMaxMs,DiagnosticCount,ClassificationCount,CompletionCount");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",",
                row.LineCount,
                row.Directive,
                Format(row.SyntacticMeanMs),
                Format(row.SyntacticMaxMs),
                Format(row.FullPassMeanMs),
                Format(row.FullPassMaxMs),
                Format(row.CompletionMeanMs),
                Format(row.CompletionMaxMs),
                row.DiagnosticCount,
                row.ClassificationCount,
                row.CompletionCount));
        }
    }

    private static string Format(double milliseconds) => milliseconds.ToString("F2", CultureInfo.InvariantCulture);

    private static void PrintSummaryTable(IReadOnlyList<BenchmarkRow> rows)
    {
        Console.WriteLine($"{"Lines",6} {"Directive",-9} {"Syntactic",-14} {"FullPass",-14} {"Completion",-14}");

        foreach (var row in rows)
        {
            Console.WriteLine(
                $"{row.LineCount,6} {row.Directive,-9} " +
                $"{$"{Format(row.SyntacticMeanMs)} ({Format(row.SyntacticMaxMs)})",-14} " +
                $"{$"{Format(row.FullPassMeanMs)} ({Format(row.FullPassMaxMs)})",-14} " +
                $"{$"{Format(row.CompletionMeanMs)} ({Format(row.CompletionMaxMs)})",-14}");
        }

        Console.WriteLine("\n(mean (max), milliseconds)");
    }
}
