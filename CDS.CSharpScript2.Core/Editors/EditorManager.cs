namespace CDS.CSharpScript2.Editors;

public class EditorManager
{
    private ScriptManager scriptManager;
    private ApplyDiagnosticsDelegate applyDiagnostics;
    private ApplyClassificationsDelegate _applyClassifications;
    private ScriptEnvironment environment;
    private CompiledScript? compiledScript;

    public bool IsReady => compiledScript != null;

    public EditorManager(
        ScriptEnvironment environment,
        ApplyDiagnosticsDelegate applyDiagnostics, 
        ApplyClassificationsDelegate applyClassifications)
    {
        this.applyDiagnostics = applyDiagnostics;
        this._applyClassifications = applyClassifications;
        this.environment = environment;
    }

    public async Task<CompiledScript> GetCompiledScriptAsync()
    {
        await CompileAsync();

        if (compiledScript == null)
        {
            throw new Exception("No compiled script available");
        }

        return compiledScript;
    }

    public async Task CompileAsync()
    {
        await CreateScriptManager();
        await scriptManager.CompileAsync();
        compiledScript = await scriptManager.GetCompiledScriptAsync();
    }

    public async Task<IEnumerable<Microsoft.CodeAnalysis.Completion.CompletionItem>> GetAutoCompletions(int cursorPosition)
    {
        await CreateScriptManager();
        var completions = await scriptManager.GetCompletionSuggestionsAsync(cursorPosition);
        return completions;
    }

    public async Task<APIInfo.IAPIInfoResult> GetAPIInfo(int cursorPosition)
    {
        await CreateScriptManager();
        var apiInfo = await scriptManager.GetSuggestionsAsync(cursorPosition);
        return apiInfo;
    }

    public async Task ApplyScript(string script)
    {
        await CreateScriptManager();

        scriptManager = await scriptManager.ApplyScriptAsync(script);
        await CompileAsync();

        var syntaxTree = await scriptManager.GetSyntaxTreeAsync();
        //var syntaxElements = Syntax.ScriptSyntaxAnalyser.Go(syntaxTree);
        var classifications = await scriptManager.GetClassifications();

        var diagnostics = await scriptManager.GetDiagnosticsAsync();
        applyDiagnostics(diagnostics);
        _applyClassifications(classifications);
    }


    public async Task<Microsoft.CodeAnalysis.SyntaxTree> GetSyntaxTree()
    {
        if (scriptManager == null)
        {
            throw new InvalidOperationException("ScriptManager is not initialized. Call ApplyScriptAsync first.");
        }

        return await scriptManager.GetSyntaxTreeAsync();
    }

    public async Task RunAsync()
    {
        await CreateScriptManager();
        await scriptManager.RunAsync();
    }

    public async Task RunAsync(object globals)
    {
        await CreateScriptManager();
        await scriptManager.RunAsync(globals);
    }

    private async Task CreateScriptManager()
    {
        scriptManager ??= await ScriptManager.CreateAsync(environment);
    }
}
