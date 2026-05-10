using Microsoft.CodeAnalysis;

namespace CDS.CSharpScript2.Editors;

/// <summary>
/// The host-facing contract for an embedded C# script editor control.
/// </summary>
/// <remarks>
/// Typical setup — drop the editor control on a form, then in OnLoad:
/// <code>
/// scriptEditor.Environment = ScriptEnvironment.Default
///     .WithGlobalType(typeof(MyGlobals));
/// scriptEditor.Script = savedScript;
/// scriptEditor.DiagnosticsUpdated += (s, e) => statusLabel.Text = e.HasErrors ? "Errors" : "OK";
/// </code>
/// To compile and run:
/// <code>
/// var compiled = await scriptEditor.CompileAsync();
/// await compiled.RunAsync(myGlobals);
/// </code>
/// Dispose the editor when it is no longer needed to release the underlying Roslyn workspace.
/// </remarks>
public interface IScriptEditor : IDisposable
{
    // ── Configuration ────────────────────────────────────────────────────────

    /// <summary>
    /// The scripting environment (assembly references, namespace imports, global type).
    /// Set this before using the editor; updating it recreates the internal engine.
    /// </summary>
    ScriptEnvironment? Environment { get; set; }

    // ── Content ──────────────────────────────────────────────────────────────

    /// <summary>Gets or sets the script text shown in the editor.</summary>
    string Script { get; set; }

    // ── Live-analysis state ───────────────────────────────────────────────────

    /// <summary>True when the most recent analysis found at least one error.</summary>
    bool HasErrors { get; }

    /// <summary>Diagnostics produced by the most recent live-analysis pass.</summary>
    IReadOnlyList<Diagnostic> CurrentDiagnostics { get; }

    /// <summary>
    /// The last successfully compiled script, or <see langword="null"/> if the
    /// script has changed since the last <see cref="CompileAsync"/> call.
    /// </summary>
    ExecutableScript? CurrentCompiledScript { get; }

    // ── Compilation ──────────────────────────────────────────────────────────

    /// <summary>
    /// Compiles the current script and returns the result.
    /// A single <see cref="ExecutableScript"/> can be run multiple times with
    /// different globals without recompiling.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="Environment"/> has not been set.
    /// </exception>
    Task<ExecutableScript> CompileAsync(CancellationToken cancellationToken = default);

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised on the UI thread after the live-analysis pass completes following
    /// a debounced edit. Inspect <see cref="DiagnosticsUpdatedEventArgs.Diagnostics"/>
    /// for errors and warnings.
    /// </summary>
    event EventHandler<DiagnosticsUpdatedEventArgs> DiagnosticsUpdated;

    /// <summary>
    /// Raised on the UI thread after the live-analysis pass completes following
    /// a debounced edit. Subscribe here to react to script content changes
    /// once analysis results are available.
    /// </summary>
    event EventHandler ScriptChanged;

    // ── Advanced ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The underlying engine manager. <see langword="null"/> until
    /// <see cref="Environment"/> is set. Exposes syntax trees, semantic
    /// models, and raw completion/API-info APIs for advanced scenarios.
    /// </summary>
    EditorManager? Manager { get; }
}
