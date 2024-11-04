using CDS.CSScripting;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace DotNet6UnitTests
{
    [TestClass]
    public class UT_ScriptManager_Diagnostics
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

            diagnostics.Should().HaveCount(0);
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

            diagnostics.Should().HaveCount(1);
            diagnostics[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
            diagnostics[0].GetMessage().Should().Be("The name 'yyy' does not exist in the current context");
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

            diagnostics.Should().HaveCount(1);
            diagnostics[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
            diagnostics[0].GetMessage().ToLower().Should().Contain("is obsolete");
        }
    }
}
