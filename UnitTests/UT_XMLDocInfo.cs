using CDS.CSharpScriptUtils;

namespace DotNet6UnitTests
{
    [TestClass]
    public partial class UT_XMLDocInfo
    {
        /// <summary>
        /// Check we get some type information when hovering over 'Console'
        /// </summary>
        [TestMethod]
        public async Task Should_ReturnConsoleTypeInfo_ForConsoleType()
        {
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript("Console");
            var xmlInfo = await scriptManager.GetSuggestionsAsync(position: 3);

            await Verify(xmlInfo);
        }
    }
}
