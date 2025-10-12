using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Finds method overloads for a given expression.
/// </summary>
public static class OverloadFinder
{
    public static IEnumerable<IMethodSymbol> Find(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        if (expression == null) return Enumerable.Empty<IMethodSymbol>();
        var symbolInfo = semanticModel.GetSymbolInfo(expression);
        var symbol = symbolInfo.Symbol;
        if (symbol == null && symbolInfo.CandidateSymbols.Any() &&
           (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure || symbolInfo.CandidateReason == CandidateReason.MemberGroup || symbolInfo.CandidateReason == CandidateReason.None))
        {
            return symbolInfo.CandidateSymbols.OfType<IMethodSymbol>();
        }
        if (symbol is IMethodSymbol methodSymbol)
        {
            var containingTypeMembers = methodSymbol.ContainingType?.GetMembers(methodSymbol.Name).OfType<IMethodSymbol>()
                                       ?? Enumerable.Empty<IMethodSymbol>();
            return containingTypeMembers;
        }
        return Enumerable.Empty<IMethodSymbol>();
    }
}
