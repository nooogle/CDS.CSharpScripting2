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
        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        return BuildForSymbol(symbol, compilation);
    }

    /// <summary>Builds the page for the symbol a <see cref="DocTarget"/> names, or the unavailable page when it no longer resolves.</summary>
    internal static async Task<DocPage> BuildAsync(Document document, DocTarget target, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
            return DocPage.Unavailable(target);

        var symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(target.DocumentationId, compilation);
        return (symbol is null ? null : BuildForSymbol(symbol, compilation)) ?? DocPage.Unavailable(target);
    }

    private static DocPage? BuildForSymbol(ISymbol? symbol, Compilation? compilation)
    {
        // An extension call binds to a reduced method; document the declared static method instead.
        if (symbol is IMethodSymbol { ReducedFrom: { } reducedFrom })
            symbol = reducedFrom;

        if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Error })
            return null;

        if (symbol is INamedTypeSymbol type)
            return DocTypePageBuilder.Build(type.OriginalDefinition, compilation);

        if (symbol is ILocalSymbol or IParameterSymbol or IFieldSymbol { ContainingType.IsScriptClass: true })
            return BuildVariablePage(symbol);

        if (symbol is not IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.Constructor } method)
            return null;

        return BuildMethodPage(method.OriginalDefinition, compilation);
    }

    private static DocPage BuildMethodPage(IMethodSymbol method, Compilation? compilation)
    {
        var comment = new DocCommentParser(compilation).Parse(method.GetDocumentationCommentXml(expandIncludes: true));
        var type = method.ContainingType;
        var isConstructor = method.MethodKind == MethodKind.Constructor;

        // A script's own methods belong to the implicit script class, which is not worth naming.
        var inScriptClass = type.IsScriptClass;

        var kindLine = inScriptClass
            ? new List<DocRun> { new("Method", DocRunKind.Text, null) }
            : new List<DocRun>
            {
                new(isConstructor ? "Constructor · " : "Method · ", DocRunKind.Text, null),
                new(type.Name, DocRunKind.Link, TargetOf(type)),
            };

        var overloads = type.GetMembers(method.Name)
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == method.MethodKind)
            .OrderBy(m => m.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0)
            .Select(m => (Method: m, Target: TargetOf(m)))
            .Where(x => x.Target is not null)
            .Select(x => new DocOverload(OverloadLabel(x.Method), x.Target!, SymbolEqualityComparer.Default.Equals(x.Method, method)))
            .ToList();

        return new DocPage
        {
            Target = TargetOf(method),
            KindLine = kindLine,
            Title = isConstructor ? type.Name : inScriptClass ? method.Name : $"{type.Name}.{method.Name}",
            Signature = method.ToDisplayString(s_signatureFormat),
            Summary = comment.Summary,
            Parameters = comment.Parameters,
            Returns = comment.Returns,
            Exceptions = comment.Exceptions,
            Remarks = comment.Remarks,
            Overloads = overloads,
        };
    }

    private static DocPage BuildVariablePage(ISymbol symbol)
    {
        var type = symbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            _ => ((IFieldSymbol)symbol).Type,
        };

        var kindLine = new List<DocRun>
        {
            new(symbol is IParameterSymbol ? "Parameter · " : "Variable · ", DocRunKind.Text, null),
            new(type is INamedTypeSymbol ? type.Name : type.ToDisplayString(s_typeFormat), type is INamedTypeSymbol ? DocRunKind.Link : DocRunKind.Text, type is INamedTypeSymbol ? TargetOf(type) : null),
        };

        return new DocPage
        {
            Target = TargetOf(symbol),
            KindLine = kindLine,
            Title = symbol.Name,
            Signature = $"{type.ToDisplayString(s_typeFormat)} {symbol.Name}",
        };
    }

    private static string OverloadLabel(IMethodSymbol method)
    {
        var name = method.MethodKind == MethodKind.Constructor ? method.ContainingType.Name : method.Name;
        return $"{name}({string.Join(", ", method.Parameters.Select(p => p.Type.ToDisplayString(s_typeFormat)))})";
    }

    /// <summary>Gets the page target for a symbol, or <see langword="null"/> when it has no documentation ID.</summary>
    internal static DocTarget? TargetOf(ISymbol symbol)
    {
        var id = DocumentationCommentId.CreateDeclarationId(symbol.OriginalDefinition);
        return id is null ? null : new DocTarget(id);
    }
}
