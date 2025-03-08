namespace CDS.CSScripting2.Editors;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class ToolTipManager
{
    private Microsoft.CodeAnalysis.Diagnostic currentToolTipDiagnostic;
    private Control editor;
    private ToolTip toolTip;


    public ToolTipManager(
        Control editor,
        ToolTip toolTip)
    {
        this.editor = editor;
        this.toolTip = toolTip;
    }


    public void HandleMouseMove(
        System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics,
        int characterPosition)
    {
        if (diagnostics.Length == 0) { return; }

        Microsoft.CodeAnalysis.Diagnostic diagnosticForTooltip = null;

        foreach (var diagnostic in diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;
            if (span.Length == 0)
            {
                span = new Microsoft.CodeAnalysis.Text.TextSpan(span.Start - 1, 1);
            }

            if (span.Contains(characterPosition))
            {
                diagnosticForTooltip = diagnostic;
                break;
            }
        }

        if (diagnosticForTooltip == null)
        {
            currentToolTipDiagnostic = null;
        }
        else
        {
            if (currentToolTipDiagnostic != diagnosticForTooltip)
            {
                currentToolTipDiagnostic = diagnosticForTooltip;

                if (diagnosticForTooltip.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                {
                    toolTip.ToolTipIcon = ToolTipIcon.Error;
                    toolTip.ToolTipTitle = $"Error {diagnosticForTooltip.Id}";
                }
                else
                {
                    toolTip.ToolTipIcon = ToolTipIcon.Warning;
                    toolTip.ToolTipTitle = $"Warning {diagnosticForTooltip.Id} (level {diagnosticForTooltip.WarningLevel})";
                }

                toolTip.SetToolTip(editor, diagnosticForTooltip.GetMessage());
            }
        }

        if (currentToolTipDiagnostic == null)
        {
            toolTip.SetToolTip(editor, "");
        }
    }
}
