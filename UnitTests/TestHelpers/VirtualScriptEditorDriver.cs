using CDS.CSharpScript2;
using CDS.CSharpScript2.APIInfo;
using CDS.CSharpScript2.Classification;
using CDS.CSharpScript2.Editors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using System.Collections.Immutable;

namespace UnitTests.TestHelpers;

/// <summary>
/// Provides a small test-oriented wrapper around <see cref="VirtualScriptEditor"/>.
/// </summary>
internal sealed class VirtualScriptEditorDriver
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualScriptEditorDriver"/> class.
    /// </summary>
    /// <param name="environment">The scripting environment to use for the wrapped editor.</param>
    public VirtualScriptEditorDriver(ScriptEnvironment? environment = null)
    {
        Editor = new VirtualScriptEditor
        {
            Environment = environment ?? ScriptEnvironment.Default,
        };

        Events = new EditorEventRecorder(Editor);
    }

    /// <summary>
    /// Gets the wrapped virtual editor instance.
    /// </summary>
    public VirtualScriptEditor Editor { get; }

    /// <summary>
    /// Gets the recorder used to capture editor events during tests.
    /// </summary>
    public EditorEventRecorder Events { get; }

    /// <summary>
    /// Gets the current script text.
    /// </summary>
    public string Script => Editor.Script;

    /// <summary>
    /// Gets the current caret position.
    /// </summary>
    public int CaretPosition => Editor.CaretPosition;

    /// <summary>
    /// Gets the current diagnostics.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => Editor.CurrentDiagnostics;

    /// <summary>
    /// Gets the current classifications.
    /// </summary>
    public IReadOnlyList<ClassifiedSymbol> Classifications => Editor.CurrentClassifications;

    /// <summary>
    /// Gets a value indicating whether the current script has at least one error diagnostic.
    /// </summary>
    public bool HasErrors => Editor.HasErrors;

    /// <summary>
    /// Gets the current compiled script, if available.
    /// </summary>
    public ExecutableScript? CurrentCompiledScript => Editor.CurrentCompiledScript;

    /// <summary>
    /// Replaces the current script text.
    /// </summary>
    /// <param name="script">The script text to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetScriptAsync(string script, CancellationToken cancellationToken = default) =>
        await Editor.SetScriptAsync(script, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Simulates typing text at the current caret position.
    /// </summary>
    /// <param name="text">The text to type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task TypeAsync(string text, CancellationToken cancellationToken = default) =>
        await Editor.TypeTextAsync(text, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Simulates one or more backspace operations.
    /// </summary>
    /// <param name="count">The number of characters to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task BackspaceAsync(int count = 1, CancellationToken cancellationToken = default) =>
        await Editor.BackspaceAsync(count, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Moves the caret to the specified position.
    /// </summary>
    /// <param name="position">The zero-based caret position.</param>
    public void MoveCaretTo(int position) => Editor.MoveCaretTo(position);

    /// <summary>
    /// Moves the caret to the end of the script.
    /// </summary>
    public void MoveCaretToEnd() => Editor.MoveCaretTo(Editor.Script.Length);

    /// <summary>
    /// Returns the current diagnostic messages.
    /// </summary>
    /// <returns>The diagnostic message strings.</returns>
    public IReadOnlyList<string> GetDiagnosticMessages()
    {
        return Editor.CurrentDiagnostics
            .Select(diagnostic => diagnostic.GetMessage())
            .ToList();
    }

    /// <summary>
    /// Returns completion display text values at the current caret position.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completion display text values.</returns>
    public async Task<IReadOnlyList<string>> GetCompletionTextsAsync(CancellationToken cancellationToken = default)
    {
        var completions = await Editor.GetCompletionsAsync(cancellationToken).ConfigureAwait(false);
        return completions.Select(completion => completion.DisplayText).ToList();
    }

    /// <summary>
    /// Returns completion items at the current caret position.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completion items.</returns>
    public async Task<ImmutableArray<CompletionItem>> GetCompletionsAsync(CancellationToken cancellationToken = default) =>
        await Editor.GetCompletionsAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Returns API information at the current caret position.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The API information, if available.</returns>
    public async Task<APIInfoResult?> GetAPIInfoAsync(CancellationToken cancellationToken = default) =>
        await Editor.GetAPIInfoAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Compiles the current script.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The compiled script.</returns>
    public async Task<ExecutableScript> CompileAsync(CancellationToken cancellationToken = default) =>
        await Editor.CompileAsync(cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Compiles and runs the current script.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="globals">The optional globals object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The script result.</returns>
    public async Task<T> CompileAndRunAsync<T>(object? globals = null, CancellationToken cancellationToken = default)
    {
        var compiledScript = await CompileAsync(cancellationToken).ConfigureAwait(false);
        return await compiledScript.RunAsync<T>(globals).ConfigureAwait(false);
    }
}
