using CDS.CSharpScript2.Documentation;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// A range of characters in the docs panel's text box that links to another documentation page.
/// </summary>
internal readonly struct DocLinkSpan
{
    public DocLinkSpan(int start, int length, DocTarget target)
    {
        Start = start;
        Length = length;
        Target = target;
    }

    /// <summary>The index of the first character of the link.</summary>
    public int Start { get; }

    /// <summary>The number of characters in the link.</summary>
    public int Length { get; }

    /// <summary>The page the link leads to.</summary>
    public DocTarget Target { get; }

    /// <summary>Whether <paramref name="charIndex"/> falls inside the link.</summary>
    public bool Contains(int charIndex) => charIndex >= Start && charIndex < Start + Length;
}
