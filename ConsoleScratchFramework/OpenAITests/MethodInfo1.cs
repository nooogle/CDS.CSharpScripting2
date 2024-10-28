using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace OpenAITests
{
    public class ScriptAnalyzer
    {
        public static void AnalyzeScript(string scriptText)
        {
            // Parse the script text
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(scriptText);

            SyntaxTreeVisualizer.DisplayTree(syntaxTree);


            var references = 
                AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).Select(x => MetadataReference.CreateFromFile(x.Location)).ToArray();



            // Create a compilation unit
            var compilation = CSharpCompilation.Create("ScriptAnalysis")
                .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
                .AddReferences(references)
                    //MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    //MetadataReference.CreateFromFile(typeof(Console).Assembly.Location))
                .AddSyntaxTrees(syntaxTree);

            var d = compilation.GetDiagnostics();

            // Get the semantic model
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Position at the end of the script text
            int position = scriptText.Length - 6;

            // Find the syntax node at the position
            SyntaxNode root = syntaxTree.GetRoot();
            SyntaxToken token = root.FindToken(position);
            SyntaxNode node = token.Parent;

            // Traverse up to the invocation expression
            while (node != null && !(node is InvocationExpressionSyntax))
            {
                node = node.Parent;
            }

            if (node is InvocationExpressionSyntax invocationExpression)
            {
                ISymbol symbol = semanticModel.GetDeclaredSymbol(node);
                SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(node);

                //ExpressionSyntax expression = invocationExpression.Expression;
                //SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression);

                // Get method symbols (overloads)
                var methodSymbols = symbolInfo.CandidateSymbols
                    .OfType<IMethodSymbol>();

                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    methodSymbols = methodSymbols.Append(methodSymbol);
                }

                foreach (var method in methodSymbols)
                {
                    // Method name
                    string methodName = method.Name;
                    Console.WriteLine($"Method: {methodName}");

                    // Parameters
                    Console.WriteLine("Parameters:");
                    foreach (var parameter in method.Parameters)
                    {
                        Console.WriteLine($" - {parameter.Type} {parameter.Name}");
                    }

                    // Documentation
                    string documentation = method.GetDocumentationCommentXml();
                    Console.WriteLine("Documentation:");
                    Console.WriteLine(documentation);
                    Console.WriteLine("-----------------------------------");
                }
            }
            else
            {
                Console.WriteLine("No invocation expression found at the given position.");
            }
        }

        public static void Test()
        {
            //Console.WriteLine(
            string scriptText = "System.Console.WriteLine(";
            ScriptAnalyzer.AnalyzeScript(scriptText);
        }

        //    const string source = @"
        //public class TestClass
        //{
        //    public void M()
        //    {
        //        M2(new DateTime());
        //        M2(new TestClass());
        //    }
        
        //    public void M2(object parameter) { }
        //}";
        //    var compilation = CreateCompilation(source);
        //    var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        //    var tree = compilation.SyntaxTrees.First();
        //    var nodes = tree.GetRoot()
        //        .DescendantNodes(x => x.GetType() != typeof(InvocationExpressionSyntax))
        //        .OfType<InvocationExpressionSyntax>();
        //    var node = nodes.First(); //M2(new DateTime());
        //    var node2 = nodes.ElementAt(1); //M2(new TestClass());

        //    var symbolInfo = semanticModel.GetSymbolInfo(node); //fails
        //    var symbolInfo2 = semanticModel.GetSymbolInfo(node2); //succeeds
        //}

        //private static Compilation CreateCompilation(string source)
        //{
        //    var tree = CSharpSyntaxTree.ParseText(source);
        //    var references = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).Select(x => MetadataReference.CreateFromFile(x.Location)).ToArray();
        //    var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
        //    var compilation = CSharpCompilation.Create("compilation", new[] { tree }, references, options);
        //    return compilation;
        //}
    }
}
