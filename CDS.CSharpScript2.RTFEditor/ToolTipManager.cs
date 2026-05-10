using System.Collections.Immutable;
using System.Text;
using System.Windows.Forms;

using CDS.CSharpScript2.APIInfo;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CDS.CSharpScript2.RTFEditor;

/// <summary>
/// Manages diagnostic and API information tooltips for the editor control.
/// </summary>
public class ToolTipManager
{
    private readonly Control _editor;
    private readonly ToolTip _toolTip;
    private Diagnostic? _currentToolTipDiagnostic;

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolTipManager"/> class.
    /// </summary>
    /// <param name="editor">The editor control that owns the tooltip.</param>
    /// <param name="toolTip">The tooltip instance used to display messages.</param>
    public ToolTipManager(
        Control editor,
        ToolTip toolTip)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _toolTip = toolTip ?? throw new ArgumentNullException(nameof(toolTip));
    }

    /// <summary>
    /// Shows API information at the specified position when type information is available.
    /// </summary>
    /// <param name="apiInfo">The API information to display.</param>
    /// <param name="position">The tooltip position relative to the editor.</param>
    public void ShowAPIInfo(APIInfoResult? apiInfo, Point position)
    {
        if (apiInfo is null)
        {
            return;
        }

        if (apiInfo.TypeInfo?.Summary != null)
        {
            _toolTip.ToolTipIcon = ToolTipIcon.Info;
            _toolTip.ToolTipTitle = apiInfo.TypeInfo.Name;
            _toolTip.SetToolTip(_editor, apiInfo.TypeInfo.Summary);
            _toolTip.Show(apiInfo.TypeInfo.Summary, _editor, position);
        }
        else
        {
            _toolTip.SetToolTip(_editor, string.Empty);
        }
    }

    /// <summary>
    /// Updates the tooltip based on API information or diagnostics at the specified character position.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to inspect.</param>
    /// <param name="characterPosition">The zero-based character position under the pointer.</param>
    /// <param name="apiInfo">The API information for the hovered symbol, if available.</param>
    public void HandleMouseMove(
        ImmutableArray<Diagnostic> diagnostics,
        int characterPosition,
        APIInfoResult? apiInfo)
    {
        Diagnostic? diagnosticForTooltip = null;

        if (apiInfo?.TypeInfo?.Summary != null)
        {
            _toolTip.ToolTipIcon = ToolTipIcon.Info;
            _toolTip.ToolTipTitle = apiInfo.TypeInfo.Name;

            var sb = new StringBuilder();
            sb.AppendLine(apiInfo.TypeInfo.Summary);

            if (apiInfo.MemberInfos?.Any() ?? false)
            {
                var firstMember = apiInfo.MemberInfos!.First();
                sb.Append($"Member: {firstMember.Name}");

                var numOverloads = apiInfo.MemberInfos.Count();
                if (numOverloads > 1)
                {
                    sb.Append($" (overloads: {numOverloads})");
                }
            }

            _toolTip.SetToolTip(_editor, sb.ToString());
        }
        else
        {
            foreach (var diagnostic in diagnostics)
            {
                var span = diagnostic.Location.SourceSpan;
                if (span.Length == 0)
                {
                    span = new TextSpan(Math.Max(0, span.Start - 1), 1);
                }

                if (span.Contains(characterPosition))
                {
                    diagnosticForTooltip = diagnostic;
                    break;
                }
            }

            if (diagnosticForTooltip == null)
            {
                _currentToolTipDiagnostic = null;
            }
            else
            {
                if (_currentToolTipDiagnostic != diagnosticForTooltip)
                {
                    _currentToolTipDiagnostic = diagnosticForTooltip;

                    if (diagnosticForTooltip.Severity == DiagnosticSeverity.Error)
                    {
                        _toolTip.ToolTipIcon = ToolTipIcon.Error;
                        _toolTip.ToolTipTitle = $"Error {diagnosticForTooltip.Id}";
                    }
                    else
                    {
                        _toolTip.ToolTipIcon = ToolTipIcon.Warning;
                        _toolTip.ToolTipTitle = $"Warning {diagnosticForTooltip.Id} (level {diagnosticForTooltip.WarningLevel})";
                    }

                    _toolTip.SetToolTip(_editor, diagnosticForTooltip.GetMessage());
                }
            }

            if (_currentToolTipDiagnostic == null)
            {
                _toolTip.SetToolTip(_editor, string.Empty);
            }
        }
    }
}
