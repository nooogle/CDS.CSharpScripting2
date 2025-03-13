namespace WinFormsTest
{
    partial class RuntimeEnvironmentInfoPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            labelSystemInfo = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // labelSystemInfo
            // 
            labelSystemInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            labelSystemInfo.Location = new System.Drawing.Point(0, 0);
            labelSystemInfo.Name = "labelSystemInfo";
            labelSystemInfo.Size = new System.Drawing.Size(150, 150);
            labelSystemInfo.TabIndex = 0;
            labelSystemInfo.Text = "label1";
            labelSystemInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SysInfoPanel
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(labelSystemInfo);
            Name = "SysInfoPanel";
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label labelSystemInfo;
    }
}
