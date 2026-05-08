using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CDS.CSharpScript2;

/// <summary>
/// A script compiled for execution. Obtain via <see cref="ScriptExecutor.CompileAsync()"/>.
/// A single instance can be run multiple times; only the globals change between runs.
/// </summary>
public class ExecutableScript
{
    private readonly CompiledScript _compiled;

    internal ExecutableScript(CompiledScript compiled)
    {
        _compiled = compiled;
    }

    /// <summary>Full compilation output including diagnostics and message strings.</summary>
    public CompilationOutput CompilationOutput => _compiled.CompilationOutput;

    /// <summary>All diagnostics produced during compilation.</summary>
    public ImmutableArray<Diagnostic> Diagnostics => _compiled.CompilationOutput.Diagnostics;

    /// <summary>True when the compilation produced at least one error-severity diagnostic.</summary>
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>Runs the script. Pass a globals instance if the environment was configured with a global type.</summary>
    public async Task RunAsync(object? globals = null)
        => await ScriptRunner.RunAsync<object>(_compiled, globals).ConfigureAwait(false);

    /// <summary>Runs the script and returns its return value cast to <typeparamref name="T"/>.</summary>
    public async Task<T> RunAsync<T>(object? globals = null)
        => await ScriptRunner.RunAsync<T>(_compiled, globals).ConfigureAwait(false);
}
