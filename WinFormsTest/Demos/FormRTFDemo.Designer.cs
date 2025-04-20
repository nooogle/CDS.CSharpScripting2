namespace WinFormsTest.Demos
{
    partial class FormRTFDemo
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
            tableLayoutPanel1 = new TableLayoutPanel();
            rtfScriptEditor = new CDS.CSharpScript2.RTFEditor.RTFScriptEditor();
            btnCompile = new Button();
            btnRun = new Button();
            outputPanel = new CDS.CSharpScript2.RTFEditor.RTFOutputPanel();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(rtfScriptEditor, 0, 0);
            tableLayoutPanel1.Controls.Add(outputPanel, 0, 1);
            tableLayoutPanel1.Location = new Point(15, 44);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 66.6666641F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.Size = new Size(913, 509);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // rtfScriptEditor
            // 
            rtfScriptEditor.Dock = DockStyle.Fill;
            rtfScriptEditor.Location = new Point(2, 1);
            rtfScriptEditor.Margin = new Padding(2, 1, 2, 1);
            rtfScriptEditor.Name = "rtfScriptEditor";
            rtfScriptEditor.Script = "";
            rtfScriptEditor.Size = new Size(909, 337);
            rtfScriptEditor.TabIndex = 5;
            // 
            // btnCompile
            // 
            btnCompile.Location = new Point(15, 15);
            btnCompile.Name = "btnCompile";
            btnCompile.Size = new Size(75, 23);
            btnCompile.TabIndex = 8;
            btnCompile.Text = "Compile";
            btnCompile.UseVisualStyleBackColor = true;
            btnCompile.Click += btnCompile_Click;
            // 
            // btnRun
            // 
            btnRun.Location = new Point(96, 15);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(75, 23);
            btnRun.TabIndex = 6;
            btnRun.Text = "Run";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // outputPanel
            // 
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.Location = new Point(3, 342);
            outputPanel.Name = "outputPanel";
            outputPanel.Size = new Size(907, 164);
            outputPanel.TabIndex = 6;
            // 
            // FormRTFDemo
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(943, 568);
            Controls.Add(btnRun);
            Controls.Add(btnCompile);
            Controls.Add(tableLayoutPanel1);
            Name = "FormRTFDemo";
            Padding = new Padding(12);
            Text = "Form1";
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private Button btnCompile;
        private Button btnRun;
        private CDS.CSharpScript2.RTFEditor.RTFScriptEditor rtfScriptEditor;
        private CDS.CSharpScript2.RTFEditor.RTFOutputPanel outputPanel;
    }
}
