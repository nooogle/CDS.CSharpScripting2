using Microsoft.CodeAnalysis.Completion;
using System.Collections.Immutable;

namespace CDS.CSharpScriptUtils.CodeCompletion;


public static class Manager
{
    public static async Task<ImmutableArray<CompletionItem>> Get(
        string scriptText, 
        Microsoft.CodeAnalysis.Document document, 
        int cursorPosition)
    {
        // make a new document that only goes up to the position
        //var subScriptText = scriptText.Substring(0, cursorPosition);
        //var subDocument = document.WithText(Microsoft.CodeAnalysis.Text.SourceText.From(subScriptText));
        var subScriptText = scriptText;
        var subDocument = document;

        var completionService = CompletionService.GetService(document);

        var defaultEmptyResult = ImmutableArray<CompletionItem>.Empty;

        try
        {

            var completionList = await completionService.GetCompletionsAsync(
                subDocument, 
                cursorPosition, 
                cancellationToken: default);

            if (completionList == null || completionList.ItemsList.Count == 0)
            {
                return defaultEmptyResult;
            }

            Mode completionMode = DetermineCompletionMode(completionList.ItemsList[0]);
            
            var spanText = GetSpanTextForCodeCompletion(
                scriptText: subScriptText,
                completionMode, 
                completionList.ItemsList[0]);

            var filteredItems = FilterCompletionItems(completionList.ItemsList.ToImmutableArray(), completionMode, spanText);

            SortCompletionItems(filteredItems, completionMode, spanText);

            return filteredItems.ToImmutableArray();
        }
        catch (Exception ex)
        {
            // TODO understand why this is happening
            System.Diagnostics.Debug.WriteLine($"Exception in GetCompletionSuggestionsAsync: {ex.Message}");
            return defaultEmptyResult;
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
