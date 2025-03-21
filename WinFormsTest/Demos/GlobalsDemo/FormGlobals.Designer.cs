namespace WinFormsTest.Demos.GlobalsDemo
{
    partial class FormGlobals
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
            scintillaScriptEditor = new CDS.CSharpScriptUtils.Editors.ScintillaEditor.ScintillaScriptEditor();
            outputPanel = new CDS.CSharpScriptUtils.OutputPanels.RTFOutputPanel();
            btnCompile = new Button();
            btnRun = new Button();
            groupBoxScript = new GroupBox();
            propertyGrid1 = new PropertyGrid();
            groupBox1 = new GroupBox();
            tableLayoutPanel1.SuspendLayout();
            groupBoxScript.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(scintillaScriptEditor, 0, 0);
            tableLayoutPanel1.Controls.Add(outputPanel, 0, 1);
            tableLayoutPanel1.Location = new Point(344, 24);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 66.6666641F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.Size = new Size(587, 520);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // scintillaScriptEditor
            // 
            scintillaScriptEditor.Dock = DockStyle.Fill;
            scintillaScriptEditor.Location = new Point(3, 3);
            scintillaScriptEditor.Name = "scintillaScriptEditor";
            scintillaScriptEditor.Script = "";
            scintillaScriptEditor.Size = new Size(581, 340);
            scintillaScriptEditor.TabIndex = 5;
            // 
            // outputPanel
            // 
            outputPanel.AllowClickLinks = true;
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.Location = new Point(3, 349);
            outputPanel.Name = "outputPanel";
            outputPanel.Size = new Size(581, 168);
            outputPanel.TabIndex = 6;
            // 
            // btnCompile
            // 
            btnCompile.Location = new Point(6, 22);
            btnCompile.Name = "btnCompile";
            btnCompile.Size = new Size(75, 23);
            btnCompile.TabIndex = 8;
            btnCompile.Text = "Compile";
            btnCompile.UseVisualStyleBackColor = true;
            btnCompile.Click += btnCompile_Click;
            // 
            // btnRun
            // 
            btnRun.Location = new Point(87, 22);
            btnRun.Name = "btnRun";
            btnRun.Size = new Size(75, 23);
            btnRun.TabIndex = 6;
            btnRun.Text = "Run";
            btnRun.UseVisualStyleBackColor = true;
            btnRun.Click += btnRun_Click;
            // 
            // groupBoxScript
            // 
            groupBoxScript.Controls.Add(btnCompile);
            groupBoxScript.Controls.Add(btnRun);
            groupBoxScript.Location = new Point(15, 24);
            groupBoxScript.Name = "groupBoxScript";
            groupBoxScript.Size = new Size(314, 60);
            groupBoxScript.TabIndex = 12;
            groupBoxScript.TabStop = false;
            groupBoxScript.Text = "Script";
            // 
            // propertyGrid1
            // 
            propertyGrid1.Location = new Point(15, 31);
            propertyGrid1.Name = "propertyGrid1";
            propertyGrid1.Size = new Size(284, 227);
            propertyGrid1.TabIndex = 13;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(propertyGrid1);
            groupBox1.Location = new Point(15, 94);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(12);
            groupBox1.Size = new Size(314, 273);
            groupBox1.TabIndex = 14;
            groupBox1.TabStop = false;
            groupBox1.Text = "Globals";
            // 
            // FormGlobals
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(943, 568);
            Controls.Add(groupBox1);
            Controls.Add(groupBoxScript);
            Controls.Add(tableLayoutPanel1);
            Name = "FormGlobals";
            Padding = new Padding(12);
            Text = "Globals demo";
            tableLayoutPanel1.ResumeLayout(false);
            groupBoxScript.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private Button btnCompile;
        private Button btnRun;
        private GroupBox groupBoxScript;
        private PropertyGrid propertyGrid1;
        private GroupBox groupBox1;
        private CDS.CSharpScriptUtils.Editors.ScintillaEditor.ScintillaScriptEditor scintillaScriptEditor;
        private CDS.CSharpScriptUtils.OutputPanels.RTFOutputPanel outputPanel;
    }
}
