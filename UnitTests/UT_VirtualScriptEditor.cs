using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Classification;
using UnitTests.TestHelpers;

namespace UnitTests;

[TestClass]
public class UT_VirtualScriptEditor
{
    /// <summary>
    /// Verifies that typing updates the script and raises editor feedback events.
    /// </summary>
    [TestMethod]
    public async Task TypeTextAsync_RaisesEvents_AndUpdatesAnalysisState()
    {
        var driver = new VirtualScriptEditorDriver();

        await driver.TypeAsync("Console.WriteLine(\"Hello\");");

        driver.Script.Should().Be("Console.WriteLine(\"Hello\");");
        driver.CaretPosition.Should().Be(driver.Script.Length);
        driver.Diagnostics.Should().BeEmpty();
        driver.HasErrors.Should().BeFalse();
        driver.Classifications.Should().Contain(classification =>
            classification.Classification == SymbolClassification.ClassName &&
            classification.SpanStart == 0 &&
            classification.SpanLength == 7);
        driver.Events.DiagnosticsUpdatedCount.Should().BeGreaterThan(0);
        driver.Events.ScriptChangedCount.Should().BeGreaterThan(0);
        driver.Events.LatestDiagnosticsUpdatedEvent.Should().NotBeNull();
        driver.Events.LatestDiagnosticsUpdatedEvent!.Diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that diagnostics are surfaced after introducing a script error.
    /// </summary>
    [TestMethod]
    public async Task TypeTextAsync_WithInvalidScript_UpdatesDiagnostics()
    {
        var driver = new VirtualScriptEditorDriver();

        await driver.SetScriptAsync("int value = missingSymbol;");

        driver.HasErrors.Should().BeTrue();
        driver.Diagnostics.Should().ContainSingle();
        driver.GetDiagnosticMessages().Should().ContainSingle(message => message.Contains("missingSymbol", StringComparison.Ordinal));
        driver.CurrentCompiledScript.Should().BeNull();
    }

    /// <summary>
    /// Verifies that completions can be queried at the current caret position.
    /// </summary>
    [TestMethod]
    public async Task GetCompletionsAsync_ReturnsMatchingCompletionItems()
    {
        var driver = new VirtualScriptEditorDriver();

        await driver.SetScriptAsync("int Fibonacci(int n) => 0; Fibo");

        var completionTexts = await driver.GetCompletionTextsAsync();

        completionTexts.Should().ContainSingle(text => text == "Fibonacci");
    }

    /// <summary>
    /// Verifies that API information can be queried at the current caret position.
    /// </summary>
    [TestMethod]
    public async Task GetAPIInfoAsync_ReturnsTypeAndMemberInformation()
    {
        var driver = new VirtualScriptEditorDriver();

        await driver.SetScriptAsync("System.Math.Pow(");
        driver.MoveCaretTo(driver.Script.IndexOf("Pow", StringComparison.Ordinal) + 2);

        var apiInfo = await driver.GetAPIInfoAsync();

        apiInfo.Should().NotBeNull();
        apiInfo!.TypeInfo.Should().NotBeNull();
        apiInfo.TypeInfo!.Name.Should().Be("Math");
        apiInfo.MemberInfos.Should().NotBeNull();
        apiInfo.MemberInfos!.Should().NotBeEmpty();
        apiInfo.MemberInfos.Should().AllSatisfy(member => member.Name.Should().Be("Pow"));
    }

    /// <summary>
    /// Verifies that compilation uses the current editor script and caches the result until the script changes.
    /// </summary>
    [TestMethod]
    public async Task CompileAsync_CachesCompiledScript_UntilScriptChanges()
    {
        var driver = new VirtualScriptEditorDriver();

        await driver.SetScriptAsync("return 42;");

        var compiled1 = await driver.CompileAsync();
        var compiled2 = await driver.CompileAsync();

        compiled1.Should().BeSameAs(compiled2);
        driver.CurrentCompiledScript.Should().NotBeNull();
        driver.CurrentCompiledScript!.Should().BeSameAs(compiled1);
        (await driver.CompileAndRunAsync<int>()).Should().Be(42);

        await driver.TypeAsync(" ");

        driver.CurrentCompiledScript.Should().BeNull();
    }
}
