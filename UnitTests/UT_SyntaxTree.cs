using CDS.CSScripting2;

namespace DotNet6UnitTests
{
    /// </summary>
    [TestClass]
    public partial class UT_SyntaxTree
    {
        [TestMethod]
        public async Task SyntaxNodeGenerated_FomrCommentLine_WhenCommentAtEndOfFile()
        {
            // Setup

            var script =
@"int x = 1;
// abc
";

            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript(script);

            // Check diagnostics
            var syntaxTree = await scriptManager.GetSyntaxTreeAsync();
            var syntaxElements = CDS.CSScripting2.Editors.Syntax.ScriptSyntaxAnalyser.Go(syntaxTree);


            // Bundle data for the verification review
            var data = new
            {
                script,
                syntaxElements
            };

            // Verify
            await Verify(data);
        }
    }
}
