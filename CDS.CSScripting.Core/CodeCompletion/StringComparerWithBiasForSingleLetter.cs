using System.Linq;

namespace CDS.CSScripting.CodeCompletion
{
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
