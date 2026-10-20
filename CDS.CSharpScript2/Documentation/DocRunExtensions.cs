using System.Text;

namespace CDS.CSharpScript2.Documentation;

/// <summary>Helpers for working with lists of <see cref="DocRun"/>.</summary>
public static class DocRunExtensions
{
    /// <summary>
    /// Concatenates the text of <paramref name="runs"/>, collapsing whitespace to single spaces
    /// and trimming the ends.
    /// </summary>
    /// <param name="runs">The runs to flatten.</param>
    public static string ToPlainText(this IReadOnlyList<DocRun> runs)
    {
        var builder = new StringBuilder();
        var pendingSpace = false;
        foreach (var run in runs)
        {
            foreach (var c in run.Text)
            {
                if (char.IsWhiteSpace(c))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
