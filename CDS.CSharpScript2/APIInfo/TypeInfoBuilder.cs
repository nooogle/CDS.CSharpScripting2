using Microsoft.CodeAnalysis;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Builds DetailedTypeInfo and MemberDetailsInfo objects from symbols.
/// </summary>
public static class TypeInfoBuilder
{
    private static readonly SymbolDisplayFormat MinimallyQualifiedFormat = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions:
            SymbolDisplayMemberOptions.IncludeParameters |
            SymbolDisplayMemberOptions.IncludeType |
            SymbolDisplayMemberOptions.IncludeModifiers |
            SymbolDisplayMemberOptions.IncludeRef,
        parameterOptions:
            SymbolDisplayParameterOptions.IncludeType |
            SymbolDisplayParameterOptions.IncludeName |
            SymbolDisplayParameterOptions.IncludeParamsRefOut |
            SymbolDisplayParameterOptions.IncludeDefaultValue,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );

    public static MemberDetailsInfo? CreateInfoForSymbol(ISymbol? symbol)
    {
        if (symbol == null) return null;
        if (symbol.IsImplicitlyDeclared && !(symbol is IMethodSymbol m && m.MethodKind == MethodKind.Constructor))
        {
            if (symbol.Kind == SymbolKind.Field) return null;
        }
        switch (symbol.Kind)
        {
            case SymbolKind.Method:
                return CreateMethodInfo(symbol as IMethodSymbol);
            case SymbolKind.Property:
                return CreatePropertyInfo(symbol as IPropertySymbol);
            case SymbolKind.Field:
                return CreateFieldInfo(symbol as IFieldSymbol);
            case SymbolKind.Event:
                return CreateEventInfo(symbol as IEventSymbol);
            case SymbolKind.Parameter:
                return CreateParameterStandaloneInfo(symbol as IParameterSymbol);
            case SymbolKind.Local:
                return CreateLocalInfo(symbol as ILocalSymbol);
            case SymbolKind.TypeParameter:
                return CreateTypeParameterInfo(symbol as ITypeParameterSymbol);
            default:
                return null;
        }
    }

    public static ITypeSymbol? GetSymbolType(ISymbol? symbol)
    {
        return symbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            IEventSymbol evt => evt.Type,
            _ => null,
        };
    }

    public static DetailedTypeInfo? GetDetailedTypeInfo(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol)
        {
            return new DetailedTypeInfo { Name = typeSymbol.ToDisplayString(MinimallyQualifiedFormat) ?? string.Empty, TypeKind = "Array", Namespace = "System" };
        }
        if (typeSymbol is IPointerTypeSymbol)
        {
            return new DetailedTypeInfo { Name = typeSymbol.ToDisplayString(MinimallyQualifiedFormat) ?? string.Empty, TypeKind = "Pointer" };
        }
        var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
        if (namedTypeSymbol == null || namedTypeSymbol.Kind == SymbolKind.ErrorType)
        {
            return typeSymbol != null ? new DetailedTypeInfo { Name = typeSymbol.ToDisplayString(MinimallyQualifiedFormat) ?? string.Empty, TypeKind = typeSymbol.TypeKind.ToString() ?? string.Empty } : null;
        }
        var docs = DocumentationParser.Parse(namedTypeSymbol);
        return new DetailedTypeInfo
        {
            Name = namedTypeSymbol.ToDisplayString(MinimallyQualifiedFormat) ?? string.Empty,
            Namespace = namedTypeSymbol.ContainingNamespace?.ToDisplayString() ?? "[Global Namespace]",
            Accessibility = namedTypeSymbol.DeclaredAccessibility.ToString() ?? string.Empty,
            TypeKind = namedTypeSymbol.TypeKind.ToString() ?? string.Empty,
            BaseType = namedTypeSymbol.BaseType?.ToDisplayString(MinimallyQualifiedFormat) ?? string.Empty,
            Interfaces = namedTypeSymbol.Interfaces.Select(i => i.ToDisplayString(MinimallyQualifiedFormat) ?? string.Empty).ToList(),
            Summary = docs.Summary ?? string.Empty,
            Remarks = docs.Remarks ?? string.Empty
        };
    }

    private static MemberDetailsInfo? CreateMethodInfo(IMethodSymbol? method)
    {
        if (method == null) return null;
        var docs = DocumentationParser.Parse(method);
        var info = new MemberDetailsInfo
        {
            Name = method.MethodKind == MethodKind.Constructor ? method.ContainingType.Name : method.Name,
            Kind = method.MethodKind.ToString(),
            Signature = method.ToDisplayString(MinimallyQualifiedFormat),
            ReturnType = method.MethodKind == MethodKind.Constructor ? string.Empty : method.ReturnType.ToDisplayString(MinimallyQualifiedFormat),
            Summary = docs.Summary ?? string.Empty,
            Remarks = docs.Remarks ?? string.Empty,
            Parameters = method.Parameters.Select(p => new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(MinimallyQualifiedFormat),
                Documentation = docs.ParamDocs.TryGetValue(p.Name, out var doc) ? doc : string.Empty
            }).ToList()
        };
        return info;
    }

    private static MemberDetailsInfo? CreatePropertyInfo(IPropertySymbol? property)
    {
        if (property == null) return null;
        var docs = DocumentationParser.Parse(property);
        var info = new MemberDetailsInfo
        {
            Name = property.Name,
            Kind = property.IsIndexer ? "Indexer" : "Property",
            Signature = property.ToDisplayString(MinimallyQualifiedFormat),
            ReturnType = property.Type.ToDisplayString(MinimallyQualifiedFormat),
            Summary = docs.Summary ?? string.Empty,
            Remarks = docs.Remarks ?? string.Empty
        };
        if (property.IsIndexer)
        {
            info.Parameters = property.Parameters.Select(p => new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(MinimallyQualifiedFormat),
                Documentation = docs.ParamDocs.TryGetValue(p.Name, out var doc) ? doc : string.Empty
            }).ToList();
        }
        return info;
    }

    private static MemberDetailsInfo? CreateFieldInfo(IFieldSymbol? field)
    {
        if (field == null) return null;
        var docs = DocumentationParser.Parse(field);
        var info = new MemberDetailsInfo
        {
            Name = field.Name,
            Kind = field.IsConst ? "Constant" : (field.IsReadOnly ? "Readonly Field" : "Field"),
            Signature = field.ToDisplayString(MinimallyQualifiedFormat),
            ReturnType = field.Type.ToDisplayString(MinimallyQualifiedFormat),
            Summary = docs.Summary ?? string.Empty,
            Remarks = docs.Remarks ?? string.Empty
        };
        if (field.HasConstantValue && field.ConstantValue != null)
        {
            info.Signature += $" = {field.ConstantValue}";
        }
        return info;
    }

    private static MemberDetailsInfo? CreateEventInfo(IEventSymbol? evt)
    {
        if (evt == null) return null;
        var docs = DocumentationParser.Parse(evt);
        return new MemberDetailsInfo
        {
            Name = evt.Name,
            Kind = "Event",
            Signature = evt.ToDisplayString(MinimallyQualifiedFormat),
            ReturnType = evt.Type.ToDisplayString(MinimallyQualifiedFormat),
            Summary = docs.Summary ?? string.Empty,
            Remarks = docs.Remarks ?? string.Empty
        };
    }

    private static MemberDetailsInfo? CreateParameterStandaloneInfo(IParameterSymbol? parameter)
    {
        if (parameter == null) return null;
        string? paramDoc = null;
        if (parameter.ContainingSymbol != null)
        {
            var docs = DocumentationParser.Parse(parameter.ContainingSymbol);
            paramDoc = docs.ParamDocs.TryGetValue(parameter.Name, out var doc) ? doc : null;
        }
        return new MemberDetailsInfo
        {
            Name = parameter.Name,
            Kind = "Parameter",
            Signature = parameter.ToDisplayString(MinimallyQualifiedFormat),
            ReturnType = parameter.Type.ToDisplayString(MinimallyQualifiedFormat),
            Summary = paramDoc ?? string.Empty
        };
    }

    private static MemberDetailsInfo? CreateLocalInfo(ILocalSymbol? local)
    {
        if (local == null) return null;
        return new MemberDetailsInfo
        {
            Name = local.Name,
            Kind = local.IsConst ? "Local Constant" : "Local Variable",
            Signature = local.ToDisplayString(MinimallyQualifiedFormat),
            ReturnType = local.Type.ToDisplayString(MinimallyQualifiedFormat),
            Summary = string.Empty
        };
    }

    private static MemberDetailsInfo? CreateTypeParameterInfo(ITypeParameterSymbol? typeParam)
    {
        if (typeParam == null) return null;
        string? typeParamDoc = null;
        if (typeParam.ContainingSymbol != null)
        {
            var docs = DocumentationParser.Parse(typeParam.ContainingSymbol);
            typeParamDoc = docs.Summary; // Best effort to provide some doc if applicable
        }
        return new MemberDetailsInfo
        {
            Name = typeParam.Name,
            Kind = "Type Parameter",
            Signature = typeParam.ToDisplayString(MinimallyQualifiedFormat),
            Summary = typeParamDoc ?? string.Empty
        };
    }
}
