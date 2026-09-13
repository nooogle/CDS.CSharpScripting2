namespace CDS.CSharpScript2.CodeCompletion;

/// <summary>
/// How well a completion item's display text matches the text the user has typed so far.
/// </summary>
/// <remarks>
/// The values are ordered from weakest to strongest match, so a plain numeric comparison ranks
/// two matches. <see cref="None"/> is the only value that means "do not offer this item".
/// </remarks>
public enum CompletionMatchKind
{
    /// <summary>The typed text does not appear in the item at all.</summary>
    None = 0,

    /// <summary>The typed text appears somewhere inside the item, mid-word.</summary>
    Substring = 1,

    /// <summary>The typed text appears inside the item at the start of a word — "Me" in "CreateMenu".</summary>
    WordBoundarySubstring = 2,

    /// <summary>The typed characters are the initials of successive words — "CM" in "CreateMenu".</summary>
    CamelCaseInitials = 3,

    /// <summary>The item begins with the typed text, ignoring case.</summary>
    CaseInsensitivePrefix = 4,

    /// <summary>The item begins with the typed text, case included.</summary>
    ExactPrefix = 5,
}
