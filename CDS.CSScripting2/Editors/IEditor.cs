using System.Collections.Immutable;

namespace CDS.CSScripting2.Editors;

public interface IEditor
{
    string Script { get; set; }


    void ApplyDiagnostics(ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics);
    
    
    void ApplySyntaxElements(ImmutableArray<Syntax.SyntaxElement> syntaxElements);



    void SetDelegates(
        ProcessScriptDelegateAsync processScriptAsync,
        GetAutoCompleteListDelegateAsync getAutoCompleteListAsync);
}

