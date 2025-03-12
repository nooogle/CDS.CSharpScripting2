namespace WinFormsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var rtfEditorManager = new CDS.CSScripting2.Editors.EditorManager(
                rtfEditor.ApplyDiagnostics,
                rtfEditor.ApplySyntaxElements);

            rtfEditor.SetProcessScriptHandler(rtfEditorManager.ProcessScript);

            rtfEditor.Script = @"Console.WriteLineX(""Hello, from the script!"");";


            // ---

            var scintillaEditorManager = new CDS.CSScripting2.Editors.EditorManager(
                scintillaEditor.ApplyDiagnostics,
                scintillaEditor.ApplySyntaxElements);

            scintillaEditor.SetProcessScriptHandler(scintillaEditorManager.ProcessScript);

            scintillaEditor.Script = @"System.Drawing.Point p = System.Drawing.Point.Empty;";
        }
    }
}
