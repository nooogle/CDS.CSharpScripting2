using Microsoft.CodeAnalysis.Completion;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CDS.CSScripting2.CodeCompletion
{
    public partial class Manager
    {
        private Microsoft.CodeAnalysis.Document document;
        private CompletionService completionService;
        private string scriptText;


        public Manager(string scriptText, Microsoft.CodeAnalysis.Document document)
        {
            this.scriptText = scriptText;
            this.document = document;
            completionService = CompletionService.GetService(document);
        }


        public async Task<ImmutableArray<CompletionItem>> GetCompletionSuggestionsAsync(int position)
        {
            var completionList = await GetCompletionListAsync(position);
            if (completionList == null || completionList.ItemsList.Count == 0)
            {
                return ImmutableArray<CompletionItem>.Empty;
            }

            Mode completionMode = DetermineCompletionMode(completionList.ItemsList[0]);
            var spanText = GetSpanTextForCodeCompletion(completionMode, completionList.ItemsList[0]);

            var filteredItems = FilterCompletionItems(completionList.ItemsList.ToImmutableArray(), completionMode, spanText);
            SortCompletionItems(filteredItems, completionMode, spanText);

            return filteredItems.ToImmutableArray();
        }


        private async Task<CompletionList> GetCompletionListAsync(int position)
        {
            return await completionService.GetCompletionsAsync(document, position, cancellationToken: default);
        }


        private Mode DetermineCompletionMode(CompletionItem firstItem)
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


        private string GetSpanTextForCodeCompletion(Mode mode, CompletionItem firstItem)
        {
            return mode == Mode.AllInAlphabeticalOrder
                ? string.Empty
                : scriptText.Substring(firstItem.Span.Start, firstItem.Span.Length);
        }


        private List<CompletionItem> FilterCompletionItems(ImmutableArray<CompletionItem> items, Mode mode, string spanText)
        {
            if (mode != Mode.MatchingFirstTwoOrMoreOnly)
            {
                return items.ToList();
            }

            return items.Where(item => item.DisplayText.StartsWith(spanText, StringComparison.OrdinalIgnoreCase)).ToList();
        }


        private void SortCompletionItems(List<CompletionItem> items, Mode mode, string spanText)
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
}
