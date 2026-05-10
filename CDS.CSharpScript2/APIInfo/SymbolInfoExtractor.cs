using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Extracts API information from a syntax node and semantic model at a specific cursor position.
/// </summary>
internal static class SymbolInfoExtractor
{
    internal static APIInfoResult? Extract(
        SymbolLocator.SymbolContext context,
        SemanticModel semanticModel,
        int position)
    {
        var node = context.Node;

        // 1. Inside a method call's argument list — show all overloads.
        var invocation = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocation?.ArgumentList.FullSpan.Contains(position) == true)
        {
            var overloads = FindOverloads(semanticModel, invocation.Expression).ToList();
            if (overloads.Count > 0)
            {
                var details = overloads.Select(TypeInfoBuilder.CreateInfoForSymbol).OfType<MemberDetailsInfo>().ToList();
                return new APIInfoResult(TypeInfoBuilder.GetDetailedTypeInfo(overloads[0].ContainingType), details);
            }
        }

        // 2. On the name part of a member access (e.g. 'WriteLine' in 'Console.WriteLine').
        var memberAccess = node.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
        if (memberAccess?.Name.Span.Contains(position) == true)
        {
            var info = semanticModel.GetSymbolInfo(memberAccess);
            var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
            if (symbol != null)
                return BuildResult(symbol, semanticModel);
        }

        // 3. Any identifier or predefined type — variable, type name, parameter, etc.
        //    Falls back to using the token's parent node for declarations (VariableDeclarator etc.).
        var symbolNode = node.AncestorsAndSelf()
            .FirstOrDefault(n => n is IdentifierNameSyntax or PredefinedTypeSyntax);

        if (symbolNode == null && context.Token.IsKind(SyntaxKind.IdentifierToken))
            symbolNode = node;

        if (symbolNode != null)
        {
            var info = semanticModel.GetSymbolInfo(symbolNode);
            var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
            if (symbol != null)
                return BuildResult(symbol, semanticModel);
        }

        // 4. On the 'new' keyword or inside an object creation argument list — show constructors.
        var objectCreation = node.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
        if (objectCreation != null && (
            objectCreation.NewKeyword.Span.Contains(position) ||
            objectCreation.ArgumentList?.FullSpan.Contains(position) == true))
        {
            var targetType = semanticModel.GetTypeInfo(objectCreation.Type).Type as INamedTypeSymbol;
            if (targetType != null)
            {
                var ctors = targetType.GetMembers(".ctor").OfType<IMethodSymbol>()
                    .Where(m => !m.IsImplicitlyDeclared)
                    .Select(TypeInfoBuilder.CreateInfoForSymbol).OfType<MemberDetailsInfo>().ToList();
                return new APIInfoResult(TypeInfoBuilder.GetDetailedTypeInfo(targetType), ctors);
            }
        }

        return null;
    }

    /// <summary>
    /// Builds an <see cref="APIInfoResult"/> from any resolved symbol.
    /// Named types produce a type-only result; methods expand to all overloads;
    /// other members produce a single-item member result with the member's type as context.
    /// </summary>
    private static APIInfoResult BuildResult(ISymbol symbol, SemanticModel semanticModel)
    {
        if (symbol is INamedTypeSymbol typeSymbol)
            return new APIInfoResult(TypeInfoBuilder.GetDetailedTypeInfo(typeSymbol));

        if (symbol is IMethodSymbol methodSymbol)
        {
            var overloads = methodSymbol.ContainingType
                .GetMembers(methodSymbol.Name)
                .OfType<IMethodSymbol>()
                .Select(TypeInfoBuilder.CreateInfoForSymbol)
                .OfType<MemberDetailsInfo>()
                .ToList();
            return new APIInfoResult(TypeInfoBuilder.GetDetailedTypeInfo(methodSymbol.ContainingType), overloads);
        }

        var memberDetail = TypeInfoBuilder.CreateInfoForSymbol(symbol);
        var memberType = TypeInfoBuilder.GetSymbolType(symbol);
        return new APIInfoResult(
            memberType != null ? TypeInfoBuilder.GetDetailedTypeInfo(memberType) : null,
            memberDetail != null ? [memberDetail] : null);
    }

    /// <summary>
    /// Finds all overloads of the method referenced by <paramref name="expression"/>.
    /// Returns candidates when overload resolution failed (incomplete argument list during editing).
    /// </summary>
    private static IEnumerable<IMethodSymbol> FindOverloads(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        var info = semanticModel.GetSymbolInfo(expression);

        if (info.Symbol is IMethodSymbol resolved)
            return resolved.ContainingType.GetMembers(resolved.Name).OfType<IMethodSymbol>();

        if (info.CandidateSymbols.Length > 0
            && info.CandidateReason is CandidateReason.OverloadResolutionFailure or CandidateReason.MemberGroup)
            return info.CandidateSymbols.OfType<IMethodSymbol>();

        return [];
    }
}
