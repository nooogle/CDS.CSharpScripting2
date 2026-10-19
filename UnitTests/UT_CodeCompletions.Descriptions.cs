using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Editors;

namespace UnitTests;

public partial class UT_CodeCompletions
{
    // ── Completion descriptions ───────────────────────────────────────────────

    [TestMethod]
    public async Task GetCompletionDescriptionAsync_ForAFrameworkMethod_ReturnsSignatureAndSummary()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("System.Math.Sqrt");
        var analyser = new ScriptAnalyser(context);

        var completions = await analyser.GetCompletionsAsync(context.ScriptText.Length);
        var sqrt = completions.Single(c => c.DisplayText == "Sqrt");

        var description = await analyser.GetCompletionDescriptionAsync(sqrt);

        description.Should().NotBeNull();
        description!.Text.Should().Contain("double Math.Sqrt(double d)");
        description.Text.Should().Contain("square root",
            "the XML-doc summary should follow the signature");
    }

    [TestMethod]
    public async Task GetCompletionDescriptionAsync_ForAScriptLocal_DescribesItsType()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("int counter = 0; coun");
        var analyser = new ScriptAnalyser(context);

        var completions = await analyser.GetCompletionsAsync(context.ScriptText.Length);
        var counter = completions.First(c => c.DisplayText == "counter");

        var description = await analyser.GetCompletionDescriptionAsync(counter);

        description.Should().NotBeNull();
        description!.Text.Should().Contain("int counter");
    }

    [TestMethod]
    public async Task GetCompletionDescriptionAsync_WhenAlreadyCancelled_Throws()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("System.Math.Sqrt");
        var analyser = new ScriptAnalyser(context);

        var completions = await analyser.GetCompletionsAsync(context.ScriptText.Length);
        var sqrt = completions.Single(c => c.DisplayText == "Sqrt");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => analyser.GetCompletionDescriptionAsync(sqrt, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task EditorManager_GetCompletionDescriptionAsync_DescribesAnItemItReturned()
    {
        using var manager = new EditorManager(ScriptEnvironment.Default);
        const string script = "System.Math.Sqrt";
        await manager.UpdateScriptDocumentAsync(script);

        var completions = await manager.GetAutoCompletions(script.Length);
        var sqrt = completions.Single(c => c.DisplayText == "Sqrt");

        var description = await manager.GetCompletionDescriptionAsync(sqrt);

        description.Should().NotBeNull();
        description!.Text.Should().Contain("Math.Sqrt(double d)");
    }
}
