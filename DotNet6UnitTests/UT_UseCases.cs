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
        /// Check script has no diagnostics messages and no compilation errors or warning.
        /// Script:
        ///     0 inputs
        ///     0 outputs
        ///     0 globals
        ///     0 additional references
        ///     0 additional assemblies
        /// </summary>
        [TestMethod]
        public async Task IsolatedScript_Compiles_WithoutErrorsOrWarnings()
        {
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript("System.Console.WriteLine();");

            var diagnostics = await scriptManager.GetDiagnosticsAsync();
            diagnostics.Length.Should().Be(0);

            await scriptManager.CompileAsync();
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();
            compilationOutput.Messages.Length.Should().Be(0);
            compilationOutput.ErrorCount.Should().Be(0);
            compilationOutput.WarningCount.Should().Be(0);
        }


        /// <summary>
        /// Check script can run without any runtime problems
        /// Script:
        ///     0 inputs
        ///     0 outputs
        ///     0 globals
        ///     0 additional references
        ///     0 additional assemblies
        /// </summary>
        [TestMethod]
        public async Task IsolatedScript_Runs_WithoutErrorsOrWarnings()
        {
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript("System.Console.WriteLine();");
            await scriptManager.RunAsync<object>();
        }


        /// <summary>
        /// Check script has no diagnostics messages and no compilation errors or warning.
        /// Script:
        ///     0 inputs
        ///     1 output
        ///     0 globals
        ///     0 additional references
        ///     0 additional assemblies
        /// </summary>
        [TestMethod]
        public async Task ScriptWithOutput_Compiles_WithoutErrorsOrWarnings()
        {
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript("return System.Math.Pow(10, 2);");

            var diagnostics = await scriptManager.GetDiagnosticsAsync();
            diagnostics.Length.Should().Be(0);

            await scriptManager.CompileAsync();
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();
            compilationOutput.Messages.Length.Should().Be(0);
            compilationOutput.ErrorCount.Should().Be(0);
            compilationOutput.WarningCount.Should().Be(0);
        }


        /// <summary>
        /// Check script can run without any runtime problems
        /// Script:
        ///     0 inputs
        ///     1 output
        ///     0 globals
        ///     0 additional references
        ///     0 additional assemblies
        /// </summary>
        [TestMethod]
        public async Task ScriptWithOutput_Runs_WithoutErrorsOrWarnings()
        {
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript("return System.Math.Pow(10, 2);");
            var result = await scriptManager.RunAsync<double>();
            result.Should().Be(100);
        }
    }
}
