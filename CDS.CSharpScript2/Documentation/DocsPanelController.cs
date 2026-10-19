namespace CDS.CSharpScript2.Documentation;

/// <summary>
/// Holds the behaviour of a documentation panel (following the caret, browsing links, history)
/// independently of any UI. It never shows or hides anything itself; a UI renders from <see cref="Changed"/>.
/// </summary>
/// <remarks>
/// A request superseded by a newer one (which cancels it), and any other exception thrown by the source,
/// is swallowed: the request returns quietly and the current page and state are kept.
/// Awaits in this class deliberately omit <c>ConfigureAwait(false)</c>, so that <see cref="Changed"/>
/// is raised on the caller's (UI) thread. Call it from the UI thread.
/// </remarks>
public sealed class DocsPanelController
{
    private readonly IDocPageSource _source;
    private readonly List<DocPage> _history = [];
    private int _historyIndex = -1;
    private CancellationTokenSource? _requestCts;

    /// <summary>Creates a controller that reads pages from <paramref name="source"/>.</summary>
    /// <param name="source">The page source, normally an <c>EditorManager</c>.</param>
    public DocsPanelController(IDocPageSource source)
    {
        _source = source;
    }

    /// <summary>Raised when <see cref="State"/>, <see cref="CurrentPage"/>, <see cref="CanGoBack"/> or <see cref="CanGoForward"/> changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The current mode.</summary>
    public DocsPanelState State { get; private set; } = DocsPanelState.Closed;

    /// <summary>The page being shown, or <see langword="null"/> when there is none.</summary>
    public DocPage? CurrentPage { get; private set; }

    /// <summary>Whether <see cref="BackAsync"/> has a page to go to.</summary>
    public bool CanGoBack => State == DocsPanelState.Following ? _history.Count > 0 : _historyIndex > 0;

    /// <summary>Whether <see cref="ForwardAsync"/> has a page to go to.</summary>
    public bool CanGoForward => State == DocsPanelState.Browsing && _historyIndex < _history.Count - 1;

    /// <summary>Starts following the caret and shows the page at <paramref name="position"/>. History is kept.</summary>
    /// <param name="position">The caret offset within the script.</param>
    public async Task ShowAtAsync(int position)
    {
        var (token, request) = BeginRequest();
        var (ok, page) = await FetchAsync(() => _source.GetDocPageAsync(position, token));
        if (!ok || !IsCurrent(request))
        {
            return;
        }

        var before = Snapshot();
        if (State == DocsPanelState.Browsing && _historyIndex >= 0 && _historyIndex < _history.Count - 1)
        {
            // Following has no forward history.
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
        }

        State = DocsPanelState.Following;
        CurrentPage = page ?? CurrentPage;
        RaiseIfChanged(before);
    }

    /// <summary>Updates the page for a caret move; ignored unless the panel is following.</summary>
    /// <param name="position">The caret offset within the script.</param>
    public async Task CaretMovedAsync(int position)
    {
        if (State != DocsPanelState.Following)
        {
            return;
        }

        var (token, request) = BeginRequest();
        var (ok, page) = await FetchAsync(() => _source.GetDocPageAsync(position, token));
        if (!ok || !IsCurrent(request) || page == null || IsSamePage(page, CurrentPage))
        {
            return;
        }

        var before = Snapshot();
        CurrentPage = page;
        RaiseIfChanged(before);
    }

    /// <summary>
    /// Follows a link to <paramref name="target"/>. The panel enters browsing mode immediately, so caret
    /// moves while the link loads are ignored.
    /// </summary>
    /// <param name="target">The link target.</param>
    public async Task NavigateAsync(DocTarget target)
    {
        var wasFollowing = State == DocsPanelState.Following;
        var (token, request) = BeginRequest();
        Apply(DocsPanelState.Browsing);

        var (ok, page) = await FetchAsync(() => _source.GetDocPageAsync(target, token));
        if (!ok || !IsCurrent(request) || page == null)
        {
            return;
        }

        var before = Snapshot();
        if (wasFollowing || _history.Count == 0)
        {
            if (wasFollowing)
            {
                TruncateAfter(_historyIndex);
            }

            if (CurrentPage != null)
            {
                _history.Add(CurrentPage);
            }
        }
        else
        {
            TruncateAfter(_historyIndex);
        }

        _history.Add(page);
        _historyIndex = _history.Count - 1;
        CurrentPage = page;
        RaiseIfChanged(before);
    }

    /// <summary>Goes to the previous page in the history; from following, shows the most recent entry and enters browsing mode.</summary>
    public Task BackAsync()
        => StepAsync(State == DocsPanelState.Following ? _history.Count - 1 : _historyIndex - 1);

    /// <summary>Goes to the next page in the history.</summary>
    public Task ForwardAsync() => StepAsync(CanGoForward ? _historyIndex + 1 : -1);

    /// <summary>Closes the panel, clearing the page and history and cancelling any request in flight.</summary>
    public void Close()
    {
        CancelRequest();
        var before = Snapshot();
        _history.Clear();
        _historyIndex = -1;
        State = DocsPanelState.Closed;
        CurrentPage = null;
        RaiseIfChanged(before);
    }

    private void TruncateAfter(int index)
    {
        if (index < _history.Count - 1)
        {
            _history.RemoveRange(index + 1, _history.Count - index - 1);
        }
    }

    private async Task StepAsync(int index)
    {
        if (index < 0 || index >= _history.Count)
        {
            return;
        }

        var stored = _history[index];
        var (token, request) = BeginRequest();
        var page = stored;
        if (stored.Target != null)
        {
            var (ok, fetched) = await FetchAsync(() => _source.GetDocPageAsync(stored.Target, token));
            if (!ok || !IsCurrent(request))
            {
                return;
            }

            page = fetched ?? stored;
        }

        var before = Snapshot();
        _history[index] = page;
        _historyIndex = index;
        State = DocsPanelState.Browsing;
        CurrentPage = page;
        RaiseIfChanged(before);
    }

    /// <summary>
    /// Runs a source request. A cancelled (superseded) request or any other source failure is not
    /// propagated: it reports <c>Ok = false</c> and the caller keeps the current page and state.
    /// </summary>
    private static async Task<(bool Ok, DocPage? Page)> FetchAsync(Func<Task<DocPage?>> fetch)
    {
        try
        {
            return (true, await fetch());
        }
        catch (OperationCanceledException)
        {
            return (false, null);
        }
        catch (Exception)
        {
            return (false, null);
        }
    }

    private (CancellationToken Token, CancellationTokenSource Request) BeginRequest()
    {
        CancelRequest();
        _requestCts = new CancellationTokenSource();
        return (_requestCts.Token, _requestCts);
    }

    private void CancelRequest()
    {
        _requestCts?.Cancel();
        _requestCts = null;
    }

    private bool IsCurrent(CancellationTokenSource request) => ReferenceEquals(_requestCts, request);

    private static bool IsSamePage(DocPage a, DocPage? b)
        => b != null && Equals(a.Target, b.Target) && a.Title == b.Title && a.Signature == b.Signature;

    private void Apply(DocsPanelState state)
    {
        var before = Snapshot();
        State = state;
        RaiseIfChanged(before);
    }

    private (DocsPanelState State, DocPage? Page, bool Back, bool Forward) Snapshot()
        => (State, CurrentPage, CanGoBack, CanGoForward);

    private void RaiseIfChanged((DocsPanelState State, DocPage? Page, bool Back, bool Forward) before)
    {
        if (before != Snapshot())
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
