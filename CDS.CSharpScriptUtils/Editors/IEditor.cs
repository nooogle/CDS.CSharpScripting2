using System.Collections.Immutable;

namespace CDS.CSharpScriptUtils.Editors;

public interface IEditor
{
    string Script { get; set; }


    void ApplyDiagnostics(ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics);
    
    
    void ApplySyntaxElements(ImmutableArray<Syntax.SyntaxElement> syntaxElements);



    void SetDelegates(
        ApplyScriptDelegateAsync processScriptAsync,
        GetAutoCompleteListDelegateAsync getAutoCompleteListAsync);
}

