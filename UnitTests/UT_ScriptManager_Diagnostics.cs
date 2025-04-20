using CDS.CSharpScript2;

namespace DotNet6UnitTests
{
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

            // Verify
            await Verify(diagnostics);
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

            var actual = new
            {
                Count = diagnostics.Length,
                Severity = diagnostics[0].Severity,
                Message = diagnostics[0].GetMessage(),
                All = diagnostics[0]
            };

            await Verify(actual);
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

            // Collate the diagnostics
            var actual = new
            {
                Count = diagnostics.Length,
                Severity = diagnostics[0].Severity,
                Message = diagnostics[0].GetMessage(),
                All = diagnostics[0]
            };

            await Verify(actual);
        }
    }
}
