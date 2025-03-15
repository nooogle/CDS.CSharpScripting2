using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CDS.CSScripting2;


/// <summary>
/// Provides functionality to visualize syntax trees.
/// </summary>
public static class SyntaxTreeVisualizer
{
    /// <summary>
    /// Displays the structure of a syntax tree to the console.
    /// </summary>
    /// <param name="tree">The syntax tree to display.</param>
    public static void DisplayTree(SyntaxTree tree)
    {
        if (tree == null)
        {
            throw new ArgumentNullException(nameof(tree));
        }

        var root = tree.GetRoot();
        Console.WriteLine("Syntax Tree Visualization:");
        Console.WriteLine("=========================");
        DisplayNode(root, "");

        // Show diagnostics if any
        var diagnostics = tree.GetDiagnostics();
        if (diagnostics.Any())
        {
            Console.WriteLine("\nDiagnostics:");
            Console.WriteLine("=========================");
            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine($"{diagnostic.Location.GetLineSpan()}: {diagnostic.Severity} {diagnostic.Id}: {diagnostic.GetMessage()}");
            }
        }
    }

    private static void DisplayNode(SyntaxNode node, string indent)
    {
        Console.WriteLine($"{indent}Node: {node.Kind()} [{node.Span}]");

        var newIndent = indent + "·";

        // Process children
        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.IsNode)
            {
                DisplayNode(child.AsNode(), newIndent);
            }
            else
            {
                DisplayToken(child.AsToken(), newIndent);
            }
        }
    }

    private static void DisplayToken(SyntaxToken token, string indent)
    {
        Console.WriteLine($"{indent}Token: {token.Kind()} '{token.Text}' [{token.Span}]");

        var triviaIndent = indent + "·";

        foreach (var trivia in token.LeadingTrivia)
        {
            DisplayTrivia(trivia, triviaIndent, "Leading");
        }

        foreach (var trivia in token.TrailingTrivia)
        {
            DisplayTrivia(trivia, triviaIndent, "Trailing");
        }
    }

    private static void DisplayTrivia(SyntaxTrivia trivia, string indent, string type)
    {
        Console.WriteLine($"{indent}{type}Trivia: {trivia.Kind()} [{trivia.Span}]");

        // Display structured trivia if present
        if (trivia.HasStructure)
        {
            DisplayNode(trivia.GetStructure(), indent + "·");
        }
        else if (!string.IsNullOrWhiteSpace(trivia.ToString()))
        {
            // Display trivia text for non-whitespace trivia
            var text = trivia.ToString().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            Console.WriteLine($"{indent}·Text: '{text}'");
        }
    }
}
