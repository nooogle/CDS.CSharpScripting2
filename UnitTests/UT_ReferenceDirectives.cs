using AwesomeAssertions;
using CDS.CSharpScript2;
using Microsoft.CodeAnalysis;

namespace UnitTests;

/// <summary>
/// Covers <c>#r</c> and <c>#load</c> support in the editor analysis path, which must accept the
/// same directives as the execution path.
/// </summary>
[TestClass]
public class UT_ReferenceDirectives
{
    private static string ExternalAssemblyPath
        => typeof(MathNet.Numerics.Distributions.Normal).Assembly.Location;

    private static string ScriptUsingExternalType
        => "var distribution = new MathNet.Numerics.Distributions.Normal(0.0, 1.0);";

    [TestMethod]
    [TestCategory("diagnostics")]
    public async Task GetDiagnostics_ScriptWithReferenceDirective_GeneratesNoErrors()
    {
        var script = $"""
            #r "{ExternalAssemblyPath}"
            {ScriptUsingExternalType}
            """;

        using var context = await ScriptContext.CreateAsync();

        var diagnostics = await new ScriptAnalyser(context.ApplyScript(script)).GetDiagnosticsAsync();

        diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty("#r should make the referenced assembly visible to the editor");
    }

    [TestMethod]
    [TestCategory("diagnostics")]
    public async Task GetDiagnostics_ScriptWithoutReferenceDirective_ReportsUnresolvedType()
    {
        using var context = await ScriptContext.CreateAsync();

        var diagnostics = await new ScriptAnalyser(context.ApplyScript(ScriptUsingExternalType))
            .GetDiagnosticsAsync();

        diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Should().NotBeEmpty("the assembly is only reachable via #r");
    }

    [TestMethod]
    [TestCategory("completions")]
    public async Task GetCompletions_TypeFromReferenceDirective_OffersItsMembers()
    {
        var script = $"""
            #r "{ExternalAssemblyPath}"
            var distribution = new MathNet.Numerics.Distributions.Normal(0.0, 1.0);
            distribution.
            """;

        using var context = await ScriptContext.CreateAsync();

        var completions = await new ScriptAnalyser(context.ApplyScript(script))
            .GetCompletionsAsync(script.LastIndexOf('.') + 1);

        completions.Select(c => c.DisplayText).Should().Contain("StdDev");
    }

    [TestMethod]
    [TestCategory("diagnostics")]
    public async Task GetDiagnostics_ScriptWithLoadDirective_SeesTheLoadedDeclarations()
    {
        var loadedScriptPath = Path.Combine(
            Path.GetTempPath(),
            "CDS.CSharpScript2.LoadDirectiveHelper.csx");

        File.WriteAllText(loadedScriptPath, "int Triple(int value) => value * 3;");

        var script = $"""
            #load "{loadedScriptPath}"
            var tripled = Triple(4);
            """;

        using var context = await ScriptContext.CreateAsync();

        var diagnostics = await new ScriptAnalyser(context.ApplyScript(script)).GetDiagnosticsAsync();

        diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty("#load should bring the loaded file's declarations into scope");
    }

    [TestMethod]
    [TestCategory("compilation")]
    public async Task Compile_ScriptWithReferenceDirective_CompilesWithoutErrors()
    {
        var script = $"""
            #r "{ExternalAssemblyPath}"
            {ScriptUsingExternalType}
            """;

        using var context = await ScriptContext.CreateAsync();

        var executable = await new ScriptExecutor(context.ApplyScript(script)).CompileAsync();

        executable.HasErrors.Should().BeFalse();
    }
}
