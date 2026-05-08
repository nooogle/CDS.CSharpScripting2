namespace WinFormsTest.Demos.SyntaxTreeViewDemo
{
    partial class FormTreeView
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            scintillaScriptEditor = new CDS.CSharpScript2.ScintillaEditor.ScintillaScriptEditor();
            treeView1 = new TreeView();
            SuspendLayout();
            // 
            // scintillaScriptEditor
            // 
            scintillaScriptEditor.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            scintillaScriptEditor.Location = new Point(12, 12);
            scintillaScriptEditor.Name = "scintillaScriptEditor";
            scintillaScriptEditor.Script = "";
            scintillaScriptEditor.Size = new Size(609, 161);
            scintillaScriptEditor.TabIndex = 6;
            // 
            // treeView1
            // 
            treeView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            treeView1.Location = new Point(12, 179);
            treeView1.Name = "treeView1";
            treeView1.Size = new Size(609, 371);
            treeView1.TabIndex = 7;
            treeView1.AfterSelect += treeView1_AfterSelect;
            // 
            // FormTreeView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(633, 562);
            Controls.Add(treeView1);
            Controls.Add(scintillaScriptEditor);
            Name = "FormTreeView";
            Text = "FormTreeView";
            ResumeLayout(false);
        }

        #endregion

        private CDS.CSharpScript2.ScintillaEditor.ScintillaScriptEditor scintillaScriptEditor;
        private TreeView treeView1;
    }
}