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
            this.components = new System.ComponentModel.Container();
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanelLeft = new System.Windows.Forms.TableLayoutPanel();
            this.lblSource = new System.Windows.Forms.Label();
            this.lblDest = new System.Windows.Forms.Label();
            this.zoomPictureBoxSource = new CDS.CSharpScript2.WinForms.Sample.Demos.OpenCvSharpLiveDemo.ZoomPictureBox();
            this.zoomPictureBoxDest = new CDS.CSharpScript2.WinForms.Sample.Demos.OpenCvSharpLiveDemo.ZoomPictureBox();
            this.chkWebcamRunning = new System.Windows.Forms.CheckBox();
            this.comboBoxCameras = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanelRight = new System.Windows.Forms.TableLayoutPanel();
            this.scintillaScriptEditor = new CDS.CSharpScript2.ScintillaEditor.ScintillaScriptEditor();
            this.outputPanel = new CDS.CSharpScript2.ScintillaEditor.RTFOutputPanel();
            this.captureTimer = new System.Windows.Forms.Timer(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.tableLayoutPanelMain.SuspendLayout();
            this.tableLayoutPanelLeft.SuspendLayout();
            this.tableLayoutPanelRight.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.ColumnCount = 2;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanelMain.Controls.Add(this.tableLayoutPanelLeft, 0, 0);
            this.tableLayoutPanelMain.Controls.Add(this.tableLayoutPanelRight, 1, 0);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(7, 7);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.RowCount = 1;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(929, 523);
            this.tableLayoutPanelMain.TabIndex = 0;
            // 
            // tableLayoutPanelLeft
            // 
            this.tableLayoutPanelLeft.ColumnCount = 2;
            this.tableLayoutPanelLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelLeft.Controls.Add(this.lblSource, 0, 0);
            this.tableLayoutPanelLeft.Controls.Add(this.lblDest, 1, 0);
            this.tableLayoutPanelLeft.Controls.Add(this.zoomPictureBoxSource, 0, 1);
            this.tableLayoutPanelLeft.Controls.Add(this.zoomPictureBoxDest, 1, 1);
            this.tableLayoutPanelLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelLeft.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanelLeft.Name = "tableLayoutPanelLeft";
            this.tableLayoutPanelLeft.RowCount = 2;
            this.tableLayoutPanelLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 17F));
            this.tableLayoutPanelLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanelLeft.Size = new System.Drawing.Size(551, 517);
            this.tableLayoutPanelLeft.TabIndex = 0;
            this.tableLayoutPanelLeft.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanelLeft_Paint);
            // 
            // lblSource
            // 
            this.lblSource.AutoSize = true;
            this.lblSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSource.Location = new System.Drawing.Point(3, 0);
            this.lblSource.Name = "lblSource";
            this.lblSource.Size = new System.Drawing.Size(269, 17);
            this.lblSource.TabIndex = 0;
            this.lblSource.Text = "Source";
            // 
            // lblDest
            // 
            this.lblDest.AutoSize = true;
            this.lblDest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDest.Location = new System.Drawing.Point(278, 0);
            this.lblDest.Name = "lblDest";
            this.lblDest.Size = new System.Drawing.Size(270, 17);
            this.lblDest.TabIndex = 1;
            this.lblDest.Text = "Dest";
            // 
            // zoomPictureBoxSource
            // 
            this.zoomPictureBoxSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zoomPictureBoxSource.Location = new System.Drawing.Point(3, 20);
            this.zoomPictureBoxSource.Name = "zoomPictureBoxSource";
            this.zoomPictureBoxSource.Size = new System.Drawing.Size(269, 494);
            this.zoomPictureBoxSource.TabIndex = 2;
            // 
            // zoomPictureBoxDest
            // 
            this.zoomPictureBoxDest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.zoomPictureBoxDest.Location = new System.Drawing.Point(278, 20);
            this.zoomPictureBoxDest.Name = "zoomPictureBoxDest";
            this.zoomPictureBoxDest.Size = new System.Drawing.Size(270, 494);
            this.zoomPictureBoxDest.TabIndex = 3;
            // 
            // chkWebcamRunning
            // 
            this.chkWebcamRunning.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.chkWebcamRunning.AutoSize = true;
            this.chkWebcamRunning.Location = new System.Drawing.Point(3, 7);
            this.chkWebcamRunning.Name = "chkWebcamRunning";
            this.chkWebcamRunning.Size = new System.Drawing.Size(46, 17);
            this.chkWebcamRunning.TabIndex = 4;
            this.chkWebcamRunning.Text = "Run";
            this.chkWebcamRunning.UseVisualStyleBackColor = true;
            this.chkWebcamRunning.CheckedChanged += new System.EventHandler(this.chkWebcamRunning_CheckedChanged);
            // 
            // comboBoxCameras
            // 
            this.comboBoxCameras.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCameras.FormattingEnabled = true;
            this.comboBoxCameras.Location = new System.Drawing.Point(55, 3);
            this.comboBoxCameras.Name = "comboBoxCameras";
            this.comboBoxCameras.Size = new System.Drawing.Size(270, 21);
            this.comboBoxCameras.TabIndex = 1;
            // 
            // tableLayoutPanelRight
            // 
            this.tableLayoutPanelRight.ColumnCount = 1;
            this.tableLayoutPanelRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelRight.Controls.Add(this.scintillaScriptEditor, 0, 1);
            this.tableLayoutPanelRight.Controls.Add(this.outputPanel, 0, 2);
            this.tableLayoutPanelRight.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelRight.Location = new System.Drawing.Point(560, 3);
            this.tableLayoutPanelRight.Name = "tableLayoutPanelRight";
            this.tableLayoutPanelRight.RowCount = 3;
            this.tableLayoutPanelRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 41F));
            this.tableLayoutPanelRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 66.66666F));
            this.tableLayoutPanelRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanelRight.Size = new System.Drawing.Size(366, 517);
            this.tableLayoutPanelRight.TabIndex = 1;
            // 
            // scintillaScriptEditor
            // 
            this.scintillaScriptEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scintillaScriptEditor.Location = new System.Drawing.Point(3, 44);
            this.scintillaScriptEditor.Name = "scintillaScriptEditor";
            this.scintillaScriptEditor.Size = new System.Drawing.Size(360, 311);
            this.scintillaScriptEditor.TabIndex = 5;
            // 
            // outputPanel
            // 
            this.outputPanel.AllowClickLinks2 = true;
            this.outputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputPanel.Location = new System.Drawing.Point(3, 361);
            this.outputPanel.Name = "outputPanel";
            this.outputPanel.Size = new System.Drawing.Size(360, 153);
            this.outputPanel.TabIndex = 6;
            // 
            // captureTimer
            // 
            this.captureTimer.Interval = 33;
            this.captureTimer.Tick += new System.EventHandler(this.captureTimer_Tick);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.comboBoxCameras);
            this.panel1.Controls.Add(this.chkWebcamRunning);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(360, 35);
            this.panel1.TabIndex = 7;
            // 
            // FormOpenCvSharpLiveDemo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(943, 537);
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Name = "FormOpenCvSharpLiveDemo";
            this.Padding = new System.Windows.Forms.Padding(7);
            this.Text = "OpenCvSharp Live Demo";
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.tableLayoutPanelLeft.ResumeLayout(false);
            this.tableLayoutPanelLeft.PerformLayout();
            this.tableLayoutPanelRight.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

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
        private ComboBox comboBoxCameras;
        private Panel panel1;
    }
}
