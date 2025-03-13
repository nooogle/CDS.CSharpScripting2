using System.Diagnostics;

namespace WinFormsTest
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Globals class for the demo
        /// </summary>
        public class Globals
        {
            public string Animal { get; set; } = "Cat";
        }


        private CDS.CSScripting2.Editors.IEditor? editor;
        private CDS.CSScripting2.Editors.EditorManager? editorManager;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnCreateScintillaEditor_Click(object sender, EventArgs e)
        {
            CreateEditor(() => new CDS.CSScripting2.Editors.ScintillaEditor.ScintillaScriptEditor());
        }


        private void CreateEditor(Func<CDS.CSScripting2.Editors.IEditor> editorFactory)
        {
            editor = editorFactory();
            InitialiseEditor();
            PlaceEditorOnForm();
            groupBoxCreate.Enabled = false;
            groupBoxScript.Enabled = true;
        }

        private void InitialiseEditor()
        {
            if (editor == null)
            {
                throw new InvalidOperationException("Editor control not created");
            }

            var env =
                CDS.CSScripting2.Env.Default
                .WithDrawingReferences()
                .WithGlobalType(typeof(Globals));

            editorManager = new CDS.CSScripting2.Editors.EditorManager(
                environment: env,
                editor.ApplyDiagnostics,
                editor.ApplySyntaxElements);

            editor.SetProcessScriptHandler(editorManager.ProcessScript);

            editor.Script = @"System.Drawing.Point p = System.Drawing.Point.Empty;";
        }

        private void btnCreateRTFEditor_Click(object sender, EventArgs e)
        {
            CreateEditor(() => new CDS.CSScripting2.Editors.RichTextEditor.RTFScriptEditor());
        }

        private void PlaceEditorOnForm()
        {
            var editorAsControl = editor as Control;

            if (editorAsControl == null)
            {
                throw new InvalidOperationException("Editor control not created");
            }

            tableLayoutPanel1.Controls.Add(editorAsControl, 0, 0);
            editorAsControl.Dock = DockStyle.Fill;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {

        }


        private async void btnCompile_Click(object sender, EventArgs e)
        {
            Enabled = false;
            outputPanel.Clear();

            var stopWatch = Stopwatch.StartNew();
            var compilationOutput = await editorManager!.CompileAsync();
            stopWatch.Stop();

            foreach (var message in compilationOutput.Messages)
            {
                outputPanel.AppendText(message + Environment.NewLine);
            }

            outputPanel.AppendText(
                $"Compilation took {stopWatch.ElapsedMilliseconds}ms and completed with " +
                $"{compilationOutput.WarningCount} warnings and " +
                $"{compilationOutput.ErrorCount} errors.");

            Enabled = true;
        }
    }
}
