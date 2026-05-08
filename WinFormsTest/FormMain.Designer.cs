namespace WinFormsTest
{
    partial class FormMain
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            demosTreeView = new CDS.WinFormsMenus.Basic.MenuTree();
            systemInfoPanel1 = new TestUtils.RuntimeEnvironmentInfoPanel();
            SuspendLayout();
            // 
            // demosTreeView
            // 
            demosTreeView.Dock = DockStyle.Fill;
            demosTreeView.Location = new Point(0, 56);
            demosTreeView.Margin = new Padding(2, 1, 2, 1);
            demosTreeView.Name = "demosTreeView";
            demosTreeView.Size = new Size(800, 394);
            demosTreeView.TabIndex = 0;
            // 
            // systemInfoPanel1
            // 
            systemInfoPanel1.BorderStyle = BorderStyle.FixedSingle;
            systemInfoPanel1.Dock = DockStyle.Top;
            systemInfoPanel1.Location = new Point(0, 0);
            systemInfoPanel1.Name = "systemInfoPanel1";
            systemInfoPanel1.Size = new Size(800, 56);
            systemInfoPanel1.TabIndex = 1;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(demosTreeView);
            Controls.Add(systemInfoPanel1);
            Name = "FormMain";
            Text = "FormMain";
            ResumeLayout(false);
        }

        #endregion

        private CDS.WinFormsMenus.Basic.MenuTree demosTreeView;
        private TestUtils.RuntimeEnvironmentInfoPanel systemInfoPanel1;
    }
}