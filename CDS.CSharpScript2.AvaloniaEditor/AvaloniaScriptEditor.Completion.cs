using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using CDS.CSharpScript2.Classification;
using CDS.CSharpScript2.CodeCompletion;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <content>
/// Code completion: the Roslyn-backed completion list and its triggers.
/// </content>
public partial class AvaloniaScriptEditor
{
    private static readonly TimeSpan CompletionDelay = TimeSpan.FromMilliseconds(150);

    // Typing one of these while the list is open commits the highlighted entry and then inserts
    // the character itself, matching Visual Studio. The closing bracket enclosing the caret is
    // added per session — see ShowCompletionAsync.
    private const string CompletionFillUpCharacters = ".([;";

    private DispatcherTimer _completionTimer = null!;
    private CancellationTokenSource? _completionCts;
    private CompletionWindow? _completionWindow;
    private char? _completionClosingBracket;

    /// <summary>
    /// Gets a value indicating whether the completion list is currently open.
    /// </summary>
    public bool IsCompletionListOpen => _completionWindow is not null;

    /// <summary>The open completion window, if any. Exposed for UI tests.</summary>
    internal CompletionWindow? ActiveCompletionWindow => _completionWindow;

    private void InitialiseCompletion()
    {
        _completionTimer = new DispatcherTimer { Interval = CompletionDelay };
        _completionTimer.Tick += OnCompletionTimerTick;

        var textArea = _textEditor.TextArea;
        textArea.TextEntering += OnTextEntering;
        textArea.TextEntered += OnTextEntered;
        textArea.AddHandler(KeyDownEvent, OnTextAreaKeyDown, RoutingStrategies.Tunnel);
        _textEditor.Document.Changed += OnDocumentChangedForCompletion;
    }

    private void DetachCompletion()
    {
        var textArea = _textEditor.TextArea;
        textArea.TextEntering -= OnTextEntering;
        textArea.TextEntered -= OnTextEntered;
        textArea.RemoveHandler(KeyDownEvent, OnTextAreaKeyDown);
        _textEditor.Document.Changed -= OnDocumentChangedForCompletion;
    }

    // ── Triggers ─────────────────────────────────────────────────────────────

