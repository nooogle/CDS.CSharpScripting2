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
            | SymbolDisplayMemberOptions.IncludeParameters
            | SymbolDisplayMemberOptions.IncludeConstantValue,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType
            | SymbolDisplayParameterOptions.IncludeName
            | SymbolDisplayParameterOptions.IncludeExtensionThis,
        propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
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

        if (symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.Constructor } or IPropertySymbol or IFieldSymbol or IEventSymbol)
            return BuildMemberPage(symbol.OriginalDefinition, compilation);

        return null;
    }

    /// <summary>Whether a symbol referenced from documentation text has a page to link to.</summary>
    internal static bool HasPage(ISymbol symbol) => symbol switch
    {
        INamedTypeSymbol { TypeKind: TypeKind.Error } => false,
        INamedTypeSymbol => true,
        IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.Constructor } => true,
        IPropertySymbol or IFieldSymbol or IEventSymbol => true,
        _ => false,
    };

    private static DocPage BuildMemberPage(ISymbol member, Compilation? compilation)
    {
        var comment = new DocCommentParser(compilation).Parse(member.GetDocumentationCommentXml(expandIncludes: true));
        var type = member.ContainingType;
        var method = member as IMethodSymbol;
        var isConstructor = method?.MethodKind == MethodKind.Constructor;
        var inScriptClass = type.IsScriptClass;

        var kindWord = member switch
        {
            IPropertySymbol => "Property",
            IFieldSymbol => "Field",
            IEventSymbol => "Event",
            _ => isConstructor ? "Constructor" : "Method",
        };

        // A script's own members belong to the implicit script class, which is not worth naming.
        var kindLine = inScriptClass
            ? new List<DocRun> { new(kindWord, DocRunKind.Text, null) }
            : new List<DocRun>
            {
                new(kindWord + " · ", DocRunKind.Text, null),
                new(type.Name, DocRunKind.Link, TargetOf(type)),
            };

        var name = member is IPropertySymbol { IsIndexer: true } ? "this[]" : member.Name;
        var title = isConstructor ? type.Name : inScriptClass ? name : $"{type.Name}.{name}";

        return new DocPage
        {
            Target = TargetOf(member),
            KindLine = kindLine,
            Title = title,
            Signature = member.ToDisplayString(s_signatureFormat),
            Summary = comment.Summary,
            Parameters = comment.Parameters,
            Returns = comment.Returns.Count > 0 ? comment.Returns : comment.Value,
            Exceptions = comment.Exceptions,
            Remarks = comment.Remarks,
            Overloads = OverloadsOf(member, type),
        };
    }

    private static List<DocOverload> OverloadsOf(ISymbol member, INamedTypeSymbol type)
    {
        IEnumerable<ISymbol> siblings;
        if (member is IMethodSymbol method)
            siblings = type.GetMembers(method.Name).OfType<IMethodSymbol>().Where(m => m.MethodKind == method.MethodKind);
        else if (member is IPropertySymbol { IsIndexer: true })
            siblings = type.GetMembers().OfType<IPropertySymbol>().Where(p => p.IsIndexer);
        else
            return [];

        return siblings
            // Hide non-public overloads of library types, but keep a script's own members (top-level script members need not be public).
            .Where(m => m.DeclaredAccessibility == Accessibility.Public || m.Locations.Any(l => l.IsInSource))
            .OrderBy(m => m.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0)
            .Select(m => (Member: m, Target: TargetOf(m)))
            .Where(x => x.Target is not null)
            .Select(x => new DocOverload(OverloadLabel(x.Member), x.Target!, SymbolEqualityComparer.Default.Equals(x.Member, member)))
            .ToList();
    }

    private static DocPage BuildVariablePage(ISymbol symbol)
    {
        var type = symbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            _ => ((IFieldSymbol)symbol).Type,
        };

        // Only an ordinary named type has a page to link to; nullable value types, tuples, anonymous and error types show as plain text.
        var linkable = type is INamedTypeSymbol { IsAnonymousType: false, IsTupleType: false, TypeKind: not TypeKind.Error } named
            && named.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;

        var kindLine = new List<DocRun>
        {
            new(symbol is IParameterSymbol ? "Parameter · " : "Variable · ", DocRunKind.Text, null),
            linkable
                ? new DocRun(type.Name, DocRunKind.Link, TargetOf(type))
                : new DocRun(type.ToDisplayString(s_typeFormat), DocRunKind.Text, null),
        };

        return new DocPage
        {
            Target = TargetOf(symbol),
            KindLine = kindLine,
            Title = symbol.Name,
            Signature = $"{type.ToDisplayString(s_typeFormat)} {symbol.Name}",
        };
    }

    private static string OverloadLabel(ISymbol symbol)
    {
        var parameters = symbol switch
        {
            IMethodSymbol m => m.Parameters,
            IPropertySymbol p => p.Parameters,
            _ => [],
        };
        var list = string.Join(", ", parameters.Select(p => p.Type.ToDisplayString(s_typeFormat)));
        return symbol switch
        {
            IMethodSymbol { MethodKind: MethodKind.Constructor } => $"{symbol.ContainingType.Name}({list})",
            IPropertySymbol { IsIndexer: true } => $"this[{list}]",
            _ => $"{symbol.Name}({list})",
        };
    }

    /// <summary>Gets the page target for a symbol, or <see langword="null"/> when it has no documentation ID.</summary>
    internal static DocTarget? TargetOf(ISymbol symbol)
    {
        var id = DocumentationCommentId.CreateDeclarationId(symbol.OriginalDefinition);
        return id is null ? null : new DocTarget(id);
    }
}
