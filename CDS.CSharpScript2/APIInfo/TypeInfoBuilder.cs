using Microsoft.CodeAnalysis;

namespace CDS.CSharpScript2.APIInfo;

/// <summary>
/// Builds <see cref="DetailedTypeInfo"/> and <see cref="MemberDetailsInfo"/> objects from Roslyn symbols.
/// </summary>
internal static class TypeInfoBuilder
{
    private static readonly SymbolDisplayFormat _displayFormat = new SymbolDisplayFormat(
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
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    internal static MemberDetailsInfo? CreateInfoForSymbol(ISymbol? symbol)
    {
        if (symbol == null) return null;

        if (symbol.IsImplicitlyDeclared
            && !(symbol is IMethodSymbol { MethodKind: MethodKind.Constructor })
            && symbol.Kind == SymbolKind.Field)
            return null;

        return symbol.Kind switch
        {
            SymbolKind.Method    => CreateMethodInfo(symbol as IMethodSymbol),
            SymbolKind.Property  => CreatePropertyInfo(symbol as IPropertySymbol),
            SymbolKind.Field     => CreateFieldInfo(symbol as IFieldSymbol),
            SymbolKind.Event     => CreateEventInfo(symbol as IEventSymbol),
            SymbolKind.Parameter => CreateParameterStandaloneInfo(symbol as IParameterSymbol),
            SymbolKind.Local     => CreateLocalInfo(symbol as ILocalSymbol),
            SymbolKind.TypeParameter => CreateTypeParameterInfo(symbol as ITypeParameterSymbol),
            _ => null
        };
    }

    internal static ITypeSymbol? GetSymbolType(ISymbol? symbol) => symbol switch
    {
        ILocalSymbol local       => local.Type,
        IParameterSymbol param   => param.Type,
        IFieldSymbol field       => field.Type,
        IPropertySymbol property => property.Type,
        IEventSymbol evt         => evt.Type,
        _ => null,
    };

    internal static DetailedTypeInfo? GetDetailedTypeInfo(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol)
            return new DetailedTypeInfo { Name = typeSymbol.ToDisplayString(_displayFormat), TypeKind = "Array", Namespace = "System" };

        if (typeSymbol is IPointerTypeSymbol)
            return new DetailedTypeInfo { Name = typeSymbol.ToDisplayString(_displayFormat), TypeKind = "Pointer" };

        if (typeSymbol is not INamedTypeSymbol named || named.Kind == SymbolKind.ErrorType)
            return typeSymbol != null
                ? new DetailedTypeInfo { Name = typeSymbol.ToDisplayString(_displayFormat), TypeKind = typeSymbol.TypeKind.ToString() }
                : null;

        var docs = DocumentationParser.Parse(named);
        return new DetailedTypeInfo
        {
            Name          = named.ToDisplayString(_displayFormat),
            Namespace     = named.ContainingNamespace?.ToDisplayString() ?? "[Global Namespace]",
            Accessibility = named.DeclaredAccessibility.ToString(),
            TypeKind      = named.TypeKind.ToString(),
            BaseType      = named.BaseType?.ToDisplayString(_displayFormat) ?? string.Empty,
            Interfaces    = named.Interfaces.Select(i => i.ToDisplayString(_displayFormat)).ToList(),
            Summary       = docs.Summary,
            Remarks       = docs.Remarks,
        };
    }

    private static MemberDetailsInfo? CreateMethodInfo(IMethodSymbol? method)
    {
        if (method == null) return null;
        var docs = DocumentationParser.Parse(method);
        return new MemberDetailsInfo
        {
            Name       = method.MethodKind == MethodKind.Constructor ? method.ContainingType.Name : method.Name,
            Kind       = method.MethodKind.ToString(),
            Signature  = method.ToDisplayString(_displayFormat),
            ReturnType = method.MethodKind == MethodKind.Constructor ? string.Empty : method.ReturnType.ToDisplayString(_displayFormat),
            Summary    = docs.Summary,
            Remarks    = docs.Remarks,
            Parameters = method.Parameters
                .Select(p => new ParameterInfo(
                    p.Name,
                    p.Type.ToDisplayString(_displayFormat),
                    docs.ParamDocs.TryGetValue(p.Name, out var d) ? d : null))
                .ToList(),
        };
    }

