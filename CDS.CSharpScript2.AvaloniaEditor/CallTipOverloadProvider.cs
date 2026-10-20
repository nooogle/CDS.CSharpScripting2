using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using CDS.CSharpScript2.APIInfo;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// The content of a call tip: the overloads of one method call site, which of them is shown, and
/// which argument the caret is in. The Avalonia counterpart of the Scintilla editor's
/// <c>CallTipSession</c>; AvaloniaEdit's <see cref="OverloadInsightWindow"/> supplies the
/// "n of m" counter, overload cycling, and the window itself.
/// </summary>
/// <remarks>
/// Shown as:
/// <code>
/// ▲ 2 of 3 ▼ ReturnType Name(type0 p0, type1 p1)
/// Summary.
/// p1: Parameter documentation.
/// </code>
/// with the active parameter in bold.
/// </remarks>
internal sealed class CallTipOverloadProvider : IOverloadProvider
{
    private const double MaxDocumentationWidth = 500;

    private readonly IReadOnlyList<MemberDetailsInfo> _overloads;
    private int _selectedIndex;
    private int _argumentIndex;

    internal CallTipOverloadProvider(IReadOnlyList<MemberDetailsInfo> overloads, int openParenPosition, int argumentIndex)
    {
        _overloads = overloads;
        OpenParenPosition = openParenPosition;
        _argumentIndex = argumentIndex;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The offset of the opening parenthesis of the call this tip describes.
    /// </summary>
    public int OpenParenPosition { get; }

    /// <inheritdoc/>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < 0 || value >= _overloads.Count || value == _selectedIndex)
            {
                return;
            }

            _selectedIndex = value;
            RaiseContentChanged(nameof(SelectedIndex), nameof(CurrentIndexText));
        }
    }

    /// <summary>
    /// The zero-based index of the argument the caret is in.
    /// </summary>
    public int ArgumentIndex
    {
        get => _argumentIndex;
        internal set
        {
            if (value == _argumentIndex)
            {
                return;
            }

            _argumentIndex = value;
            RaiseContentChanged(nameof(ArgumentIndex));
        }
    }

    /// <inheritdoc/>
    public int Count => _overloads.Count;

    /// <inheritdoc/>
    public string CurrentIndexText => $"{_selectedIndex + 1} of {_overloads.Count}";

    /// <inheritdoc/>
    public object CurrentHeader => BuildHeader();

    /// <inheritdoc/>
    public object? CurrentContent => BuildContent();

    /// <summary>
    /// The shown overload's signature as plain text, e.g. <c>double Pow(double x, double y)</c>.
    /// </summary>
    public string SignatureText
    {
        get
        {
            var member = Current;
            var parameters = string.Join(", ", member.Parameters.Select(FormatParameter));
            return $"{ReturnTypePrefix(member)}{member.Name}({parameters})";
        }
    }

    /// <summary>
    /// The highlighted parameter as shown in the signature, or <see langword="null"/> when the
    /// caret is past the shown overload's last parameter.
    /// </summary>
    public string? ActiveParameterText => ActiveParameter is { } parameter ? FormatParameter(parameter) : null;

    /// <summary>
    /// The summary and active parameter's documentation as plain text, one per line.
    /// </summary>
    public string DocumentationText => string.Join("\n", DocumentationLines().Select(line => line.Label + line.Text));

    private MemberDetailsInfo Current => _overloads[_selectedIndex];

    private ParameterInfo? ActiveParameter =>
        _argumentIndex >= 0 && _argumentIndex < Current.Parameters.Count
            ? Current.Parameters[_argumentIndex]
            : null;

    private TextBlock BuildHeader()
    {
        var member = Current;
        var header = new TextBlock();
        var inlines = header.Inlines!;

        inlines.Add(new Run(ReturnTypePrefix(member) + member.Name + "("));

        for (int i = 0; i < member.Parameters.Count; i++)
        {
            if (i > 0)
            {
                inlines.Add(new Run(", "));
            }

            var run = new Run(FormatParameter(member.Parameters[i]));
            if (i == _argumentIndex)
            {
                run.FontWeight = FontWeight.Bold;
            }

            inlines.Add(run);
        }

        inlines.Add(new Run(")"));
        return header;
    }

    private TextBlock? BuildContent()
    {
        var lines = DocumentationLines().ToList();
        if (lines.Count == 0)
        {
            return null;
        }

        var content = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = MaxDocumentationWidth,
        };
        var inlines = content.Inlines!;

        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                inlines.Add(new LineBreak());
            }

            if (lines[i].Label.Length > 0)
            {
                inlines.Add(new Run(lines[i].Label) { FontWeight = FontWeight.Bold });
            }

            inlines.Add(new Run(lines[i].Text));
        }

        return content;
    }

    private IEnumerable<(string Label, string Text)> DocumentationLines()
    {
        if (!string.IsNullOrWhiteSpace(Current.Summary))
        {
            yield return (string.Empty, Current.Summary);
        }

        if (ActiveParameter is { Documentation: { } documentation } parameter &&
            !string.IsNullOrWhiteSpace(documentation))
        {
            yield return ($"{parameter.Name}: ", documentation);
        }
    }

    private static string ReturnTypePrefix(MemberDetailsInfo member)
    {
        return string.IsNullOrEmpty(member.ReturnType) ? string.Empty : member.ReturnType + " ";
    }

    private static string FormatParameter(ParameterInfo parameter)
    {
        return string.IsNullOrEmpty(parameter.DefaultValue)
            ? $"{parameter.Type} {parameter.Name}"
            : $"{parameter.Type} {parameter.Name} = {parameter.DefaultValue}";
    }

    private void RaiseContentChanged(params string[] extraProperties)
    {
        foreach (var property in extraProperties)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentHeader)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentContent)));
    }
}
