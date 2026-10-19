namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Compact information about the symbol at a position, for an editor's hover popup: what the
/// symbol is and a one-paragraph summary.
/// </summary>
/// <remarks>
/// Deliberately light — the fuller documentation (returns, exceptions, parameters) belongs in a
/// separate, richer view. For a method this describes the overload the code actually calls.
/// </remarks>
public sealed record QuickInfo
{
    /// <summary>
    /// What the symbol is, as Roslyn describes it — for example
    /// <c>double Math.Round(double value, int digits) (+ 7 overloads)</c>,
    /// <c>class System.Math</c>, or <c>(field) double price</c>.
    /// </summary>
    public string Signature { get; init; } = string.Empty;

    /// <summary>The XML-doc summary, or empty when the symbol has none.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The offset where the described word starts.</summary>
    public int Start { get; init; }

    /// <summary>The length of the described word.</summary>
    public int Length { get; init; }
}
