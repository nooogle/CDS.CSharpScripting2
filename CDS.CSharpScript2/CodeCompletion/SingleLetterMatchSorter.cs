using Microsoft.CodeAnalysis.Completion;

namespace CDS.CSharpScript2.CodeCompletion
{
    class SingleLetterMatchSorter
    {
        StringComparerWithBiasForSingleLetter stringComparerWithBiasForSingleLetter;

        public SingleLetterMatchSorter(char singleLetter)
        {
            stringComparerWithBiasForSingleLetter = new StringComparerWithBiasForSingleLetter(singleLetter);
        }

        public int Compare(CompletionItem left, CompletionItem right)
        {
            return stringComparerWithBiasForSingleLetter.Compare(left.DisplayText, right.DisplayText);
        }
    }
}
