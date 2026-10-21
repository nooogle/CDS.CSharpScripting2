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
            this.scriptPanel = new CDS.CSharpScript2.ScintillaEditor.ScriptEditorPanel();
            this.btnCompile = new System.Windows.Forms.Button();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnExpandAllFolds = new System.Windows.Forms.Button();
            this.btnCollapseAllFolds = new System.Windows.Forms.Button();
            this.groupBoxTheme = new System.Windows.Forms.GroupBox();
            this.rbThemeLight = new System.Windows.Forms.RadioButton();
            this.rbThemeDark = new System.Windows.Forms.RadioButton();
            this.rbThemeSystem = new System.Windows.Forms.RadioButton();
            this.groupBoxTheme.SuspendLayout();
            this.SuspendLayout();
            // 
            // scriptPanel
            // 
            this.scriptPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scriptPanel.Location = new System.Drawing.Point(13, 48);
            this.scriptPanel.Name = "scriptPanel";
            this.scriptPanel.Size = new System.Drawing.Size(513, 334);
            this.scriptPanel.TabIndex = 2;
            // 
            // btnCompile
            // 
            this.btnCompile.Location = new System.Drawing.Point(13, 13);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(64, 20);
            this.btnCompile.TabIndex = 8;
            this.btnCompile.Text = "Compile";
            this.btnCompile.UseVisualStyleBackColor = true;
            this.btnCompile.Click += new System.EventHandler(this.btnCompile_Click);
            // 
            // btnRun
            // 
            this.btnRun.Location = new System.Drawing.Point(82, 13);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(64, 20);
            this.btnRun.TabIndex = 6;
            this.btnRun.Text = "Run";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // btnExpandAllFolds
            // 
            this.btnExpandAllFolds.Location = new System.Drawing.Point(152, 13);
            this.btnExpandAllFolds.Name = "btnExpandAllFolds";
            this.btnExpandAllFolds.Size = new System.Drawing.Size(86, 20);
            this.btnExpandAllFolds.TabIndex = 9;
            this.btnExpandAllFolds.Text = "Expand All";
            this.btnExpandAllFolds.UseVisualStyleBackColor = true;
            this.btnExpandAllFolds.Click += new System.EventHandler(this.btnExpandAllFolds_Click);
            // 
            // btnCollapseAllFolds
            // 
            this.btnCollapseAllFolds.Location = new System.Drawing.Point(243, 13);
            this.btnCollapseAllFolds.Name = "btnCollapseAllFolds";
            this.btnCollapseAllFolds.Size = new System.Drawing.Size(86, 20);
            this.btnCollapseAllFolds.TabIndex = 10;
            this.btnCollapseAllFolds.Text = "Collapse All";
            this.btnCollapseAllFolds.UseVisualStyleBackColor = true;
            this.btnCollapseAllFolds.Click += new System.EventHandler(this.btnCollapseAllFolds_Click);
            // 
            // groupBoxTheme
            // 
            this.groupBoxTheme.Controls.Add(this.rbThemeLight);
            this.groupBoxTheme.Controls.Add(this.rbThemeDark);
            this.groupBoxTheme.Controls.Add(this.rbThemeSystem);
            this.groupBoxTheme.Location = new System.Drawing.Point(342, 7);
            this.groupBoxTheme.Name = "groupBoxTheme";
            this.groupBoxTheme.Size = new System.Drawing.Size(180, 35);
            this.groupBoxTheme.TabIndex = 11;
            this.groupBoxTheme.TabStop = false;
            this.groupBoxTheme.Text = "Theme";
            // 
            // rbThemeLight
            // 
            this.rbThemeLight.AutoSize = true;
            this.rbThemeLight.Location = new System.Drawing.Point(9, 15);
            this.rbThemeLight.Name = "rbThemeLight";
            this.rbThemeLight.Size = new System.Drawing.Size(48, 17);
            this.rbThemeLight.TabIndex = 0;
            this.rbThemeLight.Text = "Light";
            this.rbThemeLight.UseVisualStyleBackColor = true;
            this.rbThemeLight.CheckedChanged += new System.EventHandler(this.rbThemeLight_CheckedChanged);
            // 
            // rbThemeDark
            // 
            this.rbThemeDark.AutoSize = true;
            this.rbThemeDark.Location = new System.Drawing.Point(62, 15);
            this.rbThemeDark.Name = "rbThemeDark";
            this.rbThemeDark.Size = new System.Drawing.Size(48, 17);
            this.rbThemeDark.TabIndex = 1;
            this.rbThemeDark.Text = "Dark";
            this.rbThemeDark.UseVisualStyleBackColor = true;
            this.rbThemeDark.CheckedChanged += new System.EventHandler(this.rbThemeDark_CheckedChanged);
            // 
            // rbThemeSystem
            // 
            this.rbThemeSystem.AutoSize = true;
            this.rbThemeSystem.Location = new System.Drawing.Point(116, 15);
            this.rbThemeSystem.Name = "rbThemeSystem";
            this.rbThemeSystem.Size = new System.Drawing.Size(59, 17);
            this.rbThemeSystem.TabIndex = 2;
            this.rbThemeSystem.Text = "System";
            this.rbThemeSystem.UseVisualStyleBackColor = true;
            this.rbThemeSystem.CheckedChanged += new System.EventHandler(this.rbThemeSystem_CheckedChanged);
            // 
            // FormBasicDemo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(537, 403);
            this.Controls.Add(this.groupBoxTheme);
            this.Controls.Add(this.btnCollapseAllFolds);
            this.Controls.Add(this.btnExpandAllFolds);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.btnCompile);
            this.Controls.Add(this.scriptPanel);
            this.Name = "FormBasicDemo";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.Text = "Basic demo";
            this.groupBoxTheme.ResumeLayout(false);
            this.groupBoxTheme.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private CDS.CSharpScript2.ScintillaEditor.ScriptEditorPanel scriptPanel;
        private Button btnCompile;
        private Button btnRun;
        private Button btnExpandAllFolds;
        private Button btnCollapseAllFolds;
        private GroupBox groupBoxTheme;
        private RadioButton rbThemeLight;
        private RadioButton rbThemeDark;
        private RadioButton rbThemeSystem;
    }
}
