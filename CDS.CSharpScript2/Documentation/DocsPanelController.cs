namespace CDS.CSharpScript2.Documentation;

/// <summary>
/// Holds the behaviour of a documentation panel (following the caret, browsing links, history)
/// independently of any UI. It never shows or hides anything itself; a UI renders from <see cref="Changed"/>.
/// </summary>
/// <remarks>
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
    public bool CanGoBack => _historyIndex > 0;

    /// <summary>Whether <see cref="ForwardAsync"/> has a page to go to.</summary>
    public bool CanGoForward => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

    /// <summary>Starts following the caret and shows the page at <paramref name="position"/>.</summary>
    /// <param name="position">The caret offset within the script.</param>
    public async Task ShowAtAsync(int position)
    {
        var (token, request) = BeginRequest();
        var page = await _source.GetDocPageAsync(position, token);
        if (!IsCurrent(request))
        {
            return;
        }

        Apply(DocsPanelState.Following, page ?? CurrentPage, clearHistory: true);
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
        var page = await _source.GetDocPageAsync(position, token);
        if (!IsCurrent(request) || page == null || IsSamePage(page, CurrentPage))
        {
            return;
        }

        Apply(DocsPanelState.Following, page, clearHistory: false);
    }

    /// <summary>Follows a link to <paramref name="target"/>, entering browsing mode.</summary>
    /// <param name="target">The link target.</param>
    public async Task NavigateAsync(DocTarget target)
    {
        var (token, request) = BeginRequest();
        var page = await _source.GetDocPageAsync(target, token);
        if (!IsCurrent(request) || page == null)
        {
            return;
        }

        var before = Snapshot();
        if (_history.Count == 0 && CurrentPage != null)
        {
            _history.Add(CurrentPage);
            _historyIndex = 0;
        }

        if (_historyIndex < _history.Count - 1)
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
        }

        _history.Add(page);
        _historyIndex = _history.Count - 1;
        State = DocsPanelState.Browsing;
        CurrentPage = page;
        RaiseIfChanged(before);
    }

    /// <summary>Goes to the previous page in the history.</summary>
    public Task BackAsync() => StepAsync(-1);

    /// <summary>Goes to the next page in the history.</summary>
    public Task ForwardAsync() => StepAsync(1);

    /// <summary>Closes the panel, clearing the page and history and cancelling any request in flight.</summary>
    public void Close()
    {
        CancelRequest();
        Apply(DocsPanelState.Closed, null, clearHistory: true);
    }

    private async Task StepAsync(int direction)
    {
        var index = _historyIndex + direction;
        if (index < 0 || index >= _history.Count)
        {
            return;
        }

        var stored = _history[index];
        var (token, request) = BeginRequest();
        var page = stored;
        if (stored.Target != null)
        {
            page = await _source.GetDocPageAsync(stored.Target, token) ?? stored;
            if (!IsCurrent(request))
            {
                return;
            }
        }

        var before = Snapshot();
        _history[index] = page;
        _historyIndex = index;
        CurrentPage = page;
        RaiseIfChanged(before);
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

    private void Apply(DocsPanelState state, DocPage? page, bool clearHistory)
    {
        var before = Snapshot();
        if (clearHistory)
        {
            _history.Clear();
            _historyIndex = -1;
        }

        State = state;
        CurrentPage = page;
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
