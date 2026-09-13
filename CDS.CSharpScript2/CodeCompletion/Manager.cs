using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace CDS.CSharpScript2.CodeCompletion;

/// <summary>
/// Provides code completion suggestions for a Roslyn-backed C# script document.
/// </summary>
public static class Manager
{
    /// <summary>
    /// Returns completion items for the given cursor position within the script document.
    /// </summary>
    /// <param name="scriptText">The full source text of the script, used to extract the span text for filtering.</param>
    /// <param name="document">The Roslyn <see cref="Document"/> representing the script.</param>
    /// <param name="cursorPosition">The caret offset within the document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A filtered and sorted array of <see cref="CompletionItem"/> values, or an empty array if none are available.</returns>
    /// <remarks>
    /// Items are matched against the text typed so far with <see cref="CompletionMatcher"/>, so the
    /// typed characters need not be a prefix — see that type for what counts as a match. The
    /// strongest matches come first, which is what an editor should highlight.
    /// </remarks>
    public static async Task<ImmutableArray<CompletionItem>> GetAsync(
        string scriptText,
        Document document,
        int cursorPosition,
        CancellationToken cancellationToken = default)
    {
        var completionService = CompletionService.GetService(document);
        if (completionService == null)
        {
            return ImmutableArray<CompletionItem>.Empty;
        }

        try
        {
            var completionList = await completionService.GetCompletionsAsync(
                document,
                cursorPosition,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (completionList == null || completionList.ItemsList.Count == 0)
            {
                return ImmutableArray<CompletionItem>.Empty;
            }

            var typedText = GetTypedText(scriptText, completionList.ItemsList[0]);

            return typedText.Length == 0
                ? SortAlphabetically(completionList.ItemsList)
                : MatchAndRank(completionList.ItemsList, typedText);
        }
        catch (Exception ex)
        {
            // TODO understand why this is happening
            Debug.WriteLine($"Exception in GetCompletionSuggestionsAsync: {ex.Message}");
            return ImmutableArray<CompletionItem>.Empty;
        }
    }

    /// <summary>
    /// Returns the text the user has already typed at the caret — the span the completion list is
    /// filtered against. Empty when the caret sits at the start of a fresh identifier, as it does
    /// straight after a dot.
    /// </summary>
    private static string GetTypedText(string scriptText, CompletionItem firstItem)
    {
        var span = firstItem.Span;

        return span.Length == 0
            ? string.Empty
            : scriptText.Substring(span.Start, span.Length);
    }

    private static ImmutableArray<CompletionItem> SortAlphabetically(IReadOnlyList<CompletionItem> items)
    {
        var sorted = items.ToList();
        sorted.Sort();
        return sorted.ToImmutableArray();
    }

    /// <summary>
    /// Keeps the items that match the typed text and orders them best match first, falling back to
    /// Roslyn's own ordering between items that match equally well.
    /// </summary>
    private static ImmutableArray<CompletionItem> MatchAndRank(IReadOnlyList<CompletionItem> items, string typedText)
    {
        var matched = new List<(CompletionItem Item, CompletionMatch Match)>();

        foreach (var item in items)
        {
            var match = CompletionMatcher.Match(item.DisplayText, typedText);
            if (match.IsMatch)
            {
                matched.Add((item, match));
            }
        }

        matched.Sort((left, right) =>
        {
            int byQuality = left.Match.CompareQualityTo(right.Match);
            return byQuality != 0
                ? byQuality
                : Comparer<CompletionItem>.Default.Compare(left.Item, right.Item);
        });

        return matched.Select(entry => entry.Item).ToImmutableArray();
    }
}
