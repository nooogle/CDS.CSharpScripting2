using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CDS.CSharpScript2;

/// <summary>
/// Represents a compiled script that is ready to execute.
/// </summary>
/// <remarks>
/// Obtain instances from editor controls or higher-level compilation APIs.
/// A single compiled script can be executed multiple times with different globals objects.
/// </remarks>
public class ExecutableScript
{
    private readonly CompiledScript _compiled;

    internal ExecutableScript(CompiledScript compiled)
    {
        _compiled = compiled;
    }

    /// <summary>
    /// Gets the full compilation output, including diagnostics and preformatted diagnostic messages.
    /// </summary>
    public CompilationOutput CompilationOutput => _compiled.CompilationOutput;

    /// <summary>
    /// Gets all diagnostics produced during compilation.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics => _compiled.CompilationOutput.Diagnostics;

    /// <summary>
    /// Gets a value indicating whether compilation produced at least one error-severity diagnostic.
    /// </summary>
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Runs the script without returning a typed value.
    /// </summary>
    /// <param name="globals">
    /// The globals object exposed to the script, or <see langword="null"/> when the script does not use globals.
    /// </param>
    /// <returns>A task that completes when script execution finishes.</returns>
    public async Task RunAsync(object? globals = null)
        => await ScriptRunner.RunAsync<object>(_compiled, globals).ConfigureAwait(false);

    /// <summary>
    /// Runs the script and returns its result cast to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="globals">
    /// The globals object exposed to the script, or <see langword="null"/> when the script does not use globals.
    /// </param>
    /// <returns>A task that completes with the script return value.</returns>
    public async Task<T> RunAsync<T>(object? globals = null)
        => await ScriptRunner.RunAsync<T>(_compiled, globals).ConfigureAwait(false);
}
