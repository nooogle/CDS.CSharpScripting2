using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Folding;

namespace UnitTests;

/// <summary>
/// Covers the fold-span calculator that backs code folding in the Scintilla editor — brace
/// blocks and <c>#region</c>/<c>#endregion</c> pairs, derived from the syntax tree alone.
/// </summary>
[TestClass]
[TestCategory("folding")]
public class UT_FoldSpanCalculator
{
    private static async Task<IReadOnlyList<FoldSpan>> CalculateAsync(string script)
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);
        return await new ScriptAnalyser(context).GetFoldSpansAsync();
    }

    [TestMethod]
    public async Task Calculate_MethodBody_ReturnsSpanCoveringBraces()
    {
        var script = "int F()\n{\n    return 1;\n}\n";

        var spans = await CalculateAsync(script);

        var openBrace = script.IndexOf('{');
        var closeBrace = script.IndexOf('}');

        spans.Should().ContainSingle(s => s.SpanStart == openBrace && s.SpanStart + s.SpanLength - 1 == closeBrace);
    }

    [TestMethod]
    public async Task Calculate_NestedBlocks_ReturnsOneSpanPerBraceLevel()
    {
        var script = "class C\n{\n    void M()\n    {\n        if (true)\n        {\n        }\n    }\n}\n";

        var spans = await CalculateAsync(script);

        spans.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task Calculate_Region_ReturnsSpanFromRegionToEndRegion()
    {
        var script = "#region Setup\nint x = 1;\n#endregion\n";

        var spans = await CalculateAsync(script);

        var regionStart = script.IndexOf("#region", StringComparison.Ordinal);
        var endRegionKeywordEnd = script.IndexOf("#endregion", StringComparison.Ordinal) + "#endregion".Length;

        // A directive's FullSpan reaches through its trailing line break — that break is trivia
        // belonging to the directive's EndOfDirectiveToken.
        var regionEnd = script.IndexOf('\n', endRegionKeywordEnd) + 1;

        spans.Should().ContainSingle(s => s.SpanStart == regionStart && s.SpanStart + s.SpanLength == regionEnd);
    }

    [TestMethod]
    public async Task Calculate_NestedRegions_ReturnsOneSpanPerRegion()
    {
        var script = "#region Outer\n#region Inner\nint x = 1;\n#endregion\n#endregion\n";

        var spans = await CalculateAsync(script);

        spans.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Calculate_UnmatchedOpenBrace_DoesNotThrowAndOmitsTheBrace()
    {
        var script = "int F()\n{\n    return 1;\n";

        var act = async () => await CalculateAsync(script);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Calculate_SingleLineBlock_StillReturnsSpan()
    {
        // Line-height filtering (skipping spans that don't cross a line) is the editor's job,
        // not the calculator's — it reports every matched pair regardless of layout.
        var script = "int F() { return 1; }";

        var spans = await CalculateAsync(script);

        spans.Should().ContainSingle();
    }

    [TestMethod]
    public async Task Calculate_EmptyScript_ReturnsEmpty()
    {
        var spans = await CalculateAsync(string.Empty);

        spans.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Calculate_InterpolatedString_DoesNotFoldTheInterpolationHole()
    {
        var script = "var x = 1;\nvar s = $\"value: {x}\";\n";

        var spans = await CalculateAsync(script);

        spans.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Calculate_InterpolatedStringInsideBlock_ReturnsOnlyTheBlockSpan()
    {
        var script = "int F()\n{\n    var x = 1;\n    return $\"{x}\".Length;\n}\n";

        var spans = await CalculateAsync(script);

        var openBrace = script.IndexOf('{');
        var closeBrace = script.LastIndexOf('}');

        spans.Should().ContainSingle(s => s.SpanStart == openBrace && s.SpanStart + s.SpanLength - 1 == closeBrace);
    }

    [TestMethod]
    public async Task Calculate_MultiLineRawInterpolatedString_DoesNotFoldTheInterpolationHole()
    {
        var script = "var x = 1;\nvar s = $\"\"\"\n    value: {x}\n    \"\"\";\n";

        var spans = await CalculateAsync(script);

        spans.Should().BeEmpty();
    }
}
