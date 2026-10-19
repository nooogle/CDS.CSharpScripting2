using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using CDS.CSharpScript2.APIInfo;
using CDS.CSharpScript2.Classification;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <content>
/// The hover popup: diagnostics and compact quick info for whatever the pointer rests on.
/// </content>
public partial class AvaloniaScriptEditor
{
    private CancellationTokenSource? _hoverCts;
    private Style? _hoverStyle;

    /// <summary>
    /// How many hover requests have run to completion, shown or not. Exposed so UI tests can wait
    /// for a request that rightly shows nothing.
    /// </summary>
    internal int CompletedHoverRequests { get; private set; }

    private void InitialiseHover()
    {
        _textEditor.PointerHover += OnPointerHover;
        _textEditor.PointerHoverStopped += OnPointerHoverStopped;
        _textEditor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChangedForHover;
    }

    private void DetachHover()
    {
        _textEditor.PointerHover -= OnPointerHover;
        _textEditor.PointerHoverStopped -= OnPointerHoverStopped;
        _textEditor.TextArea.TextView.ScrollOffsetChanged -= OnScrollOffsetChangedForHover;
    }

    private void OnPointerHover(object? sender, PointerEventArgs e)
    {
        var offset = GetOffsetUnderPointer(e);
        if (offset is null)
        {
            return;
        }

        CancelAndDispose(ref _hoverCts);
        _hoverCts = new CancellationTokenSource();
        _ = ShowHoverAsync(offset.Value, _hoverCts.Token);
        e.Handled = true;
    }

    private void OnPointerHoverStopped(object? sender, PointerEventArgs e) => CloseHover();

    private void OnScrollOffsetChangedForHover(object? sender, EventArgs e) => CloseHover();

    /// <summary>
    /// The offset of the character the pointer is over, or <see langword="null"/> when it is
    /// outside the text.
    /// </summary>
    /// <remarks>
    /// Floors rather than rounds: <see cref="AvaloniaEdit.TextEditor.GetPositionFromPoint"/> rounds
    /// to the nearest boundary between characters, so resting on the right half of a word's last
    /// character — or anywhere past the end of a line — would land just after the word and still
    /// describe it.
    /// </remarks>
    private int? GetOffsetUnderPointer(PointerEventArgs e)
    {
        var textView = _textEditor.TextArea.TextView;
        var position = textView.GetPositionFloor(e.GetPosition(textView) + textView.ScrollOffset);
        return position is null ? null : _textEditor.Document.GetOffset(position.Value.Location);
    }

    private async Task ShowHoverAsync(int offset, CancellationToken cancellationToken)
    {
        var manager = _manager;
        var stateVersion = _editorStateVersion;
        var documentVersion = _documentVersion;

        var diagnostics = _diagnosticRenderer.GetDiagnosticsAt(offset)
            .Select(d => $"{d.Id}: {d.GetMessage()}")
            .ToList();

        QuickInfo? quickInfo = null;

        try
        {
            if (manager is not null && !_disposed)
            {
                await EnsureManagerDocumentCurrentAsync(cancellationToken);

                quickInfo = await manager.GetQuickInfoAsync(offset, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ObjectDisposedException) when (!IsCurrent(documentVersion, stateVersion))
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested || !IsCurrent(documentVersion, stateVersion))
        {
            return;
        }

        // Roslyn also answers for a position just after a word; only describe the word the pointer
        // is actually on.
        if (quickInfo is not null && (offset < quickInfo.Start || offset >= quickInfo.Start + quickInfo.Length))
        {
            quickInfo = null;
        }

        CompletedHoverRequests++;

        var view = new HoverInfoView(
            diagnostics,
            quickInfo,
            _textEditor.FontFamily,
            HoverBorderColor(EffectiveTheme).ToBrush());
        if (view.IsEmpty)
        {
            return;
        }

        ToolTip.SetTip(_textEditor, view);
        ToolTip.SetIsOpen(_textEditor, true);
    }

    private void CloseHover()
    {
        CancelAndDispose(ref _hoverCts);
        ToolTip.SetIsOpen(_textEditor, false);
        ToolTip.SetTip(_textEditor, null);
    }

    private static System.Drawing.Color HoverBorderColor(EditorTheme theme)
    {
        return System.Drawing.Color.FromArgb(96, theme.Foreground);
    }

    private void ApplyHoverTheme(EditorTheme theme)
    {
        // The tooltip's logical parent is the text editor, so a style on this control reaches it
        // and wins over the app-wide Fluent theme. Matches the completion description panel.
        if (_hoverStyle is not null)
        {
            Styles.Remove(_hoverStyle);
        }

        _hoverStyle = new Style(x => x.OfType<ToolTip>())
        {
            Setters =
            {
                new Setter(BackgroundProperty, theme.Background.ToBrush()),
                new Setter(ForegroundProperty, theme.Foreground.ToBrush()),
                new Setter(BorderBrushProperty, HoverBorderColor(theme).ToBrush()),
                new Setter(BorderThicknessProperty, new Thickness(1)),
                new Setter(PaddingProperty, new Thickness(8, 6)),
                new Setter(MaxWidthProperty, double.PositiveInfinity),
            },
        };
        Styles.Add(_hoverStyle);
    }
}
