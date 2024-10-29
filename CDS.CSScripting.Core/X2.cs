using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace CDS.CSScripting
{
    public class X2
    {
        public static IEnumerable<MethodOverloadInfo> Test(SyntaxTree syntaxTree, SemanticModel semanticModel, int position)
        {
            var root = syntaxTree.GetRoot();

            var invocationNode = root.FindToken(position - 1).Parent
                .AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (invocationNode != null)
            {
                return GetMethodOverloads(invocationNode, semanticModel);
            }

            return Enumerable.Empty<MethodOverloadInfo>();
        }


        private static IEnumerable<MethodOverloadInfo> GetMethodOverloads(
            InvocationExpressionSyntax invocationNode,
            SemanticModel semanticModel)
        {
            var overloadInfos = new List<MethodOverloadInfo>();

            // Get the expression being invoked (e.g., 'System.Diagnostics.Debug.WriteLine')
            var expression = invocationNode.Expression;

            // Get symbol info for the expression
            var symbolInfo = semanticModel.GetSymbolInfo(expression);

            // Get the method symbol (could be null if code is incomplete)
            ISymbol methodSymbol = symbolInfo.Symbol;

            // If the method symbol is null, check candidate symbols
            if (methodSymbol == null && symbolInfo.CandidateSymbols.Length > 0)
            {
                methodSymbol = symbolInfo.CandidateSymbols[0];
            }

            if (methodSymbol != null)
            {
                // Cast to method symbol if possible
                var method = methodSymbol as IMethodSymbol;

                if (method != null)
                {
                    // Get the method name
                    var methodName = method.Name;

                    // Get the containing type
                    var containingType = method.ContainingType;

                    // Get all overloads of the method in the containing type
                    var methodOverloads = containingType.GetMembers(methodName)
                        .OfType<IMethodSymbol>();

                    // Extract information for each overload
                    foreach (var overload in methodOverloads)
                    {
                        var overloadInfo = new MethodOverloadInfo
                        {
                            Name = overload.Name,
                            Signature = overload.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                            ReturnType = overload.ReturnType.ToDisplayString(),
                            Parameters = overload.Parameters.Select(p => new ParameterInfo
                            {
                                Name = p.Name,
                                Type = p.Type.ToDisplayString()
                            }).ToList()
                        };

                        // Parse XML documentation
                        string xmlDocumentation = overload.GetDocumentationCommentXml();

                        if (!string.IsNullOrEmpty(xmlDocumentation))
                        {
                            var xmlDoc = System.Xml.Linq.XDocument.Parse(xmlDocumentation);
                            overloadInfo.Summary = xmlDoc.Root.Element("summary")?.Value.Trim();
                            overloadInfo.Remarks = xmlDoc.Root.Element("remarks")?.Value.Trim();

                            // Parse parameter documentation
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

                        overloadInfos.Add(overloadInfo);
                    }
                }
            }

            return overloadInfos;
        }
    }
}
