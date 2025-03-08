using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CDS.CSScripting2
{
    public static class SyntaxTreeVisualizer
    {
        public static void DisplayTree(SyntaxTree tree)
        {
            var root = tree.GetRoot();
            DisplayNode(root, "");
        }

        private static void DisplayNode(SyntaxNode node, string indent)
        {
            Console.WriteLine($"{indent}{node.Kind()}");

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    DisplayNode(child.AsNode(), indent + "  ");
                }
                else
                {
                    DisplayToken(child.AsToken(), indent + "  ");
                }
            }
        }

        private static void DisplayToken(SyntaxToken token, string indent)
        {
            Console.WriteLine($"{indent}{token.Kind()} - '{token.Text}'");

            foreach (var trivia in token.LeadingTrivia)
            {
                Console.WriteLine($"{indent}  LeadingTrivia: {trivia.Kind()}");
            }

            foreach (var trivia in token.TrailingTrivia)
            {
                Console.WriteLine($"{indent}  TrailingTrivia: {trivia.Kind()}");
            }
        }
    }
}
