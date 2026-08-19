namespace CDS.CSharpScript2.WinForms.Sample.Demos.BasicDemo
{
    partial class FormBasicDemo
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
            scintillaScriptEditor = new CDS.CSharpScript2.ScintillaEditor.ScintillaScriptEditor();
            outputPanel = new CDS.CSharpScript2.ScintillaEditor.RTFOutputPanel();
            btnCompile = new Button();
            btnRun = new Button();
            btnExpandAllFolds = new Button();
            btnCollapseAllFolds = new Button();
            groupBoxTheme = new GroupBox();
            rbThemeLight = new RadioButton();
            rbThemeDark = new RadioButton();
            rbThemeSystem = new RadioButton();
            tableLayoutPanel1.SuspendLayout();
            groupBoxTheme.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(scintillaScriptEditor, 0, 0);
            tableLayoutPanel1.Controls.Add(outputPanel, 0, 1);
            tableLayoutPanel1.Location = new Point(15, 44);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 66.6666641F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.Size = new Size(599, 397);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // scintillaScriptEditor
            // 
            scintillaScriptEditor.Dock = DockStyle.Fill;
            scintillaScriptEditor.Location = new Point(3, 3);
            scintillaScriptEditor.Name = "scintillaScriptEditor";
            scintillaScriptEditor.Size = new Size(593, 258);
            scintillaScriptEditor.TabIndex = 5;
            // 
            // outputPanel
            // 
            outputPanel.AllowClickLinks2 = true;
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.Location = new Point(3, 267);
            outputPanel.Name = "outputPanel";
            outputPanel.Size = new Size(593, 127);
            outputPanel.TabIndex = 6;
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
            // btnExpandAllFolds
            //
            btnExpandAllFolds.Location = new Point(177, 15);
            btnExpandAllFolds.Name = "btnExpandAllFolds";
            btnExpandAllFolds.Size = new Size(100, 23);
            btnExpandAllFolds.TabIndex = 9;
            btnExpandAllFolds.Text = "Expand All";
            btnExpandAllFolds.UseVisualStyleBackColor = true;
            btnExpandAllFolds.Click += btnExpandAllFolds_Click;
            //
            // btnCollapseAllFolds
            //
            btnCollapseAllFolds.Location = new Point(283, 15);
            btnCollapseAllFolds.Name = "btnCollapseAllFolds";
            btnCollapseAllFolds.Size = new Size(100, 23);
            btnCollapseAllFolds.TabIndex = 10;
            btnCollapseAllFolds.Text = "Collapse All";
            btnCollapseAllFolds.UseVisualStyleBackColor = true;
            btnCollapseAllFolds.Click += btnCollapseAllFolds_Click;
            //
            // groupBoxTheme
            //
            groupBoxTheme.Controls.Add(rbThemeLight);
            groupBoxTheme.Controls.Add(rbThemeDark);
            groupBoxTheme.Controls.Add(rbThemeSystem);
            groupBoxTheme.Location = new Point(399, 8);
            groupBoxTheme.Name = "groupBoxTheme";
            groupBoxTheme.Size = new Size(210, 40);
            groupBoxTheme.TabIndex = 11;
            groupBoxTheme.TabStop = false;
            groupBoxTheme.Text = "Theme";
            //
            // rbThemeLight
            //
            rbThemeLight.AutoSize = true;
            rbThemeLight.Location = new Point(10, 17);
            rbThemeLight.Name = "rbThemeLight";
            rbThemeLight.Size = new Size(51, 19);
            rbThemeLight.TabIndex = 0;
            rbThemeLight.Text = "Light";
            rbThemeLight.UseVisualStyleBackColor = true;
            rbThemeLight.CheckedChanged += rbThemeLight_CheckedChanged;
            //
            // rbThemeDark
            //
            rbThemeDark.AutoSize = true;
            rbThemeDark.Location = new Point(72, 17);
            rbThemeDark.Name = "rbThemeDark";
            rbThemeDark.Size = new Size(52, 19);
            rbThemeDark.TabIndex = 1;
            rbThemeDark.Text = "Dark";
            rbThemeDark.UseVisualStyleBackColor = true;
            rbThemeDark.CheckedChanged += rbThemeDark_CheckedChanged;
            //
            // rbThemeSystem
            //
            rbThemeSystem.AutoSize = true;
            rbThemeSystem.Location = new Point(135, 17);
            rbThemeSystem.Name = "rbThemeSystem";
            rbThemeSystem.Size = new Size(68, 19);
            rbThemeSystem.TabIndex = 2;
            rbThemeSystem.Text = "System";
            rbThemeSystem.UseVisualStyleBackColor = true;
            rbThemeSystem.CheckedChanged += rbThemeSystem_CheckedChanged;
            //
            // FormBasicDemo
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(626, 465);
            Controls.Add(groupBoxTheme);
            Controls.Add(btnCollapseAllFolds);
            Controls.Add(btnExpandAllFolds);
            Controls.Add(btnRun);
            Controls.Add(btnCompile);
            Controls.Add(tableLayoutPanel1);
            Name = "FormBasicDemo";
            Padding = new Padding(12);
            Text = "Basic demo";
            tableLayoutPanel1.ResumeLayout(false);
            groupBoxTheme.ResumeLayout(false);
            groupBoxTheme.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private Button btnCompile;
        private Button btnRun;
        private Button btnExpandAllFolds;
        private Button btnCollapseAllFolds;
        private CDS.CSharpScript2.ScintillaEditor.ScintillaScriptEditor scintillaScriptEditor;
        private CDS.CSharpScript2.ScintillaEditor.RTFOutputPanel outputPanel;
        private GroupBox groupBoxTheme;
        private RadioButton rbThemeLight;
        private RadioButton rbThemeDark;
        private RadioButton rbThemeSystem;
    }
}
