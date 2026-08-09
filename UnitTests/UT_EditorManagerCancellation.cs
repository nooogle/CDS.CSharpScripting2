using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Editors;
using Microsoft.CodeAnalysis;

namespace UnitTests;

/// <summary>
/// Covers the cancellation contract of <see cref="EditorManager"/>.
/// </summary>
/// <remarks>
/// The editor supersedes analysis passes constantly while the user types, so two properties
/// matter and neither is visible from ordinary use: cancellation must actually reach Roslyn
/// rather than merely discarding a completed result, and an abandoned pass must not leave
/// half-updated state behind.
/// </remarks>
[TestClass]
[TestCategory("diagnostics")]
public class UT_EditorManagerCancellation
{
    private static string BuildScript(int statements) =>
        string.Concat(Enumerable.Range(0, statements).Select(i =>
            $"var v{i} = {i} * 2 + 1;\nvar t{i} = \"item \" + v{i}.ToString();\n"));

    [TestMethod]
    public async Task ApplyScript_CancelledBeforeStart_Throws()
    {
        using var manager = new EditorManager(ScriptEnvironment.Default);
        await manager.ApplyScript("var x = 1;");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await manager.ApplyScript("var y = 2;", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ApplyScript_Cancelled_LeavesPreviousResultsIntact()
    {
        using var manager = new EditorManager(ScriptEnvironment.Default);
        await manager.ApplyScript("var x = 1;");

        var diagnosticsBefore = manager.LastDiagnostics;
        var classificationsBefore = manager.LastClassifications;

        classificationsBefore.Should().NotBeEmpty("the first pass must have produced something to preserve");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await manager.ApplyScript("this is not valid c# @@@", cts.Token);
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        manager.LastDiagnostics.Should().BeEquivalentTo(diagnosticsBefore);
        manager.LastClassifications.Should().BeEquivalentTo(classificationsBefore);
    }

    [TestMethod]
    public async Task ApplyScript_NotCancelled_PublishesBothResultsTogether()
    {
        using var manager = new EditorManager(ScriptEnvironment.Default);
        await manager.ApplyScript("undefinedThingXyz();");

        manager.LastDiagnostics.Should().Contain(d => d.Severity == DiagnosticSeverity.Error);
        manager.LastClassifications.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetAutoCompletions_CancelledBeforeStart_Throws()
    {
        using var manager = new EditorManager(ScriptEnvironment.Default);
        var script = "var s = \"hello\";\ns.";
        await manager.ApplyScript(script);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await manager.GetAutoCompletions(script.Length, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ApplyScript_CancelledMidFlight_AbortsWellBeforeCompleting()
    {
        // The point of threading a token into Roslyn is that superseded work stops, rather
        // than running to completion and having its answer thrown away.
        var script = BuildScript(400);

        using var manager = new EditorManager(ScriptEnvironment.Default);
        await manager.ApplyScript(script);

        var uncancelled = System.Diagnostics.Stopwatch.StartNew();
        await manager.ApplyScript(script + "\nvar extra = 1;");
        uncancelled.Stop();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(5));

        var cancelled = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await manager.ApplyScript(script + "\nvar extra2 = 2;", cts.Token);
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        cancelled.Stop();

        // Generous margin: this asserts "gave up early", not a specific timing.
        cancelled.ElapsedMilliseconds.Should().BeLessThan(
            Math.Max(uncancelled.ElapsedMilliseconds, 20),
            "a cancelled pass must abandon Roslyn's work rather than run it to completion");
    }

    [TestMethod]
    public async Task ApplySyntacticPass_DoesNotDisturbTheFullPassResults()
    {
        // The fast colouring pass shares the manager with the full pass; it must not
        // overwrite the diagnostics the editor is still displaying.
        using var manager = new EditorManager(ScriptEnvironment.Default);
        await manager.ApplyScript("undefinedThingXyz();");

        var diagnostics = manager.LastDiagnostics;
        var classifications = manager.LastClassifications;

        var syntactic = await manager.ApplySyntacticPassAsync("var x = 1;", CancellationToken.None);

        syntactic.Should().NotBeEmpty();
        manager.LastDiagnostics.Should().BeEquivalentTo(diagnostics);
        manager.LastClassifications.Should().BeEquivalentTo(classifications);
    }
}
