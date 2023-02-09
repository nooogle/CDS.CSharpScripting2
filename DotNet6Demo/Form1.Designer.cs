namespace DotNet6Demo
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
            this.editor1 = new CDS.CSScripting.DotNet6Editors.Scintilla.Editor();
            this.SuspendLayout();
            // 
            // editor1
            // 
            this.editor1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.editor1.Location = new System.Drawing.Point(33, 59);
            this.editor1.Name = "editor1";
            this.editor1.Size = new System.Drawing.Size(721, 340);
            this.editor1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(15F, 37F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.editor1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private CDS.CSScripting.DotNet6Editors.Scintilla.Editor editor1;
    }
}