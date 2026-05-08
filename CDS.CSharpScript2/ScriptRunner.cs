using System.Threading.Tasks;

namespace CDS.CSharpScript2;

/// <summary>
/// Run compiled scripts
/// </summary>
internal static class ScriptRunner
{
    /// <summary>
    /// Run the compiled script
    /// </summary>
    /// <param name="compiledScript">Script to run</param>
    public static void Run(CompiledScript compiledScript)
    {
        Run<object>(
            compiledScript: compiledScript, 
            globals: null);
    }


    /// <summary>
    /// Run the compiled script
    /// </summary>
    /// <param name="compiledScript">Script to run</param>
    /// <param name="globals">Instance of the Globals type used during compilation <see cref="ScriptCompiler.Compile{ReturnType}(string, System.Type[], System.Type[], System.Type)"/>, or null if not required.</param>
    public static void Run(CompiledScript compiledScript, object globals)
    {
        Run<object>(
            compiledScript: compiledScript, 
            globals: globals);
    }


    /// <summary>
    /// Run the compiled script
    /// </summary>
    /// <param name="compiledScript">Script to run</param>
    /// <param name="globals">Instance of the Globals type passed into <see cref="ScriptCompiler.Compile{ReturnType}(string, System.Type[], System.Type[], System.Type)"/>, or null if not required.</param>
    /// <returns>Data returned from the script</returns>
    /// <typeparam name="ReturnType">Type of object returned from the script</typeparam>
    public static ReturnType Run<ReturnType>(CompiledScript compiledScript, object? globals)
    {
        var task = RunAsync<ReturnType>(compiledScript, globals);
        task.Wait();
        return task.Result;
    }



    /// <summary>
    /// Runs the script from a task. Note that the script itself will run to completion and 
    /// cannot be cancelled using normal CancellationTokens. (The global data can be used
    /// by the host to manually signal the script to stop.)
    /// </summary>
    /// <typeparam name="ReturnType">Type of object returned from the script</typeparam>
    /// <param name="compiledScript">Script to run</param>
    /// <param name="globals">Instance of the Globals type passed into <see cref="ScriptCompiler.Compile{ReturnType}(string, System.Type[], System.Type[], System.Type)"/>, or null if not required.</param>
    /// <returns>Data returned from the script</returns>
    public static async Task<ReturnType> RunAsync<ReturnType>(CompiledScript compiledScript, object? globals)
    {
        var scriptState = await compiledScript.ActualScript.RunAsync(globals);
        var result = scriptState.ReturnValue;
        return (ReturnType)result;

        /*
        runTask.Wait();
        var returnValue = (ReturnType)runTask.Result.ReturnValue;
        return returnValue;


        Task<ReturnType> task = Task<ReturnType>.Run(() =>
        {
            return Run<ReturnType>(compiledScript, globals);
        });

        return task;
        */
    }
}
