namespace TestUtils;

/// <summary>
/// Panel to display system information
/// </summary>
public partial class RuntimeEnvironmentInfoPanel : UserControl
{
    /// <summary>
    /// Constructor
    /// </summary>
    public RuntimeEnvironmentInfoPanel()
    {
        InitializeComponent();
    }


    /// <summary>
    /// Load the system information
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        labelSystemInfo.Text = TestUtils.RuntimeEnvironmentInfo.Get();
    }
}
