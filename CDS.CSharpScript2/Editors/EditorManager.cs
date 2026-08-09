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
/// <para>
/// That contract covers this class's own mutable state only. The Roslyn analysis itself is
/// dispatched to the thread pool, because <see cref="Microsoft.CodeAnalysis.Solution"/> and
/// <see cref="Document"/> are immutable snapshots and safe to analyse from any thread. Callers
/// stay on their own synchronisation context across the await.
/// </para>
/// </remarks>
public class EditorManager : IDisposable
{
    private ScriptContext? _context;
    private ExecutableScript? _cachedExecutableScript;
    private readonly ScriptEnvironment _environment;

    private ImmutableArray<Diagnostic> _lastDiagnostics = [];
    private IReadOnlyList<Classification.ClassifiedSymbol> _lastClassifications = [];

    /// <summary>True once the script context has been initialised (after the first <see cref="ApplyScript(string, CancellationToken)"/> call).</summary>
    public bool IsReady => _context != null;

    /// <summary>Diagnostics from the most recent <see cref="ApplyScript(string, CancellationToken)"/> call.</summary>
    public ImmutableArray<Diagnostic> LastDiagnostics => _lastDiagnostics;

    /// <summary>Classifications from the most recent <see cref="ApplyScript(string, CancellationToken)"/> call.</summary>
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
    /// Does NOT compile for execution — call <see cref="CompileAsync(CancellationToken)"/> explicitly.
    /// </summary>
    public Task ApplyScript(string script) => ApplyScript(script, CancellationToken.None);

    /// <summary>
    /// Analyses the script text and stores fresh diagnostics and classifications.
    /// Awaiting this method returns on the calling synchronisation context, so
    /// callers on the UI thread remain on the UI thread after the await.
    /// Does NOT compile for execution — call <see cref="CompileAsync(CancellationToken)"/> explicitly.
    /// </summary>
    /// <param name="script">The script text to analyse.</param>
    /// <param name="cancellationToken">A token that abandons a pass superseded by a newer edit.</param>
    /// <remarks>
    /// A cancelled pass leaves <see cref="LastDiagnostics"/> and <see cref="LastClassifications"/>
    /// untouched: both are assigned only once the whole pass has succeeded, so a caller can never
    /// observe diagnostics from one revision of the script alongside classifications from another.
    /// The document itself is still advanced to <paramref name="script"/>, since the next request
    /// should build on the latest text regardless.
    /// </remarks>
    public async Task ApplyScript(string script, CancellationToken cancellationToken)
    {
        await EnsureContext(cancellationToken).ConfigureAwait(false);

        _context = _context!.ApplyScript(script);
        _cachedExecutableScript = null;

        var result = await RunAnalysisAsync(
            async analyser => (
                Diagnostics: await analyser.GetDiagnosticsAsync(cancellationToken).ConfigureAwait(false),
                Classifications: await analyser.GetClassificationsAsync(cancellationToken).ConfigureAwait(false)),
            cancellationToken).ConfigureAwait(false);

        _lastDiagnostics = result.Diagnostics;
        _lastClassifications = result.Classifications;
    }

