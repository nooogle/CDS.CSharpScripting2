using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Editors;

namespace UnitTests;

[TestClass]
public partial class UT_CodeCompletions
{
    // ── Existing coverage ─────────────────────────────────────────────────────

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
        completions.Length.Should().BeGreaterThan(1, "Window matches multiple Console members");
        completions.Should().AllSatisfy(c =>
            c.DisplayText.Should().ContainEquivalentOf("Window",
                "every completion should contain the typed text somewhere"));
    }

    // ── Substring matching (Visual Studio style) ──────────────────────────────

    [TestMethod]
    public async Task GetCompletionsAsync_WithTextFromTheMiddleOfAName_OffersThatMember()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("System.Console.Window");

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(context.ScriptText.Length);
        var displayTexts = completions.Select(c => c.DisplayText).ToList();

        displayTexts.Should().Contain("SetWindowSize",
            "'Window' starts a word inside SetWindowSize, so it should be offered as it is in Visual Studio");
    }

    [TestMethod]
    public async Task GetCompletionsAsync_WithTextFromTheMiddleOfAName_RanksPrefixMatchesFirst()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("System.Console.Window");

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(context.ScriptText.Length);
        var displayTexts = completions.Select(c => c.DisplayText).ToList();

        int windowWidthIndex = displayTexts.IndexOf("WindowWidth");
        int setWindowSizeIndex = displayTexts.IndexOf("SetWindowSize");

        windowWidthIndex.Should().BeGreaterThanOrEqualTo(0, "WindowWidth should be offered");
        setWindowSizeIndex.Should().BeGreaterThanOrEqualTo(0, "SetWindowSize should be offered");
        windowWidthIndex.Should().BeLessThan(setWindowSizeIndex,
            "items starting with the typed text should come before items that merely contain it");
    }

    [TestMethod]
    public async Task GetCompletionsAsync_WithWordInitials_OffersTheMatchingMember()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("System.Console.wl");

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(context.ScriptText.Length);

        completions.Should().Contain(c => c.DisplayText == "WriteLine",
            "'wl' is the initials of WriteLine, which Visual Studio matches");
    }

    // ── Member-access (dot-trigger) ───────────────────────────────────────────

    [TestMethod]
    public async Task GetCompletionsAsync_AfterMemberAccessDot_ReturnsMemberCompletions()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("System.Console.");

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(context.ScriptText.Length);

        completions.Should().NotBeEmpty("the dot-trigger should produce Console member completions");
        completions.Should().Contain(c => c.DisplayText == "WriteLine");
        completions.Should().Contain(c => c.DisplayText == "ReadLine");
    }

    // ── Filtering out non-matches ─────────────────────────────────────────────

    [TestMethod]
    public async Task GetCompletionsAsync_WithUnmatchedPrefix_ReturnsEmptyList()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("System.Console.ZZZZZ");

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(context.ScriptText.Length);

        completions.Should().BeEmpty("no Console member contains 'ZZZZZ' anywhere");
    }

    // ── Robustness while typing ───────────────────────────────────────────────

    [TestMethod]
    public async Task GetCompletionsAsync_InIncompleteScript_StillReturnsCompletions()
    {
        // The user is mid-type; the script is not syntactically valid yet.
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("Math.");

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(context.ScriptText.Length);

        completions.Should().NotBeEmpty("Roslyn returns completions even when the script has syntax errors");
        completions.Should().Contain(c => c.DisplayText == "Abs");
    }

    [TestMethod]
    public async Task GetCompletionsAsync_AtMidScriptPosition_ReturnsContextualCompletions()
    {
        const string script = "int Fibonacci(int n) => 0; Fibo\nvar x = 42;";
        int position = script.IndexOf("Fibo\n", StringComparison.Ordinal) + 4; // just after "Fibo"

        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(position);

        completions.Should().Contain(c => c.DisplayText == "Fibonacci",
            "Fibonacci should be offered for the 'Fibo' prefix at a mid-script position");
    }

    // ── Sort order ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GetCompletionsAsync_WithSingleLetterPrefix_SortsItemsStartingWithLetterBeforeItemsContainingIt()
    {
        // FooBar starts with 'F'; BarFoo contains 'F' but does not start with it.
        const string script = "int FooBar(int n) => 0; int BarFoo(int n) => 0; F";

        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);

        var completions = await new ScriptAnalyser(context).GetCompletionsAsync(context.ScriptText.Length);
        var displayTexts = completions.Select(c => c.DisplayText).ToList();

        int fooBarIndex = displayTexts.IndexOf("FooBar");
        int barFooIndex = displayTexts.IndexOf("BarFoo");

        fooBarIndex.Should().BeGreaterThanOrEqualTo(0, "FooBar should appear in the completion list");
        barFooIndex.Should().BeGreaterThanOrEqualTo(0, "BarFoo should appear in the completion list");
        fooBarIndex.Should().BeLessThan(barFooIndex,
            "items starting with the typed letter should precede items that merely contain it");
    }

    // ── EditorManager.UpdateScriptDocumentAsync ───────────────────────────────

    [TestMethod]
    public async Task UpdateScriptDocumentAsync_EnablesCompletionsForCurrentScriptText()
    {
        var manager = new EditorManager(ScriptEnvironment.Default);

        await manager.UpdateScriptDocumentAsync("System.Console.Write");

        var completions = await manager.GetAutoCompletions("System.Console.Write".Length);

        completions.Should().NotBeEmpty("completions should reflect the script passed to UpdateScriptDocumentAsync");
        completions.Should().Contain(c => c.DisplayText == "WriteLine",
            "WriteLine is the expected completion for the 'Write' prefix");
    }

    [TestMethod]
    public async Task UpdateScriptDocumentAsync_DoesNotUpdateLastDiagnostics()
    {
        var manager = new EditorManager(ScriptEnvironment.Default);
        await manager.ApplyScript("var x = 42;");
        int diagnosticCountAfterApply = manager.LastDiagnostics.Length;

        // Switch to an incomplete script via the lightweight path — should NOT trigger diagnostics.
        await manager.UpdateScriptDocumentAsync("var y = ");

        manager.LastDiagnostics.Length.Should().Be(diagnosticCountAfterApply,
            "UpdateScriptDocumentAsync must not run a diagnostics pass");
    }
}
