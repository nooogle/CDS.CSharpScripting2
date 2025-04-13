using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml; // For XmlException

namespace CDS.CSharpScriptUtils.APIInfo;

/// <summary>
/// Service for extracting API information (including XML documentation)
/// from code symbols at a specific position. Geared towards hover tooltips.
/// </summary>
public static class APIInfoService
{
    // Define preferred display format using the constructor for clarity
    private static readonly SymbolDisplayFormat MinimallyQualifiedFormat = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly, // Correctly set via constructor parameter
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
                                                                                                                                                                                                                      // Add other options like localOptions, kindOptions etc. if needed
    );


    /// <summary>
    /// Gets API information at the specified position in the syntax tree.
    /// Designed primarily for hover information.
    /// </summary>
    /// <param name="syntaxTree">The syntax tree to analyze.</param>
    /// <param name="semanticModel">The semantic model for the tree.</param>
    /// <param name="position">The cursor position within the code.</param>
    /// <returns>API information result, or an empty result if no symbol is found.</returns>
    public static APIInfoResult Get(
        SyntaxTree syntaxTree,
        SemanticModel semanticModel,
        int position)
    {
        try
        {
            var root = syntaxTree.GetRoot();
            // Find token; allow finding inside trivia to handle positions within comments/whitespace
            var token = root.FindToken(position, findInsideTrivia: true);

            // --- Correction for IsTrivia ---
            // If the found token is actually trivia, get the token *before* the trivia.
            // This handles cases where the cursor is in whitespace or comments.
            // Need to check RawKind as Kind() throws on trivia tokens.
            if (SyntaxFacts.IsTrivia((SyntaxKind)token.RawKind))
            {
                // If position is within leading trivia, get previous token.
                // If within trailing trivia, the current token is likely the relevant one for context,
                // but GetPreviousToken might still be desired depending on exact UI behavior.
                // Let's try GetPreviousToken if we are in trivia.
                token = token.GetPreviousToken();
            }

            // Handle edge case: If position is 0 or we moved back to the beginning of the file
            if (!token.Span.IntersectsWith(position) && position == 0)
            {
                token = root.GetFirstToken(); // Try getting the very first token
            }

            // If token is still invalid or EOF after adjustments, exit.
            if (token == default || token.IsKind(SyntaxKind.None) || (token.IsKind(SyntaxKind.EndOfFileToken) && token.FullSpan.Length == 0))
            {
                return new APIInfoResult(null, null);
            }
            // --- End Correction ---


            var node = token.Parent; // Get parent node *after* potentially adjusting the token
            if (node == null) return new APIInfoResult(null, null);


            // 1. Check for Method Invocation (Signature Help Context potentially)
            //    If inside argument list, show overloads. If on method name, show specific method (handled below).
            var invocation = node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (invocation != null && invocation.ArgumentList.FullSpan.Contains(position)) // Use FullSpan for argument list
            {
                // Note: SemanticModel queries might fail if syntax tree has errors.
                var methodGroup = semanticModel.GetSymbolInfo(invocation.Expression);
                var methodSymbol = methodGroup.Symbol as IMethodSymbol;
                var overloads = FindOverloads(semanticModel, invocation.Expression);

                if (overloads.Any())
                {
                    var overloadDetails = overloads.Select(m => CreateInfoForSymbol(m)).Where(info => info != null).ToList();
                    var firstOverload = overloads.FirstOrDefault();
                    var typeInfo = firstOverload != null ? GetDetailedTypeInfo(firstOverload.ContainingType) : null;
                    return new APIInfoResult(typeInfo, overloadDetails);
                }
                // Fallback if overload resolution fails but we are in invocation
                if (methodSymbol != null)
                {
                    var memberDetail = CreateInfoForSymbol(methodSymbol);
                    var typeInfo = GetDetailedTypeInfo(methodSymbol.ContainingType);
                    return new APIInfoResult(typeInfo, memberDetail != null ? new[] { memberDetail } : null);
                }
            }

            // 2. Check for Member Access (e.g., instance.Member) - Focus on the Name part
            var memberAccess = node.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            // Ensure the cursor is on the Name part of the access (e.g., 'WriteLine' in 'Console.WriteLine')
            if (memberAccess != null && memberAccess.Name.Span.Contains(position))
            {
                var symbolInfo = semanticModel.GetSymbolInfo(memberAccess); // Use memberAccess here, not just Name

                IEnumerable<ISymbol> symbols =
                    symbolInfo.Symbol == null ?
                    symbolInfo.CandidateSymbols :
                    new[] { symbolInfo.Symbol };

                if (symbols.Any())
                {
                    var typeInfo = GetDetailedTypeInfo(symbols.First().ContainingType);
                    List<MemberDetailsInfo> membersDetails = [];

                    foreach (var symbol in symbols)
                    {
                        var memberDetail = CreateInfoForSymbol(symbol);
                        if (memberDetail != null)
                        {
                            membersDetails.Add(memberDetail);
                        }
                    }

                    return new APIInfoResult(typeInfo, membersDetails.Count > 0 ? membersDetails : null);
                }

                //var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
                //if (symbol != null)
                //{
                //    var memberDetail = CreateInfoForSymbol(symbol);
                //    var typeInfo = GetDetailedTypeInfo(symbol.ContainingType); // Type where member is defined
                //    return new APIInfoResult(typeInfo, memberDetail != null ? new[] { memberDetail } : null);
                //}
            }

            // 3. Check for Identifier (Type name, variable, parameter, method group name)
            var identifier = node.AncestorsAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault();
            // Also check for predefined types like 'int', 'string' etc.
            var predefinedTypeSyntax = node.AncestorsAndSelf().OfType<PredefinedTypeSyntax>().FirstOrDefault();

            // Prefer identifier if found, otherwise use predefined type if cursor is on it
            SyntaxNode symbolNode = null;
            if (identifier != null && identifier.Span.Contains(position))
            {
                symbolNode = identifier;
            }
            else if (predefinedTypeSyntax != null && predefinedTypeSyntax.Span.Contains(position))
            {
                symbolNode = predefinedTypeSyntax;
            }
            // Handle the case where the original token itself is the identifier/keyword
            else if (node is IdentifierNameSyntax idNode && idNode.Span.Contains(position))
            {
                symbolNode = idNode;
            }
            else if (node is PredefinedTypeSyntax pdNode && pdNode.Span.Contains(position))
            {
                symbolNode = pdNode;
            }
            // If the cursor is directly on an identifier *token*
            else if (token.IsKind(SyntaxKind.IdentifierToken) && token.Span.Contains(position))
            {
                symbolNode = node; // The parent node (e.g., IdentifierNameSyntax) is likely what we need
            }


            if (symbolNode != null)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(symbolNode);
                var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                if (symbol is INamedTypeSymbol typeSymbol)
                {
                    // Hovering directly on a type name (e.g., 'Console', 'string')
                    var typeInfo = GetDetailedTypeInfo(typeSymbol);
                    // Getting members might be too verbose for hover; maybe return just type info?
                    return new APIInfoResult(typeInfo, null); // Return only Type info for direct type hover
                }
                else if (symbol is IMethodSymbol methodSymbol)
                {
                    // Hovering on a method name itself (not invocation or member access)
                    // Show overloads for the method group.
                    var overloads = methodSymbol.ContainingType.GetMembers(methodSymbol.Name).OfType<IMethodSymbol>();
                    var methodInfos = overloads.Select(CreateInfoForSymbol).Where(info => info != null).ToList();
                    var typeInfo = GetDetailedTypeInfo(methodSymbol.ContainingType);
                    return new APIInfoResult(typeInfo, methodInfos);
                }
                else if (symbol != null) // Variable, Parameter, Field, Property symbol directly
                {
                    var memberDetail = CreateInfoForSymbol(symbol); // Info for the variable/param itself
                    var variableType = GetSymbolType(symbol);      // Type of the variable/param
                    var typeInfo = GetDetailedTypeInfo(variableType);

                    // For hover on a variable/parameter, often the variable's own info (if any)
                    // and its type's info are most useful. Returning members of the type might be too much.
                    return new APIInfoResult(typeInfo, memberDetail != null ? new[] { memberDetail } : null);
                }
            }

            // 4. Check for Object Creation (e.g., new ClassName(...)) - Show constructors
            var objectCreation = node.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
            // Check if cursor is on 'new', the Type, or within the argument list '()'
            if (objectCreation != null &&
               (objectCreation.NewKeyword.Span.Contains(position) ||
                objectCreation.Type.Span.Contains(position) ||
                (objectCreation.ArgumentList != null && objectCreation.ArgumentList.FullSpan.Contains(position))
               ))
            {
                ISymbol symbol = null;
                // If on the Type part, get type info first
                if (objectCreation.Type.Span.Contains(position))
                {
                    symbol = semanticModel.GetSymbolInfo(objectCreation.Type).Symbol;
                }
                else
                {
                    // Otherwise (on 'new' or in args), get symbol for the whole creation expression (usually constructor)
                    symbol = semanticModel.GetSymbolInfo(objectCreation).Symbol;
                }

                var methodSymbol = symbol as IMethodSymbol; // Should be a constructor
                INamedTypeSymbol targetType = null;

                if (methodSymbol != null)
                {
                    targetType = methodSymbol.ContainingType;
                }
                else if (symbol is INamedTypeSymbol typeSymbolForNew)
                {
                    // Could be hovering directly on the Type name within the new expression
                    targetType = typeSymbolForNew;
                }
                else
                {
                    // Fallback: try getting type info from the TypeSyntax part
                    targetType = semanticModel.GetTypeInfo(objectCreation.Type).Type as INamedTypeSymbol;
                }


                if (targetType != null)
                {
                    var constructors = targetType.GetMembers(".ctor").OfType<IMethodSymbol>()
                                                .Where(m => !m.IsImplicitlyDeclared); // Exclude implicit default constructor if desired
                    var ctorInfos = constructors.Select(CreateInfoForSymbol).Where(info => info != null).ToList();
                    var typeInfo = GetDetailedTypeInfo(targetType);
                    return new APIInfoResult(typeInfo, ctorInfos);
                }
            }

            // Add checks for other SyntaxKinds as needed (e.g., PropertyDeclarationSyntax, MethodDeclarationSyntax)

            return new APIInfoResult(null, null); // Default empty result
        }
        catch (Exception ex) // Catch specific exceptions if possible
        {
            // Log the exception (optional but recommended)
            System.Diagnostics.Debug.WriteLine($"Error getting API info at position {position}: {ex}");
            // Return empty results on error rather than crashing
            return new APIInfoResult(null, null);
        }
    }


    /// <summary>
    /// Attempts to find all relevant method overloads for a given expression (method group).
    /// </summary>
    private static IEnumerable<IMethodSymbol> FindOverloads(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        if (expression == null) return Enumerable.Empty<IMethodSymbol>();

        var symbolInfo = semanticModel.GetSymbolInfo(expression);
        var symbol = symbolInfo.Symbol;

        // Handle method groups directly (common when overload resolution fails or isn't complete)
        if (symbol == null && symbolInfo.CandidateSymbols.Any() &&
           (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure || symbolInfo.CandidateReason == CandidateReason.MemberGroup || symbolInfo.CandidateReason == CandidateReason.None)) // None might mean incomplete code
        {
            return symbolInfo.CandidateSymbols.OfType<IMethodSymbol>();
        }

        // Handle single resolved method (still might have overloads in the type)
        if (symbol is IMethodSymbol methodSymbol)
        {
            // Look for regular methods and extension methods
            var containingTypeMembers = methodSymbol.ContainingType?.GetMembers(methodSymbol.Name).OfType<IMethodSymbol>()
                                       ?? Enumerable.Empty<IMethodSymbol>();

            // Extension method lookup might be needed depending on the expression type
            // Example: if expression is just an identifier, lookup extensions in scope
            // if (expression is IdentifierNameSyntax) {
            //    var extensionMethods = semanticModel.LookupExtensionMethods(expression.SpanStart, null, methodSymbol.Name);
            //    return containingTypeMembers.Concat(extensionMethods).Distinct();
            // }

            return containingTypeMembers;
        }

        return Enumerable.Empty<IMethodSymbol>();
    }


    /// <summary>
    /// Creates the appropriate member details info based on the symbol kind.
    /// </summary>
    private static MemberDetailsInfo CreateInfoForSymbol(ISymbol symbol)
    {
        if (symbol == null) return null;

        // Avoid showing details for implicitly declared symbols like backing fields unless desired
        if (symbol.IsImplicitlyDeclared && !(symbol is IMethodSymbol m && m.MethodKind == MethodKind.Constructor)) // Allow implicit constructors maybe? Adjust as needed.
        {
            // Example: Don't show backing field for auto-property directly
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
                // Usually handled via method info, but can create standalone if needed
                return CreateParameterStandaloneInfo(symbol as IParameterSymbol);
            case SymbolKind.Local:
                // Locals typically don't have XML docs, but basic info can be shown
                return CreateLocalInfo(symbol as ILocalSymbol);
            case SymbolKind.TypeParameter:
                return CreateTypeParameterInfo(symbol as ITypeParameterSymbol);
            // Add other kinds as needed
            default:
                // Provide basic info even for unhandled kinds?
                // return new MemberDetailsInfo { Name = symbol.Name, Kind = symbol.Kind.ToString(), Signature = symbol.ToDisplayString(MinimallyQualifiedFormat) };
                return null;
        }
    }

    /// <summary>
    /// Gets the type symbol associated with various other symbol kinds.
    /// </summary>
    private static ITypeSymbol GetSymbolType(ISymbol symbol) // Changed return type to ITypeSymbol for flexibility
    {
        return symbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            IEventSymbol evt => evt.Type,
            // Add others if necessary (e.g., IMethodSymbol for return type - though less common use case here)
            _ => null,
        };
    }

    /// <summary>
    /// Gets detailed information about a type symbol.
    /// </summary>
    private static DetailedTypeInfo GetDetailedTypeInfo(ITypeSymbol typeSymbol) // Changed parameter to ITypeSymbol
    {
        // Handle non-named types like arrays or pointers if necessary
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            // Could create basic info for array types
            return new DetailedTypeInfo { Name = typeSymbol.ToDisplayString(MinimallyQualifiedFormat), TypeKind = "Array", Namespace = "System" };
        }
        if (typeSymbol is IPointerTypeSymbol pointerType)
        {
            return new DetailedTypeInfo { Name = typeSymbol.ToDisplayString(MinimallyQualifiedFormat), TypeKind = "Pointer" };
        }

        // Proceed with INamedTypeSymbol
        var namedTypeSymbol = typeSymbol as INamedTypeSymbol;
        if (namedTypeSymbol == null || namedTypeSymbol.Kind == SymbolKind.ErrorType)
        {
            // Attempt to display *something* even for error types if available
            return typeSymbol != null ? new DetailedTypeInfo { Name = typeSymbol.ToDisplayString(MinimallyQualifiedFormat), TypeKind = typeSymbol.TypeKind.ToString() } : null;
        }


        var docs = ParseDocumentation(namedTypeSymbol);
        return new DetailedTypeInfo
        {
            Name = namedTypeSymbol.ToDisplayString(MinimallyQualifiedFormat), // Use format for consistency
            Namespace = namedTypeSymbol.ContainingNamespace?.ToDisplayString() ?? "[Global Namespace]",
            Accessibility = namedTypeSymbol.DeclaredAccessibility.ToString(),
            TypeKind = namedTypeSymbol.TypeKind.ToString(),
            BaseType = namedTypeSymbol.BaseType?.ToDisplayString(MinimallyQualifiedFormat),
            Interfaces = namedTypeSymbol.Interfaces.Select(i => i.ToDisplayString(MinimallyQualifiedFormat)).ToList(),
            Summary = docs.Summary,
            Remarks = docs.Remarks
        };
    }


    // --- Specific Symbol Info Creators ---

    private static MemberDetailsInfo CreateMethodInfo(IMethodSymbol method)
    {
        if (method == null) return null; // Removed implicit check, handled in CreateInfoForSymbol

        var docs = ParseDocumentation(method);
        var info = new MemberDetailsInfo
        {
            Name = method.MethodKind == MethodKind.Constructor ? method.ContainingType.Name : method.Name, // Use type name for constructor
            Kind = method.MethodKind.ToString(), // Constructor, Ordinary, ExtensionMethod, etc.
            Signature = method.ToDisplayString(MinimallyQualifiedFormat),
            // ReturnType applicable unless it's a constructor
            ReturnType = method.MethodKind == MethodKind.Constructor ? null : method.ReturnType.ToDisplayString(MinimallyQualifiedFormat),
            Summary = docs.Summary,
            Remarks = docs.Remarks,
            Parameters = method.Parameters.Select(p => new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(MinimallyQualifiedFormat),
                Documentation = docs.ParamDocs.TryGetValue(p.Name, out var doc) ? doc : null
            }).ToList()
        };
        return info;
    }

    private static MemberDetailsInfo CreatePropertyInfo(IPropertySymbol property)
    {
        if (property == null) return null;

        var docs = ParseDocumentation(property);
        var info = new MemberDetailsInfo
        {
            Name = property.Name,
            Kind = property.IsIndexer ? "Indexer" : "Property",
            Signature = property.ToDisplayString(MinimallyQualifiedFormat),
            ReturnType = property.Type.ToDisplayString(MinimallyQualifiedFormat),
            Summary = docs.Summary,
            Remarks = docs.Remarks
        };
        if (property.IsIndexer)
        {
            info.Parameters = property.Parameters.Select(p => new ParameterInfo
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(MinimallyQualifiedFormat),
                Documentation = docs.ParamDocs.TryGetValue(p.Name, out var doc) ? doc : null
            }).ToList();
        }
        return info;
    }

    private static MemberDetailsInfo CreateFieldInfo(IFieldSymbol field)
    {
        if (field == null) return null;

        var docs = ParseDocumentation(field);
        var info = new MemberDetailsInfo
        {
            Name = field.Name,
            Kind = field.IsConst ? "Constant" : (field.IsReadOnly ? "Readonly Field" : "Field"),
            Signature = field.ToDisplayString(MinimallyQualifiedFormat),
            ReturnType = field.Type.ToDisplayString(MinimallyQualifiedFormat),
            Summary = docs.Summary,
            Remarks = docs.Remarks
        };
        if (field.HasConstantValue && field.ConstantValue != null)
        {
            info.Signature += $" = {field.ConstantValue}"; // Append constant value if available
                                                           // Or add a separate property for ConstantValue
        }
        return info;
    }

    private static MemberDetailsInfo CreateEventInfo(IEventSymbol evt)
    {
        if (evt == null) return null;

        var docs = ParseDocumentation(evt);
        return new MemberDetailsInfo
        {
            Name = evt.Name,
            Kind = "Event",
            Signature = evt.ToDisplayString(MinimallyQualifiedFormat),
            ReturnType = evt.Type.ToDisplayString(MinimallyQualifiedFormat), // Event handler type
            Summary = docs.Summary,
            Remarks = docs.Remarks
        };
    }

    private static MemberDetailsInfo CreateParameterStandaloneInfo(IParameterSymbol parameter)
    {
        if (parameter == null) return null;
        // Documentation for parameter usually comes from its owning method/indexer
        string paramDoc = null;
        if (parameter.ContainingSymbol != null)
        {
            var docs = ParseDocumentation(parameter.ContainingSymbol);
            paramDoc = docs.ParamDocs.TryGetValue(parameter.Name, out var doc) ? doc : null;
        }

        return new MemberDetailsInfo
        {
            Name = parameter.Name,
            Kind = "Parameter",
            Signature = parameter.ToDisplayString(MinimallyQualifiedFormat),
            ReturnType = parameter.Type.ToDisplayString(MinimallyQualifiedFormat),
            Summary = paramDoc // Use extracted doc here
        };
    }

    private static MemberDetailsInfo CreateLocalInfo(ILocalSymbol local)
    {
        if (local == null) return null;
        // Locals don't have XML documentation
        return new MemberDetailsInfo
        {
            Name = local.Name,
            Kind = local.IsConst ? "Local Constant" : "Local Variable",
            Signature = local.ToDisplayString(MinimallyQualifiedFormat), // May just show type and name
            ReturnType = local.Type.ToDisplayString(MinimallyQualifiedFormat)
            // Could add constant value if local.HasConstantValue == true
        };
    }

    private static MemberDetailsInfo CreateTypeParameterInfo(ITypeParameterSymbol typeParam)
    {
        if (typeParam == null) return null;
        // Documentation for type parameters usually comes from the containing type/method
        string typeParamDoc = null;
        if (typeParam.ContainingSymbol != null)
        {
            var docs = ParseDocumentation(typeParam.ContainingSymbol);
            // XML Doc tag is <typeparam name="...">
            // Need to adjust ParseDocumentation to extract this if needed, or do it here.
            // For now, leave summary null.
        }

        return new MemberDetailsInfo
        {
            Name = typeParam.Name,
            Kind = "Type Parameter",
            Signature = typeParam.ToDisplayString(MinimallyQualifiedFormat), // Shows constraints
            Summary = typeParamDoc
        };
    }


    // --- XML Documentation Parsing Helper ---

    /// <summary>
    /// Parses the XML documentation comment for a symbol efficiently.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>A tuple containing Summary, Remarks, and Parameter Documentation.</returns>
    private static (string Summary, string Remarks, Dictionary<string, string> ParamDocs) ParseDocumentation(ISymbol symbol)
    {
        if (symbol == null) return (null, null, new Dictionary<string, string>());

        // Use CancellationToken.None if not in an async context where cancellation is relevant
        string xmlDocs = symbol.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: System.Threading.CancellationToken.None);
        if (string.IsNullOrEmpty(xmlDocs))
        {
            return (null, null, new Dictionary<string, string>());
        }

        try
        {
            // Using XmlReader for potentially better performance and lower memory usage with large doc comments
            using (var reader = new System.IO.StringReader(xmlDocs))
            {
                var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment }; // Handle potential lack of single root
                using (var xmlReader = XmlReader.Create(reader, settings))
                {
                    string summary = null;
                    string remarks = null;
                    var paramDocs = new Dictionary<string, string>();
                    string currentParamName = null;

                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            switch (xmlReader.Name.ToLowerInvariant())
                            {
                                case "summary":
                                    summary = xmlReader.ReadInnerXml().Trim(); // ReadInnerXml preserves inner tags like <see/> if needed
                                    break;
                                case "remarks":
                                    remarks = xmlReader.ReadInnerXml().Trim();
                                    break;
                                case "param":
                                    currentParamName = xmlReader.GetAttribute("name");
                                    if (currentParamName != null)
                                    {
                                        // ReadInnerXml preserves inner tags, Value gets only text content
                                        string paramValue = xmlReader.ReadInnerXml().Trim();
                                        if (!paramDocs.ContainsKey(currentParamName)) // Handle potential duplicates, keep first
                                        {
                                            paramDocs.Add(currentParamName, paramValue);
                                        }
                                    }
                                    currentParamName = null; // Reset after reading
                                    break;
                                    // Add cases for typeparam, returns, exception etc. if needed
                            }
                        }
                    }
                    return (summary, remarks, paramDocs);
                }
            }
        }
        catch (XmlException ex)
        {
            System.Diagnostics.Debug.WriteLine($"XML Documentation parse error for {symbol.Name}: {ex.Message}");
            return (null, null, new Dictionary<string, string>()); // Return empty/null on error
        }
        catch (Exception ex) // Catch other potential exceptions during parsing
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error parsing documentation for {symbol.Name}: {ex}");
            return (null, null, new Dictionary<string, string>());
        }
    }
}