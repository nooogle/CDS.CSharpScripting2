namespace CDS.CSharpScript2.Classification;

public class ClassifiedSymbol
{
    public int SpanStart { get; } 
    public int SpanLength { get; }
    public SymbolClassification Classification { get; }


    public ClassifiedSymbol(
        int spanStart,
        int spanLength,
        SymbolClassification symbolClassification)
    {
        SpanStart = spanStart;
        SpanLength = spanLength;
        Classification = symbolClassification;
    }
}
