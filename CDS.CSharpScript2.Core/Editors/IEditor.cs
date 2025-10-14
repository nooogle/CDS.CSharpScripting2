using System.Collections.Immutable;

namespace CDS.CSharpScript2.Editors;

public interface IEditor
{
    string Script { get; set; }


    void ApplyDiagnostics(ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics);
    
    
    void ApplyClassifications(IReadOnlyList<Classification.ClassifiedSymbol> classifications);



    void SetDelegates(
        ApplyScriptDelegateAsync processScriptAsync,
        GetAutoCompleteListDelegateAsync getAutoCompleteListAsync,
        GetAPIInfoDelegateAsync getAPIInfoDelegate);
}