    private static MemberDetailsInfo? CreatePropertyInfo(IPropertySymbol? property)
    {
        if (property == null) return null;
        var docs = DocumentationParser.Parse(property);

        var parameters = property.IsIndexer
            ? property.Parameters
                .Select(p => new ParameterInfo(
                    p.Name,
                    p.Type.ToDisplayString(_displayFormat),
                    docs.ParamDocs.TryGetValue(p.Name, out var d) ? d : null))
                .ToList<ParameterInfo>()
            : [];

        return new MemberDetailsInfo
        {
            Name       = property.Name,
            Kind       = property.IsIndexer ? "Indexer" : "Property",
            Signature  = property.ToDisplayString(_displayFormat),
            ReturnType = property.Type.ToDisplayString(_displayFormat),
            Summary    = docs.Summary,
            Remarks    = docs.Remarks,
            Parameters = parameters,
        };
    }

    private static MemberDetailsInfo? CreateFieldInfo(IFieldSymbol? field)
    {
        if (field == null) return null;
        var docs = DocumentationParser.Parse(field);

        var signature = field.ToDisplayString(_displayFormat);
        if (field.HasConstantValue && field.ConstantValue != null)
            signature += $" = {field.ConstantValue}";

        return new MemberDetailsInfo
        {
            Name       = field.Name,
            Kind       = field.IsConst ? "Constant" : (field.IsReadOnly ? "Readonly Field" : "Field"),
            Signature  = signature,
            ReturnType = field.Type.ToDisplayString(_displayFormat),
            Summary    = docs.Summary,
            Remarks    = docs.Remarks,
        };
    }

    private static MemberDetailsInfo? CreateEventInfo(IEventSymbol? evt)
    {
        if (evt == null) return null;
        var docs = DocumentationParser.Parse(evt);
        return new MemberDetailsInfo
        {
            Name       = evt.Name,
            Kind       = "Event",
            Signature  = evt.ToDisplayString(_displayFormat),
            ReturnType = evt.Type.ToDisplayString(_displayFormat),
            Summary    = docs.Summary,
            Remarks    = docs.Remarks,
        };
    }

    private static MemberDetailsInfo? CreateParameterStandaloneInfo(IParameterSymbol? parameter)
    {
        if (parameter == null) return null;
        string? paramDoc = null;
        if (parameter.ContainingSymbol != null)
        {
            var docs = DocumentationParser.Parse(parameter.ContainingSymbol);
            docs.ParamDocs.TryGetValue(parameter.Name, out paramDoc);
        }
        return new MemberDetailsInfo
        {
            Name       = parameter.Name,
            Kind       = "Parameter",
            Signature  = parameter.ToDisplayString(_displayFormat),
            ReturnType = parameter.Type.ToDisplayString(_displayFormat),
            Summary    = paramDoc ?? string.Empty,
        };
    }

    private static MemberDetailsInfo? CreateLocalInfo(ILocalSymbol? local)
    {
        if (local == null) return null;
        return new MemberDetailsInfo
        {
            Name       = local.Name,
            Kind       = local.IsConst ? "Local Constant" : "Local Variable",
            Signature  = local.ToDisplayString(_displayFormat),
            ReturnType = local.Type.ToDisplayString(_displayFormat),
        };
    }

    private static MemberDetailsInfo? CreateTypeParameterInfo(ITypeParameterSymbol? typeParam)
    {
        if (typeParam == null) return null;
        string? typeParamDoc = null;
        if (typeParam.ContainingSymbol != null)
        {
            var docs = DocumentationParser.Parse(typeParam.ContainingSymbol);
            docs.TypeParamDocs.TryGetValue(typeParam.Name, out typeParamDoc);
        }
        return new MemberDetailsInfo
        {
            Name      = typeParam.Name,
            Kind      = "Type Parameter",
            Signature = typeParam.ToDisplayString(_displayFormat),
            Summary   = typeParamDoc ?? string.Empty,
        };
    }
}
