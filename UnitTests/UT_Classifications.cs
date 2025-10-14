using CDS.CSharpScript2;
using AwesomeAssertions;
using Microsoft.CodeAnalysis.Classification;

namespace DotNet6UnitTests
{
    /// </summary>
    [TestClass]
    public class UT_Classifications
    {
        [TestMethod]
        public async Task Classification_ForClassName_IsClassName()
        {
            // Setup
            var script = "Console.WriteLine(\"Hello\");";
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript(script);
            var classifications = await scriptManager.GetClassifications();

            // Check classification
            classifications[0].TextSpan.Start.Should().Be(0);
            classifications[0].TextSpan.End.Should().Be(7);
            classifications[0].ClassificationType.Should().Be(ClassificationTypeNames.ClassName); ; 
        }


        [TestMethod]
        public async Task Classification_ForMethodName_IsMethodName()
        {
            // Setup
            var script = "Console.WriteLine(\"Hello\");";
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript(script);
            var classifications = await scriptManager.GetClassifications();

            // Check classification
            classifications[2].TextSpan.Start.Should().Be(8);
            classifications[2].TextSpan.End.Should().Be(17);
            classifications[2].ClassificationType.Should().Be(ClassificationTypeNames.MethodName); ;
        }
    }
}
