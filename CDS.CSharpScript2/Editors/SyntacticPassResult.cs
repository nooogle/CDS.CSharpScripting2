namespace CDS.CSharpScript2.Editors;

/// <summary>
/// Result of a syntax-only analysis pass: everything derivable from the syntax tree alone,
/// without a compilation or semantic model.
/// </summary>
public record SyntacticPassResult(
    IReadOnlyList<Classification.ClassifiedSymbol> Classifications,
    IReadOnlyList<Folding.FoldSpan> FoldSpans);
