using System.Text;

using CDS.CSharpScript2.APIInfo;

using ScintillaNET;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// Manages a single Scintilla call tip session for one method call site.
/// Handles overload cycling and active-parameter highlighting.
/// </summary>
internal sealed class CallTipSession
{
    private readonly Scintilla _scintilla;
    private readonly IReadOnlyList<MemberDetailsInfo> _overloads;
    private int _overloadIndex;
    private int _argumentIndex;

    /// <summary>
    /// Character position of the opening parenthesis that started this session.
    /// Used to anchor the call tip and to detect when the cursor leaves the call.
    /// </summary>
    public int OpenParenPosition { get; }

    /// <summary>
    /// Starts a call tip session and immediately shows the tip at <paramref name="openParenPosition"/>.
    /// </summary>
    public CallTipSession(
        Scintilla scintilla,
        IReadOnlyList<MemberDetailsInfo> overloads,
        int openParenPosition,
        int argumentIndex)
    {
        _scintilla = scintilla;
        _overloads = overloads;
        _overloadIndex = 0;
        _argumentIndex = argumentIndex;
        OpenParenPosition = openParenPosition;

        Show();
    }

    /// <summary>Updates the highlighted parameter when the cursor moves to a different argument.</summary>
    public void UpdateArgument(int newIndex)
    {
        _argumentIndex = newIndex;
        Show();
    }

    /// <summary>Advances to the next overload (wraps around).</summary>
    public void NextOverload()
    {
        if (_overloads.Count <= 1)
            return;

        _overloadIndex = (_overloadIndex + 1) % _overloads.Count;
        Show();
    }

    /// <summary>Retreats to the previous overload (wraps around).</summary>
    public void PreviousOverload()
    {
        if (_overloads.Count <= 1)
            return;

        _overloadIndex = (_overloadIndex - 1 + _overloads.Count) % _overloads.Count;
        Show();
    }

    /// <summary>Hides the call tip and ends the session.</summary>
    public void Cancel() => _scintilla.CallTipCancel();

    // ── Private helpers ───────────────────────────────────────────────────────

    private void Show()
    {
        var member = _overloads[_overloadIndex];
        var (text, hltStart, hltEnd) = BuildCallTip(member, _overloadIndex, _overloads.Count, _argumentIndex);

        _scintilla.CallTipShow(OpenParenPosition, text);

        if (hltStart < hltEnd)
            _scintilla.CallTipSetHlt(hltStart, hltEnd);
    }

    /// <summary>
    /// Formats the call tip text and returns the highlight range for the active parameter.
    /// </summary>
    /// <remarks>
    /// <para>Format (single overload):</para>
    /// <code>
    /// ReturnType Name(type0 p0, type1 p1)
    /// Summary.
    /// p1: Parameter documentation.
    /// </code>
    /// <para>Format (multiple overloads):</para>
    /// <code>
    /// ↑ 2 of 3 ↓
    /// ReturnType Name(type0 p0, type1 p1)
    /// Summary.
    /// p1: Parameter documentation.
    /// </code>
    /// Scintilla renders \x01 as an up-arrow button and \x02 as a down-arrow button.
    /// </remarks>
    private static (string text, int hltStart, int hltEnd) BuildCallTip(
        MemberDetailsInfo member,
        int overloadIndex,
        int overloadCount,
        int activeParamIndex)
    {
        var sb = new StringBuilder();

        if (overloadCount > 1)
            sb.Append($"\x01 {overloadIndex + 1} of {overloadCount} \x02\n");

        if (!string.IsNullOrEmpty(member.ReturnType))
        {
            sb.Append(member.ReturnType);
            sb.Append(' ');
        }

        sb.Append(member.Name);
        sb.Append('(');

        int hltStart = 0;
        int hltEnd = 0;

        for (int i = 0; i < member.Parameters.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");

            var param = member.Parameters[i];
            var paramText = string.IsNullOrEmpty(param.DefaultValue)
                ? $"{param.Type} {param.Name}"
                : $"{param.Type} {param.Name} = {param.DefaultValue}";

            if (i == activeParamIndex)
                hltStart = sb.Length;

            sb.Append(paramText);

            if (i == activeParamIndex)
                hltEnd = sb.Length;
        }

        sb.Append(')');

        if (!string.IsNullOrWhiteSpace(member.Summary))
        {
            sb.Append('\n');
            sb.Append(member.Summary);
        }

        if (activeParamIndex >= 0 && activeParamIndex < member.Parameters.Count)
        {
            var doc = member.Parameters[activeParamIndex].Documentation;
            if (!string.IsNullOrWhiteSpace(doc))
            {
                sb.Append('\n');
                sb.Append(member.Parameters[activeParamIndex].Name);
                sb.Append(": ");
                sb.Append(doc);
            }
        }

        return (sb.ToString(), hltStart, hltEnd);
    }
}
