using AwesomeAssertions;
using CDS.CSharpScript2;
using CDS.CSharpScript2.Classification;

namespace UnitTests;

/// <summary>
/// Unit tests for code classification.
/// </summary>
[TestClass]
public class UT_Classifications
{
    [TestMethod]
    public async Task Classification_ForClassName_IsClassName()
    {
        // Setup
        var script = "Console.WriteLine(\"Hello\");";
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript(script);
        var classifications = await scriptManager.GetClassifications();

        // Check classification
        classifications[0].SpanStart.Should().Be(0);
        classifications[0].SpanLength.Should().Be(7);
        classifications[0].Classification.Should().Be(SymbolClassification.ClassName); 
    }

    [TestMethod]
    public async Task Classification_ForOperator_IsOperator()
    {
        // Setup
        var script = "Console.WriteLine(\"Hello\");";
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript(script);
        var classifications = await scriptManager.GetClassifications();

        // Check classification
        classifications[1].SpanStart.Should().Be(7);
        classifications[1].SpanLength.Should().Be(1);
        classifications[1].Classification.Should().Be(SymbolClassification.Operator);
    }

    [TestMethod]
    public async Task Classification_ForMethodName_IsMethodName()
    {
        // Setup
        var script = "Console.WriteLine(\"Hello\");";
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript(script);
        var classifications = await scriptManager.GetClassifications();

        // Check classification
        classifications[2].SpanStart.Should().Be(8);
        classifications[2].SpanLength.Should().Be(9);
        classifications[2].Classification.Should().Be(SymbolClassification.MethodName);
    }


    [TestMethod]
    public async Task Classification_ForPunctuation_IsPunctuation()
    {
        // Setup
        var script = "Console.WriteLine(\"Hello\");";
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript(script);
        var classifications = await scriptManager.GetClassifications();

        // Check classification of the opening parenthesis after WriteLine
        classifications[3].SpanStart.Should().Be(17);
        classifications[3].SpanLength.Should().Be(1);
        classifications[3].Classification.Should().Be(SymbolClassification.Punctuation);
    }


    [TestMethod]
    public async Task Classification_ForStringLiteral_IsStringLiteral()
    {
        // Setup
        var script = "Console.WriteLine(\"Hello\");";
        var scriptManager = await ScriptManager.CreateAsync();
        scriptManager = scriptManager.ApplyScript(script);
        var classifications = await scriptManager.GetClassifications();

        // Check classification - the string "Hello" is at position 18, length 7 (includes quotes)
        classifications[4].SpanStart.Should().Be(18);
        classifications[4].SpanLength.Should().Be(7);
        classifications[4].Classification.Should().Be(SymbolClassification.StringLiteral);
    }
}
