using Microsoft.CodeAnalysis;

namespace CDS.CSharpScript2.Documentation;

/// <summary>Builds the <see cref="DocPage"/> for a type, listing its public members by group.</summary>
internal static class DocTypePageBuilder
{
    private static readonly SymbolDisplayFormat s_declarationFormat = new(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);

    internal static DocPage Build(INamedTypeSymbol type, Compilation? compilation)
    {
        var comment = new DocCommentParser(compilation).Parse(type.GetDocumentationCommentXml(expandIncludes: true));

        var kindText = KindWord(type);
        var ns = type.ContainingNamespace;
        if (ns is { IsGlobalNamespace: false })
            kindText += " · " + ns.ToDisplayString();

        return new DocPage
        {
            Target = DocPageBuilder.TargetOf(type),
            KindLine = [new DocRun(kindText, DocRunKind.Text, null)],
            Title = type.Name,
            Signature = SignatureOf(type),
            Summary = comment.Summary,
            Remarks = comment.Remarks,
            MemberGroups = BuildGroups(type),
        };
    }

    private static string SignatureOf(INamedTypeSymbol type)
    {
        // The display format omits accessibility and modifiers on types, so they are added here.
        var modifiers = type.DeclaredAccessibility == Accessibility.Public ? "public " : string.Empty;
        if (type.TypeKind == TypeKind.Class)
        {
            if (type.IsStatic)
                modifiers += "static ";
            else if (type.IsAbstract)
                modifiers += "abstract ";
            else if (type.IsSealed)
                modifiers += "sealed ";
        }

        return modifiers + type.ToDisplayString(s_declarationFormat);
    }

    private static string KindWord(INamedTypeSymbol type) => type.TypeKind switch
    {
        TypeKind.Interface => "Interface",
        TypeKind.Enum => "Enum",
        TypeKind.Delegate => "Delegate",
        TypeKind.Struct => type.IsRecord ? "Record struct" : "Struct",
        _ => type.IsRecord ? "Record" : type.IsStatic ? "Static class" : "Class",
    };

    private static List<DocMemberGroup> BuildGroups(INamedTypeSymbol type)
    {
        var members = type.GetMembers()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.IsImplicitlyDeclared)
            .ToList();

        var groups = new List<DocMemberGroup>();
        AddGroup(groups, "Fields", members.OfType<IFieldSymbol>());
        AddGroup(groups, "Properties", members.OfType<IPropertySymbol>());
        AddGroup(groups, "Methods", members.OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Ordinary));
        AddGroup(groups, "Events", members.OfType<IEventSymbol>());
        AddGroup(groups, "Constructors", members.OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Constructor));
        return groups;
    }

    private static void AddGroup(List<DocMemberGroup> groups, string heading, IEnumerable<ISymbol> symbols)
    {
        // Overloads share a name, so each name is listed once, targeting the first overload declared.
        var entries = symbols
            .GroupBy(s => s is IMethodSymbol { MethodKind: MethodKind.Constructor } ? s.ContainingType.Name : s.Name)
            .Select(g => new DocMember(g.Key, g.Count(), DocPageBuilder.TargetOf(g.OrderBy(DeclarationOrder).First())))
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (entries.Count > 0)
            groups.Add(new DocMemberGroup(heading, entries));
    }

    private static int DeclarationOrder(ISymbol symbol)
        => symbol.Locations.FirstOrDefault(l => l.IsInSource)?.SourceSpan.Start ?? 0;
}
