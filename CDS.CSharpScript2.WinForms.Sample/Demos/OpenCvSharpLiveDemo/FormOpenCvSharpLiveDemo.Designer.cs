namespace CDS.CSharpScript2.WinForms.Sample.Demos.OpenCvSharpLiveDemo
{
    partial class FormOpenCvSharpLiveDemo
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
            components = new System.ComponentModel.Container();
            tableLayoutPanelMain = new TableLayoutPanel();
            tableLayoutPanelLeft = new TableLayoutPanel();
            lblSource = new Label();
            lblDest = new Label();
            zoomPictureBoxSource = new ZoomPictureBox();
            zoomPictureBoxDest = new ZoomPictureBox();
            chkWebcamRunning = new CheckBox();
            tableLayoutPanelRight = new TableLayoutPanel();
            scintillaScriptEditor = new CDS.CSharpScript2.ScintillaEditor.ScintillaScriptEditor();
            outputPanel = new CDS.CSharpScript2.ScintillaEditor.RTFOutputPanel();
            captureTimer = new System.Windows.Forms.Timer(components);
            tableLayoutPanelMain.SuspendLayout();
            tableLayoutPanelLeft.SuspendLayout();
            tableLayoutPanelRight.SuspendLayout();
            SuspendLayout();
            //
            // tableLayoutPanelMain
            //
            tableLayoutPanelMain.ColumnCount = 2;
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutPanelMain.Controls.Add(tableLayoutPanelLeft, 0, 0);
            tableLayoutPanelMain.Controls.Add(tableLayoutPanelRight, 1, 0);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.RowCount = 1;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.TabIndex = 0;
            //
            // tableLayoutPanelLeft
            //
            tableLayoutPanelLeft.ColumnCount = 2;
            tableLayoutPanelLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelLeft.Controls.Add(lblSource, 0, 0);
            tableLayoutPanelLeft.Controls.Add(lblDest, 1, 0);
            tableLayoutPanelLeft.Controls.Add(zoomPictureBoxSource, 0, 1);
            tableLayoutPanelLeft.Controls.Add(zoomPictureBoxDest, 1, 1);
            tableLayoutPanelLeft.Controls.Add(chkWebcamRunning, 0, 2);
            tableLayoutPanelLeft.Dock = DockStyle.Fill;
            tableLayoutPanelLeft.Name = "tableLayoutPanelLeft";
            tableLayoutPanelLeft.RowCount = 3;
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tableLayoutPanelLeft.TabIndex = 0;
            //
            // lblSource
            //
            lblSource.AutoSize = true;
            lblSource.Dock = DockStyle.Fill;
            lblSource.Name = "lblSource";
            lblSource.TabIndex = 0;
            lblSource.Text = "Source";
            //
            // lblDest
            //
            lblDest.AutoSize = true;
            lblDest.Dock = DockStyle.Fill;
            lblDest.Name = "lblDest";
            lblDest.TabIndex = 1;
            lblDest.Text = "Dest";
            //
            // zoomPictureBoxSource
            //
            zoomPictureBoxSource.Dock = DockStyle.Fill;
            zoomPictureBoxSource.IsInteractive = true;
            zoomPictureBoxSource.Name = "zoomPictureBoxSource";
            zoomPictureBoxSource.TabIndex = 2;
            //
            // zoomPictureBoxDest
            //
            zoomPictureBoxDest.Dock = DockStyle.Fill;
            zoomPictureBoxDest.IsInteractive = false;
            zoomPictureBoxDest.Name = "zoomPictureBoxDest";
            zoomPictureBoxDest.TabIndex = 3;
            //
            // chkWebcamRunning
            //
            chkWebcamRunning.AutoSize = true;
            chkWebcamRunning.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            chkWebcamRunning.Name = "chkWebcamRunning";
            chkWebcamRunning.TabIndex = 4;
            chkWebcamRunning.Text = "Webcam running";
            chkWebcamRunning.UseVisualStyleBackColor = true;
            chkWebcamRunning.CheckedChanged += chkWebcamRunning_CheckedChanged;
            tableLayoutPanelLeft.SetColumnSpan(chkWebcamRunning, 2);
            //
            // tableLayoutPanelRight
            //
            tableLayoutPanelRight.ColumnCount = 1;
            tableLayoutPanelRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelRight.Controls.Add(scintillaScriptEditor, 0, 0);
            tableLayoutPanelRight.Controls.Add(outputPanel, 0, 1);
            tableLayoutPanelRight.Dock = DockStyle.Fill;
            tableLayoutPanelRight.Name = "tableLayoutPanelRight";
            tableLayoutPanelRight.RowCount = 2;
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Percent, 66.6666641F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanelRight.TabIndex = 1;
            //
            // scintillaScriptEditor
            //
            scintillaScriptEditor.Dock = DockStyle.Fill;
            scintillaScriptEditor.Name = "scintillaScriptEditor";
            scintillaScriptEditor.TabIndex = 5;
            //
            // outputPanel
            //
            outputPanel.AllowClickLinks2 = true;
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.Name = "outputPanel";
            outputPanel.TabIndex = 6;
            //
            // captureTimer
            //
            captureTimer.Interval = 33;
            captureTimer.Tick += captureTimer_Tick;
            //
            // FormOpenCvSharpLiveDemo
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 620);
            Controls.Add(tableLayoutPanelMain);
            Name = "FormOpenCvSharpLiveDemo";
            Padding = new Padding(8);
            Text = "OpenCvSharp Live Demo";
            tableLayoutPanelMain.ResumeLayout(false);
            tableLayoutPanelLeft.ResumeLayout(false);
            tableLayoutPanelLeft.PerformLayout();
            tableLayoutPanelRight.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanelMain;
        private TableLayoutPanel tableLayoutPanelLeft;
        private Label lblSource;
        private Label lblDest;
        private ZoomPictureBox zoomPictureBoxSource;
        private ZoomPictureBox zoomPictureBoxDest;
        private CheckBox chkWebcamRunning;
        private TableLayoutPanel tableLayoutPanelRight;
        private CDS.CSharpScript2.ScintillaEditor.ScintillaScriptEditor scintillaScriptEditor;
        private CDS.CSharpScript2.ScintillaEditor.RTFOutputPanel outputPanel;
        private System.Windows.Forms.Timer captureTimer;
    }
}
