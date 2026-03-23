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
            systemInfoPanel1 = new RuntimeEnvironmentInfoPanel();
            button1 = new Button();
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
            // button1
            // 
            button1.Location = new Point(43, 12);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 2;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button1);
            Controls.Add(demosTreeView);
            Controls.Add(systemInfoPanel1);
            Name = "FormMain";
            Text = "FormMain";
            ResumeLayout(false);
        }

        #endregion

        private CDS.WinFormsMenus.Basic.MenuTree demosTreeView;
        private RuntimeEnvironmentInfoPanel systemInfoPanel1;
        private Button button1;
    }
}