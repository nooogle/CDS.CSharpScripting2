using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;

namespace CDS.CSScripting2.APIInfo;

/// <summary>
/// Service for extracting API information from code at a specific position
/// </summary>
public static class APIInfoService
{
    /// <summary>
    /// Gets API information at the specified position in the syntax tree
    /// </summary>
    /// <param name="syntaxTree">The syntax tree to analyze</param>
    /// <param name="semanticModel">The semantic model</param>
    /// <param name="position">The position to get information for</param>
    /// <returns>API information at the specified position</returns>
    public static APIInfo Get(
        SyntaxTree syntaxTree,
        SemanticModel semanticModel,
        int position)
    {
        try
        {
            var root = syntaxTree.GetRoot();
            var token = root.FindToken(position);

            // Get the node at the current position
            var node = token.Parent;
            if (node == null)
            {
                return new APIInfo(null, Enumerable.Empty<MethodOverloadInfo>());
            }

            // Check for method invocation
            var apiInfo = TryGetMethodInvocationInfo(node, semanticModel);
            if (apiInfo != null)
            {
                return apiInfo;
            }

            // Check for member access
            apiInfo = TryGetMemberAccessInfo(node, semanticModel);
            if (apiInfo != null)
            {
                return apiInfo;
            }

            // Check for type identifier
            apiInfo = TryGetTypeIdentifierInfo(node, semanticModel);
            if (apiInfo != null)
            {
                return apiInfo;
            }

            return new APIInfo(null, Enumerable.Empty<MethodOverloadInfo>());
        }
        catch (Exception)
        {
            // Return empty results on error rather than crashing
            return new APIInfo(null, Enumerable.Empty<MethodOverloadInfo>());
        }
    }

    /// <summary>
    /// Attempts to get API information for a method invocation
    /// </summary>
    private static APIInfo TryGetMethodInvocationInfo(SyntaxNode node, SemanticModel semanticModel)
    {
        var invocationNode = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        if (invocationNode == null)
        {
            return null;
        }

        var methodInfos = GetMethodOverloads(invocationNode, semanticModel);
        return new APIInfo(null, methodInfos);
    }

