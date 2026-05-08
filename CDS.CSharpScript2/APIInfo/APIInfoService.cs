using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml; // For XmlException

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Service for extracting API information (including XML documentation)
/// from code symbols at a specific position. Geared towards hover tooltips.
/// </summary>
public static class APIInfoService
{
    // Only the public entry point remains here. All logic is delegated to specialized classes.
    public static APIInfoResult Get(
        SyntaxTree syntaxTree,
        SemanticModel semanticModel,
        int position)
    {
        // SymbolLocator: finds the relevant node/token for the position
        var symbolContext = SymbolLocator.Locate(syntaxTree, position);
        if (symbolContext == null)
        {
            return new APIInfoResult(null, null);
        }

        // SymbolInfoExtractor: determines what kind of symbol is at the location and what info to extract
        var symbolInfo = SymbolInfoExtractor.Extract(symbolContext, semanticModel, position);
        return symbolInfo ?? new APIInfoResult(null, null);
    }
}