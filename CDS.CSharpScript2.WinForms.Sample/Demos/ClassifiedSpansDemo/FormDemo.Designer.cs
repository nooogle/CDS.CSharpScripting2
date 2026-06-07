namespace CDS.CSharpScript2.WinForms.Sample.Demos.ClassifiedSpansDemo
{
    partial class FormDemo
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
            listViewInfo = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
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
            // listViewInfo
            // 
            listViewInfo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listViewInfo.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4 });
            listViewInfo.FullRowSelect = true;
            listViewInfo.GridLines = true;
            listViewInfo.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listViewInfo.HideSelection = true;
            listViewInfo.Location = new Point(12, 179);
            listViewInfo.Name = "listViewInfo";
            listViewInfo.Size = new Size(609, 371);
            listViewInfo.TabIndex = 7;
            listViewInfo.UseCompatibleStateImageBehavior = false;
            listViewInfo.View = View.Details;
            listViewInfo.SelectedIndexChanged += listViewInfo_SelectedIndexChanged;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Type";
            columnHeader1.Width = 100;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Start";
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Length";
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "Script";
            // 
            // FormDemo
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(633, 562);
            Controls.Add(listViewInfo);
            Controls.Add(scintillaScriptEditor);
            Name = "FormDemo";
            Text = "FormTreeView";
            ResumeLayout(false);
        }

        #endregion

        private CDS.CSharpScript2.ScintillaEditor.ScintillaScriptEditor scintillaScriptEditor;
        private ListView listViewInfo;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader4;
    }
}