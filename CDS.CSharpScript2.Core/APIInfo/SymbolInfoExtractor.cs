using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Extracts symbol information from a syntax node and semantic model.
/// </summary>
public static class SymbolInfoExtractor
{
    public static APIInfoResult Extract(SymbolLocator.SymbolContext context, SemanticModel semanticModel, int position)
    {
        var node = context.Node;
        var token = context.Token;

        // 1. Method Invocation
        var invocation = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation != null && invocation.ArgumentList.FullSpan.Contains(position))
        {
            var methodGroup = semanticModel.GetSymbolInfo(invocation.Expression);
            var methodSymbol = methodGroup.Symbol as IMethodSymbol;
            var overloads = OverloadFinder.Find(semanticModel, invocation.Expression);
            if (overloads.Any())
            {
                var overloadDetails = overloads.Select(TypeInfoBuilder.CreateInfoForSymbol).Where(info => info != null).ToList();
                var firstOverload = overloads.FirstOrDefault();
                var typeInfo = firstOverload != null ? TypeInfoBuilder.GetDetailedTypeInfo(firstOverload.ContainingType) : null;
                return new APIInfoResult(typeInfo, overloadDetails);
            }
            if (methodSymbol != null)
            {
                var memberDetail = TypeInfoBuilder.CreateInfoForSymbol(methodSymbol);
                var typeInfo = TypeInfoBuilder.GetDetailedTypeInfo(methodSymbol.ContainingType);
                return new APIInfoResult(typeInfo, memberDetail != null ? new[] { memberDetail } : null);
            }
        }

        // 2. Member Access
        var memberAccess = node.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
        if (memberAccess != null && memberAccess.Name.Span.Contains(position))
        {
            var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
            IEnumerable<ISymbol> symbols =
                symbolInfo.Symbol == null ?
                symbolInfo.CandidateSymbols :
                new[] { symbolInfo.Symbol };
            if (symbols.Any())
            {
                var typeInfo = TypeInfoBuilder.GetDetailedTypeInfo(symbols.First().ContainingType);
                var membersDetails = symbols.Select(TypeInfoBuilder.CreateInfoForSymbol).Where(info => info != null).ToList();
                return new APIInfoResult(typeInfo, membersDetails.Count > 0 ? membersDetails : null);
            }
        }

        // 3. Identifier or Predefined Type
        var identifier = node.AncestorsAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault();
        var predefinedTypeSyntax = node.AncestorsAndSelf().OfType<PredefinedTypeSyntax>().FirstOrDefault();
        SyntaxNode symbolNode = null;
        if (identifier != null && identifier.Span.Contains(position))
        {
            symbolNode = identifier;
        }
        else if (predefinedTypeSyntax != null && predefinedTypeSyntax.Span.Contains(position))
        {
            symbolNode = predefinedTypeSyntax;
        }
        else if (node is IdentifierNameSyntax idNode && idNode.Span.Contains(position))
        {
            symbolNode = idNode;
        }
        else if (node is PredefinedTypeSyntax pdNode && pdNode.Span.Contains(position))
        {
            symbolNode = pdNode;
        }
        else if (token.IsKind(SyntaxKind.IdentifierToken) && token.Span.Contains(position))
        {
            symbolNode = node;
        }
        if (symbolNode != null)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(symbolNode);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            if (symbol is INamedTypeSymbol typeSymbol)
            {
                var typeInfo = TypeInfoBuilder.GetDetailedTypeInfo(typeSymbol);
                return new APIInfoResult(typeInfo, null);
            }
            else if (symbol is IMethodSymbol methodSymbol)
            {
                var overloads = methodSymbol.ContainingType.GetMembers(methodSymbol.Name).OfType<IMethodSymbol>();
                var methodInfos = overloads.Select(TypeInfoBuilder.CreateInfoForSymbol).Where(info => info != null).ToList();
                var typeInfo = TypeInfoBuilder.GetDetailedTypeInfo(methodSymbol.ContainingType);
                return new APIInfoResult(typeInfo, methodInfos);
            }
            else if (symbol != null)
            {
                var memberDetail = TypeInfoBuilder.CreateInfoForSymbol(symbol);
                var variableType = TypeInfoBuilder.GetSymbolType(symbol);
                var typeInfo = TypeInfoBuilder.GetDetailedTypeInfo(variableType);
                return new APIInfoResult(typeInfo, memberDetail != null ? new[] { memberDetail } : null);
            }
        }

        // 4. Object Creation
        var objectCreation = node.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
        if (objectCreation != null &&
           (objectCreation.NewKeyword.Span.Contains(position) ||
            objectCreation.Type.Span.Contains(position) ||
            (objectCreation.ArgumentList != null && objectCreation.ArgumentList.FullSpan.Contains(position))))
        {
            ISymbol symbol = null;
            if (objectCreation.Type.Span.Contains(position))
            {
                symbol = semanticModel.GetSymbolInfo(objectCreation.Type).Symbol;
            }
            else
            {
                symbol = semanticModel.GetSymbolInfo(objectCreation).Symbol;
            }
            var methodSymbol = symbol as IMethodSymbol;
            INamedTypeSymbol targetType = null;
            if (methodSymbol != null)
            {
                targetType = methodSymbol.ContainingType;
            }
            else if (symbol is INamedTypeSymbol typeSymbolForNew)
            {
                targetType = typeSymbolForNew;
            }
            else
            {
                targetType = semanticModel.GetTypeInfo(objectCreation.Type).Type as INamedTypeSymbol;
            }
            if (targetType != null)
            {
                var constructors = targetType.GetMembers(".ctor").OfType<IMethodSymbol>()
                    .Where(m => !m.IsImplicitlyDeclared);
                var ctorInfos = constructors.Select(TypeInfoBuilder.CreateInfoForSymbol).Where(info => info != null).ToList();
                var typeInfo = TypeInfoBuilder.GetDetailedTypeInfo(targetType);
                return new APIInfoResult(typeInfo, ctorInfos);
            }
        }
        return null;
    }
}
