namespace CDS.CSharpScript2.ScintillaEditor.CustomToolTip;

public sealed class PopupHost : ToolStripDropDown
{
    public PopupHost(Control content)
    {
        AutoClose = true;  // closes on outside click or Esc
        DoubleBuffered = true;
        Padding = Margin = Padding.Empty;

        var host = new ToolStripControlHost(content)
        {
            AutoSize = false,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        Items.Add(host);

        content.SizeChanged += (_, __) => host.Size = content.Size;
    }

    public void ShowAt(Control anchor, Point clientLocation)
    {
        var screen = anchor.PointToScreen(clientLocation);
        Show(screen);
    }
}