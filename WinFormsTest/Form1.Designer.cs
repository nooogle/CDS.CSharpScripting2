namespace WinFormsTest
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            rtfEditor = new CDS.CSScripting2.Editors.RichTextEditor.RTFScriptEditor();
            scintillaEditor = new CDS.CSScripting2.Editors.ScintillaEditor.ScintillaScriptEditor();
            tableLayoutPanel1 = new TableLayoutPanel();
            label1 = new Label();
            label2 = new Label();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // rtfEditor
            // 
            rtfEditor.Dock = DockStyle.Fill;
            rtfEditor.Location = new Point(2, 25);
            rtfEditor.Margin = new Padding(2, 1, 2, 1);
            rtfEditor.Name = "rtfEditor";
            rtfEditor.Script = "";
            rtfEditor.Size = new Size(384, 400);
            rtfEditor.TabIndex = 0;
            // 
            // scintillaEditor
            // 
            scintillaEditor.Dock = DockStyle.Fill;
            scintillaEditor.Location = new Point(391, 27);
            scintillaEditor.Name = "scintillaEditor";
            scintillaEditor.Script = "";
            scintillaEditor.Size = new Size(382, 396);
            scintillaEditor.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(label2, 1, 0);
            tableLayoutPanel1.Controls.Add(rtfEditor, 0, 1);
            tableLayoutPanel1.Controls.Add(scintillaEditor, 1, 1);
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Location = new Point(12, 12);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 5.633803F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 94.3661957F));
            tableLayoutPanel1.Size = new Size(776, 426);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(26, 15);
            label1.TabIndex = 2;
            label1.Text = "RTF";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(391, 0);
            label2.Name = "label2";
            label2.Size = new Size(48, 15);
            label2.TabIndex = 3;
            label2.Text = "Scintilla";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(tableLayoutPanel1);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private CDS.CSScripting2.Editors.RichTextEditor.RTFScriptEditor rtfEditor;
        private CDS.CSScripting2.Editors.ScintillaEditor.ScintillaScriptEditor scintillaEditor;
        private TableLayoutPanel tableLayoutPanel1;
        private Label label2;
        private Label label1;
    }
}
