using AwesomeAssertions;
using CDS.CSharpScript2;

namespace UnitTests;

[TestClass]
public partial class UT_UseCases
{
    [TestMethod]
    [DataRow("", DisplayName = "Empty script")]
    [DataRow("System.Console.WriteLine();", DisplayName = "Console write blank line")]
    [DataRow("int x = 10; int y = 20; int z = x + y;", DisplayName = "Basic integer maths")]
    [DataRow("System.Math.Pow(2, 10);", DisplayName = "Basic Math class test")]
    public async Task IsolatedScript_DiagnosticsAndCompilation_HasNoErrorsOrWarnings(string script)
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);

        var diagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();
        var executable = await new ScriptExecutor(context).CompileAsync();

        diagnostics.Should().BeEmpty("script should have no diagnostics");
        executable.CompilationOutput.Should().NotBeNull();
        executable.CompilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        executable.CompilationOutput.Diagnostics.Should().BeEmpty("compilation should have no errors or warnings");

        await executable.RunAsync();
    }

    [TestMethod]
    public async Task ScriptWithOutput_Compiles_WithoutErrorsOrWarnings()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("return System.Math.Pow(10, 2);");

        var diagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();
        var executable = await new ScriptExecutor(context).CompileAsync<double>();
        var result = await executable.RunAsync<double>();

        diagnostics.Should().BeEmpty("script should have no diagnostics");
        executable.CompilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        executable.CompilationOutput.Diagnostics.Should().BeEmpty("compilation should have no errors or warnings");
        result.Should().Be(100.0, "Math.Pow(10, 2) should equal 100");
    }

    [TestMethod]
    public async Task ScriptWithSuppliedUsing_Compiles_WithoutErrorsOrWarnings()
    {
        var environment = ScriptEnvironment.Default.WithAdditionalNamespaceType(typeof(System.Collections.ArrayList));
        var context = await ScriptContext.CreateAsync(environment);
        context = context.ApplyScript("var arrayList = new ArrayList(); arrayList.Add(1);");

        var diagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();
        var executable = await new ScriptExecutor(context).CompileAsync();
        await executable.RunAsync();

        diagnostics.Should().BeEmpty("script should have no diagnostics");
        executable.CompilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        executable.CompilationOutput.Diagnostics.Should().BeEmpty("compilation should have no errors or warnings");
    }

    [TestMethod]
    public async Task ScriptWithExtraReferences_CompilesAndRuns_WithoutErrorsOrWarnings()
    {
        var environment =
            ScriptEnvironment
            .Default
            .WithAdditionalNamespaceName("System.Drawing")
            .WithDrawingReferences();

        var context = await ScriptContext.CreateAsync(environment);
        context = context.ApplyScript("var p = new Point(1, 2); return p.X;");

        var diagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();
        var executable = await new ScriptExecutor(context).CompileAsync<int>();
        var pX = await executable.RunAsync<int>();

        diagnostics.Should().BeEmpty("script should have no diagnostics");
        executable.CompilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        executable.CompilationOutput.Diagnostics.Should().BeEmpty("compilation should have no errors or warnings");
        pX.Should().Be(1, "Point(1, 2).X should equal 1");
    }

    public class GlobalData
    {
        public string Animal { get; set; } = "Donkey";
    }

    [TestMethod]
    public async Task ScriptWithGlobalData_CompilesAndRuns_WithoutErrorsOrWarnings()
    {
        var environment = ScriptEnvironment.Default.WithGlobalType(typeof(GlobalData));
        var globalData = new GlobalData();

        var context = await ScriptContext.CreateAsync(environment);
        context = context.ApplyScript("Animal += \" (modified by script)\"");

        var diagnostics = await new ScriptAnalyser(context).GetDiagnosticsAsync();
        var executable = await new ScriptExecutor(context).CompileAsync();
        await executable.RunAsync(globalData);

        diagnostics.Should().BeEmpty("script should have no diagnostics");
        executable.CompilationOutput.ErrorCount.Should().Be(0, "script should compile successfully");
        executable.CompilationOutput.Diagnostics.Should().BeEmpty("compilation should have no errors or warnings");
        globalData.Animal.Should().Be("Donkey (modified by script)");
    }
}
