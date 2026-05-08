using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace CDS.CSharpScript2.Editors;

public class EditorManager
{
    private ScriptContext? _context;
    private ExecutableScript? _executableScript;
    private readonly ApplyDiagnosticsDelegate _applyDiagnostics;
    private readonly ApplyClassificationsDelegate _applyClassifications;
    private readonly ScriptEnvironment _environment;

    /// <summary>True once the script context has been initialised (after the first <see cref="ApplyScript"/> call).</summary>
    public bool IsReady => _context != null;

    public EditorManager(
        ScriptEnvironment environment,
        ApplyDiagnosticsDelegate applyDiagnostics,
        ApplyClassificationsDelegate applyClassifications)
    {
        _environment = environment;
        _applyDiagnostics = applyDiagnostics;
        _applyClassifications = applyClassifications;
    }

    /// <summary>
    /// Updates the script text, then pushes fresh diagnostics and classifications to the editor.
    /// Does NOT compile for execution — call <see cref="CompileAsync"/> explicitly when needed.
    /// </summary>
    public async Task ApplyScript(string script)
    {
        await EnsureContext().ConfigureAwait(false);

        _context = _context!.ApplyScript(script);
        _executableScript = null;

        var analyser = new ScriptAnalyser(_context);

        var diagnostics = await analyser.GetDiagnosticsAsync().ConfigureAwait(false);
        var classifications = await analyser.GetClassificationsAsync().ConfigureAwait(false);

        _applyDiagnostics(diagnostics);
        _applyClassifications(classifications);
    }

    /// <summary>Compiles the current script for execution.</summary>
    public async Task CompileAsync()
    {
        await EnsureContext().ConfigureAwait(false);
        _executableScript = await new ScriptExecutor(_context!).CompileAsync().ConfigureAwait(false);
    }

    /// <summary>Returns the compiled script, compiling first if necessary.</summary>
    public async Task<ExecutableScript> GetCompiledScriptAsync()
    {
        if (_executableScript == null)
            await CompileAsync().ConfigureAwait(false);

        return _executableScript!;
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

    /// <summary>Returns the syntax tree for the current script (no compilation required).</summary>
    public async Task<SyntaxTree?> GetSyntaxTreeAsync()
    {
        await EnsureContext().ConfigureAwait(false);
        return await new ScriptAnalyser(_context!).GetSyntaxTreeAsync().ConfigureAwait(false);
    }

    /// <summary>Returns the semantic model for the current script (no compilation required).</summary>
    public async Task<SemanticModel?> GetSemanticModelAsync()
    {
        await EnsureContext().ConfigureAwait(false);
        return await new ScriptAnalyser(_context!).GetSemanticModelAsync().ConfigureAwait(false);
    }

    /// <summary>Returns classified symbol spans for the current script (no compilation required).</summary>
    public async Task<IReadOnlyList<Classification.ClassifiedSymbol>> GetClassificationsAsync()
    {
        await EnsureContext().ConfigureAwait(false);
        return await new ScriptAnalyser(_context!).GetClassificationsAsync().ConfigureAwait(false);
    }

    /// <summary>Runs the compiled script, compiling first if necessary.</summary>
    public async Task RunAsync()
    {
        if (_executableScript == null) await CompileAsync().ConfigureAwait(false);
        await _executableScript!.RunAsync().ConfigureAwait(false);
    }

    /// <summary>Runs the compiled script with globals, compiling first if necessary.</summary>
    public async Task RunAsync(object globals)
    {
        if (_executableScript == null) await CompileAsync().ConfigureAwait(false);
        await _executableScript!.RunAsync(globals).ConfigureAwait(false);
    }

    private async Task EnsureContext()
    {
        _context ??= await ScriptContext.CreateAsync(_environment).ConfigureAwait(false);
    }
}
