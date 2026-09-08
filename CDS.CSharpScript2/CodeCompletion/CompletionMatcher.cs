using System;
using System.Collections.Generic;

namespace CDS.CSharpScript2.CodeCompletion;

/// <summary>
/// Decides whether a completion item matches the text typed so far, and how well.
/// </summary>
/// <remarks>
/// <para>
/// This mirrors the filtering Visual Studio and VS Code apply: the typed characters do not have to
/// be the start of the item. Typing "Me" offers <c>CreateMenu</c> because "Me" begins one of its
/// words, and typing "CM" offers it because those are the initials of its words — but anything
/// starting with the typed text is still ranked above both.
/// </para>
/// <para>
/// Word boundaries are taken from casing and separators, the way C# identifiers are written:
/// <c>CreateMenu</c> splits into "Create" and "Menu", <c>XMLParser</c> into "XML" and "Parser",
/// <c>read_line2</c> into "read", "line" and "2".
/// </para>
/// </remarks>
public static class CompletionMatcher
{
    /// <summary>
    /// Tests <paramref name="candidate"/> against <paramref name="pattern"/>.
    /// </summary>
    /// <param name="candidate">The completion item's display text.</param>
    /// <param name="pattern">The text the user has typed so far.</param>
    /// <returns>
    /// The strongest match between the two, or <see cref="CompletionMatch.NoMatch"/> when the
    /// candidate should not be offered. An empty pattern matches everything.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="candidate"/> or <paramref name="pattern"/> is <see langword="null"/>.
    /// </exception>
    public static CompletionMatch Match(string candidate, string pattern)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        if (pattern.Length == 0)
        {
            return new CompletionMatch(CompletionMatchKind.ExactPrefix, 0);
        }

        if (candidate.Length == 0)
        {
            return CompletionMatch.NoMatch;
        }

        if (candidate.StartsWith(pattern, StringComparison.Ordinal))
        {
            return new CompletionMatch(CompletionMatchKind.ExactPrefix, 0);
        }

        if (candidate.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
        {
            return new CompletionMatch(CompletionMatchKind.CaseInsensitivePrefix, 0);
        }

        // Only worth asking about initials once there are at least two of them; a single letter is
        // already covered, and better described, by the word-boundary test below.
        if (pattern.Length > 1)
        {
            int initialsIndex = MatchWordInitials(candidate, pattern);
            if (initialsIndex >= 0)
            {
                return new CompletionMatch(CompletionMatchKind.CamelCaseInitials, initialsIndex);
            }
        }

        int substringIndex = IndexOfIgnoreCase(candidate, pattern);
        if (substringIndex < 0)
        {
            return CompletionMatch.NoMatch;
        }

        // A match that starts a word reads as intentional; one that lands mid-word is a last resort.
        int boundaryIndex = substringIndex;
        while (boundaryIndex >= 0 && !IsWordStart(candidate, boundaryIndex))
        {
            boundaryIndex = IndexOfIgnoreCase(candidate, pattern, boundaryIndex + 1);
        }

        return boundaryIndex >= 0
            ? new CompletionMatch(CompletionMatchKind.WordBoundarySubstring, boundaryIndex)
            : new CompletionMatch(CompletionMatchKind.Substring, substringIndex);
    }

    /// <summary>
    /// Matches the pattern against the initials of the candidate's words, allowing words to be
    /// skipped ("CM" matches <c>CreateSubMenu</c>). Returns the offset of the first word used, or
    /// -1 when the pattern is not a run of initials.
    /// </summary>
    private static int MatchWordInitials(string candidate, string pattern)
    {
        var wordStarts = GetWordStarts(candidate);
        if (wordStarts.Count < pattern.Length)
        {
            return -1;
        }

        // Try every word as the anchor, so "Menu" in "CreateMenuItem" can be reached by "MI".
        for (int anchor = 0; anchor <= wordStarts.Count - pattern.Length; anchor++)
        {
            if (!EqualsIgnoreCase(candidate[wordStarts[anchor]], pattern[0]))
            {
                continue;
            }

            int patternIndex = 1;
            for (int word = anchor + 1; word < wordStarts.Count && patternIndex < pattern.Length; word++)
            {
                if (EqualsIgnoreCase(candidate[wordStarts[word]], pattern[patternIndex]))
                {
                    patternIndex++;
                }
            }

            if (patternIndex == pattern.Length)
            {
                return wordStarts[anchor];
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns the offsets at which each word of the candidate begins.
    /// </summary>
    private static List<int> GetWordStarts(string candidate)
    {
        var wordStarts = new List<int>();

        for (int index = 0; index < candidate.Length; index++)
        {
            if (IsWordStart(candidate, index))
            {
                wordStarts.Add(index);
            }
        }

        return wordStarts;
    }

    /// <summary>
    /// Determines whether the character at <paramref name="index"/> begins a word.
    /// </summary>
    private static bool IsWordStart(string candidate, int index)
    {
        if (index == 0)
        {
            return true;
        }

        char current = candidate[index];
        char previous = candidate[index - 1];

        if (!char.IsLetterOrDigit(current))
        {
            return false;
        }

        // A separator, or a switch between letters and digits, always starts something new.
        if (!char.IsLetterOrDigit(previous))
        {
            return true;
        }

        if (char.IsDigit(current) != char.IsDigit(previous))
        {
            return true;
        }

        if (!char.IsUpper(current))
        {
            return false;
        }

        // "createMenu" — the run of capitals starts here.
        if (!char.IsUpper(previous))
        {
            return true;
        }

        // "XMLParser" — the last capital of a run belongs to the word that follows it.
        return index + 1 < candidate.Length && char.IsLower(candidate[index + 1]);
    }

    private static int IndexOfIgnoreCase(string candidate, string pattern)
        => IndexOfIgnoreCase(candidate, pattern, 0);

    private static int IndexOfIgnoreCase(string candidate, string pattern, int startIndex)
    {
        if (startIndex > candidate.Length - pattern.Length)
        {
            return -1;
        }

        return candidate.IndexOf(pattern, startIndex, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EqualsIgnoreCase(char left, char right)
        => left == right || char.ToUpperInvariant(left) == char.ToUpperInvariant(right);
}
