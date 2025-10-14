using AwesomeAssertions;
using CDS.CSharpScript2;

namespace UnitTests;

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

        // Verify we got completion suggestions
        completions.Should().NotBeNull();
        completions.Should().NotBeEmpty("should have at least one completion suggestion");
        
        // Verify we have exactly one completion for the unambiguous case
        completions.Length.Should().Be(1, "Fibonacci is unambiguous and should return exactly one completion");
        
        // Verify the completion is for "Fibonacci"
        completions[0].DisplayText.Should().Be("Fibonacci");
    }

    /// <summary>
    /// Check we get several completion suggestions for a slightly ambiguous script.
    /// </summary>
    [TestMethod]
    public async Task Should_ReturnSeveralCompletionSuggestion_ForSlightlyAmbiguousScript()
    {
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript("System.Console.Window");

        var completions = await scriptManager.GetCompletionSuggestionsAsync(scriptManager.ScriptText.Length);

        // Verify we got completion suggestions
        completions.Should().NotBeNull();
        completions.Should().NotBeEmpty("should have completion suggestions");
        
        // Verify we have multiple completions for the ambiguous case
        completions.Length.Should().BeGreaterThan(1, "Window prefix matches multiple Console members");
        
        // Verify all completions start with "Window"
        completions.Should().AllSatisfy(c => 
            c.DisplayText.Should().StartWith("Window", "all completions should match the Window prefix"));
    }
}

