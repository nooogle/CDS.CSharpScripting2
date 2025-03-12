namespace WinFormsTest;


/// <summary>
/// Panel to display system information
/// </summary>
public partial class SystemInfoPanel : UserControl
{
    /// <summary>
    /// Constructor
    /// </summary>
    public SystemInfoPanel()
    {
        InitializeComponent();
    }


    /// <summary>
    /// Load the system information
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        labelSystemInfo.Text = SystemInfo.Get();
    }
}