    /// <summary>
    /// Compiles the current script and returns the result ready for execution.
    /// The result is cached and reused on subsequent calls until the script changes.
    /// </summary>
    public async Task<ExecutableScript> CompileAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedExecutableScript != null)
            return _cachedExecutableScript;

        await EnsureContext(cancellationToken).ConfigureAwait(false);
        _cachedExecutableScript = await new ScriptExecutor(_context!).CompileAsync(cancellationToken).ConfigureAwait(false);
        return _cachedExecutableScript;
    }

    /// <summary>
    /// Updates the Roslyn workspace document to reflect the latest script text without running
    /// diagnostics or classification. Call this before requesting completions to keep the
    /// document current while avoiding the cost of a full analysis pass.
    /// </summary>
    public Task UpdateScriptDocumentAsync(string script)
        => UpdateScriptDocumentAsync(script, CancellationToken.None);

    /// <summary>
    /// Updates the Roslyn workspace document to reflect the latest script text without running
    /// diagnostics or classification. Call this before requesting completions to keep the
    /// document current while avoiding the cost of a full analysis pass.
    /// </summary>
    /// <param name="script">The script text to apply.</param>
    /// <param name="cancellationToken">A token that abandons the initial workspace build.</param>
    public async Task UpdateScriptDocumentAsync(string script, CancellationToken cancellationToken)
    {
        await EnsureContext(cancellationToken).ConfigureAwait(false);
        _context = _context!.ApplyScript(script);
        _cachedExecutableScript = null;
    }

    /// <summary>Returns code completion suggestions at the given cursor position.</summary>
    public Task<IEnumerable<CompletionItem>> GetAutoCompletions(int cursorPosition)
        => GetAutoCompletions(cursorPosition, CancellationToken.None);

    /// <summary>Returns code completion suggestions at the given cursor position.</summary>
    /// <param name="cursorPosition">The caret offset within the script.</param>
    /// <param name="cancellationToken">A token that abandons a request the caret has moved past.</param>
    public async Task<IEnumerable<CompletionItem>> GetAutoCompletions(
        int cursorPosition,
        CancellationToken cancellationToken)
    {
        await EnsureContext(cancellationToken).ConfigureAwait(false);
        return await RunAnalysisAsync(
            a => a.GetCompletionsAsync(cursorPosition, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Returns API info (type, overloads, XML docs) at the given cursor position.</summary>
    public Task<APIInfo.APIInfoResult?> GetAPIInfo(int cursorPosition)
        => GetAPIInfo(cursorPosition, CancellationToken.None);

    /// <summary>Returns API info (type, overloads, XML docs) at the given cursor position.</summary>
    /// <param name="cursorPosition">The caret offset within the script.</param>
    /// <param name="cancellationToken">A token that abandons a request the pointer has moved past.</param>
    public async Task<APIInfo.APIInfoResult?> GetAPIInfo(
        int cursorPosition,
        CancellationToken cancellationToken)
    {
        await EnsureContext(cancellationToken).ConfigureAwait(false);
        return await RunAnalysisAsync(
            a => a.GetAPIInfoAsync(cursorPosition, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the active argument index and opening-paren position when the cursor
    /// sits inside a method or indexer argument list; otherwise returns <see langword="null"/>.
    /// </summary>
    public Task<APIInfo.CallTipContext?> GetCallTipContext(int cursorPosition)
        => GetCallTipContext(cursorPosition, CancellationToken.None);

    /// <summary>
    /// Returns the active argument index and opening-paren position when the cursor
    /// sits inside a method or indexer argument list; otherwise returns <see langword="null"/>.
    /// </summary>
    /// <param name="cursorPosition">The caret offset within the script.</param>
    /// <param name="cancellationToken">A token that abandons a request the caret has moved past.</param>
    public async Task<APIInfo.CallTipContext?> GetCallTipContext(
        int cursorPosition,
        CancellationToken cancellationToken)
    {
        await EnsureContext(cancellationToken).ConfigureAwait(false);
        return await RunAnalysisAsync(
            a => a.GetCallTipContextAsync(cursorPosition, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Returns the syntax tree for the current script.</summary>
    public Task<SyntaxTree?> GetSyntaxTreeAsync() => GetSyntaxTreeAsync(CancellationToken.None);

    /// <summary>Returns the syntax tree for the current script.</summary>
    /// <param name="cancellationToken">A token that abandons the request.</param>
    public async Task<SyntaxTree?> GetSyntaxTreeAsync(CancellationToken cancellationToken)
    {
        await EnsureContext(cancellationToken).ConfigureAwait(false);
        return await RunAnalysisAsync(
            a => a.GetSyntaxTreeAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Returns the semantic model for the current script.</summary>
    public Task<SemanticModel?> GetSemanticModelAsync() => GetSemanticModelAsync(CancellationToken.None);

    /// <summary>Returns the semantic model for the current script.</summary>
    /// <param name="cancellationToken">A token that abandons the request.</param>
    public async Task<SemanticModel?> GetSemanticModelAsync(CancellationToken cancellationToken)
    {
        await EnsureContext(cancellationToken).ConfigureAwait(false);
        return await RunAnalysisAsync(
            a => a.GetSemanticModelAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies the given script text and returns syntax-only classifications for it, without
    /// running diagnostics or semantic analysis.
    /// </summary>
    /// <param name="script">The script text to apply.</param>
    /// <param name="cancellationToken">A token that abandons a pass superseded by a newer edit.</param>
    /// <remarks>
    /// The cheap half of a two-tier scheme: fast enough to run while the user types, so newly
    /// typed code is coloured immediately rather than waiting for the debounced
    /// <see cref="ApplyScript(string, CancellationToken)"/> pass. Identifiers come back unresolved and are refined
    /// when that pass lands. Does not touch <see cref="LastDiagnostics"/> or
    /// <see cref="LastClassifications"/>, which continue to describe the last full pass.
    /// </remarks>
    public async Task<IReadOnlyList<Classification.ClassifiedSymbol>> ApplySyntacticPassAsync(
        string script,
        CancellationToken cancellationToken)
    {
        await EnsureContext(cancellationToken).ConfigureAwait(false);

        _context = _context!.ApplyScript(script);
        _cachedExecutableScript = null;

        return await RunAnalysisAsync(
            a => a.GetSyntacticClassificationsAsync(cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Returns classified symbol spans for the current script.</summary>
    public Task<IReadOnlyList<Classification.ClassifiedSymbol>> GetClassificationsAsync()
        => GetClassificationsAsync(CancellationToken.None);

    /// <summary>Returns classified symbol spans for the current script.</summary>
    /// <param name="cancellationToken">A token that abandons the request.</param>
    public async Task<IReadOnlyList<Classification.ClassifiedSymbol>> GetClassificationsAsync(
        CancellationToken cancellationToken)
    {
        await EnsureContext(cancellationToken).ConfigureAwait(false);
        return await RunAnalysisAsync(
            a => a.GetClassificationsAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _context?.Dispose();
        _context = null;
    }

    private async Task EnsureContext(CancellationToken cancellationToken)
    {
        _context ??= await ScriptContext.CreateAsync(_environment, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs an analysis over the current context on the thread pool.
    /// </summary>
    /// <remarks>
    /// Every Roslyn call underneath completes synchronously in this workspace configuration, so
    /// without this hop the whole analysis runs inline on the caller's thread — the UI thread, for
    /// an editor, which is what made typing stutter. <c>ConfigureAwait(false)</c> does not help:
    /// it governs where a continuation resumes, not where synchronous work runs.
    /// <para>
    /// The context is captured first and handed to the pool as an immutable snapshot, so a later
    /// edit replacing <see cref="_context"/> cannot change what this pass is analysing.
    /// </para>
    /// </remarks>
    private Task<T> RunAnalysisAsync<T>(
        Func<ScriptAnalyser, Task<T>> analysis,
        CancellationToken cancellationToken)
    {
        var context = _context!;
        return Task.Run(() => analysis(new ScriptAnalyser(context)), cancellationToken);
    }
}
