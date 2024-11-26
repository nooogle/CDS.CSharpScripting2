using CDS.CSScripting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

namespace DotNet6UnitTests
{
    [TestClass]
    public partial class UT_CodeCompletions
    {
        /// <summary>
        /// Check we get a single completion suggestion for a non-ambiguous script.
        /// </summary>
        [TestMethod]
        public async Task Should_Return1CompletionSuggestion_ForNonAmbiguousScript()
        {
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript("int Fibonacci(int n) => 0; Fibo");

            var completions = await scriptManager.GetCompletionSuggestionsAsync(scriptManager.ScriptText.Length);

            await VerifyHelper.Verify(completions);
        }


        /// <summary>
        /// Check we get a single completion suggestion for a non-ambiguous script.
        /// </summary>
        [TestMethod]
        public async Task Should_ReturnSeveralCompletionSuggestion_ForSlightlyAmbiguousScript()
        {
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript("System.Console.Window");

            var completions = 
                await scriptManager
                .GetCompletionSuggestionsAsync(scriptManager.ScriptText.Length);

            await VerifyHelper.Verify(completions);
        }
    }
}

