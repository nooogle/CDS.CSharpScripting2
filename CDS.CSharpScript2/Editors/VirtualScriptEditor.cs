using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using System.Collections.Immutable;

namespace CDS.CSharpScript2.Editors;

/// <summary>
/// Provides a non-visual script editor implementation for unit tests and other headless scenarios.
/// </summary>
/// <remarks>
/// This type simulates script editing operations while reusing <see cref="EditorManager"/> for
/// analysis, completions, API info, and compilation.
/// </remarks>
public class VirtualScriptEditor : IScriptEditor
{
    private string _script = string.Empty;
    private ImmutableArray<Diagnostic> _currentDiagnostics = [];
    private IReadOnlyList<Classification.ClassifiedSymbol> _currentClassifications = [];
    private ExecutableScript? _currentCompiledScript;
    private EditorManager? _manager;
    private ScriptEnvironment? _environment;
    private bool _analysisIsCurrent;

    /// <inheritdoc/>
    public event EventHandler<DiagnosticsUpdatedEventArgs>? DiagnosticsUpdated;

    /// <inheritdoc/>
    public event EventHandler? ScriptChanged;

    /// <inheritdoc/>
    public ScriptEnvironment? Environment
    {
        get => _environment;
        set
        {
            _manager?.Dispose();
            _environment = value;
            _manager = value is null ? null : new EditorManager(value);
            CaretPosition = Math.Min(CaretPosition, _script.Length);
            ResetAnalysisState();
        }
    }

    /// <inheritdoc/>
    public string Script
    {
        get => _script;
        set
        {
            _script = value ?? string.Empty;
            CaretPosition = Math.Min(CaretPosition, _script.Length);
            ResetAnalysisState();
        }
    }

    /// <inheritdoc/>
    public bool HasErrors =>
        _currentDiagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

    /// <inheritdoc/>
    public IReadOnlyList<Diagnostic> CurrentDiagnostics => _currentDiagnostics;

    /// <inheritdoc/>
    public ExecutableScript? CurrentCompiledScript => _currentCompiledScript;

    /// <inheritdoc/>
    public EditorManager? Manager => _manager;

    /// <summary>
    /// Gets the current caret position within <see cref="Script"/>.
    /// </summary>
    public int CaretPosition { get; private set; }

    /// <summary>
    /// Gets the classifications from the most recent analysis pass.
    /// </summary>
    public IReadOnlyList<Classification.ClassifiedSymbol> CurrentClassifications => _currentClassifications;

    /// <summary>
    /// Replaces the entire script text, moves the caret to the end, and performs analysis.
    /// </summary>
    /// <param name="script">The new script text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        Script = script;
        CaretPosition = _script.Length;
        await ApplyAnalysisAsync(raiseEvents: true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Simulates typing text at the current caret position.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task TypeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        foreach (var character in text)
        {
            InsertText(character.ToString());
            await ApplyAnalysisAsync(raiseEvents: true, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Simulates backspace key presses immediately before the current caret position.
    /// </summary>
    /// <param name="count">The number of characters to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task BackspaceAsync(int count = 1, CancellationToken cancellationToken = default)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        for (var i = 0; i < count && CaretPosition > 0; i++)
        {
            _script = _script.Remove(CaretPosition - 1, 1);
            CaretPosition--;
            ResetAnalysisState();
            await ApplyAnalysisAsync(raiseEvents: true, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Moves the caret to the specified character position.
    /// </summary>
    /// <param name="position">The zero-based caret position.</param>
    public void MoveCaretTo(int position)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        if (position > _script.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        CaretPosition = position;
    }

    /// <summary>
    /// Returns code completion suggestions at the current caret position.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completion items returned by Roslyn.</returns>
    public async Task<ImmutableArray<CompletionItem>> GetCompletionsAsync(CancellationToken cancellationToken = default)
    {
        EnsureManager();
        await EnsureAnalysisCurrentAsync(cancellationToken).ConfigureAwait(false);

        var completions = await _manager!
            .GetAutoCompletions(CaretPosition)
            .ConfigureAwait(false);

        return completions.ToImmutableArray();
    }

    /// <summary>
    /// Returns API information at the current caret position.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The API information for the current caret position, if any.</returns>
    public async Task<APIInfo.APIInfoResult?> GetAPIInfoAsync(CancellationToken cancellationToken = default)
    {
        EnsureManager();
        await EnsureAnalysisCurrentAsync(cancellationToken).ConfigureAwait(false);

        return await _manager!
            .GetAPIInfo(CaretPosition)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ExecutableScript> CompileAsync(CancellationToken cancellationToken = default)
    {
        EnsureManager();
        await EnsureAnalysisCurrentAsync(cancellationToken).ConfigureAwait(false);

        _currentCompiledScript = await _manager!
            .CompileAsync(cancellationToken)
            .ConfigureAwait(false);

        return _currentCompiledScript;
    }

    /// <summary>
    /// Performs analysis for the current script text and raises editor events.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task RefreshAnalysisAsync(CancellationToken cancellationToken = default)
    {
        EnsureManager();
        await ApplyAnalysisAsync(raiseEvents: true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _manager?.Dispose();
        _manager = null;
    }

    /// <summary>
    /// Inserts text at the current caret position.
    /// </summary>
    /// <param name="text">The text to insert.</param>
    private void InsertText(string text)
    {
        _script = _script.Insert(CaretPosition, text);
        CaretPosition += text.Length;
        ResetAnalysisState();
    }

    /// <summary>
    /// Resets cached analysis and compilation state.
    /// </summary>
    private void ResetAnalysisState()
    {
        _currentDiagnostics = [];
        _currentClassifications = [];
        _currentCompiledScript = null;
        _analysisIsCurrent = false;
    }

    /// <summary>
    /// Ensures that the current script has been analysed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task EnsureAnalysisCurrentAsync(CancellationToken cancellationToken)
    {
        if (_analysisIsCurrent)
        {
            return;
        }

        await ApplyAnalysisAsync(raiseEvents: false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies Roslyn analysis to the current script.
    /// </summary>
    /// <param name="raiseEvents">True to raise editor events after analysis.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task ApplyAnalysisAsync(bool raiseEvents, CancellationToken cancellationToken)
    {
        EnsureManager();

        cancellationToken.ThrowIfCancellationRequested();
        await _manager!
            .ApplyScript(_script)
            .ConfigureAwait(false);

        _currentDiagnostics = _manager.LastDiagnostics;
        _currentClassifications = _manager.LastClassifications;
        _currentCompiledScript = null;
        _analysisIsCurrent = true;

        if (!raiseEvents)
        {
            return;
        }

        DiagnosticsUpdated?.Invoke(this, new DiagnosticsUpdatedEventArgs(_currentDiagnostics));
        ScriptChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Validates that the scripting environment has been configured.
    /// </summary>
    private void EnsureManager()
    {
        if (_manager is null)
        {
            throw new InvalidOperationException($"{nameof(Environment)} must be set before using the editor.");
        }
    }
}
