namespace CDS.CSharpScriptUtils.OutputPanels
{
    partial class RTFOutputPanel
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
            richTextBox = new RichTextBox();
            SuspendLayout();
            // 
            // richTextBox
            // 
            richTextBox.BorderStyle = BorderStyle.None;
            richTextBox.Dock = DockStyle.Fill;
            richTextBox.Font = new Font("Cascadia Code", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBox.Location = new Point(0, 0);
            richTextBox.Name = "richTextBox";
            richTextBox.Size = new Size(386, 257);
            richTextBox.TabIndex = 0;
            richTextBox.Text = "";
            richTextBox.LinkClicked += richTextBox_LinkClicked;
            // 
            // RTFOutputPanel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(richTextBox);
            Name = "RTFOutputPanel";
            Size = new Size(386, 257);
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox richTextBox;
    }
}
