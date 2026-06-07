using CDS.CSharpScript2.Editors;

namespace UnitTests.TestHelpers;

/// <summary>
/// Records editor events for use in unit tests.
/// </summary>
internal sealed class EditorEventRecorder
{
    private readonly List<DiagnosticsUpdatedEventArgs> _diagnosticsUpdatedEvents = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorEventRecorder"/> class.
    /// </summary>
    /// <param name="editor">The editor whose events should be recorded.</param>
    public EditorEventRecorder(IScriptEditor editor)
    {
        if(editor == null) throw new ArgumentNullException(nameof(editor));

        editor.DiagnosticsUpdated += OnDiagnosticsUpdated;
        editor.ScriptChanged += OnScriptChanged;
    }

    /// <summary>
    /// Gets the number of recorded <see cref="IScriptEditor.DiagnosticsUpdated"/> events.
    /// </summary>
    public int DiagnosticsUpdatedCount => _diagnosticsUpdatedEvents.Count;

    /// <summary>
    /// Gets the number of recorded <see cref="IScriptEditor.ScriptChanged"/> events.
    /// </summary>
    public int ScriptChangedCount { get; private set; }

    /// <summary>
    /// Gets the most recently recorded diagnostics event.
    /// </summary>
    public DiagnosticsUpdatedEventArgs? LatestDiagnosticsUpdatedEvent => _diagnosticsUpdatedEvents.LastOrDefault();

    /// <summary>
    /// Clears all recorded events.
    /// </summary>
    public void Reset()
    {
        _diagnosticsUpdatedEvents.Clear();
        ScriptChangedCount = 0;
    }

    /// <summary>
    /// Records a diagnostics-updated event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnDiagnosticsUpdated(object? sender, DiagnosticsUpdatedEventArgs e)
    {
        _diagnosticsUpdatedEvents.Add(e);
    }

    /// <summary>
    /// Records a script-changed event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnScriptChanged(object? sender, EventArgs e)
    {
        ScriptChangedCount++;
    }
}
