using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace CDS.CSharpScript2.Documentation;

/// <summary>The sections of a parsed XML documentation comment.</summary>
internal sealed record ParsedDocComment(
    IReadOnlyList<DocRun> Summary,
    IReadOnlyList<DocNamedItem> Parameters,
    IReadOnlyList<DocRun> Returns,
    IReadOnlyList<DocNamedItem> Exceptions,
    IReadOnlyList<DocRun> Remarks)
{
    internal static ParsedDocComment Empty { get; } = new([], [], [], [], []);
}

/// <summary>Parses XML documentation comments into <see cref="DocRun"/> sections.</summary>
internal sealed class DocCommentParser
{
    private const string ParagraphBreak = "\n\n";

    private readonly Compilation? _compilation;

    internal DocCommentParser(Compilation? compilation)
    {
        _compilation = compilation;
    }

    internal ParsedDocComment Parse(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return ParsedDocComment.Empty;

        XElement root;
        try
        {
            root = XElement.Parse(xml);
        }
        catch (XmlException)
        {
            return ParsedDocComment.Empty;
        }

        return new ParsedDocComment(
            Runs(root.Element("summary")),
            root.Elements("param").Select(p => new DocNamedItem(
                [new DocRun((string?)p.Attribute("name") ?? string.Empty, DocRunKind.Code, null)],
                Runs(p))).ToList(),
            Runs(root.Element("returns")),
            root.Elements("exception").Select(ExceptionItem).ToList(),
            Runs(root.Element("remarks")));
    }

    private DocNamedItem ExceptionItem(XElement element)
    {
        var cref = (string?)element.Attribute("cref") ?? string.Empty;
        return new DocNamedItem([CrefRun(cref)], Runs(element));
    }

    private IReadOnlyList<DocRun> Runs(XElement? element)
    {
        if (element == null)
            return [];

        var runs = new List<DocRun>();
        AppendNodes(element, runs);
        return Normalise(runs);
    }

    private void AppendNodes(XElement parent, List<DocRun> runs)
    {
        foreach (var node in parent.Nodes())
        {
            if (node is XText text)
            {
                runs.Add(new DocRun(text.Value, DocRunKind.Text, null));
            }
            else if (node is XElement child)
            {
                switch (child.Name.LocalName)
                {
                    case "see" when child.Attribute("cref") != null:
                        runs.Add(CrefRun((string)child.Attribute("cref")!));
                        break;
                    case "see" when child.Attribute("langword") != null:
                        runs.Add(new DocRun((string)child.Attribute("langword")!, DocRunKind.Code, null));
                        break;
                    case "paramref":
                    case "typeparamref":
                        runs.Add(new DocRun((string?)child.Attribute("name") ?? string.Empty, DocRunKind.Code, null));
                        break;
                    case "c":
                        runs.Add(new DocRun(child.Value, DocRunKind.Code, null));
                        break;
                    case "para":
                        runs.Add(new DocRun(ParagraphBreak, DocRunKind.Text, null));
                        AppendNodes(child, runs);
                        runs.Add(new DocRun(ParagraphBreak, DocRunKind.Text, null));
                        break;
                    default:
                        AppendNodes(child, runs);
                        break;
                }
            }
        }
    }

    private DocRun CrefRun(string cref)
    {
        ISymbol? symbol = null;
        if (_compilation != null && !cref.StartsWith("!:", StringComparison.Ordinal))
            symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(cref, _compilation);

        if (symbol != null)
            return new DocRun(symbol.Name, DocRunKind.Link, new DocTarget(cref));

        var name = cref.Length > 2 && cref[1] == ':' ? cref.Substring(2) : cref;
        var paren = name.IndexOf('(');
        if (paren >= 0)
            name = name.Substring(0, paren);

        // A cref that cannot be resolved has nowhere to go, so it is shown as plain text.
        return new DocRun(name.Substring(name.LastIndexOf('.') + 1), DocRunKind.Text, null);
    }

    private static bool IsBreak(DocRun run) => run.Kind == DocRunKind.Text && run.Text == ParagraphBreak;

    private static IReadOnlyList<DocRun> Normalise(List<DocRun> runs)
    {
        // Merge adjacent prose, collapsing XML whitespace; paragraph breaks stay as their own runs.
        var merged = new List<DocRun>();
        foreach (var run in runs)
        {
            if (run.Kind != DocRunKind.Text || IsBreak(run))
            {
                merged.Add(run);
                continue;
            }

            var last = merged.Count - 1;
            if (last >= 0 && merged[last].Kind == DocRunKind.Text && !IsBreak(merged[last]))
                merged[last] = merged[last] with { Text = Regex.Replace(merged[last].Text + run.Text, @"\s+", " ") };
            else
                merged.Add(run with { Text = Regex.Replace(run.Text, @"\s+", " ") });
        }

        // Trim the ends and the edges of paragraph breaks, then drop what is left empty.
        for (var i = 0; i < merged.Count; i++)
        {
            var run = merged[i];
            if (run.Kind != DocRunKind.Text || IsBreak(run))
                continue;

            var text = run.Text;
            if (i == 0 || IsBreak(merged[i - 1]))
                text = text.TrimStart();
            if (i == merged.Count - 1 || IsBreak(merged[i + 1]))
                text = text.TrimEnd();
            merged[i] = run with { Text = text };
        }

        merged.RemoveAll(r => r.Text.Length == 0);
        while (merged.Count > 0 && IsBreak(merged[0]))
            merged.RemoveAt(0);
        while (merged.Count > 0 && IsBreak(merged[merged.Count - 1]))
            merged.RemoveAt(merged.Count - 1);
        for (var i = merged.Count - 1; i > 0; i--)
        {
            if (IsBreak(merged[i]) && IsBreak(merged[i - 1]))
                merged.RemoveAt(i);
        }

        return merged;
    }
}
