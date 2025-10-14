using AwesomeAssertions;
using CDS.CSharpScript2;

namespace UnitTests;

/// <summary>
/// This demonstrates that the key use cases are all satisfied
/// </summary>
[TestClass]
public partial class UT_UseCases
{
    /// <summary>
    /// Check a good script using default settings doesn't have any diagnostics or compilation errors
    /// and can also run without throwing exceptions.
    /// </summary>
    [TestMethod]
    [DataRow("", DisplayName = "Empty script")]
    [DataRow("System.Console.WriteLine();", DisplayName = "Console write blank line")]
    [DataRow("int x = 10; int y = 20; int z = x + y;", DisplayName = "Basic integer maths")]
    [DataRow("System.Math.Pow(2, 10);", DisplayName = "Basic Math class test")]
    public async Task IsolatedScript_DiagnosticsAndCompilation_HasNoErrorsOrWarnings(string script)
    {
        // Setup
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript(script);

        // Check diagnostics
        var diagnostics = await scriptManager.GetDiagnosticsAsync();

        // Compile
        await scriptManager.CompileAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();

        // Verify no diagnostics
        diagnostics.Should().BeEmpty("script should have no diagnostics");

        // Verify compilation succeeded
        compilationOutput.Should().NotBeNull();
        compilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        compilationOutput.Diagnostics.Should().BeEmpty("compilation should have no errors or warnings");

        // Run - no exceptions
        await scriptManager.RunAsync();
    }

    /// <summary>
    /// Check a script can return a calculated value.
    /// </summary>
    [TestMethod]
    public async Task ScriptWithOutput_Compiles_WithoutErrorsOrWarnings()
    {
        // Setup
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript("return System.Math.Pow(10, 2);");

        // Check editor
        var diagnostics = await scriptManager.GetDiagnosticsAsync();

        // Compile
        await scriptManager.CompileAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();

        // Execute
        var result = await scriptManager.RunAsync<double>();

        // Verify no diagnostics
        diagnostics.Should().BeEmpty("script should have no diagnostics");

        // Verify compilation succeeded
        compilationOutput.Should().NotBeNull();
        compilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        compilationOutput.Diagnostics.Should().BeEmpty("compilation should have no errors or warnings");

        // Verify result is correct (10^2 = 100)
        result.Should().Be(100.0, "Math.Pow(10, 2) should equal 100");
    }

    /// <summary>
    /// Test a script that has a usings statement applied (so that the 
    /// script itself doesn't have to use an explicit using statement or the
    /// namespace prefix).
    /// </summary>
    [TestMethod]
    public async Task ScriptWithSuppliedUsing_Compiles_WithoutErrorsOrWarnings()
    {
        // Setup
        var environment = ScriptEnvironment.Default.WithAdditionalNamespaceType(typeof(System.Collections.ArrayList));
        var scriptManager = await ScriptManager.CreateAsync(environment);
        scriptManager = scriptManager.ApplyScript("var arrayList = new ArrayList(); arrayList.Add(1);");

        // Check editor
        var diagnostics = await scriptManager.GetDiagnosticsAsync();

        // Compile 
        await scriptManager.CompileAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();

        // Check runtime
        await scriptManager.RunAsync();

        // Verify no diagnostics
        diagnostics.Should().BeEmpty("script should have no diagnostics");

        // Verify compilation succeeded
        compilationOutput.Should().NotBeNull();
        compilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        compilationOutput.Diagnostics.Should().BeEmpty("compilation should have no errors or warnings");
    }

    /// <summary>
    /// Test a script that has additional namespaces and references applied.
    /// </summary>
    [TestMethod]
    public async Task ScriptWithExtraReferences_CompilesAndRuns_WithoutErrorsOrWarnings()
    {
        // Setup to use the System.Drawing namespace. Note: for .NetFramework this requires
        // the System.Drawing assembly, and for .NetCore/.Net5 
        // onwards this also requires System.Drawing.Primitives assembly.
        var environment =
            ScriptEnvironment
            .Default
            .WithAdditionalNamespaceName("System.Drawing")
            .WithDrawingReferences();

        // Setup
        var scriptManager = await ScriptManager.CreateAsync(environment);
        scriptManager = scriptManager.ApplyScript("var p = new Point(1, 2); return p.X;");

        // Check editor
        var diagnostics = await scriptManager.GetDiagnosticsAsync();

        // Compile
        await scriptManager.CompileAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();

        // Check runtime
        var pX = await scriptManager.RunAsync<int>();

        // Verify no diagnostics
        diagnostics.Should().BeEmpty("script should have no diagnostics");

        // Verify compilation succeeded
        compilationOutput.Should().NotBeNull();
        compilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        compilationOutput.Diagnostics.Should().BeEmpty("compilation should have no errors or warnings");

        // Verify the Point.X value is correct
        pX.Should().Be(1, "Point(1, 2).X should equal 1");
    }

    /// <summary>
    /// Simple global data class for testing.
    /// </summary>
    public class GlobalData
    {
        public string Animal { get; set; } = "Donkey";
    }

    /// <summary>
    /// Tests accessing and modifying global data from within a script.
    /// </summary>
    [TestMethod]
    public async Task ScriptWithGlobalData_CompilesAndRuns_WithoutErrorsOrWarnings()
    {
        // Setup the environment with a global data type
        var environment =
            ScriptEnvironment
            .Default
            .WithGlobalType(typeof(GlobalData));

        // Setup the data and the script
        var globalData = new GlobalData();
        var scriptManager = await ScriptManager.CreateAsync(environment);
        scriptManager = scriptManager.ApplyScript("Animal += \" (modified by script)\"");

        // Get editor diagnostics
        var diagnostics = await scriptManager.GetDiagnosticsAsync();

        // Compile
        await scriptManager.CompileAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();

        // Check runtime
        await scriptManager.RunAsync(globalData);

        // Verify no diagnostics
        diagnostics.Should().BeEmpty("script should have no diagnostics");

        // Verify compilation succeeded
        compilationOutput.Should().NotBeNull();
        compilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        compilationOutput.Diagnostics.Should().BeEmpty("compilation should have no errors or warnings");

        // Verify the global data was modified correctly
        globalData.Animal.Should().Be("Donkey (modified by script)", "script should have appended text to Animal property");
    }
}
