using System.Collections.Immutable;

namespace CDS.CSScripting2.Editors;

public delegate void ApplyDiagnosticsDelegate(ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics);
public delegate void ApplySyntaxElementsDelegate(ImmutableArray<Syntax.SyntaxElement> syntaxElements);


/// <summary>
/// Delegate for asynchronous script processing.
/// </summary>
/// <param name="script">The script content to process.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task ProcessScriptDelegateAsync(string script);
