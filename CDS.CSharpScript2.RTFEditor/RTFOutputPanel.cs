using System.ComponentModel;
using System.Diagnostics;

namespace CDS.CSharpScript2.RTFEditor;

/// <summary>
/// A rich text format output panel that displays text with formatting capabilities.
/// Implements the <see cref="IOutputPanel"/> interface for consistent output handling.
/// </summary>
public partial class RTFOutputPanel : UserControl, Output.IOutputPanel
{
    private const string CDSCategory = "CDS";

    /// <summary>
    /// True to allow the user to click on links in the rich text box, false to prevent the user from clicking on links.
    /// </summary>
    [Category(CDSCategory)]
    [Description("True to allow the user to click on links in the rich text box, false to prevent the user from clicking on links.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool AllowClickLinks2 { get; set; } = true;


    /// <summary>
    /// Initializes a new instance of the <see cref="RTFOutputPanel"/> class.
    /// Sets up the rich text box control and any required event handlers.
    /// </summary>
    public RTFOutputPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Appends the specified text to the output panel without adding a line break.
    /// </summary>
    /// <param name="text">The text to append to the output panel.</param>
    public void Append(string text)
    {
        richTextBox.AppendText(text);
    }

    /// <summary>
    /// Appends the specified text to the output panel and adds a line break.
    /// </summary>
    /// <param name="text">The text to append to the output panel.</param>
    public void AppendLine(string text)
    {
        richTextBox.AppendText(text + Environment.NewLine);
    }

    /// <summary>
    /// Clears all text from the output panel.
    /// </summary>
    public void Clear()
    {
        richTextBox.Clear();
    }


    /// <summary>
    /// The user has clicked on a link in the rich text box. Open the link in the default browser (if allowed).
    /// </summary>
    private void richTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
    {
        if (!AllowClickLinks2)
        {
            return;
        }

        try
        {
            if (e.LinkText != null)
            {
                Process.Start(e.LinkText);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}
