using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Completion;
using System.Collections.Immutable;

namespace CDS.CSharpScript2.Editors;

public delegate void ApplyDiagnosticsDelegate(ImmutableArray<Diagnostic> diagnostics);
public delegate void ApplyClassificationsDelegate(IReadOnlyList<Classification.ClassifiedSymbol> classifications);


/// <summary>
/// Delegate for asynchronous script processing.
/// </summary>
/// <param name="script">The script content to process.</param>
/// <returns>A task representing the asynchronous operation.</returns>
public delegate Task ApplyScriptDelegateAsync(string script);


/// <summary>
/// Delegate for asynchronous auto-complete list retrieval.
/// </summary>
/// <param name="cursorPosition">
/// Position of the cursor in the script.
/// </param>
/// <returns>
/// Returns the list of auto-complete items.
/// </returns>
public delegate Task<IEnumerable<CompletionItem>> GetAutoCompleteListDelegateAsync(int cursorPosition);


public delegate Task<APIInfo.IAPIInfoResult> GetAPIInfoDelegateAsync(int cursorPosition);
