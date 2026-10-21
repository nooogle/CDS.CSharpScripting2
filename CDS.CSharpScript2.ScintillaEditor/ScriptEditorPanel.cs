using System.ComponentModel;
using CDS.CSharpScript2.Classification;

namespace CDS.CSharpScript2.ScintillaEditor;

/// <summary>
/// A ready-made scripting surface: a <see cref="ScintillaScriptEditor"/> above an
/// <see cref="RTFOutputPanel"/>, with a <see cref="DocsPanel"/> in a column to their right that
/// appears when the user presses F1 in the editor and goes away when they close it.
/// </summary>
/// <remarks>
/// <para>
/// This is the composition most hosts want, already wired together: drop it on a form instead of
/// placing and connecting the three controls yourself. Hosts that need a different arrangement
/// (a docking pane, a tab, a floating documentation window) can still use the three controls
/// individually.
/// </para>
/// <para>
/// Use <see cref="API"/> for the script, its environment and compilation, <see cref="Output"/> to
/// write results, and <see cref="Theme"/> to restyle the whole surface in one go. As with the
/// individual controls, nothing here follows the OS theme on its own; set <see cref="Theme"/> when
/// the host's theme changes.
/// </para>
/// </remarks>
[Description("A C# script editor with an output panel and an F1 documentation panel.")]
public partial class ScriptEditorPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptEditorPanel"/> class.
    /// </summary>
    public ScriptEditorPanel()
    {
        InitializeComponent();

        // DocsPanel never shows or hides itself. Here it occupies a fixed column that stays hidden
        // until the user asks for help (F1 in the editor) and is hidden again when they close it.
        docsPanel.Editor = scintillaScriptEditor;
        docsPanel.ShowRequested += (_, _) => docsPanel.Visible = true;
        docsPanel.CloseRequested += (_, _) => docsPanel.Visible = false;
    }

    /// <summary>
    /// Gets the script editor control.
    /// </summary>
    /// <remarks>
    /// Most scripting work goes through <see cref="API"/>; use this for the editor's WinForms
    /// surface, such as <see cref="Control.Enabled"/> or its events.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ScintillaScriptEditor Editor => scintillaScriptEditor;

    /// <summary>
    /// Gets the editor's scripting API: the script text, environment, compilation, folds and events.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ScintillaScriptEditor.ScintillaScriptEditorApi API => scintillaScriptEditor.API;

    /// <summary>
    /// Gets the output panel that script output and compiler messages are written to.
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RTFOutputPanel Output => outputPanel;

    /// <summary>
    /// Gets the documentation panel shown beside the editor.
    /// </summary>
    /// <remarks>
    /// The panel is already connected to the editor and shown or hidden by this control. It is
    /// exposed for hosts that need to reach it, for example to hide it by their own means: set
    /// <see cref="Control.Visible"/> to <see langword="false"/> and call <see cref="DocsPanel.Close"/>.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DocsPanel Docs => docsPanel;

    /// <summary>
    /// Gets or sets the theme applied to the editor, the output panel and the documentation panel.
    /// </summary>
    /// <remarks>
    /// The documentation panel follows the editor's theme by itself, so only the editor and the
    /// output panel are set directly. The default is <see cref="EditorTheme.Light"/>.
    /// </remarks>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EditorTheme Theme
    {
        get => scintillaScriptEditor.Theme;
        set
        {
            scintillaScriptEditor.Theme = value;
            outputPanel.Theme = value;
        }
    }
}
