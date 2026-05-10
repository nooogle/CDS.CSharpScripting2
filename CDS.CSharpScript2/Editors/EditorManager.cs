using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using System.Collections.Immutable;

namespace CDS.CSharpScript2.Editors;

/// <summary>
/// Orchestrates Roslyn analysis and compilation for an embedded script editor.
/// Each editor control owns one instance; host applications access this via
/// <see cref="IScriptEditor.Manager"/> only when advanced APIs are needed.
/// </summary>
/// <remarks>
/// This class is not thread-safe. All methods must be called from a single thread
/// (typically the UI thread). Dispose when the owning editor is disposed.
/// </remarks>
public class EditorManager : IDisposable
{
    private ScriptContext? _context;
    private ExecutableScript? _cachedExecutableScript;
    private readonly ScriptEnvironment _environment;

    private ImmutableArray<Diagnostic> _lastDiagnostics = [];
    private IReadOnlyList<Classification.ClassifiedSymbol> _lastClassifications = [];

    /// <summary>True once the script context has been initialised (after the first <see cref="ApplyScript"/> call).</summary>
    public bool IsReady => _context != null;

    /// <summary>Diagnostics from the most recent <see cref="ApplyScript"/> call.</summary>
    public ImmutableArray<Diagnostic> LastDiagnostics => _lastDiagnostics;

    /// <summary>Classifications from the most recent <see cref="ApplyScript"/> call.</summary>
    public IReadOnlyList<Classification.ClassifiedSymbol> LastClassifications => _lastClassifications;

    /// <summary>Initialises a new manager for the given scripting environment.</summary>
    public EditorManager(ScriptEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Analyses the script text and stores fresh diagnostics and classifications.
    /// Awaiting this method returns on the calling synchronisation context, so
    /// callers on the UI thread remain on the UI thread after the await.
    /// Does NOT compile for execution — call <see cref="CompileAsync"/> explicitly.
    /// </summary>
    public async Task ApplyScript(string script)
    {
        await EnsureContext().ConfigureAwait(false);

        _context = _context!.ApplyScript(script);
        _cachedExecutableScript = null;

        var analyser = new ScriptAnalyser(_context);
        _lastDiagnostics = await analyser.GetDiagnosticsAsync().ConfigureAwait(false);
        _lastClassifications = await analyser.GetClassificationsAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Compiles the current script and returns the result ready for execution.
    /// The result is cached and reused on subsequent calls until the script changes.
    /// </summary>
    public async Task<ExecutableScript> CompileAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedExecutableScript != null)
            return _cachedExecutableScript;

        await EnsureContext().ConfigureAwait(false);
        _cachedExecutableScript = await new ScriptExecutor(_context!).CompileAsync(cancellationToken).ConfigureAwait(false);
        return _cachedExecutableScript;
    }

    /// <summary>Returns code completion suggestions at the given cursor position.</summary>
    public async Task<IEnumerable<CompletionItem>> GetAutoCompletions(int cursorPosition)
    {
        await EnsureContext().ConfigureAwait(false);
        return await new ScriptAnalyser(_context!).GetCompletionsAsync(cursorPosition).ConfigureAwait(false);
    }

    /// <summary>Returns API info (type, overloads, XML docs) at the given cursor position.</summary>
    public async Task<APIInfo.IAPIInfoResult> GetAPIInfo(int cursorPosition)
    {
        await EnsureContext().ConfigureAwait(false);
        return (await new ScriptAnalyser(_context!).GetAPIInfoAsync(cursorPosition).ConfigureAwait(false))!;
    }

    /// <summary>Returns the syntax tree for the current script.</summary>
    public async Task<SyntaxTree?> GetSyntaxTreeAsync()
    {
        await EnsureContext().ConfigureAwait(false);
        return await new ScriptAnalyser(_context!).GetSyntaxTreeAsync().ConfigureAwait(false);
    }

    /// <summary>Returns the semantic model for the current script.</summary>
    public async Task<SemanticModel?> GetSemanticModelAsync()
    {
        await EnsureContext().ConfigureAwait(false);
        return await new ScriptAnalyser(_context!).GetSemanticModelAsync().ConfigureAwait(false);
    }

    /// <summary>Returns classified symbol spans for the current script.</summary>
    public async Task<IReadOnlyList<Classification.ClassifiedSymbol>> GetClassificationsAsync()
    {
        await EnsureContext().ConfigureAwait(false);
        return await new ScriptAnalyser(_context!).GetClassificationsAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _context?.Dispose();
        _context = null;
    }

    private async Task EnsureContext()
    {
        _context ??= await ScriptContext.CreateAsync(_environment).ConfigureAwait(false);
    }
}
