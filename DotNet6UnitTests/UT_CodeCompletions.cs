using CDS.CSScripting;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyMSTest;
using VerifyTests;

namespace DotNet6UnitTests
{
    [TestClass]
    public partial class UT_CodeCompletions
    {
        private readonly VerifySettings verifySettings;


        public UT_CodeCompletions()
        {
            verifySettings = new();
            verifySettings.UseDirectory("VerifySnapshots");
        }


        /// <summary>
        /// Check we get a single completion suggestion for a non-ambiguous script.
        /// </summary>
        [TestMethod]
        public async Task Should_Return1CompletionSuggestion_ForNonAmbiguousScript()
        {
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript("int Fibonacci(int n) => 0; Fibo");

            var completions = await scriptManager.GetCompletionSuggestionsAsync(scriptManager.ScriptText.Length);
            await Verifier.Verify(completions, verifySettings);
        }


        /// <summary>
        /// Check we get a single completion suggestion for a non-ambiguous script.
        /// </summary>
        [TestMethod]
        public async Task Should_ReturnSeveralCompletionSuggestion_ForSlightlyAmbiguousScript()
        {
            var scriptManager = await ScriptManager.CreateAsync();
            scriptManager = scriptManager.ApplyScript("System.Console.Window");

            var d = await scriptManager.GetDiagnosticsAsync();

            var completions = await scriptManager.GetCompletionSuggestionsAsync(scriptManager.ScriptText.Length);

            await Verifier.Verify(completions);
            //completions.Should().HaveCount(4);
            //completions.Should().Contain(c => c.DisplayText == "WindowHeight");
            //completions.Should().Contain(c => c.DisplayText == "WindowLeft");
            //completions.Should().Contain(c => c.DisplayText == "WindowTop");
            //completions.Should().Contain(c => c.DisplayText == "WindowWidth");
        }
    }
}

