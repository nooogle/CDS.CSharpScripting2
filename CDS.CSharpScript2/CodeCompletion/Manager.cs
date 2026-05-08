using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace CDS.CSharpScript2.CodeCompletion;

public static class Manager
{
    public static async Task<ImmutableArray<CompletionItem>> GetAsync(
        string scriptText, 
        Document document, 
        int cursorPosition)
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
                cancellationToken: default).ConfigureAwait(false);

            if (completionList == null || completionList.ItemsList.Count == 0)
            {
                return ImmutableArray<CompletionItem>.Empty;
            }

            Mode completionMode = DetermineCompletionMode(completionList.ItemsList[0]);
            
            var spanText = GetSpanTextForCodeCompletion(
                scriptText: scriptText,
                mode: completionMode,
                firstItem: completionList.ItemsList[0]);

            var filteredItems = FilterCompletionItems(completionList.ItemsList.ToImmutableArray(), completionMode, spanText);

            SortCompletionItems(filteredItems, completionMode, spanText);

            return filteredItems.ToImmutableArray();
        }
        catch (Exception ex)
        {
            // TODO understand why this is happening
            Debug.WriteLine($"Exception in GetCompletionSuggestionsAsync: {ex.Message}");
            return ImmutableArray<CompletionItem>.Empty;
        }
    }

    private static Mode DetermineCompletionMode(CompletionItem firstItem)
    {
        int spanLength = firstItem.Span.Length;

        if (spanLength == 0)
        {
            return Mode.AllInAlphabeticalOrder;
        }
        else if (spanLength == 1)
        {
            return Mode.AllWithSingleLetterMatch;
        }
        else
        {
            return Mode.MatchingFirstTwoOrMoreOnly;
        }
    }

    private static string GetSpanTextForCodeCompletion(
        string scriptText,
        Mode mode, 
        CompletionItem firstItem)
    {
        return mode == Mode.AllInAlphabeticalOrder
            ? string.Empty
            : scriptText.Substring(firstItem.Span.Start, firstItem.Span.Length);
    }

    private static List<CompletionItem> FilterCompletionItems(ImmutableArray<CompletionItem> items, Mode mode, string spanText)
    {
        if (mode != Mode.MatchingFirstTwoOrMoreOnly)
        {
            return items.ToList();
        }

        return items.Where(item => item.DisplayText.StartsWith(spanText, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static void SortCompletionItems(List<CompletionItem> items, Mode mode, string spanText)
    {
        if (mode == Mode.AllWithSingleLetterMatch && !string.IsNullOrEmpty(spanText))
        {
            var sorter = new SingleLetterMatchSorter(spanText[0]);
            items.Sort(sorter.Compare);
        }
        else
        {
            items.Sort(); // Use default sort
        }
    }
}
