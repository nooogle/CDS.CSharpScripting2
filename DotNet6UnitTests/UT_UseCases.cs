using CDS.CSScripting;

namespace DotNet6UnitTests
{
    /// <summary>
    /// This demonstrates that the key use cases are all satistifed
    /// </summary>
    [TestClass]
    public class UT_UseCases
    {

        /// <summary>
        /// Check a script using default settings compiles and runs without any issues.
        /// </summary>
        [TestMethod]
        [DataRow("", DisplayName = "Empty script")]
        [DataRow("System.Console.WriteLine();", DisplayName = "Console write blank line;")]
        [DataRow("int x = 10; int y = 20; int z = x + y;", DisplayName = "Basic integer maths")]
        [DataRow("System.Math.Pow(2, 10);", DisplayName = "Basic Math class test")]
        public async Task IsolatedScript_Compiles_WithoutErrorsOrWarnings(string script)
        {
            // Setup
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript(script);

            // Check editor
            var diagnostics = await scriptManager.GetDiagnosticsAsync();
            diagnostics.Length.Should().Be(0);

            // Check compilation
            await scriptManager.CompileAsync();
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();
            compilationOutput.Messages.Length.Should().Be(0);
            compilationOutput.ErrorCount.Should().Be(0);
            compilationOutput.WarningCount.Should().Be(0);

            // Run
            await scriptManager.RunAsync<object>();
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
            diagnostics.Length.Should().Be(0);

            // Check compilation
            await scriptManager.CompileAsync();
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();
            compilationOutput.Messages.Length.Should().Be(0);
            compilationOutput.ErrorCount.Should().Be(0);
            compilationOutput.WarningCount.Should().Be(0);

            // Check execution
            var result = await scriptManager.RunAsync<double>();
            result.Should().Be(100);
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
            var environment = Env.Default.WithAdditionalNamespaceType(typeof(System.Collections.ArrayList));
            var scriptManager = await ScriptManager.CreateAsync(environment);
            scriptManager = scriptManager.ApplyScript("var arrayList = new ArrayList(); arrayList.Add(1);");

            // Check editor
            var diagnostics = await scriptManager.GetDiagnosticsAsync();
            diagnostics.Length.Should().Be(0);

            // Check compilation 
            await scriptManager.CompileAsync();
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();
            compilationOutput.Messages.Length.Should().Be(0);
            compilationOutput.ErrorCount.Should().Be(0);
            compilationOutput.WarningCount.Should().Be(0);

            // Check runtime
            await scriptManager.RunAsync<object>();
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
                Env
                .Default
                .WithAdditionalNamespaceName("System.Drawing")
                .WithDrawingReferences();

            // Setup
            var scriptManager = await ScriptManager.CreateAsync(environment);
            scriptManager = scriptManager.ApplyScript("var p = new Point(1, 2); return p.X;");

            // Check editor
            var diagnostics = await scriptManager.GetDiagnosticsAsync();
            diagnostics.Length.Should().Be(0);

            // Check compilation 
            await scriptManager.CompileAsync();
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();
            compilationOutput.Messages.Length.Should().Be(0);
            compilationOutput.ErrorCount.Should().Be(0);
            compilationOutput.WarningCount.Should().Be(0);

            // Check runtime
            var pX = await scriptManager.RunAsync<int>();
            pX.Should().Be(1);
        }


        /// <summary>
        /// Simple global data class for testing.
        /// </summary>
        public class GlobalData
        {
            public string Animal { get; set; } = "Donkey";
        }


        /// <summary>
        /// Tests using accessing and modifying global data from within a script.
        /// </summary>
        [TestMethod]
        public async Task ScriptWithGlobalData_CompilesAndRuns_WithoutErrorsOrWarnings()
        {
            // Setup the environment with a global data type
            var environment =
                Env
                .Default
                .WithGlobalType(typeof(GlobalData));

            // Setup the data and the script
            var globalData = new GlobalData();
            var scriptManager = await ScriptManager.CreateAsync(environment);
            scriptManager = scriptManager.ApplyScript("Animal += \" (modified by script)\"");

            // Check editor
            var diagnostics = await scriptManager.GetDiagnosticsAsync();
            diagnostics.Length.Should().Be(0);

            // Check compilation 
            await scriptManager.CompileAsync();
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();
            compilationOutput.Messages.Length.Should().Be(0);
            compilationOutput.ErrorCount.Should().Be(0);
            compilationOutput.WarningCount.Should().Be(0);

            // Check runtime
            await scriptManager.RunAsync<object>(globalData);
            globalData.Animal.Should().Be("Donkey (modified by script)");
        }
    }
}
