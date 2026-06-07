namespace CDS.CSharpScript2.CodeCompletion;

/// <summary>
/// Controls how completion items are filtered and sorted based on the length of the
/// current identifier span at the cursor position.
/// </summary>
public enum Mode
{
    /// <summary>No span is present; all items are returned in alphabetical order.</summary>
    AllInAlphabeticalOrder,

    /// <summary>The span is a single character; all items are returned, sorted by single-letter relevance.</summary>
    AllWithSingleLetterMatch,

    /// <summary>The span is two or more characters; only items whose display text starts with the span are returned.</summary>
    MatchingFirstTwoOrMoreOnly,
}
