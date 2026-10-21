using CDS.CSharpScript2.Documentation;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// Writes a <see cref="DocPage"/> into a read-only <see cref="RichTextBox"/> as styled runs, and
/// reports where its links are so the panel can hit-test them.
/// </summary>
/// <remarks>
/// Sections follow the Avalonia panel: kind line, title, signature, summary, then Parameters,
/// Returns, Exceptions, Remarks, Overloads, and the member groups, each omitted when empty.
/// A text box cannot draw boxes or paragraph spacing, so the signature is shaded behind its text
/// and spacing is made with small blank lines.
/// </remarks>
internal static class DocPageRenderer
{
    internal const string EmptyHint = "Put the caret on a symbol and press F1";
    internal const string UnavailableText = "This item is no longer available.";
    internal const string ThisCallText = "← this call";

    private const float BodyFontSize = 9f;
    private const float SmallFontSize = 8f;
    private const float HeadingFontSize = 7.5f;
    private const float TitleFontSize = 12f;
    private const float CodeFontSize = 8.5f;

    /// <summary>Replaces the contents of <paramref name="box"/> with <paramref name="page"/>.</summary>
    /// <param name="box">The text box to write into; it is cleared first.</param>
    /// <param name="page">The page to show, or <see langword="null"/> for none.</param>
    /// <param name="state">The panel's state; "← this call" marks the current overload only when following.</param>
    /// <param name="loading">Whether a page is still on its way; with no page yet, nothing is drawn rather than the hint.</param>
    /// <param name="palette">The colours and code font to draw with.</param>
    /// <returns>The links in the text, in text order.</returns>
    public static IReadOnlyList<DocLinkSpan> Render(
        RichTextBox box,
        DocPage? page,
        DocsPanelState state,
        bool loading,
        DocsPanelPalette palette)
    {
        using var redraw = new RedrawSuspender(box);
        box.Clear();
        using var writer = new Writer(box, palette);

        if (page is null && loading)
        {
            return writer.Links;
        }

        if (page is null)
        {
            writer.Text(EmptyHint, BodyFontSize, palette.Dim);
            return writer.Links;
        }

        if (page.IsUnavailable)
        {
            writer.Text(UnavailableText, BodyFontSize, palette.Dim);
            return writer.Links;
        }

        if (page.KindLine.Count > 0)
        {
            writer.Runs(page.KindLine, SmallFontSize, palette.Dim);
            writer.EndParagraph(2);
        }

        writer.Text(page.Title, TitleFontSize, palette.Foreground);
        writer.EndParagraph(6);

        if (page.Signature.Length > 0)
        {
            writer.Code(page.Signature, CodeFontSize, palette.Foreground, palette.Inset);
            writer.EndParagraph(8);
        }

        if (page.Summary.Count > 0)
        {
            writer.Runs(page.Summary, BodyFontSize, palette.Foreground);
            writer.EndParagraph(0);
        }

        AddNamedItems(writer, "PARAMETERS", page.Parameters, palette);

        if (page.Returns.Count > 0)
        {
            writer.Heading("RETURNS");
            writer.Runs(page.Returns, BodyFontSize, palette.Foreground);
            writer.EndParagraph(0);
        }

        AddNamedItems(writer, "EXCEPTIONS", page.Exceptions, palette);

        if (page.Remarks.Count > 0)
        {
            writer.Heading("REMARKS");
            writer.Runs(page.Remarks, BodyFontSize, palette.Foreground);
            writer.EndParagraph(0);
        }

        if (page.Overloads.Count > 0)
        {
            writer.Heading($"OVERLOADS ({page.Overloads.Count})");
            foreach (var overload in page.Overloads)
            {
                AddOverload(writer, overload, state, palette);
            }
        }

        foreach (var group in page.MemberGroups)
        {
            if (group.Members.Count == 0)
            {
                continue;
            }

            writer.Heading(group.Heading.ToUpperInvariant());
            AddMemberGroup(writer, group, palette);
        }

        return writer.Links;
    }

    private static void AddNamedItems(
        Writer writer,
        string heading,
        IReadOnlyList<DocNamedItem> items,
        DocsPanelPalette palette)
    {
        if (items.Count == 0)
        {
            return;
        }

        writer.Heading(heading);
        foreach (var item in items)
        {
            // The name is shown as code unless it is already a link.
            foreach (var run in item.Name)
            {
                writer.Run(run.Kind == DocRunKind.Text ? new DocRun(run.Text, DocRunKind.Code, null) : run, BodyFontSize, palette.Foreground);
            }

            if (item.Description.Count > 0)
            {
                writer.Text(" ", BodyFontSize, palette.Foreground);
                writer.Runs(item.Description, BodyFontSize, palette.Foreground);
            }

            writer.EndParagraph(2);
        }
    }

    private static void AddOverload(Writer writer, DocOverload overload, DocsPanelState state, DocsPanelPalette palette)
    {
        if (overload.IsCurrent)
        {
            writer.Code(overload.Label, CodeFontSize, palette.CurrentForeground, palette.CurrentBackground);
            if (state == DocsPanelState.Following)
            {
                writer.Text("  " + ThisCallText, CodeFontSize, palette.Dim);
            }
        }
        else
        {
            writer.Link(overload.Label, overload.Target, CodeFontSize, useCodeFont: true);
        }

        writer.EndParagraph(1);
    }

