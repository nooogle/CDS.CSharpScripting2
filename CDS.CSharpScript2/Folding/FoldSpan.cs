namespace CDS.CSharpScript2.Folding;

/// <summary>
/// Represents a foldable range within a script, given as a character span.
/// </summary>
public record FoldSpan(int SpanStart, int SpanLength);
