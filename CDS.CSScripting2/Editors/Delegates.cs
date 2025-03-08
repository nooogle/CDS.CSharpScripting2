using System.Collections.Immutable;

namespace CDS.CSScripting2.Editors;

public delegate void ApplyDiagnosticsDelegate(ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics);
public delegate void ApplySyntaxElementsDelegate(ImmutableArray<Syntax.SyntaxElement> syntaxElements);
public delegate void ProcessScriptDelegate(string script);
