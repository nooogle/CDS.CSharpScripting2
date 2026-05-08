using AwesomeAssertions;
using CDS.CSharpScript2;

namespace UnitTests;

[TestClass]
public partial class UT_CodeCompletions
{
    [TestMethod]
    public async Task Should_Return1CompletionSuggestion_ForNonAmbiguousScript()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("int Fibonacci(int n) => 0; Fibo");

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(context.ScriptText.Length);

        completions.Should().NotBeNull();
        completions.Should().NotBeEmpty("should have at least one completion suggestion");
        completions.Length.Should().Be(1, "Fibonacci is unambiguous and should return exactly one completion");
        completions[0].DisplayText.Should().Be("Fibonacci");
    }

    [TestMethod]
    public async Task Should_ReturnSeveralCompletionSuggestion_ForSlightlyAmbiguousScript()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("System.Console.Window");

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(context.ScriptText.Length);

        completions.Should().NotBeNull();
        completions.Should().NotBeEmpty("should have completion suggestions");
        completions.Length.Should().BeGreaterThan(1, "Window prefix matches multiple Console members");
        completions.Should().AllSatisfy(c =>
            c.DisplayText.Should().StartWith("Window", "all completions should match the Window prefix"));
    }
}
