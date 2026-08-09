using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Classification;

namespace UnitTests;

/// <summary>
/// Covers the syntax-only classifier used for live colouring while the user types.
/// </summary>
/// <remarks>
/// This classifier is written by hand — Roslyn exposes no public syntactic-only
/// classification API — so its mapping is worth pinning down. It runs before any semantic
/// information exists, which is why identifiers are expected to come back unresolved.
/// </remarks>
[TestClass]
[TestCategory("classifications")]
public class UT_SyntacticClassifier
{
    private static async Task<IReadOnlyList<ClassifiedSymbol>> ClassifyAsync(string script)
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);
        return await new ScriptAnalyser(context).GetSyntacticClassificationsAsync();
    }

    private static async Task<SymbolClassification?> ClassificationAtAsync(string script, string token)
    {
        var start = script.IndexOf(token, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, "the test token must appear in the script");

        var classifications = await ClassifyAsync(script);

        return classifications
            .FirstOrDefault(c => c.SpanStart == start && c.SpanLength == token.Length)
            ?.Classification;
    }

    [TestMethod]
    public async Task Classify_Keyword_IsKeyword()
    {
        var result = await ClassificationAtAsync("var x = 1;", "var");
        result.Should().Be(SymbolClassification.Keyword);
    }

    [TestMethod]
    public async Task Classify_ControlFlowKeyword_IsControlKeyword()
    {
        var result = await ClassificationAtAsync("if (true) { }", "if");
        result.Should().Be(SymbolClassification.ControlKeyword);
    }

    [TestMethod]
    public async Task Classify_ReturnInsideMethod_IsControlKeyword()
    {
        var result = await ClassificationAtAsync("int F() { return 1; }", "return");
        result.Should().Be(SymbolClassification.ControlKeyword);
    }

    [TestMethod]
    public async Task Classify_DefaultAsValueExpression_IsOrdinaryKeyword()
    {
        // "default" only directs control flow as a switch label; as a value it is ordinary.
        var result = await ClassificationAtAsync("int x = default;", "default");
        result.Should().Be(SymbolClassification.Keyword);
    }

    [TestMethod]
    public async Task Classify_VarInForeach_IsKeyword()
    {
        var result = await ClassificationAtAsync("foreach (var i in new[] { 1 }) { }", "var");
        result.Should().Be(SymbolClassification.Keyword);
    }

    [TestMethod]
    public async Task Classify_VariableNamedVar_IsIdentifierNotKeyword()
    {
        // "var" is contextual, so the declaration around it decides — text alone would be
        // enough to colour an ordinary variable as a keyword.
        var result = await ClassificationAtAsync("int @var = 1;", "@var");
        result.Should().Be(SymbolClassification.Identifier);
    }

    [TestMethod]
    public async Task Classify_DirectiveKeyword_IsPreprocessorKeyword()
    {
        // #if and if share a SyntaxKind; only the directive context separates them.
        var result = await ClassificationAtAsync("#if DEBUG\n#endif\nvar x = 1;", "if");
        result.Should().Be(SymbolClassification.PreprocessorKeyword);
    }

    [TestMethod]
    public async Task Classify_StringLiteral_IsStringLiteral()
    {
        var result = await ClassificationAtAsync("var s = \"hello\";", "\"hello\"");
        result.Should().Be(SymbolClassification.StringLiteral);
    }

    [TestMethod]
    public async Task Classify_VerbatimStringLiteral_IsVerbatimStringLiteral()
    {
        var result = await ClassificationAtAsync("var s = @\"c:\\temp\";", "@\"c:\\temp\"");
        result.Should().Be(SymbolClassification.VerbatimStringLiteral);
    }

    [TestMethod]
    public async Task Classify_NumericLiteral_IsNumericLiteral()
    {
        var result = await ClassificationAtAsync("var x = 42;", "42");
        result.Should().Be(SymbolClassification.NumericLiteral);
    }

    [TestMethod]
    public async Task Classify_SingleLineComment_IsComment()
    {
        var result = await ClassificationAtAsync("// note\nvar x = 1;", "// note");
        result.Should().Be(SymbolClassification.Comment);
    }

    [TestMethod]
    public async Task Classify_MultiLineComment_IsComment()
    {
        var result = await ClassificationAtAsync("/* note */ var x = 1;", "/* note */");
        result.Should().Be(SymbolClassification.Comment);
    }

    [TestMethod]
    public async Task Classify_StructuralPunctuation_IsPunctuation()
    {
        var result = await ClassificationAtAsync("var x = 1;", ";");
        result.Should().Be(SymbolClassification.Punctuation);
    }

    [TestMethod]
    public async Task Classify_ArithmeticOperator_IsOperator()
    {
        var result = await ClassificationAtAsync("var x = 1 + 2;", "+");
        result.Should().Be(SymbolClassification.Operator);
    }

    [TestMethod]
    public async Task Classify_Identifier_IsUnresolvedIdentifier()
    {
        // Without a semantic model a type name is indistinguishable from any other
        // identifier. The full pass refines this; here it must not guess.
        var result = await ClassificationAtAsync("Console.WriteLine(1);", "Console");
        result.Should().Be(SymbolClassification.Identifier);
    }

    [TestMethod]
    public async Task Classify_SpansAreOrderedByPosition()
    {
        var classifications = await ClassifyAsync("var x = 1; // done\nvar y = \"s\";");

        classifications.Select(c => c.SpanStart)
            .Should().BeInAscendingOrder("the editor applies spans in order, so later ones win where they overlap");
    }

    [TestMethod]
    public async Task Classify_EmptyScript_ReturnsNoSpans()
    {
        var classifications = await ClassifyAsync(string.Empty);
        classifications.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Classify_MalformedScript_StillClassifiesWhatItCan()
    {
        // Live colouring runs constantly against half-typed code, so a broken parse must
        // still yield spans rather than throwing.
        var classifications = await ClassifyAsync("var x = \"unterminated");

        classifications.Should().NotBeEmpty();
        classifications.Should().Contain(c => c.Classification == SymbolClassification.Keyword);
    }

    [TestMethod]
    public async Task Classify_CancelledBeforeStart_Throws()
    {
        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript("var x = 1;");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await new ScriptAnalyser(context).GetSyntacticClassificationsAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task Classify_CoversMostOfWhatTheSemanticPassProduces()
    {
        // The syntactic pass is a stand-in until the semantic pass lands, so it should be
        // in the same ballpark for span count rather than a sparse subset.
        var script = string.Concat(Enumerable.Range(0, 40).Select(i =>
            $"var v{i} = {i} * 2 + 1;\nvar t{i} = \"item \" + v{i}.ToString(); // note {i}\n"));

        var context = await ScriptContext.CreateAsync();
        context = context.ApplyScript(script);
        var analyser = new ScriptAnalyser(context);

        var syntactic = await analyser.GetSyntacticClassificationsAsync();
        var semantic = await analyser.GetClassificationsAsync();

        syntactic.Count.Should().BeGreaterThan((int)(semantic.Count * 0.8));
    }
}
