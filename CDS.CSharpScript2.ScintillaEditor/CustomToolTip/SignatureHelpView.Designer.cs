namespace CDS.CSharpScript2.ScintillaEditor.CustomToolTip
{
    partial class SignatureHelpView
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
            labelHeader = new Label();
            btnPrevious = new Button();
            btnNext = new Button();
            rtfInfo = new RichTextBox();
            SuspendLayout();
            // 
            // labelHeader
            // 
            labelHeader.AutoSize = true;
            labelHeader.Location = new Point(165, 7);
            labelHeader.Name = "labelHeader";
            labelHeader.Size = new Size(43, 15);
            labelHeader.TabIndex = 0;
            labelHeader.Text = "header";
            // 
            // btnPrevious
            // 
            btnPrevious.Location = new Point(3, 3);
            btnPrevious.Name = "btnPrevious";
            btnPrevious.Size = new Size(75, 23);
            btnPrevious.TabIndex = 1;
            btnPrevious.Text = "Previous";
            btnPrevious.UseVisualStyleBackColor = true;
            btnPrevious.Click += btnPrevious_Click;
            // 
            // btnNext
            // 
            btnNext.Location = new Point(84, 3);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(75, 23);
            btnNext.TabIndex = 2;
            btnNext.Text = "Next";
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += btnNext_Click;
            // 
            // rtfInfo
            // 
            rtfInfo.Location = new Point(3, 32);
            rtfInfo.Name = "rtfInfo";
            rtfInfo.Size = new Size(282, 133);
            rtfInfo.TabIndex = 3;
            rtfInfo.Text = "";
            // 
            // SignatureHelpView
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(rtfInfo);
            Controls.Add(btnNext);
            Controls.Add(btnPrevious);
            Controls.Add(labelHeader);
            Name = "SignatureHelpView";
            Size = new Size(288, 168);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label labelHeader;
        private Button btnPrevious;
        private Button btnNext;
        private RichTextBox rtfInfo;
    }
}
