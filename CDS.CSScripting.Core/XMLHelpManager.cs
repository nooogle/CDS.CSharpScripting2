using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CDS.CSScripting
{
    public static class XMLHelpManager
    {
        public static (DetailedTypeInfo typeInfo, IEnumerable<MethodOverloadInfo> memberInfos) Test(SyntaxTree syntaxTree, SemanticModel semanticModel, int position)
        {
            var root = syntaxTree.GetRoot();
            var token = root.FindToken(position).Parent; // or position - 1 ????

            // Determine if we're dealing with a method invocation or a type identifier.
            var invocationNode = token.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (invocationNode != null)
            {
                // Method invocation: Get method overloads only
                var methodInfos = GetMethodOverloads(invocationNode, semanticModel);
                return (null, methodInfos);
            }

            var identifierNode = token.AncestorsAndSelf().OfType<IdentifierNameSyntax>().FirstOrDefault();
            if (identifierNode != null)
            {
                // Type identifier: Get type information and its members
                var typeSymbol = semanticModel.GetTypeInfo(identifierNode).Type as INamedTypeSymbol;
                if (typeSymbol != null)
                {
                    var typeInfo = GetDetailedTypeInfo(typeSymbol);
                    var memberInfos = GetTypeMembers(typeSymbol);
                    return (typeInfo, memberInfos);
                }
            }

            return (null, Enumerable.Empty<MethodOverloadInfo>());
        }

        private static DetailedTypeInfo GetDetailedTypeInfo(INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol == null) return null;

            // Create and populate DetailedTypeInfo object
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

        private static IEnumerable<MethodOverloadInfo> GetTypeMembers(INamedTypeSymbol typeSymbol)
        {
            var memberInfos = new List<MethodOverloadInfo>();

            foreach (var member in typeSymbol.GetMembers())
            {
                if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
                {
                    memberInfos.Add(CreateMethodOverloadInfo(method));
                }
                else if (member is IPropertySymbol property)
                {
                    memberInfos.Add(new MethodOverloadInfo
                    {
                        Name = property.Name,
                        Signature = property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        ReturnType = property.Type.ToDisplayString(),
                        Summary = GetDocumentationSummary(property)
                    });
                }
            }

            return memberInfos;
        }

        private static IEnumerable<MethodOverloadInfo> GetMethodOverloads(InvocationExpressionSyntax invocationNode, SemanticModel semanticModel)
        {
            var overloadInfos = new List<MethodOverloadInfo>();

            // Get the expression being invoked (e.g., 'System.Diagnostics.Debug.WriteLine')
            var expression = invocationNode.Expression;

            // Get symbol info for the expression
            var symbolInfo = semanticModel.GetSymbolInfo(expression);

            // Get the method symbol (could be null if code is incomplete)
            ISymbol methodSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

            if (methodSymbol is IMethodSymbol method)
            {
                // Get the containing type and all overloads of the method in the containing type
                var methodOverloads = method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>();

                // Extract information for each overload
                foreach (var overload in methodOverloads)
                {
                    overloadInfos.Add(CreateMethodOverloadInfo(overload));
                }
            }

            return overloadInfos;
        }

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

            var xmlDocumentation = method.GetDocumentationCommentXml();
            if (!string.IsNullOrEmpty(xmlDocumentation))
            {
                var xmlDoc = XDocument.Parse(xmlDocumentation);
                overloadInfo.Remarks = xmlDoc.Root.Element("remarks")?.Value.Trim();

                foreach (var param in overloadInfo.Parameters)
                {
                    var paramDoc = xmlDoc.Root.Elements("param")
                        .FirstOrDefault(p => p.Attribute("name")?.Value == param.Name);
                    if (paramDoc != null)
                    {
                        param.Documentation = paramDoc.Value.Trim();
                    }
                }
            }
            return overloadInfo;
        }

        private static string GetDocumentationSummary(ISymbol symbol)
        {
            var xmlDocumentation = symbol.GetDocumentationCommentXml();
            if (string.IsNullOrEmpty(xmlDocumentation)) return null;
            var xmlDoc = XDocument.Parse(xmlDocumentation);
            return xmlDoc.Root.Element("summary")?.Value.Trim();
        }

        private static string GetDocumentationRemarks(ISymbol symbol)
        {
            var xmlDocumentation = symbol.GetDocumentationCommentXml();
            if (string.IsNullOrEmpty(xmlDocumentation)) return null;
            var xmlDoc = XDocument.Parse(xmlDocumentation);
            return xmlDoc.Root.Element("remarks")?.Value.Trim();
        }
    }

    public class DetailedTypeInfo
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Accessibility { get; set; }
        public string BaseType { get; set; }
        public List<string> Interfaces { get; set; } = new List<string>();
        public string Summary { get; set; }
        public string Remarks { get; set; }
    }

    public class MethodOverloadInfo
    {
        public string Name { get; set; }
        public string Signature { get; set; }
        public string ReturnType { get; set; }
        public string Summary { get; set; }
        public string Remarks { get; set; }
        public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Documentation { get; set; }
    }
}
