using CDS.CSharpScript2.Output;
using System.ComponentModel;
using System.Diagnostics;

namespace CDS.CSharpScript2.ScintillaEditor;


/// <summary>
/// Provides a RichTextBox-based output panel for displaying script output and diagnostics.
/// </summary>
public partial class RTFOutputPanel : UserControl, IOutputPanel
{
    private const string CDSCategory = "CDS";


    /// <summary>
    /// Gets or sets a value indicating whether hyperlinks in the output can be opened by the user.
    /// </summary>
    [Category(CDSCategory)]
    [Description("True to allow the user to click on links in the rich text box, false to prevent the user from clicking on links.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool AllowClickLinks2 { get; set; } = true;


    /// <summary>
    /// Initializes a new instance of the <see cref="RTFOutputPanel"/> class.
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
    /// Opens a clicked hyperlink in the default shell handler when link navigation is enabled.
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
