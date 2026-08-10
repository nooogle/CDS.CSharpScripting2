using CDS.CSharpScript2;
using CDS.CSharpScript2.Editors;

namespace ConsoleTest.Demos.Completions;

/// <summary>
/// Simulates a user typing a line of code and times how long each completion trigger takes to
/// return a list, using the same immediate ('.') / debounced (150ms after a letter) trigger
/// policy that <c>ScintillaScriptEditor</c> uses in production.
/// </summary>
/// <remarks>
/// Drives <see cref="EditorManager"/> directly rather than <c>VirtualScriptEditor</c>:
/// <c>VirtualScriptEditor.TypeTextAsync</c> runs a full diagnostics-and-classification pass on
/// every keystroke, which is heavier and un-debounced compared to what the shipped editor
/// actually does for a completion request (<c>UpdateScriptDocumentAsync</c> plus the trigger
/// policy replicated below). See optimise.md for the production design this mirrors.
/// </remarks>
class TypingSessionDemo
{
    public static string Name => "Typing session (responsiveness)";
    public static string Description => "Simulates typing a line of code with the Scintilla editor's own completion trigger policy, and times each trigger.";

    private const int CompletionDebounceMs = 150;
    private const int TypingDelayMs = 45;

    public static void Run() => new TypingSessionDemo().RunAsync().Wait();

    private readonly object _consoleLock = new();
    private readonly TimedConsoleLogger _logger = new();
    private readonly List<Task> _pendingRequests = [];

    private string _script = string.Empty;
    private int _caret;
    private bool _completionListShown;
    private bool _atLineStart = true;
    private CancellationTokenSource? _debounceCts;
    private EditorManager? _manager;

    public async Task RunAsync()
    {
        Console.Clear();
        Console.WriteLine("Typing session - completion responsiveness");
        Console.WriteLine("============================================\n");

        using var manager = new EditorManager(ScriptEnvironment.Default);
        _manager = manager;

        WriteLog("Warming up (the first analysis pass builds the workspace and is always slow - excluded from the timings below)");
        await manager.ApplyScript(string.Empty);

        WriteLog("Typing 'Console.WriteLine(msg)' at ~45ms/keystroke.");
        WriteLog("The first letter of a new word triggers a completion request after a 150ms pause; '.' triggers one immediately - exactly as ScintillaScriptEditor does. Once a list is showing, further letters just filter it locally (no new request) until a non-identifier character dismisses it.");
        WriteLog("Run twice in this process: pass 1 pays whatever one-off JIT/warm-up cost the completion service itself has never paid before; pass 2 shows steady-state latency.\n");

        WriteLog("--- Pass 1 (cold) ---\n");
        await RunTypingPassAsync();

        WriteLog("\n--- Pass 2 (warm) ---\n");
        await RunTypingPassAsync();

        NewLineIfNeeded();
        Console.WriteLine("\nDone - press any key to return to the menu.");
        Console.ReadKey(intercept: true);
    }

    private async Task RunTypingPassAsync()
    {
        _script = string.Empty;
        _caret = 0;
        _completionListShown = false;
        _debounceCts?.Cancel();
        _debounceCts = null;

        await TypeAsync("C");
        await _pendingRequests[^1]; // let the debounced request fully settle before continuing
        await TypeAsync("onsole.");
        await _pendingRequests[^1]; // let the immediate '.' request fully settle before continuing
        await TypeAsync("WriteLine(msg)");

        await Task.WhenAll(_pendingRequests);
    }

    private async Task TypeAsync(string text)
    {
        foreach (var ch in text)
        {
            _script = _script.Insert(_caret, ch.ToString());
            _caret++;

            lock (_consoleLock)
            {
                Console.Write(ch);
                _atLineStart = false;
            }

            HandleChar(ch);

            await Task.Delay(TypingDelayMs);
        }
    }

    /// <summary>Mirrors the branch that matters here from <c>ScintillaScriptEditor.scintilla_CharAdded</c>.</summary>
    private void HandleChar(char ch)
    {
        if (ch == '.')
        {
            _debounceCts?.Cancel();
            _completionListShown = false;
            _pendingRequests.Add(RequestCompletionAsync("'.' (immediate)"));
        }
        else if (char.IsLetter(ch) || ch == '_')
        {
            if (!_completionListShown)
            {
                RestartDebounce();
            }
        }
        else
        {
            // Non-identifier character: dismiss, same as AutoCCancel/CancelCompletion in production.
            _debounceCts?.Cancel();
            _completionListShown = false;
        }
    }

    private void RestartDebounce()
    {
        _debounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _debounceCts = cts;

        _pendingRequests.Add(RunDebouncedAsync(cts));
    }

    private async Task RunDebouncedAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(CompletionDebounceMs, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!ReferenceEquals(_debounceCts, cts))
        {
            return;
        }

        await RequestCompletionAsync("150ms pause after a letter");
    }

    private async Task RequestCompletionAsync(string triggerLabel)
    {
        var manager = _manager!;
        var script = _script;
        var caret = _caret;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await manager.UpdateScriptDocumentAsync(script);
        var completions = (await manager.GetAutoCompletions(caret)).ToList();
        stopwatch.Stop();

        _completionListShown = completions.Count > 0;

        var preview = string.Join(", ", completions.Take(5).Select(c => c.DisplayText));
        WriteLog($"[{triggerLabel}] {completions.Count} completions in {stopwatch.ElapsedMilliseconds} ms - {preview}");
    }

    private void WriteLog(string message)
    {
        lock (_consoleLock)
        {
            if (!_atLineStart)
            {
                Console.WriteLine();
            }

            _logger.Log(message);
            _atLineStart = true;
        }
    }

    private void NewLineIfNeeded()
    {
        lock (_consoleLock)
        {
            if (!_atLineStart)
            {
                Console.WriteLine();
                _atLineStart = true;
            }
        }
    }
}
