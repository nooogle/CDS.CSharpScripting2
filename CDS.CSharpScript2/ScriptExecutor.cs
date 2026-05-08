namespace CDS.CSharpScript2;

/// <summary>
/// Compiles a <see cref="ScriptContext"/> for execution, producing an <see cref="ExecutableScript"/>.
/// Compilation is CPU-bound and intentionally explicit — call only when the user requests it,
/// not on every keystroke. For editor feedback use <see cref="ScriptAnalyser"/> instead.
/// </summary>
public class ScriptExecutor
{
    private readonly ScriptContext _context;

    /// <param name="context">The script context to compile.</param>
    public ScriptExecutor(ScriptContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Compiles the script for execution with an <c>object</c> return type.
    /// Use <see cref="CompileAsync{T}"/> when a specific return type is known.
    /// </summary>
    public Task<ExecutableScript> CompileAsync(CancellationToken ct = default)
        => CompileAsync<object>(ct);

    /// <summary>Compiles the script for execution with the specified return type.</summary>
    public async Task<ExecutableScript> CompileAsync<T>(CancellationToken ct = default)
    {
        var compiled = await Task.Run(() => ScriptCompiler.Compile<T>(
            _context.ScriptText,
            namespaces: _context.Environment.NamespaceNames,
            references: _context.Environment.References,
            typeOfGlobals: _context.Environment.GlobalType), ct).ConfigureAwait(false);

        return new ExecutableScript(compiled);
    }
}
