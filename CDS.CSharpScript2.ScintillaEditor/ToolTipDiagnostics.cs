using System.Text;

using CDS.CSharpScript2.APIInfo;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// Manages hover tooltips for an editor control.
/// Shows API symbol information when available; falls back to diagnostics.
/// </summary>
public class ToolTipDiagnostics
{
    private const int ToolTipOffsetX = 16;
    private const int ToolTipOffsetY = 16;

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
    /// Updates the tooltip when the pointer dwells over a character position.
    /// When <paramref name="apiInfo"/> is provided and has displayable content it takes
    /// priority; diagnostics are shown as a secondary line or used as the sole content
    /// when no symbol info is available.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to inspect.</param>
    /// <param name="characterPosition">The zero-based character position under the pointer.</param>
    /// <param name="apiInfo">Optional symbol information for the hovered position.</param>
    public void HandleDwellStart(
        ImmutableArray<Diagnostic> diagnostics,
        int characterPosition,
        APIInfoResult? apiInfo = null)
    {
        if (apiInfo is not null && HasDisplayableInfo(apiInfo))
        {
            ShowAPIInfoToolTip(apiInfo, diagnostics, characterPosition);
            return;
        }

        UpdateHoverDiagnostic(diagnostics, characterPosition);
    }

    /// <summary>
    /// Clears any active diagnostic tooltip when hover tracking ends.
    /// </summary>
    public void HandleDwellEnd() => ClearHover();

    /// <summary>
    /// Clears the current hover state and hides any visible tooltip.
    /// </summary>
    public void ClearHover()
    {
        _currentToolTipDiagnostic = null;
        ClearToolTip();
    }

    // ── API info display ──────────────────────────────────────────────────────

    private static bool HasDisplayableInfo(APIInfoResult apiInfo) =>
        !string.IsNullOrWhiteSpace(apiInfo.TypeInfo?.Summary) ||
        (apiInfo.MemberInfos?.Count > 0);

    private void ShowAPIInfoToolTip(
        APIInfoResult apiInfo,
        ImmutableArray<Diagnostic> diagnostics,
        int characterPosition)
    {
        var sb = new StringBuilder();
        string title;

        if (apiInfo.MemberInfos?.Count > 0)
        {
            var first = apiInfo.MemberInfos[0];
            title = first.Name;
            sb.AppendLine(first.Signature);

            if (!string.IsNullOrWhiteSpace(first.Summary))
                sb.AppendLine(first.Summary);

            int extra = apiInfo.MemberInfos.Count - 1;
            if (extra > 0)
                sb.AppendLine($"+{extra} overload{(extra > 1 ? "s" : "")}");
        }
        else
        {
            var typeInfo = apiInfo.TypeInfo!;
            title = typeInfo.Name;
            sb.AppendLine($"{typeInfo.TypeKind} {typeInfo.Name}");

            if (!string.IsNullOrWhiteSpace(typeInfo.Summary))
                sb.AppendLine(typeInfo.Summary);
        }

        var diagnostic = FindDiagnosticAtPosition(diagnostics, characterPosition);
        if (diagnostic is not null)
        {
            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append(diagnostic.GetMessage());
        }

        _currentToolTipDiagnostic = null;
        _toolTip.ToolTipIcon = ToolTipIcon.Info;
        _toolTip.ToolTipTitle = title;
        _toolTip.Hide(_editor);

        var mousePosition = _editor.PointToClient(Control.MousePosition);
        _toolTip.Show(
            sb.ToString().TrimEnd(),
            _editor,
            mousePosition.X + ToolTipOffsetX,
            mousePosition.Y + ToolTipOffsetY,
            duration: _toolTip.AutoPopDelay);
    }

    // ── Diagnostic display ────────────────────────────────────────────────────

    /// <summary>
    /// Updates the hover diagnostic for the specified character position.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to inspect.</param>
    /// <param name="characterPosition">The zero-based character position under the pointer.</param>
    private void UpdateHoverDiagnostic(ImmutableArray<Diagnostic> diagnostics, int characterPosition)
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
    /// Finds the most appropriate diagnostic that contains the specified character position.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to search.</param>
    /// <param name="characterPosition">The zero-based character position.</param>
    /// <returns>The diagnostic at the specified position, or <see langword="null"/> if none was found.</returns>
    private static Diagnostic? FindDiagnosticAtPosition(ImmutableArray<Diagnostic> diagnostics, int characterPosition)
    {
        if (characterPosition < 0)
        {
            return null;
        }

        Diagnostic? bestDiagnostic = null;

        foreach (var diagnostic in diagnostics)
        {
            if (!ShouldShowToolTipForDiagnostic(diagnostic))
            {
                continue;
            }

            var span = GetInteractiveSpan(diagnostic);

            if (!span.Contains(characterPosition))
            {
                continue;
            }

            if (bestDiagnostic is null || IsBetterMatch(candidate: diagnostic, currentBest: bestDiagnostic))
            {
                bestDiagnostic = diagnostic;
            }
        }

        return bestDiagnostic;
    }

    /// <summary>
    /// Returns the interactive span used for tooltip hit-testing.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to inspect.</param>
    /// <returns>The normalized span used for user interaction.</returns>
    private static TextSpan GetInteractiveSpan(Diagnostic diagnostic)
    {
        var span = diagnostic.Location.SourceSpan;

        if (span.Length > 0)
        {
            return span;
        }

        var start = Math.Max(0, span.Start - 1);
        return new TextSpan(start, 1);
    }

    /// <summary>
    /// Returns a value indicating whether the diagnostic should participate in tooltip hit-testing.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to inspect.</param>
    /// <returns><see langword="true"/> when the diagnostic is represented in the editor UI; otherwise, <see langword="false"/>.</returns>
    private static bool ShouldShowToolTipForDiagnostic(Diagnostic diagnostic)
    {
        return diagnostic.Location.IsInSource &&
            diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning;
    }

    /// <summary>
    /// Determines whether one diagnostic is a better hover match than another.
    /// </summary>
    /// <param name="candidate">The candidate diagnostic.</param>
    /// <param name="currentBest">The current best diagnostic.</param>
    /// <returns><see langword="true"/> when the candidate should replace the current best match.</returns>
    private static bool IsBetterMatch(Diagnostic candidate, Diagnostic currentBest)
    {
        var candidateSeverityRank = GetSeverityRank(candidate.Severity);
        var currentSeverityRank = GetSeverityRank(currentBest.Severity);

        if (candidateSeverityRank != currentSeverityRank)
        {
            return candidateSeverityRank > currentSeverityRank;
        }

        return GetInteractiveSpan(candidate).Length < GetInteractiveSpan(currentBest).Length;
    }

    /// <summary>
    /// Converts a diagnostic severity into a hover priority rank.
    /// </summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <returns>A numeric priority where larger values have higher priority.</returns>
    private static int GetSeverityRank(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => 2,
            DiagnosticSeverity.Warning => 1,
            _ => 0,
        };
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

        _toolTip.Hide(_editor);

        var mousePosition = _editor.PointToClient(Control.MousePosition);
        _toolTip.Show(
            diagnostic.GetMessage(),
            _editor,
            mousePosition.X + ToolTipOffsetX,
            mousePosition.Y + ToolTipOffsetY,
            duration: _toolTip.AutoPopDelay);
    }

    /// <summary>
    /// Clears the tooltip content from the editor.
    /// </summary>
    private void ClearToolTip()
    {
        _toolTip.Hide(_editor);
        _toolTip.ToolTipIcon = ToolTipIcon.None;
        _toolTip.ToolTipTitle = string.Empty;
    }
}
