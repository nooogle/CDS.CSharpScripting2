using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// Manages diagnostic tooltips for an editor control.
/// </summary>
public class ToolTipDiagnostics
{
    private readonly Control _editor;
    private readonly ToolTip _toolTip;
    private Diagnostic? _currentToolTipDiagnostic;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolTipDiagnostics"/> class.
    /// </summary>
    /// <param name="editor">The editor control that owns the tooltip.</param>
    /// <param name="toolTip">The tooltip instance used to display diagnostic messages.</param>
    public ToolTipDiagnostics(Control editor, ToolTip toolTip)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _toolTip = toolTip ?? throw new ArgumentNullException(nameof(toolTip));
    }

    /// <summary>
    /// Updates the tooltip based on the diagnostic under the specified character position.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to inspect.</param>
    /// <param name="characterPosition">The zero-based character position under the mouse pointer.</param>
    public void HandleMouseMove(ImmutableArray<Diagnostic> diagnostics, int characterPosition)
    {
        var diagnosticForToolTip = FindDiagnosticAtPosition(diagnostics, characterPosition);

        if (!Equals(_currentToolTipDiagnostic, diagnosticForToolTip))
        {
            _currentToolTipDiagnostic = diagnosticForToolTip;

            if (_currentToolTipDiagnostic is null)
            {
                ClearToolTip();
            }
            else
            {
                UpdateToolTip(_currentToolTipDiagnostic);
            }
        }
    }

    /// <summary>
    /// Finds the first diagnostic that contains the specified character position.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to search.</param>
    /// <param name="characterPosition">The zero-based character position.</param>
    /// <returns>The diagnostic at the specified position, or <see langword="null"/> if none was found.</returns>
    private static Diagnostic? FindDiagnosticAtPosition(ImmutableArray<Diagnostic> diagnostics, int characterPosition)
    {
        foreach (var diagnostic in diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;

            if (span.Length > 0 && span.Contains(characterPosition))
            {
                return diagnostic;
            }
        }

        return null;
    }

    /// <summary>
    /// Updates the tooltip content and styling for the specified diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to display.</param>
    private void UpdateToolTip(Diagnostic diagnostic)
    {
        switch (diagnostic.Severity)
        {
            case DiagnosticSeverity.Error:
                _toolTip.ToolTipIcon = ToolTipIcon.Error;
                _toolTip.ToolTipTitle = $"Error {diagnostic.Id}";
                break;

            case DiagnosticSeverity.Warning:
                _toolTip.ToolTipIcon = ToolTipIcon.Warning;
                _toolTip.ToolTipTitle = $"Warning {diagnostic.Id} (level {diagnostic.WarningLevel})";
                break;

            default:
                _toolTip.ToolTipIcon = ToolTipIcon.Info;
                _toolTip.ToolTipTitle = $"{diagnostic.Severity} {diagnostic.Id}";
                break;
        }

        _toolTip.SetToolTip(_editor, diagnostic.GetMessage());
    }

    /// <summary>
    /// Clears the tooltip content from the editor.
    /// </summary>
    private void ClearToolTip()
    {
        _toolTip.ToolTipIcon = ToolTipIcon.None;
        _toolTip.ToolTipTitle = string.Empty;
        _toolTip.SetToolTip(_editor, string.Empty);
    }
}
