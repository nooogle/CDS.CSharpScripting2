using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit;

namespace CDS.CSharpScript2.AvaloniaEditor;

/// <summary>
/// Cross-platform script editor control built on AvaloniaEdit.
/// </summary>
/// <remarks>
/// Placeholder: currently plain text editing only. Roslyn-backed classification, completion and
/// diagnostics will be added by wiring this control to <c>EditorManager</c>.
/// </remarks>
public class AvaloniaScriptEditor : UserControl
{
    private readonly TextEditor _textEditor;

    /// <summary>
    /// Initialises a new instance of the <see cref="AvaloniaScriptEditor"/> class.
    /// </summary>
    public AvaloniaScriptEditor()
    {
        _textEditor = new TextEditor
        {
            ShowLineNumbers = true,
            FontFamily = new FontFamily("Cascadia Mono, Menlo, Consolas, monospace"),
            FontSize = 14,
        };

        Content = _textEditor;
    }

    /// <summary>
    /// Gets or sets the script text.
    /// </summary>
    public string Text
    {
        get => _textEditor.Text;
        set => _textEditor.Text = value;
    }
}
