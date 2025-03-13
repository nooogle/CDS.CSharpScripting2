namespace WinFormsTest.Demos.OpenCvSharpDemo
{
    partial class FormOpenCvSharpDemo
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
            scintillaScriptEditor = new CDS.CSScripting2.Editors.ScintillaEditor.ScintillaScriptEditor();
            outputPanel = new CDS.CSScripting2.OutputPanels.RTFOutputPanel();
            btnCompile = new Button();
            btnRun = new Button();
            groupBoxScript = new GroupBox();
            tableLayoutPanel2 = new TableLayoutPanel();
            pictureBoxDest = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            pictureBoxSource = new PictureBox();
            tableLayoutPanel1.SuspendLayout();
            groupBoxScript.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxDest).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxSource).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(scintillaScriptEditor, 0, 0);
            tableLayoutPanel1.Controls.Add(outputPanel, 0, 1);
            tableLayoutPanel1.Location = new Point(604, 24);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 66.6666641F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.Size = new Size(327, 529);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // scintillaScriptEditor
            // 
            scintillaScriptEditor.Dock = DockStyle.Fill;
            scintillaScriptEditor.Location = new Point(3, 3);
            scintillaScriptEditor.Name = "scintillaScriptEditor";
            scintillaScriptEditor.Script = "";
            scintillaScriptEditor.Size = new Size(321, 346);
            scintillaScriptEditor.TabIndex = 5;
            // 
            // outputPanel
            // 
            outputPanel.AllowClickLinks = true;
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.Location = new Point(3, 355);
            outputPanel.Name = "outputPanel";
            outputPanel.Size = new Size(321, 171);
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
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel2.Controls.Add(pictureBoxDest, 0, 3);
            tableLayoutPanel2.Controls.Add(label1, 0, 0);
            tableLayoutPanel2.Controls.Add(label2, 0, 2);
            tableLayoutPanel2.Controls.Add(pictureBoxSource, 0, 1);
            tableLayoutPanel2.Location = new Point(21, 90);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 4;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 21F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 21F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Size = new Size(577, 463);
            tableLayoutPanel2.TabIndex = 13;
            // 
            // pictureBoxDest
            // 
            pictureBoxDest.Dock = DockStyle.Fill;
            pictureBoxDest.Location = new Point(3, 255);
            pictureBoxDest.Name = "pictureBoxDest";
            pictureBoxDest.Size = new Size(571, 205);
            pictureBoxDest.SizeMode = PictureBoxSizeMode.CenterImage;
            pictureBoxDest.TabIndex = 17;
            pictureBoxDest.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(43, 15);
            label1.TabIndex = 14;
            label1.Text = "Source";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(3, 231);
            label2.Name = "label2";
            label2.Size = new Size(30, 15);
            label2.TabIndex = 15;
            label2.Text = "Dest";
            // 
            // pictureBoxSource
            // 
            pictureBoxSource.Dock = DockStyle.Fill;
            pictureBoxSource.Location = new Point(3, 24);
            pictureBoxSource.Name = "pictureBoxSource";
            pictureBoxSource.Size = new Size(571, 204);
            pictureBoxSource.SizeMode = PictureBoxSizeMode.CenterImage;
            pictureBoxSource.TabIndex = 16;
            pictureBoxSource.TabStop = false;
            // 
            // FormOpenCvSharpDemo
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(943, 568);
            Controls.Add(tableLayoutPanel2);
            Controls.Add(groupBoxScript);
            Controls.Add(tableLayoutPanel1);
            Name = "FormOpenCvSharpDemo";
            Padding = new Padding(12);
            Text = "Globals demo";
            tableLayoutPanel1.ResumeLayout(false);
            groupBoxScript.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxDest).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxSource).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private TableLayoutPanel tableLayoutPanel1;
        private Button btnCompile;
        private Button btnRun;
        private GroupBox groupBoxScript;
        private CDS.CSScripting2.Editors.ScintillaEditor.ScintillaScriptEditor scintillaScriptEditor;
        private CDS.CSScripting2.OutputPanels.RTFOutputPanel outputPanel;
        private TableLayoutPanel tableLayoutPanel2;
        private PictureBox pictureBoxDest;
        private Label label1;
        private Label label2;
        private PictureBox pictureBoxSource;
    }
}