    private static void AddMemberGroup(Writer writer, DocMemberGroup group, DocsPanelPalette palette)
    {
        for (var i = 0; i < group.Members.Count; i++)
        {
            var member = group.Members[i];
            if (i > 0)
            {
                writer.Text(" · ", CodeFontSize, palette.Dim);
            }

            writer.Link(member.Name, member.Target, CodeFontSize, useCodeFont: true);
            if (member.OverloadCount > 1)
            {
                writer.Text($" ({member.OverloadCount})", CodeFontSize, palette.Dim);
            }
        }

        writer.EndParagraph(0);
    }

    /// <summary>
    /// Collects styled text as RTF and loads it into the text box in one go (setting formatting run by
    /// run through the text box is an order of magnitude slower), remembering which characters are links.
    /// </summary>
    private sealed class Writer : IDisposable
    {
        private readonly RichTextBox _box;
        private readonly DocsPanelPalette _palette;
        private readonly string _bodyFont;
        private readonly List<string> _fonts = [];
        private readonly List<Color> _colors = [];
        private readonly System.Text.StringBuilder _body = new();
        private readonly List<DocLinkSpan> _links = [];
        private int _length;

        public Writer(RichTextBox box, DocsPanelPalette palette)
        {
            _box = box;
            _palette = palette;
            _bodyFont = box.Font.FontFamily.Name;
        }

        public IReadOnlyList<DocLinkSpan> Links => _links;

        public void Text(string text, float size, Color color)
            => Append(text, _bodyFont, size, FontStyle.Regular, color, null);

        public void Code(string text, float size, Color color, Color back)
            => Append(text, _palette.CodeFontName, size, FontStyle.Regular, color, back);

        public void Link(string text, DocTarget target, float size, bool useCodeFont)
        {
            var start = _length;
            Append(text, useCodeFont ? _palette.CodeFontName : _bodyFont, size, FontStyle.Underline, _palette.Link, null);
            _links.Add(new DocLinkSpan(start, text.Length, target));
        }

        public void Heading(string text)
        {
            // Space above, then a bold dim caption.
            Gap(8);
            Append(text, _bodyFont, HeadingFontSize, FontStyle.Bold, _palette.Heading, null);
            EndParagraph(3);
        }

        public void Runs(IReadOnlyList<DocRun> runs, float size, Color color)
        {
            foreach (var run in runs)
            {
                Run(run, size, color);
            }
        }

        public void Run(DocRun run, float size, Color color)
        {
            switch (run.Kind)
            {
                case DocRunKind.Link when run.Target is not null:
                    Link(run.Text, run.Target, size, useCodeFont: false);
                    break;

                case DocRunKind.Code:
                    Code(run.Text, Math.Min(size, CodeFontSize), _palette.Code, _palette.Background);
                    break;

                default:
                    Text(run.Text, size, color);
                    break;
            }
        }

        /// <summary>Ends the line and leaves <paramref name="gapPoints"/> of space below it.</summary>
        public void EndParagraph(float gapPoints)
        {
            Newline(BodyFontSize);
            Gap(gapPoints);
        }

        /// <summary>Loads the collected text into the text box.</summary>
        public void Dispose()
        {
            var rtf = new System.Text.StringBuilder(@"{\rtf1\ansi\deff0{\fonttbl");
            for (var i = 0; i < _fonts.Count; i++)
            {
                rtf.Append(@"{\f").Append(i).Append(' ').Append(Escape(_fonts[i])).Append(";}");
            }

            rtf.Append(@"}{\colortbl;");
            foreach (var color in _colors)
            {
                rtf.Append(@"\red").Append(color.R).Append(@"\green").Append(color.G).Append(@"\blue").Append(color.B).Append(';');
            }

            rtf.Append('}').Append(_body).Append('}');
            _box.Rtf = rtf.ToString();
        }

        private void Gap(float points)
        {
            if (points > 0)
            {
                Newline(points);
            }
        }

        // The size of the paragraph mark sets the height of an otherwise empty line.
        private void Newline(float size)
        {
            _body.Append(@"{\fs").Append(HalfPoints(size)).Append(@"\par}");
            _length++;
        }

        private void Append(string text, string fontName, float size, FontStyle style, Color color, Color? back)
        {
            // One character in the box per character here, so link offsets stay right: a CR LF pair is one break.
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            _body.Append(@"{\f").Append(Index(_fonts, fontName))
                .Append(@"\fs").Append(HalfPoints(size))
                .Append(@"\cf").Append(Index(_colors, color) + 1);

            if (back is { } highlight)
            {
                _body.Append(@"\highlight").Append(Index(_colors, highlight) + 1);
            }

            if ((style & FontStyle.Bold) != 0)
            {
                _body.Append(@"\b");
            }

            if ((style & FontStyle.Underline) != 0)
            {
                _body.Append(@"\ul");
            }

            _body.Append(' ').Append(Escape(text)).Append('}');
            _length += text.Length;
        }

        private static int HalfPoints(float points) => Math.Max(2, (int)Math.Round(points * 2));

        private static int Index<T>(List<T> list, T item)
        {
            var index = list.IndexOf(item);
            if (index < 0)
            {
                list.Add(item);
                index = list.Count - 1;
            }

            return index;
        }

        // Each UTF-16 unit above ASCII becomes one signed \u escape, so it is still one character in the box.
        private static string Escape(string text)
        {
            var sb = new System.Text.StringBuilder(text.Length + 8);
            foreach (var c in text)
            {
                switch (c)
                {
                    case '\\':
                    case '{':
                    case '}':
                        sb.Append('\\').Append(c);
                        break;

                    case '\n':
                        sb.Append(@"\line ");
                        break;

                    case '\t':
                        sb.Append(@"\tab ");
                        break;

                    default:
                        if (c > 126)
                        {
                            sb.Append(@"\u").Append((short)c).Append('?');
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            return sb.ToString();
        }
    }
}
