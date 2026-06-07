namespace CDS.CSharpScript2.CodeCompletion;

/// <summary>
/// Compares two strings alphabetically, but biases results so that strings beginning with
/// a specified letter sort first, followed by strings that merely contain that letter.
/// </summary>
public class StringComparerWithBiasForSingleLetter
{
    private readonly char _lowerChar;
    private readonly char _upperChar;
    private readonly string _lowerString;
    private readonly string _upperString;

    /// <summary>
    /// Initializes a new instance with the letter that receives sort priority.
    /// </summary>
    /// <param name="singleLetter">The letter whose presence or absence drives the sort bias.</param>
    public StringComparerWithBiasForSingleLetter(char singleLetter)
    {
        _lowerChar = char.ToLower(singleLetter);
        _upperChar = char.ToUpper(singleLetter);
        _lowerString = _lowerChar.ToString();
        _upperString = _upperChar.ToString();
    }

    /// <summary>
    /// Compares <paramref name="left"/> and <paramref name="right"/> using the sort bias.
    /// </summary>
    /// <remarks>
    /// Priority order:
    /// <list type="number">
    ///   <item>Strings that begin with the biased letter (both: alphabetical; one: that one first).</item>
    ///   <item>Strings that contain the biased letter anywhere (same tie-break).</item>
    ///   <item>All remaining strings in alphabetical order.</item>
    /// </list>
    /// </remarks>
    /// <param name="left">The first string to compare.</param>
    /// <param name="right">The second string to compare.</param>
    /// <returns>A negative value if <paramref name="left"/> sorts first, positive if <paramref name="right"/> sorts first, zero if equal.</returns>
    public int Compare(string left, string right)
    {
        var leftBegins = left.StartsWith(_upperString) || left.StartsWith(_lowerString);
        var rightBegins = right.StartsWith(_upperString) || right.StartsWith(_lowerString);

        if (leftBegins && rightBegins) { return left.CompareTo(right); }
        if (leftBegins) { return -1; }
        if (rightBegins) { return 1; }

        var leftContains = left.IndexOf(_upperChar) >= 0 || left.IndexOf(_lowerChar) >= 0;
        var rightContains = right.IndexOf(_upperChar) >= 0 || right.IndexOf(_lowerChar) >= 0;

        if (leftContains && !rightContains) { return -1; }
        if (!leftContains && rightContains) { return 1; }

        return left.CompareTo(right);
    }
}
