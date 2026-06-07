namespace WinFormsTest;

public partial class FormMain : Form
{
    private TestUtils.SettingsManager<AppSettings> settingsManager;

    public FormMain()
    {
        InitializeComponent();
        settingsManager = new TestUtils.SettingsManager<AppSettings>(name: "AppSettings");
    }


    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var scintillaGroup = demosTreeView.AddGroup(name: "Scintilla");

        scintillaGroup.AddItem(
            name: "Basic",
            tooltip: "A simple Scintilla editor with no additional features",
            parent: this,
            createForm: () => new Demos.BasicDemo.FormBasicDemo(settingsManager.Settings.Demos.BasicDemo));

        scintillaGroup.AddItem(
            name: "Globals",
            tooltip: "Simple global variables to allow data sharing between the application and the script",
            parent: this,
            createForm: () => new Demos.GlobalsDemo.FormGlobals(settingsManager.Settings.Demos.GlobalsDemo));

        scintillaGroup.AddItem(
            name: "OpenCvSharp",
            tooltip: "Demonstrates using a script to perform image processing",
            parent: this,
            createForm: () => new Demos.OpenCvSharpStaticDemo.FormOpenCvSharpDemo(settingsManager.Settings.Demos.OpenCvSharpStaticDemo));

        var otherGroup = demosTreeView.AddGroup(name: "Other");

        otherGroup.AddItem(
            name: "RTF editor",
            tooltip:
                "A basic demo showing the RTF editor - this is currently being used to demonstrate " +
                "that different editors can be supported by the library and is probably not " +
                "intended for use over the Scintilla editor",
            parent: this,
            createForm: () => new Demos.FormRTFDemo());

        otherGroup.AddItem(
            name: "Syntax tree view",
            tooltip:
                "TBD",
            parent: this,
            createForm: () => new Demos.SyntaxTreeViewDemo.FormTreeView(settingsManager.Settings.Demos.TreeViewDemo));

        otherGroup.AddItem(
            name: "Classified spans demo",
            tooltip:
                "TBD",
            parent: this,
            createForm: () => new Demos.ClassifiedSpansDemo.FormDemo(settingsManager.Settings.Demos.ClassifiedSpansDemo));

        demosTreeView.ExpandAllGroups();
    }


    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        settingsManager.Save();
    }
}
