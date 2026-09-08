using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CDS.CSharpScript2.CodeCompletion;

/// <summary>
/// Finds the bracket that is still open at a caret position, so an editor can offer the matching
/// closing bracket as a commit character for the completion list — Visual Studio accepts the
/// highlighted suggestion when you type the closing bracket, not only when you press Tab.
/// </summary>
/// <remarks>
/// The scan is lexical: the script is run through the C# lexer, so brackets inside string
/// literals, character literals and comments are never counted. Angle brackets are deliberately
/// ignored — <c>&lt;</c> is ambiguous between a type argument list and a less-than operator, and
/// no purely lexical rule can tell them apart.
/// </remarks>
public static class EnclosingBracket
{
    private static readonly CSharpParseOptions s_parseOptions =
        CSharpParseOptions.Default.WithKind(SourceCodeKind.Script);

    /// <summary>
    /// Returns the closing character of the innermost bracket left open before
    /// <paramref name="cursorPosition"/>.
    /// </summary>
    /// <param name="scriptText">The full source text of the script.</param>
    /// <param name="cursorPosition">The caret offset within <paramref name="scriptText"/>.</param>
    /// <returns>
    /// <c>')'</c>, <c>']'</c> or <c>'}'</c> when the caret sits inside a bracket of that kind;
    /// otherwise <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="scriptText"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cursorPosition"/> lies outside <paramref name="scriptText"/>.</exception>
    public static char? GetClosingCharacter(string scriptText, int cursorPosition)
    {
        if (scriptText is null)
        {
            throw new ArgumentNullException(nameof(scriptText));
        }

        if (cursorPosition < 0 || cursorPosition > scriptText.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cursorPosition),
                cursorPosition,
                $"Cursor position must lie within the script text (length {scriptText.Length}).");
        }

        var openBrackets = new Stack<char>();

        foreach (var token in SyntaxFactory.ParseTokens(scriptText, options: s_parseOptions))
        {
            // Only brackets the caret has actually moved past can enclose it. A token starting at
            // the caret is to its right, and one merely reaching the caret (an unterminated string,
            // say) is not a bracket token anyway.
            if (token.SpanStart >= cursorPosition)
            {
                break;
            }

            switch (token.Kind())
            {
                case SyntaxKind.OpenParenToken:
                    openBrackets.Push(')');
                    break;

                case SyntaxKind.OpenBracketToken:
                    openBrackets.Push(']');
                    break;

                case SyntaxKind.OpenBraceToken:
                    openBrackets.Push('}');
                    break;

                case SyntaxKind.CloseParenToken:
                    Close(openBrackets, ')');
                    break;

                case SyntaxKind.CloseBracketToken:
                    Close(openBrackets, ']');
                    break;

                case SyntaxKind.CloseBraceToken:
                    Close(openBrackets, '}');
                    break;
            }
        }

        return openBrackets.Count > 0 ? openBrackets.Peek() : null;
    }

    /// <summary>
    /// Matches a closing bracket against the open ones. A closer that doesn't match the innermost
    /// opener is left alone rather than unwinding the stack: half-typed code is routinely
    /// unbalanced, and discarding openers on a mismatch loses more context than it recovers.
    /// </summary>
    private static void Close(Stack<char> openBrackets, char closingCharacter)
    {
        if (openBrackets.Count > 0 && openBrackets.Peek() == closingCharacter)
        {
            openBrackets.Pop();
        }
    }
}
