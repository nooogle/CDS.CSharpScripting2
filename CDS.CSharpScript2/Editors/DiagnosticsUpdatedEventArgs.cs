using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CDS.CSharpScript2.Editors;

/// <summary>
/// Event arguments for the <see cref="IScriptEditor.DiagnosticsUpdated"/> event.
/// </summary>
public class DiagnosticsUpdatedEventArgs : EventArgs
{
    /// <summary>All diagnostics from the most recent analysis pass.</summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>True when at least one error-severity diagnostic is present.</summary>
    public bool HasErrors { get; }

    /// <summary>Initializes a new instance with the given diagnostics array.</summary>
    public DiagnosticsUpdatedEventArgs(ImmutableArray<Diagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
        HasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
    }
}