    private void OnTextAreaKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.Control)
        {
            CloseCompletionWindow();
            StartCompletionSession(immediate: true);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Runs before typed text is inserted: commits or dismisses an open list.
    /// </summary>
    private void OnTextEntering(object? sender, TextInputEventArgs e)
    {
        if (_completionWindow is null || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        var ch = e.Text[0];
        if (IsIdentifierCharacter(ch))
        {
            return;
        }

        var isFillUp = CompletionFillUpCharacters.IndexOf(ch) >= 0 || ch == _completionClosingBracket;
        if (isFillUp && _completionWindow.CompletionList.SelectedItem is not null)
        {
            // Inserts the entry; the typed character then follows as normal because the event
            // is left unhandled.
            _completionWindow.CompletionList.RequestInsertion(e);
        }
        else
        {
            CloseCompletionWindow();
            CancelCompletion();
        }
    }

    /// <summary>
    /// Runs after typed text is inserted: starts a completion request where one is wanted.
    /// </summary>
    private void OnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        var ch = e.Text[^1];

        if (ch == '.')
        {
            // Member access — start a fresh list immediately.
            CloseCompletionWindow();
            StartCompletionSession(immediate: true);
        }
        else if (char.IsLetter(ch) || ch == '_' || (_completionWindow is not null && char.IsDigit(ch)))
        {
            // Starting or extending an identifier. Re-requested on every keystroke (debounced)
            // because the core re-filters and re-ranks per request: substring and word-initial
            // matches must stay offered as the user types on.
            StartCompletionSession(immediate: false);
        }
    }

    private void OnDocumentChangedForCompletion(object? sender, DocumentChangeEventArgs e)
    {
        // Backspacing inside the word an open list is filtering: re-filter for the shorter text.
        // Deferred because Changed fires mid-edit, before the caret has moved back. AvaloniaEdit
        // also closes the list itself once the edit completes, so the check afterwards is not
        // "is the list still open" but "is the caret still inside a word" — the way the Scintilla
        // editor reopens after a deleted character.
        if (_completionWindow is not null && e.RemovalLength > 0 && e.InsertionLength == 0)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_disposed && IsCaretAfterIdentifierCharacter())
                {
                    StartCompletionSession(immediate: true);
                }
            });
        }
    }

    private bool IsCaretAfterIdentifierCharacter()
    {
        var caret = _textEditor.CaretOffset;
        return caret > 0 && IsIdentifierCharacter(_textEditor.Document.GetCharAt(caret - 1));
    }

    private static bool IsIdentifierCharacter(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_';
    }

    // ── Requests ─────────────────────────────────────────────────────────────

    /// <remarks>
    /// The debounce is a restarted timer rather than a cancelled delay, so a superseded request
    /// simply never begins instead of throwing a cancellation per keystroke.
    /// </remarks>
    private void StartCompletionSession(bool immediate)
    {
        _completionTimer.Stop();

        if (immediate)
        {
            BeginCompletionRequest();
        }
        else
        {
            _completionTimer.Start();
        }
    }

    private void OnCompletionTimerTick(object? sender, EventArgs e)
    {
        _completionTimer.Stop();
        BeginCompletionRequest();
    }

    private void BeginCompletionRequest()
    {
        CancelAndDispose(ref _completionCts);
        _completionCts = new CancellationTokenSource();
        _ = ShowCompletionAsync(_completionCts.Token);
    }

    private void CancelCompletion()
    {
        _completionTimer.Stop();
        _completionCts?.Cancel();
    }

    /// <summary>
    /// Fetches completions for the caret position and shows or refreshes the list.
    /// </summary>
    private async Task ShowCompletionAsync(CancellationToken cancellationToken)
    {
        var manager = _manager;
        var stateVersion = _editorStateVersion;

        if (manager is null || _disposed || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var script = Script;
        var documentVersion = _documentVersion;
        var caret = _textEditor.CaretOffset;

        try
        {
            // Keep the Roslyn document current without paying for a full diagnostics pass.
            _managerDocumentVersion = documentVersion;
            await manager.UpdateScriptDocumentAsync(script, cancellationToken);

            var completions = (await manager.GetAutoCompletions(caret, cancellationToken)).ToList();

            // Drop the result if anything moved on while Roslyn was working.
            if (cancellationToken.IsCancellationRequested ||
                !IsCurrent(documentVersion, stateVersion) ||
                _textEditor.CaretOffset != caret)
            {
                return;
            }

            if (completions.Count == 0)
            {
                CloseCompletionWindow();
                return;
            }

            // Roslyn's span covers the part of the identifier already typed (empty after a dot).
            var wordStart = Math.Min(completions[0].Span.Start, caret);
            _completionClosingBracket = EnclosingBracket.GetClosingCharacter(script, caret);

            var window = _completionWindow ?? OpenCompletionWindow();
            window.StartOffset = wordStart;
            window.EndOffset = caret;

            var list = window.CompletionList;
            var data = list.CompletionData;
            data.Clear();
            foreach (var item in completions)
            {
                data.Add(new RoslynCompletionData(item));
            }

            // CompletionData is a plain List, not an observable collection, and with filtering off
            // AvaloniaEdit binds its list box to it once, in OnApplyTemplate. Entries added after
            // that never reach the list box — the popup renders empty — so re-point the list box
            // at the refilled list to make it re-read it.
            if (list.ListBox is { } listBox)
            {
                listBox.ItemsSource = null;
                listBox.ItemsSource = data;
            }

            // The list is already ranked best match first, so highlight the top entry.
            list.SelectedItem = data[0];
            list.ScrollIntoView(data[0]);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException) when (!IsCurrent(documentVersion, stateVersion))
        {
        }
    }

    // ── Window ───────────────────────────────────────────────────────────────

    private CompletionWindow OpenCompletionWindow()
    {
        var window = new CompletionWindow(_textEditor.TextArea)
        {
            CloseAutomatically = true,
            MinWidth = 260,
        };

        // Ranking is the core's job (CompletionMatcher); AvaloniaEdit's own filter would drop or
        // reorder entries the core deliberately kept.
        window.CompletionList.IsFiltering = false;
        window.CompletionList.FontFamily = _textEditor.FontFamily;
        window.CompletionList.FontSize = _textEditor.FontSize;
        ApplyCompletionTheme(window, EffectiveTheme);

        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_completionWindow, window))
            {
                _completionWindow = null;
            }
        };

        _completionWindow = window;
        window.Show();
        return window;
    }

    private void CloseCompletionWindow()
    {
        var window = _completionWindow;
        _completionWindow = null;
        window?.Close();
    }

    private static void ApplyCompletionTheme(CompletionWindow window, EditorTheme theme)
    {
        var list = window.CompletionList;
        list.Background = theme.Background.ToBrush();
        list.Foreground = theme.Foreground.ToBrush();

        var selectedBackground = theme.AutocompleteSelectedBackground.ToBrush();
        var selectedForeground = new SolidColorBrush(Colors.White);

        // Styles on the window win over the app-wide Fluent theme for the same selector.
        window.Styles.Clear();
        window.Styles.Add(new Style(x => x
            .OfType<ListBoxItem>().Class(":selected")
            .Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
        {
            Setters =
            {
                new Setter(ContentPresenter.BackgroundProperty, selectedBackground),
                new Setter(ContentPresenter.ForegroundProperty, selectedForeground),
            },
        });
    }
}
