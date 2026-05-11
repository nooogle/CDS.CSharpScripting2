namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Describes the cursor's position within a method or indexer argument list.
/// </summary>
public sealed record CallTipContext
{
    /// <summary>Zero-based index of the argument the cursor currently occupies.</summary>
    public int ArgumentIndex { get; init; }

    /// <summary>Character position of the opening parenthesis (or bracket) of the call.</summary>
    public int OpenParenPosition { get; init; }
}
