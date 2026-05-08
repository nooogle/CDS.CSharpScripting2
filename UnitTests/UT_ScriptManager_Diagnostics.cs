using AwesomeAssertions;
using CDS.CSharpScript2;
using Microsoft.CodeAnalysis;

namespace UnitTests;

[TestClass]
public partial class UT_ScriptManager_Diagnostics
{
    [TestMethod]
    public async Task Script_WithNoErrors_Generates0Diagnostics()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("int xxx = 10; xxx = xxx * 2;");

        var diagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();

        diagnostics.Should().NotBeNull();
        diagnostics.Should().BeEmpty("script should compile without any diagnostics");
    }

    [TestMethod]
    public async Task Script_WithOneError_Generates1Error()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("int xxx = 10; xxx = xxx * yyy;");

        var diagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();

        diagnostics.Should().NotBeNull();
        diagnostics.Length.Should().Be(1, "script should generate exactly one error");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostics[0].GetMessage().Should().Contain("yyy");
    }

    [TestMethod]
    public async Task Script_WithOneWarning_Generates1Warning()
    {
        var script = @"
                [Obsolete(""This method is obsolete. Use NewMethod instead."")]
                static void ObsoleteMethod() { }

               ObsoleteMethod();
            ";

        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);

        var diagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();

        diagnostics.Should().NotBeNull();
        diagnostics.Length.Should().Be(1, "script should generate exactly one warning");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].GetMessage().Should().Contain("obsolete");
    }
}