    /// <summary>
    /// Attempts to get API information for a member access expression
    /// </summary>
    private static APIInfo TryGetMemberAccessInfo(SyntaxNode node, SemanticModel semanticModel)
    {
        var memberAccess = node.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
        if (memberAccess == null)
        {
            return null;
        }

        // Handle dot notation - get the symbol being accessed
        var memberName = memberAccess.Name;
        if (memberName == null)
        {
            return null;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(memberName);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

        if (symbol == null)
        {
            return null;
        }

        if (symbol is IMethodSymbol method)
        {
            // Get all overloads of this method
            var methodOverloads = method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>().ToList();
            var methodInfos = methodOverloads.Select(CreateMethodOverloadInfo).ToList();
            return new APIInfo(null, methodInfos);
        }
        else if (symbol is IPropertySymbol property)
        {
            var propertyInfo = CreatePropertyInfo(property);
            return new APIInfo(null, new[] { propertyInfo });
        }
        else if (symbol is IFieldSymbol field)
        {
            var fieldInfo = CreateFieldInfo(field);
            return new APIInfo(null, new[] { fieldInfo });
        }
        else if (symbol is INamedTypeSymbol typeSymbol)
        {
            var typeInfo = GetDetailedTypeInfo(typeSymbol);
            var memberInfos = GetTypeMembers(typeSymbol);
            return new APIInfo(typeInfo, memberInfos);
        }

        return null;
    }

    /// <summary>
    /// Attempts to get API information for a type identifier
    /// </summary>
    private static APIInfo TryGetTypeIdentifierInfo(SyntaxNode node, SemanticModel semanticModel)
    {
        var identifierNode = node.AncestorsAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault();
        if (identifierNode == null)
        {
            return null;
        }

        // Check if it's a type
        var typeInfo = semanticModel.GetTypeInfo(identifierNode);
        var typeSymbol = typeInfo.Type as INamedTypeSymbol;
        if (typeSymbol != null)
        {
            var detailedTypeInfo = GetDetailedTypeInfo(typeSymbol);
            var memberInfos = GetTypeMembers(typeSymbol);
            return new APIInfo(detailedTypeInfo, memberInfos);
        }

        // Check if it's a variable/parameter
        var symbolInfo = semanticModel.GetSymbolInfo(identifierNode);
        var symbol = symbolInfo.Symbol;
        if (symbol != null && symbol.Kind != SymbolKind.Method)
        {
            var variableType = GetSymbolType(symbol);
            if (variableType != null)
            {
                var detailedTypeInfo = GetDetailedTypeInfo(variableType);
                var memberInfos = GetTypeMembers(variableType);
                return new APIInfo(detailedTypeInfo, memberInfos);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the type of a symbol
    /// </summary>
    private static INamedTypeSymbol GetSymbolType(ISymbol symbol)
    {
        switch (symbol)
        {
            case ILocalSymbol local:
                return local.Type as INamedTypeSymbol;
            case IParameterSymbol parameter:
                return parameter.Type as INamedTypeSymbol;
            case IFieldSymbol field:
                return field.Type as INamedTypeSymbol;
            case IPropertySymbol property:
                return property.Type as INamedTypeSymbol;
            default:
                return null;
        }
    }

    /// <summary>
    /// Gets detailed information about a type
    /// </summary>
    private static DetailedTypeInfo GetDetailedTypeInfo(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
        {
            return null;
        }

        return new DetailedTypeInfo
        {
            Name = typeSymbol.Name,
            Namespace = typeSymbol.ContainingNamespace.ToDisplayString(),
            Accessibility = typeSymbol.DeclaredAccessibility.ToString(),
            BaseType = typeSymbol.BaseType?.ToDisplayString(),
            Interfaces = typeSymbol.Interfaces.Select(i => i.ToDisplayString()).ToList(),
            Summary = GetDocumentationSummary(typeSymbol),
            Remarks = GetDocumentationRemarks(typeSymbol)
        };
    }

    /// <summary>
    /// Gets information about all members of a type
    /// </summary>
    private static IEnumerable<MethodOverloadInfo> GetTypeMembers(INamedTypeSymbol typeSymbol)
    {
        var memberInfos = new List<MethodOverloadInfo>();

        foreach (var member in typeSymbol.GetMembers())
        {
            try
            {
                switch (member)
                {
                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary ||
                                                   method.MethodKind == MethodKind.Constructor:
                        memberInfos.Add(CreateMethodOverloadInfo(method));
                        break;

                    case IPropertySymbol property:
                        memberInfos.Add(CreatePropertyInfo(property));
                        break;

                    case IFieldSymbol field:
                        memberInfos.Add(CreateFieldInfo(field));
                        break;

                    case IEventSymbol eventSymbol:
                        memberInfos.Add(CreateEventInfo(eventSymbol));
                        break;
                }
            }
            catch (Exception)
            {
                // Skip this member if there's an error processing it
                continue;
            }
        }

        return memberInfos;
    }

    /// <summary>
    /// Gets information about all overloads of a method
    /// </summary>
    private static IEnumerable<MethodOverloadInfo> GetMethodOverloads(InvocationExpressionSyntax invocationNode, SemanticModel semanticModel)
    {
        var overloadInfos = new List<MethodOverloadInfo>();

        try
        {
            var expression = invocationNode.Expression;
            var symbolInfo = semanticModel.GetSymbolInfo(expression);

            // Try to get the method symbol
            ISymbol methodSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

            if (methodSymbol is IMethodSymbol method)
            {
                // Handle extension methods
                var containingType = method.IsExtensionMethod ?
                    method.Parameters[0].Type as INamedTypeSymbol :
                    method.ContainingType;

                if (containingType != null)
                {
                    // Get all overloads of this method
                    var methodOverloads = containingType.GetMembers(method.Name).OfType<IMethodSymbol>();

                    // For extension methods, only include other extension methods
                    if (method.IsExtensionMethod)
                    {
                        methodOverloads = methodOverloads.Where(m => m.IsExtensionMethod);
                    }

                    // Extract information for each overload
                    foreach (var overload in methodOverloads)
                    {
                        overloadInfos.Add(CreateMethodOverloadInfo(overload));
                    }
                }
            }
        }
        catch (Exception)
        {
            // Return empty list on error
            return Enumerable.Empty<MethodOverloadInfo>();
        }

        return overloadInfos;
    }

    /// <summary>
    /// Creates information about a method
    /// </summary>
    private static MethodOverloadInfo CreateMethodOverloadInfo(IMethodSymbol method)
    {
        var overloadInfo = new MethodOverloadInfo
        {
            Name = method.Name,
            Signature = method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            ReturnType = method.ReturnType.ToDisplayString(),
            Parameters = method.Parameters.Select(p => new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString()
            }).ToList(),
            Summary = GetDocumentationSummary(method)
        };

        try
        {
            var xmlDocumentation = method.GetDocumentationCommentXml();
            if (!string.IsNullOrEmpty(xmlDocumentation) && xmlDocumentation != "<!-- No documentation found. -->")
            {
                var xmlDoc = XDocument.Parse(xmlDocumentation);
                overloadInfo.Remarks = xmlDoc.Root?.Element("remarks")?.Value.Trim();

                foreach (var param in overloadInfo.Parameters)
                {
                    var paramDoc = xmlDoc.Root?.Elements("param")
                        .FirstOrDefault(p => p.Attribute("name")?.Value == param.Name);
                    if (paramDoc != null)
                    {
                        param.Documentation = paramDoc.Value.Trim();
                    }
                }
            }
        }
        catch (Exception)
        {
            // Ignore XML parsing errors
        }

        return overloadInfo;
    }

    /// <summary>
    /// Creates information about a property
    /// </summary>
    private static MethodOverloadInfo CreatePropertyInfo(IPropertySymbol property)
    {
        return new MethodOverloadInfo
        {
            Name = property.Name,
            Signature = property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            ReturnType = property.Type.ToDisplayString(),
            Summary = GetDocumentationSummary(property),
            Remarks = GetDocumentationRemarks(property)
        };
    }

    /// <summary>
    /// Creates information about a field
    /// </summary>
    private static MethodOverloadInfo CreateFieldInfo(IFieldSymbol field)
    {
        var fieldInfo = new MethodOverloadInfo
        {
            Name = field.Name,
            Signature = field.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            ReturnType = field.Type.ToDisplayString(),
            Summary = GetDocumentationSummary(field),
            Remarks = GetDocumentationRemarks(field)
        };

        // Add additional processing for XML documentation
        try
        {
            var xmlDocumentation = field.GetDocumentationCommentXml();
            if (!string.IsNullOrEmpty(xmlDocumentation) && xmlDocumentation != "<!-- No documentation found. -->")
            {
                var xmlDoc = XDocument.Parse(xmlDocumentation);
                fieldInfo.Remarks = xmlDoc.Root?.Element("remarks")?.Value.Trim();

                // For constants and well-known values, there might be additional documentation
                var valueDoc = xmlDoc.Root?.Element("value");
                if (valueDoc != null)
                {
                    // If there's a value tag, include it in the Summary with the existing summary
                    var valueText = valueDoc.Value.Trim();
                    if (!string.IsNullOrEmpty(valueText))
                    {
                        fieldInfo.Summary = string.IsNullOrEmpty(fieldInfo.Summary)
                            ? valueText
                            : $"{fieldInfo.Summary}\n\nValue: {valueText}";
                    }
                }
            }
        }
        catch (Exception)
        {
            // Ignore XML parsing errors
        }

        return fieldInfo;
    }

    /// <summary>
    /// Creates information about an event
    /// </summary>
    private static MethodOverloadInfo CreateEventInfo(IEventSymbol eventSymbol)
    {
        return new MethodOverloadInfo
        {
            Name = eventSymbol.Name,
            Signature = eventSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            ReturnType = eventSymbol.Type.ToDisplayString(),
            Summary = GetDocumentationSummary(eventSymbol),
            Remarks = GetDocumentationRemarks(eventSymbol)
        };
    }

    /// <summary>
    /// Gets the summary documentation for a symbol
    /// </summary>
    private static string GetDocumentationSummary(ISymbol symbol)
    {
        try
        {
            var xmlDocumentation = symbol.GetDocumentationCommentXml();
            if (string.IsNullOrEmpty(xmlDocumentation) || xmlDocumentation == "<!-- No documentation found. -->")
            {
                return null;
            }

            var xmlDoc = XDocument.Parse(xmlDocumentation);
            return xmlDoc.Root?.Element("summary")?.Value.Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the remarks documentation for a symbol
    /// </summary>
    private static string GetDocumentationRemarks(ISymbol symbol)
    {
        try
        {
            var xmlDocumentation = symbol.GetDocumentationCommentXml();
            if (string.IsNullOrEmpty(xmlDocumentation) || xmlDocumentation == "<!-- No documentation found. -->")
            {
                return null;
            }

            var xmlDoc = XDocument.Parse(xmlDocumentation);
            return xmlDoc.Root?.Element("remarks")?.Value.Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
