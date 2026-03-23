namespace WinFormsTest;

public partial class FormMain : Form
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

        scintillaGroup.AddDemo(
            name: "OpenCvSharp",
            tooltip: "Demonstrates using a script to perform image processing",
            parent: this,
            createForm: () => new Demos.OpenCvSharpDemo.FormOpenCvSharpDemo(settingsManager.Settings.Demos.OpenCvSharpDemo));

        var otherGroup = demosTreeView.AddGroup(name: "Other");

        otherGroup.AddDemo(
            name: "RTF editor",
            tooltip:
                "A basic demo showing the RTF editor - this is currently being used to demonstrate " +
                "that different editors can be supported by the library and is probably not " +
                "intended for use over the Scintilla editor",
            parent: this,
            createForm: () => new Demos.FormRTFDemo());

        otherGroup.AddDemo(
            name: "Syntax tree view",
            tooltip:
                "TBD",
            parent: this,
            createForm: () => new Demos.SyntaxTreeViewDemo.FormTreeView(settingsManager.Settings.Demos.TreeViewDemo));

        otherGroup.AddDemo(
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

    private void TestPopup()
    {
        var c = new CDS.CSharpScript2.ScintillaEditor.CustomToolTip.SignatureHelpView();

        var items = new List<CDS.CSharpScript2.ScintillaEditor.CustomToolTip.SignatureItem>();

        var p1 = new CDS.CSharpScript2.ScintillaEditor.CustomToolTip.Param("param1");
        var p2 = new CDS.CSharpScript2.ScintillaEditor.CustomToolTip.Param("param2");

        var si1 = new CDS.CSharpScript2.ScintillaEditor.CustomToolTip.SignatureItem(
            prefix: "PREFIX1",
            parameters: [p1, p2],
            suffix: "SUFFIX1",
            documentation: "DOCUMENTATION1");

        var p3 = new CDS.CSharpScript2.ScintillaEditor.CustomToolTip.Param("param3");

        var si2 = new CDS.CSharpScript2.ScintillaEditor.CustomToolTip.SignatureItem(
            prefix: "PREFIX2",
            parameters: [p3],
            suffix: "SUFFIX2",
            documentation: "DOCUMENTATION2");

        items.Add(si1);
        items.Add(si2);

        c.SetItems(items, currentIndex: 0);

        var p = new CDS.CSharpScript2.ScintillaEditor.CustomToolTip.PopupHost(content: c);
        p.Show(control: systemInfoPanel1, position: new Point(50, 50));
    }

    private void button1_Click(object sender, EventArgs e)
    {
        TestPopup();
    }
}
