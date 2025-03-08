namespace CDS.CSScripting2.Editors;

public class EditorManager
{
    private ScriptManager scriptManager;
    private ApplyDiagnosticsDelegate applyDiagnostics;
    private ApplySyntaxElementsDelegate applySyntaxElements;


    public EditorManager(
        ApplyDiagnosticsDelegate applyDiagnostics, 
        ApplySyntaxElementsDelegate applySyntaxElements)
    {
        this.applyDiagnostics = applyDiagnostics;
        this.applySyntaxElements = applySyntaxElements;
    }


    public async void ProcessScript(string script)
    {
        scriptManager ??= await ScriptManager.CreateAsync();

        scriptManager = await scriptManager.ApplyScriptAsync(script);
        await scriptManager.CompileAsync();
        
        var syntaxTree = await scriptManager.GetSyntaxTreeAsync();
        var syntaxElements = Syntax.ScriptSyntaxAnalyser.Go(syntaxTree);
        
        var diagnostics = await scriptManager.GetDiagnosticsAsync();
        applyDiagnostics(diagnostics);
        applySyntaxElements(syntaxElements);
    }
}
