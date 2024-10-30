using CDS.CSScripting;

namespace DotNet6UnitTests
{
    [TestClass]
    public class UT_XMLDocInfo
    {
        /// <summary>
        /// Check we get some type information when hovering over 'Console'
        /// </summary>
        [TestMethod]
        public async Task Should_ReturnConsoleTypeInfo_ForConsoleType()
        {
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript("Console");
            (var typeInfo, var memberInfo) = await scriptManager.GetSuggestionsAsync(position: 3);

            typeInfo.Should().NotBeNull();
            typeInfo.Name.Should().Be(nameof(Console));
            typeInfo.Summary.Should().NotBeNullOrEmpty();
        }
    }
}
