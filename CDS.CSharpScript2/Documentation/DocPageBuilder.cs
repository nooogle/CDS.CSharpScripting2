using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace CDS.CSharpScript2.Documentation;

/// <summary>Builds <see cref="DocPage"/>s for the symbol at a position.</summary>
internal static class DocPageBuilder
{
    private static readonly SymbolDisplayFormat s_signatureFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility
            | SymbolDisplayMemberOptions.IncludeModifiers
            | SymbolDisplayMemberOptions.IncludeType
            | SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType
            | SymbolDisplayParameterOptions.IncludeName
            | SymbolDisplayParameterOptions.IncludeExtensionThis,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat s_typeFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    internal static async Task<DocPage?> BuildAsync(Document document, int position, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
        // An extension call binds to a reduced method; document the declared static method instead.
        if (symbol is IMethodSymbol { ReducedFrom: { } reducedFrom })
            symbol = reducedFrom;

        if (symbol is not IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.Constructor } method)
            return null;

        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        return BuildMethodPage(method.OriginalDefinition, compilation);
    }

    private static DocPage BuildMethodPage(IMethodSymbol method, Compilation? compilation)
    {
        var comment = new DocCommentParser(compilation).Parse(method.GetDocumentationCommentXml(expandIncludes: true));
        var type = method.ContainingType;
        var isConstructor = method.MethodKind == MethodKind.Constructor;

        var kindLine = new List<DocRun>
        {
            new(isConstructor ? "Constructor · " : "Method · ", DocRunKind.Text, null),
            new(type.Name, DocRunKind.Link, TargetOf(type)),
        };

        var overloads = type.GetMembers(method.Name)
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == method.MethodKind)
            .OrderBy(m => m.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0)
            .Select(m => new DocOverload(OverloadLabel(m), TargetOf(m), SymbolEqualityComparer.Default.Equals(m, method)))
            .ToList();

        return new DocPage
        {
            Target = TargetOf(method),
            KindLine = kindLine,
            Title = isConstructor ? type.Name : $"{type.Name}.{method.Name}",
            Signature = method.ToDisplayString(s_signatureFormat),
            Summary = comment.Summary,
            Parameters = comment.Parameters,
            Returns = comment.Returns,
            Exceptions = comment.Exceptions,
            Remarks = comment.Remarks,
            Overloads = overloads,
        };
    }

    private static string OverloadLabel(IMethodSymbol method)
    {
        var name = method.MethodKind == MethodKind.Constructor ? method.ContainingType.Name : method.Name;
        return $"{name}({string.Join(", ", method.Parameters.Select(p => p.Type.ToDisplayString(s_typeFormat)))})";
    }

    private static DocTarget TargetOf(ISymbol symbol)
        => new(DocumentationCommentId.CreateDeclarationId(symbol.OriginalDefinition) ?? symbol.Name);
}
