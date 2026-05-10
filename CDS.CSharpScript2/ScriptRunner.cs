namespace CDS.CSharpScript2;

/// <summary>
/// Executes compiled scripts via the Roslyn scripting runtime.
/// </summary>
internal static class ScriptRunner
{
    /// <summary>
    /// Runs the compiled script and returns its result cast to <typeparamref name="ReturnType"/>.
    /// </summary>
    /// <remarks>
    /// The script runs to completion. Cancellation via <see cref="System.Threading.CancellationToken"/>
    /// is not supported by the Roslyn scripting runtime; use the globals object to signal the script
    /// to stop cooperatively if needed.
    /// </remarks>
    public static async Task<ReturnType> RunAsync<ReturnType>(CompiledScript compiledScript, object? globals)
    {
        var scriptState = await compiledScript.ActualScript.RunAsync(globals).ConfigureAwait(false);
        return (ReturnType)scriptState.ReturnValue;
    }
}
