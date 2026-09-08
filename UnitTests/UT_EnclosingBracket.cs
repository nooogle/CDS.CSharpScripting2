using AwesomeAssertions;
using CDS.CSharpScript2.CodeCompletion;

namespace UnitTests;

/// <summary>
/// Covers <see cref="EnclosingBracket"/>, which decides whether typing a closing bracket should
/// commit the open completion list. The Scintilla editor turns the returned character into an
/// autocomplete fill-up for the session it is about to show.
/// </summary>
[TestClass]
public class UT_EnclosingBracket
{
    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_InsideArgumentList_ReturnsCloseParen()
    {
        var script = "Console.WriteLine(mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().Be(')');
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_InsideIndexer_ReturnsCloseBracket()
    {
        var script = "var x = values[ind";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().Be(']');
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_InsideBlock_ReturnsCloseBrace()
    {
        var script = "if (ready) { Cons";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().Be('}');
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_AtTopLevel_ReturnsNull()
    {
        var script = "var message = mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().BeNull(
            "typing ')' outside any bracket should stay an ordinary character");
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_AfterBalancedBrackets_ReturnsNull()
    {
        var script = "Console.WriteLine(values[0]); var y = mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().BeNull(
            "every bracket opened before the caret has already been closed");
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_WhenBracketsNest_ReturnsTheInnermost()
    {
        var script = "Console.WriteLine(values[ind";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().Be(']',
            "the indexer is the innermost bracket still open");
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_WhenInnerBracketClosed_ReturnsTheOuter()
    {
        var script = "Console.WriteLine(values[0], mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().Be(')');
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_InsideNestedBlocksAndCalls_ReturnsTheInnermost()
    {
        var script = "void Run() { if (ready) { Console.WriteLine(mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().Be(')');
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_IgnoresBracketsInsideStringLiterals()
    {
        var script = "var text = \"a (b\"; var y = mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().BeNull(
            "the '(' is inside a string literal, so nothing is actually open");
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_IgnoresBracketsInsideVerbatimStrings()
    {
        var script = "var path = @\"c:\\dir[0]\"; Console.WriteLine(mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().Be(')',
            "the bracket pair inside the verbatim string must not disturb the stack");
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_IgnoresBracketsInsideRawStringLiterals()
    {
        var script = "var text = \"\"\"a (b\"\"\"; Console.WriteLine(mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().Be(')');
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_IgnoresBracketsInsideCharacterLiterals()
    {
        var script = "var c = '('; var y = mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().BeNull();
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_IgnoresBracketsInsideLineComments()
    {
        var script = "// call Foo(x\nvar y = mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().BeNull();
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_IgnoresBracketsInsideBlockComments()
    {
        var script = "/* values[0] */ Console.WriteLine(mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().Be(')');
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_IgnoresAngleBrackets()
    {
        var script = "var list = new List<Str";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().BeNull(
            "'<' is ambiguous between a type argument list and a less-than operator");
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_LooksOnlyAtTextBeforeTheCaret()
    {
        var script = "Console.WriteLine(mes); var y = 1;";
        var caret = script.IndexOf("mes") + 3;

        EnclosingBracket.GetClosingCharacter(script, caret).Should().Be(')',
            "the caret sits inside the argument list even though the script continues past it");
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_WithCaretOnTheOpeningBracket_ReturnsNull()
    {
        var script = "Console.WriteLine(mes";
        var caret = script.IndexOf('(');

        EnclosingBracket.GetClosingCharacter(script, caret).Should().BeNull(
            "a bracket at the caret is to its right, so it does not enclose the caret");
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_WithUnbalancedClosingBracket_KeepsTheEnclosingOpener()
    {
        var script = "Console.WriteLine(x] + mes";

        EnclosingBracket.GetClosingCharacter(script, script.Length).Should().Be(')',
            "a stray ']' should not discard the parenthesis the caret is genuinely inside");
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_WithEmptyScript_ReturnsNull()
    {
        EnclosingBracket.GetClosingCharacter(string.Empty, 0).Should().BeNull();
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_WithNullScript_Throws()
    {
        var act = () => EnclosingBracket.GetClosingCharacter(null!, 0);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_WithCursorPastTheEnd_Throws()
    {
        var act = () => EnclosingBracket.GetClosingCharacter("Foo(", 5);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    [TestCategory("completions")]
    public void GetClosingCharacter_WithNegativeCursor_Throws()
    {
        var act = () => EnclosingBracket.GetClosingCharacter("Foo(", -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
