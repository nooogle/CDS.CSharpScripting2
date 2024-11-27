using CDS.CSScripting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;
using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

namespace DotNet6UnitTests
{
    /// <summary>
    /// This demonstrates that the key use cases are all satistifed
    /// </summary>
    [TestClass]
    public partial class UT_UseCases
    {
        /// <summary>
        /// Check a good script using default settings doesn't have any diagnostics or compilation errors
        ///  and can also run without throwing exceptions.
        /// </summary>
        [TestMethod]
        [DataRow("", "empty", DisplayName = "Empty script")]
        [DataRow("System.Console.WriteLine();", "console_blank_line", DisplayName = "Console write blank line;")]
        [DataRow("int x = 10; int y = 20; int z = x + y;", "basic_maths", DisplayName = "Basic integer maths")]
        [DataRow("System.Math.Pow(2, 10);", "math_class", DisplayName = "Basic Math class test")]
        public async Task IsolatedScript_DiagnosticsAndCompilation_HasNoErrorsOrWarnings(string script, string testName)
        {
            // Setup
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript(script);

            // Check diagnostics
            var diagnostics = await scriptManager.GetDiagnosticsAsync();

            // Compile
            await scriptManager.CompileAsync();
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();

            // Get all results
            var actual = new
            {
                Diagnostics = diagnostics,
                CompilationOutput = compilationOutput
            };

            // Verify
            await Verifier.Verify(actual, VerifyHelper.Settings).UseParameters(testName);

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


            // Collate all results
            var actual = new
            {
                Diagnostics = diagnostics,
                CompilationOutput = compilationOutput,
                Result = result
            };

            // Verify all results
            await Verifier.Verify(actual, VerifyHelper.Settings);
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

            // Compile 
            await scriptManager.CompileAsync();
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();

            // Check runtime
            await scriptManager.RunAsync();

            // Collate all results
            var actual = new
            {
                Diagnostics = diagnostics,
                CompilationOutput = compilationOutput
            };

            // Verify all results
            await Verifier.Verify(actual, VerifyHelper.Settings);
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

            // Compile
            await scriptManager.CompileAsync();
            var compilationOutput = await scriptManager.GetCompilationOutputAsync();

            // Check runtime
            var pX = await scriptManager.RunAsync<int>();

            // Collate all results
            var actual = new
            {
                Diagnostics = diagnostics,
                CompilationOutput = compilationOutput,
                PointX = pX
            };

            // Verify all results
            await Verifier.Verify(actual, VerifyHelper.Settings);
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
                Env
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

            // Collate all results
            var actual = new
            {
                Diagnostics = diagnostics,
                CompilationOutput = compilationOutput,
                GlobalData = globalData
            };

            // Verify all results
            await Verifier.Verify(actual, VerifyHelper.Settings);
        }
    }
}
