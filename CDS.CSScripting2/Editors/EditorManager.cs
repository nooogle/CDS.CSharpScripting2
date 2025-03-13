

namespace CDS.CSScripting2.Editors;

public class EditorManager
{
    private ScriptManager scriptManager;
    private ApplyDiagnosticsDelegate applyDiagnostics;
    private ApplySyntaxElementsDelegate applySyntaxElements;
    private Env environment;

    public EditorManager(
        Env environment,
        ApplyDiagnosticsDelegate applyDiagnostics, 
        ApplySyntaxElementsDelegate applySyntaxElements)
    {
        this.applyDiagnostics = applyDiagnostics;
        this.applySyntaxElements = applySyntaxElements;
        this.environment = environment;
    }

    public async Task<CompilationOutput> CompileAsync()
    {
        await CreateScriptManager();
        await scriptManager.CompileAsync();
        var compilationOutput = await scriptManager.GetCompilationOutputAsync();
        return compilationOutput;
    }

    public async Task ProcessScriptAsync(string script)
    {
        await CreateScriptManager();

        scriptManager = await scriptManager.ApplyScriptAsync(script);
        await scriptManager.CompileAsync();

        var syntaxTree = await scriptManager.GetSyntaxTreeAsync();
        var syntaxElements = Syntax.ScriptSyntaxAnalyser.Go(syntaxTree);

        var diagnostics = await scriptManager.GetDiagnosticsAsync();
        applyDiagnostics(diagnostics);
        applySyntaxElements(syntaxElements);
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
