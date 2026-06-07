namespace CDS.CSharpScript2.Classification;

/// <summary>
/// Represents a classified symbol with its span and classification within a script.
/// </summary>
public record ClassifiedSymbol(
    int SpanStart,
    int SpanLength,
    SymbolClassification Classification);
