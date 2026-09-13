using AwesomeAssertions;
using CDS.CSharpScript2.CodeCompletion;

namespace UnitTests;

/// <summary>
/// Covers the substring / camel-case matching that decides which completion items are offered and
/// in what order.
/// </summary>
[TestClass]
[TestCategory("completions")]
public class UT_CompletionMatcher
{
    // ── Match kinds ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Match_WithExactPrefix_ReturnsExactPrefix()
    {
        CompletionMatcher.Match("CreateMenu", "Create").Kind.Should().Be(CompletionMatchKind.ExactPrefix);
    }

    [TestMethod]
    public void Match_WithDifferentlyCasedPrefix_ReturnsCaseInsensitivePrefix()
    {
        CompletionMatcher.Match("CreateMenu", "crea").Kind.Should().Be(CompletionMatchKind.CaseInsensitivePrefix);
    }

    [TestMethod]
    public void Match_WithWordInitials_ReturnsCamelCaseInitials()
    {
        CompletionMatcher.Match("CreateMenu", "CM").Kind.Should().Be(CompletionMatchKind.CamelCaseInitials);
    }

    [TestMethod]
    public void Match_WithInitialsOfNonAdjacentWords_ReturnsCamelCaseInitials()
    {
        CompletionMatcher.Match("CreateSubMenu", "cm").Kind.Should().Be(CompletionMatchKind.CamelCaseInitials);
    }

    [TestMethod]
    public void Match_WithTextStartingAnInnerWord_ReturnsWordBoundarySubstring()
    {
        var match = CompletionMatcher.Match("CreateMenu", "Me");

        match.Kind.Should().Be(CompletionMatchKind.WordBoundarySubstring);
        match.Index.Should().Be(6, "the match begins at the 'M' of 'Menu'");
    }

    [TestMethod]
    public void Match_WithTextInsideAWord_ReturnsSubstring()
    {
        CompletionMatcher.Match("CreateMenu", "eat").Kind.Should().Be(CompletionMatchKind.Substring);
    }

    [TestMethod]
    public void Match_WithTextThatDoesNotAppear_ReturnsNoMatch()
    {
        var match = CompletionMatcher.Match("CreateMenu", "zzz");

        match.IsMatch.Should().BeFalse();
        match.Should().Be(CompletionMatch.NoMatch);
    }

    [TestMethod]
    public void Match_WithEmptyPattern_MatchesEverything()
    {
        CompletionMatcher.Match("CreateMenu", string.Empty).IsMatch.Should().BeTrue();
    }

    // ── Word splitting ────────────────────────────────────────────────────────

    [TestMethod]
    public void Match_WithAcronymPrefixedName_TreatsTheLastCapitalAsTheNextWord()
    {
        // XMLParser splits into "XML" and "Parser", so "XP" is its initials and "Par" starts a word.
        CompletionMatcher.Match("XMLParser", "XP").Kind.Should().Be(CompletionMatchKind.CamelCaseInitials);
        CompletionMatcher.Match("XMLParser", "Par").Kind.Should().Be(CompletionMatchKind.WordBoundarySubstring);
    }

    [TestMethod]
    public void Match_WithUnderscoreSeparatedName_TreatsTheUnderscoreAsAWordBreak()
    {
        CompletionMatcher.Match("read_line", "li").Kind.Should().Be(CompletionMatchKind.WordBoundarySubstring);
        CompletionMatcher.Match("read_line", "rl").Kind.Should().Be(CompletionMatchKind.CamelCaseInitials);
    }

    [TestMethod]
    public void Match_WithSingleCharacterPattern_NeverReportsInitials()
    {
        // One letter is already described by the prefix or word-boundary kinds.
        CompletionMatcher.Match("CreateMenu", "M").Kind.Should().Be(CompletionMatchKind.WordBoundarySubstring);
    }

    // ── Ranking ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void CompareQualityTo_RanksStrongerKindsFirst()
    {
        var prefix = CompletionMatcher.Match("Meerkat", "Me");
        var wordBoundary = CompletionMatcher.Match("CreateMenu", "Me");

        prefix.CompareQualityTo(wordBoundary).Should().BeNegative(
            "an item starting with the typed text beats one that merely contains it");
    }

    [TestMethod]
    public void CompareQualityTo_WithinOneKind_RanksTheEarlierMatchFirst()
    {
        var early = CompletionMatcher.Match("MenuBar", "Bar");
        var late = CompletionMatcher.Match("CreateMenuBar", "Bar");

        early.CompareQualityTo(late).Should().BeNegative("the earlier match is the closer one");
    }

    [TestMethod]
    public void Match_WithNullCandidate_Throws()
    {
        var act = () => CompletionMatcher.Match(null!, "Me");

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Match_WithNullPattern_Throws()
    {
        var act = () => CompletionMatcher.Match("CreateMenu", null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
