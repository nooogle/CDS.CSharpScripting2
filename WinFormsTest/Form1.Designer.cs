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
            rtfEditor = new CDS.CSScripting2.Editors.RichTextEditor.RTFEditor();
            SuspendLayout();
            // 
            // rtfEditor
            // 
            rtfEditor.Location = new Point(24, 21);
            rtfEditor.Margin = new Padding(2, 1, 2, 1);
            rtfEditor.Name = "rtfEditor";
            rtfEditor.Script = "";
            rtfEditor.Size = new Size(611, 312);
            rtfEditor.TabIndex = 0;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(rtfEditor);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private CDS.CSScripting2.Editors.RichTextEditor.RTFEditor rtfEditor;
    }
}
