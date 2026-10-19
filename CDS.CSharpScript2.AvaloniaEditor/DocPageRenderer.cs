using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using CDS.CSharpScript2.Documentation;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// Builds the docs panel's body for a <see cref="DocPage"/> out of native controls: wrapped
/// <see cref="TextBlock"/>s whose inlines carry code-styled runs and <see cref="DocLink"/>s.
/// </summary>
/// <remarks>
/// Sections follow the approved layout: kind line, title, signature, summary, then Parameters,
/// Returns, Exceptions, Remarks, Overloads, and the member groups, each omitted when empty.
/// </remarks>
internal static class DocPageRenderer
{
    internal const string EmptyHint = "Put the caret on a symbol and press F1";
    internal const string UnavailableText = "This item is no longer available.";
    internal const string ThisCallText = "← this call";

    private const double BodyFontSize = 12;
    private const double SmallFontSize = 11;
    private const double HeadingFontSize = 10;
    private const double TitleFontSize = 15;
    private const double CodeFontSize = 11;

    /// <summary>Builds the body for <paramref name="page"/>, or the hint when there is none.</summary>
    /// <param name="page">The page to show, or <see langword="null"/> for none.</param>
    /// <param name="state">The panel's state; "← this call" marks the current overload only when following.</param>
    /// <param name="loading">Whether a page is still on its way; with no page yet, nothing is drawn rather than the hint.</param>
    /// <param name="palette">The brushes and fonts to draw with.</param>
    public static Control Render(DocPage? page, DocsPanelState state, bool loading, DocsPanelPalette palette)
    {
        var body = new StackPanel { Spacing = 0 };

        if (page is null && loading)
        {
            return body;
        }

        if (page is null)
        {
            body.Children.Add(Paragraph(EmptyHint, palette.Dim));
            return body;
        }

        if (page.IsUnavailable)
        {
            body.Children.Add(Paragraph(UnavailableText, palette.Dim));
            return body;
        }

        if (page.KindLine.Count > 0)
        {
            var kind = Runs(page.KindLine, palette, SmallFontSize);
            kind.Foreground = palette.Dim;
            body.Children.Add(kind);
        }

        body.Children.Add(new TextBlock
        {
            Text = page.Title,
            FontSize = TitleFontSize,
            Foreground = palette.Foreground,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 6),
        });

