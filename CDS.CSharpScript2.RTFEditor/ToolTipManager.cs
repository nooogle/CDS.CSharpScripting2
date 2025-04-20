namespace CDS.CSharpScript2.RTFEditor;

using System.Text;
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


    public void ShowAPIInfo(APIInfo.IAPIInfoResult aPIInfo, Point position)
    {
        if (aPIInfo == null) { return; }

        if(aPIInfo?.TypeInfo?.Summary != null)
        {
            toolTip.ToolTipIcon = ToolTipIcon.Info;
            toolTip.ToolTipTitle = aPIInfo.TypeInfo.Name;
            toolTip.SetToolTip(editor, aPIInfo.TypeInfo.Summary);
            
            toolTip.Show(aPIInfo.TypeInfo.Summary, editor, position);
        }
        else
        {
            toolTip.SetToolTip(editor, "");
        }

    }


    public void HandleMouseMove(
        System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics,
        int characterPosition,
        APIInfo.IAPIInfoResult apiInfo)
    {
        Microsoft.CodeAnalysis.Diagnostic diagnosticForTooltip = null;

        if (apiInfo?.TypeInfo?.Summary != null) 
        {
            toolTip.ToolTipIcon = ToolTipIcon.Info;
            toolTip.ToolTipTitle = apiInfo.TypeInfo.Name;

            StringBuilder sb = new();
            sb.AppendLine(apiInfo.TypeInfo.Summary);

            if (apiInfo.MemberInfos.Any())
            {
                var firstMember = apiInfo.MemberInfos.First();
                sb.Append($"Member: {firstMember.Name}");

                int numOverloads = apiInfo.MemberInfos.Count();
                if (numOverloads > 1)
                {
                    sb.Append($" (overloads: {numOverloads})");
                }
            }

            toolTip.SetToolTip(editor, sb.ToString());
        }
        else
        {
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
}
