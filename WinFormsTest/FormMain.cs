namespace WinFormsTest;

public partial class FormMain: Form
{
    private TestSupport.SettingsManager<AppSettings> settingsManager;

    public FormMain()
    {
        InitializeComponent();
        settingsManager = new TestSupport.SettingsManager<AppSettings>(name: "AppSettings");
    }


    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var scintillaGroup = demosTreeView.AddGroup(name: "Scintilla");

        scintillaGroup.AddDemo(
            name: "Basic",
            tooltip: "A simple Scintilla editor with no additional features",
            parent: this,
            createForm: () => new Demos.BasicDemo.FormBasicDemo(settingsManager.Settings.Demos.BasicDemo));

        scintillaGroup.AddDemo(
            name: "Globals",
            tooltip: "Simple global variables to allow data sharing between the application and the script",
            parent: this,
            createForm: () => new Demos.GlobalsDemo.FormGlobals(settingsManager.Settings.Demos.GlobalsDemo));

        var otherGroup = demosTreeView.AddGroup(name: "Other");

        otherGroup.AddDemo(
            name: "RTF editor",
            tooltip: 
                "A basic demo showing the RTF editor - this is currently being used to demonstrate " +
                "that different editors can be supported by the library and is probably not " +
                "intended for use over the Scintilla editor",
            parent: this,
            createForm: () => new Demos.FormRTFDemo());

        demosTreeView.ExpandAllGroups();
    }


    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        settingsManager.Save();
    }
}
