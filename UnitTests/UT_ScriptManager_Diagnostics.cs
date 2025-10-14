using AwesomeAssertions;
using CDS.CSharpScript2;
using Microsoft.CodeAnalysis;

namespace UnitTests;

[TestClass]
public partial class UT_ScriptManager_Diagnostics
{
    /// <summary>
    /// Check that a script with no errors generates 0 diagnostics messages
    /// </summary>
    [TestMethod]
    public async Task Script_WithNoErrors_Generates0Diagnostics()
    {
        var script = @"int xxx = 10; xxx = xxx * 2;";

        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript(script);
        var diagnostics = await scriptManager.GetDiagnosticsAsync();

        // Verify no diagnostics
        diagnostics.Should().NotBeNull();
        diagnostics.Should().BeEmpty("script should compile without any diagnostics");
    }

    /// <summary>
    /// Check that a script with 1 error generates 1 error
    /// </summary>
    [TestMethod]
    public async Task Script_WithOneError_Generates1Error()
    {
        var script = @"int xxx = 10; xxx = xxx * yyy;";

        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript(script);
        var diagnostics = await scriptManager.GetDiagnosticsAsync();

        // Verify we have exactly one diagnostic
        diagnostics.Should().NotBeNull();
        diagnostics.Length.Should().Be(1, "script should generate exactly one error");

        // Verify it's an error (not warning)
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error, "diagnostic should be an error");

        // Verify the error message mentions the undefined variable
        var message = diagnostics[0].GetMessage();
        message.Should().NotBeNullOrWhiteSpace();
        message.Should().Contain("yyy", "error message should reference the undefined variable 'yyy'");
    }

    /// <summary>
    /// Check that a script with 1 warning generates 1 warning
    /// </summary>
    [TestMethod]
    public async Task Script_WithOneWarning_Generates1Warning()
    {
        var script = @"
                [Obsolete(""This method is obsolete. Use NewMethod instead."")]
                static void ObsoleteMethod() { }

               ObsoleteMethod();
            ";

        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript(script);
        var diagnostics = await scriptManager.GetDiagnosticsAsync();

        // Verify we have exactly one diagnostic
        diagnostics.Should().NotBeNull();
        diagnostics.Length.Should().Be(1, "script should generate exactly one warning");

        // Verify it's a warning (not error)
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning, "diagnostic should be a warning");

        // Verify the warning message mentions obsolete
        var message = diagnostics[0].GetMessage();
        message.Should().NotBeNullOrWhiteSpace();
        message.Should().Contain("obsolete", "warning message should indicate the method is obsolete");
    }
}
