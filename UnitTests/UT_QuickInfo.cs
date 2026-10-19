using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Editors;

namespace UnitTests;

[TestClass]
public class UT_QuickInfo
{
    private const string RoundScript = "var price = 19.987; var shown = Math.Round(price, 2);";

    [TestMethod]
    public async Task GetQuickInfoAsync_OnAnOverloadedMethod_DescribesTheOverloadCalled()
    {
        var info = await GetQuickInfoAsync(RoundScript, "Round");

        info.Should().NotBeNull();
        info!.Signature.Should().Be("double Math.Round(double value, int digits) (+ 7 overloads)");
        info.Summary.Should().StartWith("Rounds a double-precision floating-point value to a specified number of fractional digits");
    }

    [TestMethod]
    public async Task GetQuickInfoAsync_OnAMethod_SpansTheWholeName()
    {
        var info = await GetQuickInfoAsync(RoundScript, "Round");

        info!.Start.Should().Be(RoundScript.IndexOf("Round"));
        info.Length.Should().Be("Round".Length);
    }

    [TestMethod]
    public async Task GetQuickInfoAsync_IsCompact_LeavingOutReturnsAndExceptions()
    {
        var info = await GetQuickInfoAsync(RoundScript, "Round");

        info!.Summary.Should().NotContain("Returns");
        info.Summary.Should().NotContain("Exceptions");
    }

    [TestMethod]
    public async Task GetQuickInfoAsync_OnAScriptVariable_DescribesItsType()
    {
        var info = await GetQuickInfoAsync(RoundScript, "price,");

        info!.Signature.Should().Be("(field) double price");
        info.Summary.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetQuickInfoAsync_OnAType_DescribesTheType()
    {
        var info = await GetQuickInfoAsync(RoundScript, "Math");

        info!.Signature.Should().Be("class System.Math");
        info.Summary.Should().StartWith("Provides constants and static methods");
    }

    [TestMethod]
    public async Task GetQuickInfoAsync_OnVar_DescribesTheInferredType()
    {
        var info = await GetQuickInfoAsync(RoundScript, "var shown");

        info!.Signature.Should().Be("readonly struct System.Double");
    }

    [TestMethod]
    public async Task GetQuickInfoAsync_OnWhitespace_ReturnsNull()
    {
        var info = await GetQuickInfoAsync(RoundScript, " = 19");

        info.Should().BeNull();
    }

    [TestMethod]
    public async Task GetQuickInfoAsync_InAComment_ReturnsNull()
    {
        var info = await GetQuickInfoAsync("// a comment here", "comment");

        info.Should().BeNull();
    }

    [TestMethod]
    public async Task EditorManager_GetQuickInfoAsync_DescribesTheSymbol()
    {
        using var manager = new EditorManager(ScriptEnvironment.Default);
        await manager.UpdateScriptDocumentAsync(RoundScript);

        var info = await manager.GetQuickInfoAsync(RoundScript.IndexOf("Round") + 1);

        info!.Signature.Should().StartWith("double Math.Round(double value, int digits)");
    }

    /// <summary>Asks for quick info one character into <paramref name="marker"/>.</summary>
    private static async Task<CDS.CSharpScript2.APIInfo.QuickInfo?> GetQuickInfoAsync(string script, string marker)
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);
        return await new ScriptAnalyser(context).GetQuickInfoAsync(script.IndexOf(marker) + 1);
    }
}
