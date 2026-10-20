using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using AvaloniaEdit.CodeCompletion;
using CDS.CSharpScript2.Classification;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <content>
/// Call tips: the signature, summary, and active-parameter documentation of the method call the
/// caret is in, with overload cycling.
/// </content>
public partial class AvaloniaScriptEditor
{
    private CancellationTokenSource? _callTipCts;
    private CallTipWindow? _callTipWindow;
    private CallTipOverloadProvider? _callTip;

    /// <summary>
    /// Gets a value indicating whether a call tip is currently open.
    /// </summary>
    public bool IsCallTipOpen => _callTipWindow is not null;

    /// <summary>The open call tip's content, if any. Exposed for UI tests.</summary>
    internal CallTipOverloadProvider? ActiveCallTip => _callTip;

    private void InitialiseCallTips()
    {
        var textArea = _textEditor.TextArea;
        textArea.TextEntered += OnTextEnteredForCallTip;
        textArea.AddHandler(KeyDownEvent, OnTextAreaKeyDownForCallTip, RoutingStrategies.Tunnel);
    }

    private void DetachCallTips()
    {
        var textArea = _textEditor.TextArea;
        textArea.TextEntered -= OnTextEnteredForCallTip;
        textArea.RemoveHandler(KeyDownEvent, OnTextAreaKeyDownForCallTip);
    }

    // ── Triggers ─────────────────────────────────────────────────────────────

    private void OnTextAreaKeyDownForCallTip(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
        {
            StartCallTipRequest();
            e.Handled = true;
        }
    }

    private void OnTextEnteredForCallTip(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        switch (e.Text[^1])
        {
            case '(':
                // The completion list has already committed on '(' (a fill-up character); also
                // drop a debounced request still waiting to open one.
                CloseCompletionWindow();
                CancelCompletion();
                StartCallTipRequest();
                break;

            case ',':
                // Moves the highlight on, or brings back a tip that was lost.
                StartCallTipRequest();
                break;

            case ')':
                // This call is finished; show the enclosing call's tip if there is one.
                CloseCallTip();
                StartCallTipRequest();
                break;
        }
    }

    // ── Requests ─────────────────────────────────────────────────────────────

    private void StartCallTipRequest()
    {
        // Each request supersedes the last, so abandon the Roslyn work behind the old one.
        CancelAndDispose(ref _callTipCts);
        _callTipCts = new CancellationTokenSource();
        _ = ShowCallTipAsync(_callTipCts.Token);
    }

    /// <summary>
    /// Finds the argument list the caret is in and shows, updates, or closes the tip to match.
    /// </summary>
    /// <remarks>
    /// One path serves every trigger: when the caret is still in the call the open tip describes,
    /// only the highlighted argument changes and the chosen overload is kept.
    /// </remarks>
    private async Task ShowCallTipAsync(CancellationToken cancellationToken)
    {
        var manager = _manager;
        var stateVersion = _editorStateVersion;

        if (manager is null || _disposed)
        {
            return;
        }

        var script = Script;
        var documentVersion = _documentVersion;
        var caret = _textEditor.CaretOffset;

        bool IsStillCurrent() =>
            !cancellationToken.IsCancellationRequested &&
            IsCurrent(documentVersion, stateVersion) &&
            _textEditor.CaretOffset == caret;

        try
        {
            _managerDocumentVersion = documentVersion;
            await manager.UpdateScriptDocumentAsync(script, cancellationToken);

            var context = await manager.GetCallTipContext(caret, cancellationToken);
            if (!IsStillCurrent())
            {
                return;
            }

            if (context is null)
            {
                CloseCallTip();
                return;
            }

            if (_callTip is { } openTip && openTip.OpenParenPosition == context.OpenParenPosition)
            {
                openTip.ArgumentIndex = context.ArgumentIndex;
                return;
            }

            // API info is resolved at the character just before '(' to land on the method name.
            var apiInfo = await manager.GetAPIInfo(Math.Max(0, context.OpenParenPosition - 1), cancellationToken);
            if (!IsStillCurrent())
            {
                return;
            }

            if (apiInfo?.MemberInfos is not { Count: > 0 } overloads)
            {
                CloseCallTip();
                return;
            }

            OpenCallTip(new CallTipOverloadProvider(overloads, context.OpenParenPosition, context.ArgumentIndex));
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException) when (!IsCurrent(documentVersion, stateVersion))
        {
        }
    }

    // ── Window ───────────────────────────────────────────────────────────────

    private void OpenCallTip(CallTipOverloadProvider callTip)
    {
        CloseCallTip();

        var window = new CallTipWindow(_textEditor.TextArea, callTip.OpenParenPosition)
        {
            Provider = callTip,
        };
        ApplyCallTipTheme(window, EffectiveTheme);

        window.Closed += (_, _) =>
        {
            // AvaloniaEdit also closes the window itself: Escape, the caret leaving the call, or
            // the editor losing focus.
            if (ReferenceEquals(_callTipWindow, window))
            {
                _callTipWindow = null;
                _callTip = null;
            }
        };

        _callTipWindow = window;
        _callTip = callTip;
        window.Show();
    }

    private void CloseCallTip()
    {
        var window = _callTipWindow;
        _callTipWindow = null;
        _callTip = null;
        window?.Hide();
    }

    private void CancelCallTip()
    {
        CancelAndDispose(ref _callTipCts);
        CloseCallTip();
    }

    private void ApplyCallTipTheme(Popup window, EditorTheme theme)
    {
        // Styles on the window win over the app-wide Fluent theme for the same selector.
        window.Styles.Clear();
        window.Styles.Add(new Style(x => x.OfType<OverloadViewer>())
        {
            Setters =
            {
                new Setter(BackgroundProperty, theme.Background.ToBrush()),
                new Setter(ForegroundProperty, theme.Foreground.ToBrush()),
                new Setter(BorderBrushProperty, System.Drawing.Color.FromArgb(96, theme.Foreground).ToBrush()),
                new Setter(BorderThicknessProperty, new Thickness(1)),
                new Setter(PaddingProperty, new Thickness(6, 4)),
                new Setter(FontFamilyProperty, _textEditor.FontFamily),
                new Setter(FontSizeProperty, _textEditor.FontSize),
            },
        });
    }
}