        if (page.Signature.Length > 0)
        {
            body.Children.Add(new Border
            {
                Background = palette.Inset,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 8),
                Child = new TextBlock
                {
                    Text = page.Signature,
                    FontFamily = palette.CodeFontFamily,
                    FontSize = CodeFontSize,
                    Foreground = palette.Foreground,
                    TextWrapping = TextWrapping.Wrap,
                },
            });
        }

        if (page.Summary.Count > 0)
        {
            body.Children.Add(Runs(page.Summary, palette, BodyFontSize));
        }

        AddNamedItems(body, "PARAMETERS", page.Parameters, palette);

        if (page.Returns.Count > 0)
        {
            body.Children.Add(Heading("RETURNS", palette));
            body.Children.Add(Runs(page.Returns, palette, BodyFontSize));
        }

        AddNamedItems(body, "EXCEPTIONS", page.Exceptions, palette);

        if (page.Remarks.Count > 0)
        {
            body.Children.Add(Heading("REMARKS", palette));
            body.Children.Add(Runs(page.Remarks, palette, BodyFontSize));
        }

        if (page.Overloads.Count > 0)
        {
            body.Children.Add(Heading($"OVERLOADS ({page.Overloads.Count})", palette));
            foreach (var overload in page.Overloads)
            {
                var row = Overload(overload, state, palette);
                row.Margin = new Thickness(0, 1);
                body.Children.Add(row);
            }
        }

        foreach (var group in page.MemberGroups)
        {
            if (group.Members.Count == 0)
            {
                continue;
            }

            body.Children.Add(Heading(group.Heading.ToUpperInvariant(), palette));
            body.Children.Add(MemberGroup(group, palette));
        }

        return body;
    }

    private static void AddNamedItems(
        StackPanel body,
        string heading,
        IReadOnlyList<DocNamedItem> items,
        DocsPanelPalette palette)
    {
        if (items.Count == 0)
        {
            return;
        }

        body.Children.Add(Heading(heading, palette));
        foreach (var item in items)
        {
            var name = item.Name
                .Select(r => r.Kind == DocRunKind.Text ? r with { Kind = DocRunKind.Code } : r)
                .ToList();
            var runs = item.Description.Count == 0
                ? name
                : [.. name, new DocRun(" ", DocRunKind.Text, null), .. item.Description];

            var block = Runs(runs, palette, BodyFontSize);
            block.Margin = new Thickness(0, 0, 0, 2);
            body.Children.Add(block);
        }
    }

    private static Control Overload(DocOverload overload, DocsPanelState state, DocsPanelPalette palette)
    {
        if (!overload.IsCurrent)
        {
            return CodeLink(overload.Label, overload.Target, palette);
        }

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        row.Children.Add(new Border
        {
            Background = palette.CurrentBackground,
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(3, 0),
            Child = new TextBlock
            {
                Text = overload.Label,
                FontFamily = palette.CodeFontFamily,
                FontSize = CodeFontSize,
                Foreground = palette.CurrentForeground,
            },
        });

        if (state == DocsPanelState.Following)
        {
            row.Children.Add(new TextBlock
            {
                Text = ThisCallText,
                FontSize = CodeFontSize,
                Foreground = palette.Dim,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }

        return row;
    }

    private static TextBlock MemberGroup(DocMemberGroup group, DocsPanelPalette palette)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontFamily = palette.CodeFontFamily,
            FontSize = CodeFontSize,
            LineHeight = CodeFontSize * 1.7,
            Foreground = palette.Foreground,
        };

        var inlines = block.Inlines!;
        for (var i = 0; i < group.Members.Count; i++)
        {
            var member = group.Members[i];
            if (i > 0)
            {
                inlines.Add(new Run(" · ") { Foreground = palette.Dim });
            }

            inlines.Add(new InlineUIContainer(CodeLink(member.Name, member.Target, palette)));
            if (member.OverloadCount > 1)
            {
                inlines.Add(new Run($" ({member.OverloadCount})") { Foreground = palette.Dim });
            }
        }

        return block;
    }

    private static TextBlock Runs(IReadOnlyList<DocRun> runs, DocsPanelPalette palette, double fontSize)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = fontSize,
            Foreground = palette.Foreground,
        };

        var inlines = block.Inlines!;
        foreach (var run in runs)
        {
            switch (run.Kind)
            {
                case DocRunKind.Link when run.Target is not null:
                    inlines.Add(new InlineUIContainer(new DocLink(run.Text, run.Target)
                    {
                        FontSize = fontSize,
                        Foreground = palette.Link,
                    }));
                    break;

                case DocRunKind.Code:
                    inlines.Add(new Run(run.Text)
                    {
                        FontFamily = palette.CodeFontFamily,
                        FontSize = Math.Min(fontSize, CodeFontSize),
                        Foreground = palette.Code,
                    });
                    break;

                default:
                    inlines.Add(new Run(run.Text));
                    break;
            }
        }

        return block;
    }

    private static DocLink CodeLink(string text, DocTarget target, DocsPanelPalette palette)
    {
        return new DocLink(text, target)
        {
            FontFamily = palette.CodeFontFamily,
            FontSize = CodeFontSize,
            Foreground = palette.Link,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
    }

    private static TextBlock Heading(string text, DocsPanelPalette palette)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = HeadingFontSize,
            LetterSpacing = 0.6,
            Foreground = palette.Heading,
            Margin = new Thickness(0, 10, 0, 3),
        };
    }

    private static TextBlock Paragraph(string text, IBrush foreground)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = BodyFontSize,
            Foreground = foreground,
            TextWrapping = TextWrapping.Wrap,
        };
    }
}
