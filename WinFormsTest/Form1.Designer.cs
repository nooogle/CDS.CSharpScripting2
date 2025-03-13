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
            tableLayoutPanel1 = new TableLayoutPanel();
            outputPanel = new RichTextBox();
            btnCompile = new Button();
            btnRun = new Button();
            systemInfoPanel1 = new SystemInfoPanel();
            btnCreateScintillaEditor = new Button();
            btnCreateRTFEditor = new Button();
            groupBoxCreate = new GroupBox();
            groupBoxScript = new GroupBox();
            tableLayoutPanel1.SuspendLayout();
            groupBoxCreate.SuspendLayout();
            groupBoxScript.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(outputPanel, 0, 1);
            tableLayoutPanel1.Location = new Point(12, 187);
            tableLayoutPanel1.Margin = new Padding(12);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 66.6666641F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.Size = new Size(776, 310);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // outputPanel
            // 
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.Location = new Point(3, 209);
            outputPanel.Name = "outputPanel";
            outputPanel.Size = new Size(770, 98);
            outputPanel.TabIndex = 4;
            outputPanel.Text = "";
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
            // systemInfoPanel1
            // 
            systemInfoPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            systemInfoPanel1.BorderStyle = BorderStyle.FixedSingle;
            systemInfoPanel1.Location = new Point(12, 15);
            systemInfoPanel1.Name = "systemInfoPanel1";
            systemInfoPanel1.Size = new Size(776, 51);
            systemInfoPanel1.TabIndex = 3;
            // 
            // btnCreateScintillaEditor
            // 
            btnCreateScintillaEditor.Location = new Point(15, 22);
            btnCreateScintillaEditor.Name = "btnCreateScintillaEditor";
            btnCreateScintillaEditor.Size = new Size(140, 23);
            btnCreateScintillaEditor.TabIndex = 9;
            btnCreateScintillaEditor.Text = "Create Scintilla editor";
            btnCreateScintillaEditor.UseVisualStyleBackColor = true;
            btnCreateScintillaEditor.Click += btnCreateScintillaEditor_Click;
            // 
            // btnCreateRTFEditor
            // 
            btnCreateRTFEditor.Location = new Point(161, 22);
            btnCreateRTFEditor.Name = "btnCreateRTFEditor";
            btnCreateRTFEditor.Size = new Size(140, 23);
            btnCreateRTFEditor.TabIndex = 10;
            btnCreateRTFEditor.Text = "Create RTF editor";
            btnCreateRTFEditor.UseVisualStyleBackColor = true;
            btnCreateRTFEditor.Click += btnCreateRTFEditor_Click;
            // 
            // groupBoxCreate
            // 
            groupBoxCreate.Controls.Add(btnCreateScintillaEditor);
            groupBoxCreate.Controls.Add(btnCreateRTFEditor);
            groupBoxCreate.Location = new Point(12, 72);
            groupBoxCreate.Name = "groupBoxCreate";
            groupBoxCreate.Size = new Size(314, 60);
            groupBoxCreate.TabIndex = 11;
            groupBoxCreate.TabStop = false;
            groupBoxCreate.Text = "Editor";
            // 
            // groupBoxScript
            // 
            groupBoxScript.Controls.Add(btnCompile);
            groupBoxScript.Controls.Add(btnRun);
            groupBoxScript.Enabled = false;
            groupBoxScript.Location = new Point(332, 72);
            groupBoxScript.Name = "groupBoxScript";
            groupBoxScript.Size = new Size(314, 60);
            groupBoxScript.TabIndex = 12;
            groupBoxScript.TabStop = false;
            groupBoxScript.Text = "Script";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 521);
            Controls.Add(groupBoxScript);
            Controls.Add(groupBoxCreate);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(systemInfoPanel1);
            Name = "Form1";
            Padding = new Padding(12);
            Text = "Form1";
            tableLayoutPanel1.ResumeLayout(false);
            groupBoxCreate.ResumeLayout(false);
            groupBoxScript.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private SystemInfoPanel systemInfoPanel1;
        private Button btnCompile;
        private Button btnRun;
        private RichTextBox outputPanel;
        private Button btnCreateScintillaEditor;
        private Button btnCreateRTFEditor;
        private GroupBox groupBoxCreate;
        private GroupBox groupBoxScript;
    }
}
