namespace CDS.CSharpScriptUtils.Editors.RichTextEditor
{
    partial class RTFScriptEditor
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
            components = new System.ComponentModel.Container();
            richTextBox = new RichTextBox();
            timerChangeMonitor = new System.Windows.Forms.Timer(components);
            toolTip = new ToolTip(components);
            SuspendLayout();
            // 
            // richTextBox
            // 
            richTextBox.Dock = DockStyle.Fill;
            richTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBox.Location = new Point(0, 0);
            richTextBox.Margin = new Padding(2, 1, 2, 1);
            richTextBox.Name = "richTextBox";
            richTextBox.Size = new Size(286, 165);
            richTextBox.TabIndex = 0;
            richTextBox.Text = "";
            richTextBox.TextChanged += richTextBox_TextChanged;
            richTextBox.MouseMove += richTextBox_MouseMove;
            // 
            // timerChangeMonitor
            // 
            timerChangeMonitor.Interval = 500;
            timerChangeMonitor.Tick += timerChangeMonitor_Tick;
            // 
            // RTFEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(richTextBox);
            Margin = new Padding(2, 1, 2, 1);
            Name = "RTFEditor";
            Size = new Size(286, 165);
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox richTextBox;
        private System.Windows.Forms.Timer timerChangeMonitor;
        private ToolTip toolTip;
    }
}
