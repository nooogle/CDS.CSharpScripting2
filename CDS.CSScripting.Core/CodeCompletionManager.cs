using Microsoft.CodeAnalysis.Completion;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CDS.CSScripting.Core
{
    internal class CodeCompletionManager
    {
        private Microsoft.CodeAnalysis.Document document;
        private CompletionService completionService;
        private string scriptText;

        public CodeCompletionManager(string scriptText, Microsoft.CodeAnalysis.Document document)
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

            CodeCompletionMode completionMode = DetermineCompletionMode(completionList.ItemsList[0]);
            var spanText = GetSpanTextForCodeCompletion(completionMode, completionList.ItemsList[0]);

            var filteredItems = FilterCompletionItems(completionList.ItemsList.ToImmutableArray(), completionMode, spanText);
            SortCompletionItems(filteredItems, completionMode, spanText);

            return filteredItems.ToImmutableArray();
        }


        private async Task<CompletionList> GetCompletionListAsync(int position)
        {
            return await completionService.GetCompletionsAsync(document, position, cancellationToken: default);
        }


        private CodeCompletionMode DetermineCompletionMode(CompletionItem firstItem)
        {
            int spanLength = firstItem.Span.Length;

            if (spanLength == 0)
            {
                return CodeCompletionMode.AllInAlphabeticalOrder;
            }
            else if (spanLength == 1)
            {
                return CodeCompletionMode.AllWithSingleLetterMatch;
            }
            else
            {
                return CodeCompletionMode.MatchingFirstTwoOrMoreOnly;
            }
        }


        private string GetSpanTextForCodeCompletion(CodeCompletionMode mode, CompletionItem firstItem)
        {
            return mode == CodeCompletionMode.AllInAlphabeticalOrder
                ? string.Empty
                : scriptText.Substring(firstItem.Span.Start, firstItem.Span.Length);
        }


        private List<CompletionItem> FilterCompletionItems(ImmutableArray<CompletionItem> items, CodeCompletionMode mode, string spanText)
        {
            if (mode != CodeCompletionMode.MatchingFirstTwoOrMoreOnly)
            {
                return items.ToList();
            }

            return items.Where(item => item.DisplayText.StartsWith(spanText, StringComparison.OrdinalIgnoreCase)).ToList();
        }


        private void SortCompletionItems(List<CompletionItem> items, CodeCompletionMode mode, string spanText)
        {
            if (mode == CodeCompletionMode.AllWithSingleLetterMatch && !string.IsNullOrEmpty(spanText))
            {
                var sorter = new CompletionItemSingleLetterMatchSorter(spanText[0]);
                items.Sort(sorter.Compare);
            }
            else
            {
                items.Sort(); // Use default sort
            }
        }

        class CompletionItemSingleLetterMatchSorter
        {
            StringComparerWithBiasForSingleLetter stringComparerWithBiasForSingleLetter;

            public CompletionItemSingleLetterMatchSorter(char singleLetter)
            {
                stringComparerWithBiasForSingleLetter = new StringComparerWithBiasForSingleLetter(singleLetter);
            }

            public int Compare(CompletionItem left, CompletionItem right)
            {
                return stringComparerWithBiasForSingleLetter.Compare(left.DisplayText, right.DisplayText);
            }
        }


        public class StringComparerWithBiasForSingleLetter
        {
            private char singleLetterInLowerCaseAsChar;
            private char singleLetterInUpperCaseAsChar;
            private string singleLetterInLowerCaseAsString;
            private string singleLetterInUpperCaseAsString;

            public StringComparerWithBiasForSingleLetter(char singleLetter)
            {
                singleLetterInLowerCaseAsChar = char.ToLower(singleLetter);
                singleLetterInUpperCaseAsChar = char.ToUpper(singleLetter);
                singleLetterInLowerCaseAsString = singleLetterInLowerCaseAsChar.ToString();
                singleLetterInUpperCaseAsString = singleLetterInUpperCaseAsChar.ToString();
            }

            public int Compare(string left, string right)
            {
                // LB   RB  LC  RC  R
                // y    y   na  na  cmp(l, r)
                // y    n   na  na  l
                // n    y   na  na  r
                // n    n   y   n   l
                // n    n   n   y   r
                // n    n   y   y   cmp(l, r)
                // n    n   n   n   cmp(l, r)

                var leftContainsLetter = left.Contains(singleLetterInUpperCaseAsChar) || left.Contains(singleLetterInLowerCaseAsChar);
                var rightContainsLetter = right.Contains(singleLetterInUpperCaseAsChar) || right.Contains(singleLetterInLowerCaseAsChar);
                var leftBeginsWithLetter = left.StartsWith(singleLetterInUpperCaseAsString) || left.StartsWith(singleLetterInLowerCaseAsString);
                var rightBeginsWithLetter = right.StartsWith(singleLetterInUpperCaseAsString) || right.StartsWith(singleLetterInLowerCaseAsString);

                if (leftBeginsWithLetter)
                {
                    if (rightBeginsWithLetter)
                    {
                        return left.CompareTo(right);
                    }
                    else
                    {
                        return -1;
                    }
                }

                if (rightBeginsWithLetter)
                {
                    return 1;
                }

                if (leftContainsLetter && !rightContainsLetter)
                {
                    return -1;
                }

                if (!leftContainsLetter && rightContainsLetter)
                {
                    return 1;
                }

                return left.CompareTo(right);
            }
        }
    }
}
