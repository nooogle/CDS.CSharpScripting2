using Microsoft.CodeAnalysis;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Extracts API information (type metadata, member signatures, XML documentation)
/// for the symbol at a given cursor position. Designed for hover tooltips and signature help.
/// </summary>
internal static class APIInfoService
{
    internal static APIInfoResult? Get(SyntaxTree syntaxTree, SemanticModel semanticModel, int position)
    {
        var symbolContext = SymbolLocator.Locate(syntaxTree, position);
        if (symbolContext == null)
            return null;

        return SymbolInfoExtractor.Extract(symbolContext, semanticModel, position);
    }
}