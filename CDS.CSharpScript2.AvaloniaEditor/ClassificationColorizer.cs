using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CDS.CSharpScript2.Classification;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// Paints Roslyn classification spans onto the visible lines of an AvaloniaEdit text view.
/// </summary>
/// <remarks>
/// Spans are stored as plain offsets from the most recent analysis pass. Between an edit and the
/// next pass (60 ms for the syntactic pass) they can drift slightly from the text; every span is
/// clamped to the line being painted, so drift can only mis-colour, never throw.
/// </remarks>
internal sealed class ClassificationColorizer : DocumentColorizingTransformer
{
    private ClassifiedSymbol[] _symbols = [];
    private Dictionary<SymbolClassification, Style> _styles = [];

    public void SetTheme(EditorTheme theme)
    {
        var styles = new Dictionary<SymbolClassification, Style>();

        foreach (var (classification, scheme) in theme.ClassificationColors)
        {
            styles[classification] = new Style(
                scheme.Foreground.ToBrush(),
                scheme.Background.A == 0 ? null : scheme.Background.ToBrush(),
                scheme.Bold,
                scheme.Italics,
                scheme.Underline);
        }

        _styles = styles;
    }

    public void SetClassifications(IReadOnlyList<ClassifiedSymbol> classifications)
    {
        var symbols = classifications.ToArray();
        Array.Sort(symbols, (a, b) => a.SpanStart.CompareTo(b.SpanStart));
        _symbols = symbols;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        var symbols = _symbols;
        if (symbols.Length == 0)
        {
            return;
        }

        var lineStart = line.Offset;
        var lineEnd = line.EndOffset;

        // First symbol starting at or after the line, then step back over any that begin
        // earlier but reach into it (multi-line comments and strings).
        var index = LowerBound(symbols, lineStart);
        while (index > 0 && symbols[index - 1].SpanStart + symbols[index - 1].SpanLength > lineStart)
        {
            index--;
        }

        for (; index < symbols.Length && symbols[index].SpanStart < lineEnd; index++)
        {
            var symbol = symbols[index];
            if (!_styles.TryGetValue(symbol.Classification, out var style))
            {
                continue;
            }

            var start = Math.Max(symbol.SpanStart, lineStart);
            var end = Math.Min(symbol.SpanStart + symbol.SpanLength, lineEnd);
            if (end <= start)
            {
                continue;
            }

            ChangeLinePart(start, end, element => style.Apply(element));
        }
    }

    private static int LowerBound(ClassifiedSymbol[] symbols, int offset)
    {
        int low = 0;
        int high = symbols.Length;

        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            if (symbols[mid].SpanStart < offset)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    private sealed record Style(
        IBrush Foreground,
        IBrush? Background,
        bool Bold,
        bool Italics,
        bool Underline)
    {
        public void Apply(VisualLineElement element)
        {
            var properties = element.TextRunProperties;
            properties.SetForegroundBrush(Foreground);

            if (Background is not null)
            {
                properties.SetBackgroundBrush(Background);
            }

            if (Bold || Italics)
            {
                var typeface = properties.Typeface;
                properties.SetTypeface(new Typeface(
                    typeface.FontFamily,
                    Italics ? FontStyle.Italic : typeface.Style,
                    Bold ? FontWeight.Bold : typeface.Weight));
            }

            if (Underline)
            {
                properties.SetTextDecorations(TextDecorations.Underline);
            }
        }
    }
}
