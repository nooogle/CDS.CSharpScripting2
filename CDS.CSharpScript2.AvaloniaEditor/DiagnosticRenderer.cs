using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CDS.CSharpScript2.Classification;
using Microsoft.CodeAnalysis;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// Draws squiggly underlines beneath error and warning diagnostics.
/// </summary>
/// <remarks>
/// Diagnostics are held in a <see cref="TextSegmentCollection{T}"/> attached to the document, so
/// their positions follow edits until the next analysis pass replaces them.
/// </remarks>
internal sealed class DiagnosticRenderer : IBackgroundRenderer
{
    private const double SquiggleStep = 2.0;
    private const double SquiggleHeight = 2.0;

    private readonly TextSegmentCollection<DiagnosticSegment> _segments;
    private IPen _errorPen = new ImmutablePen(Brushes.Red, 1);
    private IPen _warningPen = new ImmutablePen(Brushes.Orange, 1);

    public DiagnosticRenderer(AvaloniaEdit.Document.TextDocument document)
    {
        _segments = new TextSegmentCollection<DiagnosticSegment>(document);
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void SetTheme(EditorTheme theme)
    {
        _errorPen = new ImmutablePen(theme.ErrorIndicatorForeColor.ToBrush(), 1);
        _warningPen = new ImmutablePen(theme.WarningIndicatorForeColor.ToBrush(), 1);
    }

    public void SetDiagnostics(IEnumerable<Diagnostic> diagnostics, int documentLength)
    {
        _segments.Clear();

        foreach (var diagnostic in diagnostics)
        {
            if (!diagnostic.Location.IsInSource ||
                diagnostic.Severity is not (DiagnosticSeverity.Error or DiagnosticSeverity.Warning))
            {
                continue;
            }

            var start = diagnostic.Location.SourceSpan.Start;
            var length = diagnostic.Location.SourceSpan.Length;

            // A zero-width diagnostic (e.g. a missing semicolon) underlines the preceding character.
            if (length == 0)
            {
                start = Math.Max(0, start - 1);
                length = 1;
            }

            start = Math.Min(start, documentLength);
            length = Math.Min(length, documentLength - start);
            if (length <= 0)
            {
                continue;
            }

            _segments.Add(new DiagnosticSegment(diagnostic)
            {
                StartOffset = start,
                Length = length,
            });
        }
    }

    public IEnumerable<Diagnostic> GetDiagnosticsAt(int offset)
    {
        return _segments.FindSegmentsContaining(offset).Select(segment => segment.Diagnostic);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_segments.Count == 0 || !textView.VisualLinesValid || textView.VisualLines.Count == 0)
        {
            return;
        }

        var viewStart = textView.VisualLines[0].FirstDocumentLine.Offset;
        var viewEnd = textView.VisualLines[^1].LastDocumentLine.EndOffset;

        foreach (var segment in _segments.FindOverlappingSegments(viewStart, viewEnd - viewStart))
        {
            var pen = segment.Diagnostic.Severity == DiagnosticSeverity.Error ? _errorPen : _warningPen;

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                drawingContext.DrawGeometry(null, pen, CreateSquiggle(rect));
            }
        }
    }

    private static StreamGeometry CreateSquiggle(Rect rect)
    {
        var geometry = new StreamGeometry();
        var baseline = rect.Bottom - 1;

        using (var context = geometry.Open())
        {
            var x = rect.Left;
            var up = true;
            context.BeginFigure(new Point(x, baseline), false);

            while (x < rect.Right)
            {
                x = Math.Min(x + SquiggleStep, rect.Right);
                context.LineTo(new Point(x, up ? baseline - SquiggleHeight : baseline));
                up = !up;
            }

            context.EndFigure(false);
        }

        return geometry;
    }

    private sealed class DiagnosticSegment(Diagnostic diagnostic) : TextSegment
    {
        public Diagnostic Diagnostic { get; } = diagnostic;
    }
}
