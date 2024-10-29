using ConsoleScratchFramework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenAITests
{
    class OpenAITestsOverloads2
    {
        public static void Run()
        {
            var code = ScriptSamples.S5.Code;
            int position = ScriptSamples.S5.Position;

            // Parse the code into a syntax tree
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            //SyntaxTreeVisualizer.DisplayTree(syntaxTree);

            // Get paths to assemblies
            string mscorlibPath = typeof(object).Assembly.Location;
            string systemConsolePath = typeof(Console).Assembly.Location;

            // Get paths to XML documentation files
            string mscorlibXmlPath = GetXmlDocumentationPath(mscorlibPath);
            string systemConsoleXmlPath = GetXmlDocumentationPath(systemConsolePath);

            // Create documentation providers
            DocumentationProvider mscorlibDocumentation = null;
            DocumentationProvider systemConsoleDocumentation = null;

            if (File.Exists(mscorlibXmlPath))
            {
                mscorlibDocumentation = XmlDocumentationProvider.CreateFromFile(mscorlibXmlPath);
            }

            if (File.Exists(systemConsoleXmlPath))
            {
                systemConsoleDocumentation = XmlDocumentationProvider.CreateFromFile(systemConsoleXmlPath);
            }

            // Create metadata references with documentation providers
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(
                    mscorlibPath,
                    documentation: mscorlibDocumentation ?? DocumentationProvider.Default),

                MetadataReference.CreateFromFile(
                    systemConsolePath,
                    documentation: systemConsoleDocumentation ?? DocumentationProvider.Default)
            };

            // Create a compilation unit
            var compilation = CSharpCompilation.Create(
                assemblyName: "MyCompilation",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            );

            var diagnostics = compilation.GetDiagnostics();
            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine($"Diag: {diagnostic}");
            }

            // Get the semantic model
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

            position = code.IndexOf(".Add") + 5;


            // Get the root of the syntax tree
            var root = syntaxTree.GetRoot();


            // Find the invocation expression node at the cursor position
            var invocationNode = root.FindToken(position - 1).Parent
                .AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();

            if (invocationNode != null)
            {
                DisplayMethodOverloads(position, invocationNode, semanticModel);
            }
            else
            {
                Console.WriteLine("Invocation node not found.");
            }
        }

        private static void DisplayMethodOverloads(int position, InvocationExpressionSyntax invocationNode, SemanticModel semanticModel)
        {
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

                    // Extract XML documentation for each overload
                    foreach (var overload in methodOverloads)
                    {
                        string signature = overload.ToDisplayString(
                            SymbolDisplayFormat.CSharpErrorMessageFormat);

                        string xmlDocumentation = overload.GetDocumentationCommentXml();

                        Console.WriteLine("Method Signature: " + signature);
                        Console.WriteLine("XML Documentation:\n" + xmlDocumentation);
                        Console.WriteLine(new string('-', 50));
                    }
                }
                else
                {
                    Console.WriteLine("The symbol is not a method.");
                }
            }
            else
            {
                Console.WriteLine("Method symbol not found.");
            }
        }


        private static string GetXmlDocumentationPath(string assemblyPath)
        {
            string xmlPath = Path.ChangeExtension(assemblyPath, ".xml");

            if (File.Exists(xmlPath))
            {
                return xmlPath;
            }

            // Fallback to common locations
            string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

            var programFilesPaths = new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            var possiblePaths = new List<string>();

            foreach (var programFilesPath in programFilesPaths)
            {
                // .NET Core/5+ paths
                possiblePaths.Add(Path.Combine(programFilesPath, "dotnet", "packs", "Microsoft.NETCore.App.Ref", "5.0.0", "ref", "net5.0", $"{assemblyName}.xml"));
                possiblePaths.Add(Path.Combine(programFilesPath, "dotnet", "shared", "Microsoft.NETCore.App", "5.0.0", $"{assemblyName}.xml"));

                // .NET Framework paths
                foreach (var netFrameworkVersion in new[] { "v4.8.1", "v4.8", "v4.7.2" })
                {
                    possiblePaths.Add(Path.Combine(programFilesPath, "Reference Assemblies", "Microsoft", "Framework", ".NETFramework", netFrameworkVersion, $"{assemblyName}.xml"));
                }

                // Add more versions if needed
                foreach (var netCoreVersion in new[] { "3.1.0", "3.0.0", "2.2.0", "2.1.0" })
                {
                    possiblePaths.Add(Path.Combine(programFilesPath, "dotnet", "packs", "Microsoft.NETCore.App.Ref", netCoreVersion, "ref", $"netcoreapp{netCoreVersion}", $"{assemblyName}.xml"));
                }
            }

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return xmlPath; // Return the original path if no fallback found
        }
    }
}
