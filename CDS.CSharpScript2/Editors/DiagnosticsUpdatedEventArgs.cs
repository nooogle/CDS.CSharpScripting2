using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CDS.CSharpScript2.Editors;

/// <summary>
/// Provides diagnostics from the most recent live-analysis pass in an editor control.
/// </summary>
public class DiagnosticsUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets all diagnostics from the most recent analysis pass.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Gets a value indicating whether at least one error-severity diagnostic is present.
    /// </summary>
    public bool HasErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsUpdatedEventArgs"/> class.
    /// </summary>
    /// <param name="diagnostics">The diagnostics captured by the completed analysis pass.</param>
    public DiagnosticsUpdatedEventArgs(ImmutableArray<Diagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
        HasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
    }
}
