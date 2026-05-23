namespace CDS.CSharpScript2.ScintillaEditor
{
    partial class FormFindReplace
    {
        private System.ComponentModel.IContainer? components = null;
        private TabControl tabControl;
        private TabPage tabPageFind;
        private TabPage tabPageReplace;
        private Label lblFind;
        private TextBox txtFindFind;
        private CheckBox chkFindMatchCase;
        private CheckBox chkFindWholeWord;
        private Button btnFindNext;
        private Button btnFindPrevious;
        private Label lblReplaceFind;
        private TextBox txtReplaceFind;
        private Label lblReplaceWith;
        private TextBox txtReplaceWith;
        private CheckBox chkReplaceMatchCase;
        private CheckBox chkReplaceWholeWord;
        private Button btnReplaceFindNext;
        private Button btnReplaceReplace;
        private Button btnReplaceAll;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            tabControl = new TabControl();
            tabPageFind = new TabPage();
            btnFindPrevious = new Button();
            btnFindNext = new Button();
            chkFindWholeWord = new CheckBox();
            chkFindMatchCase = new CheckBox();
            txtFindFind = new TextBox();
            lblFind = new Label();
            tabPageReplace = new TabPage();
            btnReplaceAll = new Button();
            btnReplaceReplace = new Button();
            btnReplaceFindNext = new Button();
            chkReplaceWholeWord = new CheckBox();
            chkReplaceMatchCase = new CheckBox();
            txtReplaceWith = new TextBox();
            lblReplaceWith = new Label();
            txtReplaceFind = new TextBox();
            lblReplaceFind = new Label();
            tabControl.SuspendLayout();
            tabPageFind.SuspendLayout();
            tabPageReplace.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabPageFind);
            tabControl.Controls.Add(tabPageReplace);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(504, 140);
            tabControl.TabIndex = 0;
            // 
            // tabPageFind
            // 
            tabPageFind.Controls.Add(btnFindPrevious);
            tabPageFind.Controls.Add(btnFindNext);
            tabPageFind.Controls.Add(chkFindWholeWord);
            tabPageFind.Controls.Add(chkFindMatchCase);
            tabPageFind.Controls.Add(txtFindFind);
            tabPageFind.Controls.Add(lblFind);
            tabPageFind.Location = new Point(4, 24);
            tabPageFind.Name = "tabPageFind";
            tabPageFind.Padding = new Padding(6);
            tabPageFind.Size = new Size(496, 112);
            tabPageFind.TabIndex = 0;
            tabPageFind.Text = "Find";
            tabPageFind.UseVisualStyleBackColor = true;
            // 
            // btnFindPrevious
            // 
            btnFindPrevious.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFindPrevious.Location = new Point(394, 44);
            btnFindPrevious.Name = "btnFindPrevious";
            btnFindPrevious.Size = new Size(100, 23);
            btnFindPrevious.TabIndex = 5;
            btnFindPrevious.Text = "Find Previous";
            btnFindPrevious.UseVisualStyleBackColor = true;
            btnFindPrevious.Click += btnFindPrevious_Click;
            // 
            // btnFindNext
            // 
            btnFindNext.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFindNext.Location = new Point(298, 44);
            btnFindNext.Name = "btnFindNext";
            btnFindNext.Size = new Size(90, 23);
            btnFindNext.TabIndex = 4;
            btnFindNext.Text = "Find Next";
            btnFindNext.UseVisualStyleBackColor = true;
            btnFindNext.Click += btnFindNext_Click;
            // 
            // chkFindWholeWord
            // 
            chkFindWholeWord.AutoSize = true;
            chkFindWholeWord.Location = new Point(110, 48);
            chkFindWholeWord.Name = "chkFindWholeWord";
            chkFindWholeWord.Size = new Size(90, 19);
            chkFindWholeWord.TabIndex = 3;
            chkFindWholeWord.Text = "Whole word";
            chkFindWholeWord.UseVisualStyleBackColor = true;
            // 
            // chkFindMatchCase
            // 
            chkFindMatchCase.AutoSize = true;
            chkFindMatchCase.Location = new Point(12, 48);
            chkFindMatchCase.Name = "chkFindMatchCase";
            chkFindMatchCase.Size = new Size(86, 19);
            chkFindMatchCase.TabIndex = 2;
            chkFindMatchCase.Text = "Match case";
            chkFindMatchCase.UseVisualStyleBackColor = true;
            // 
            // txtFindFind
            // 
            txtFindFind.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFindFind.Location = new Point(90, 15);
            txtFindFind.Name = "txtFindFind";
            txtFindFind.Size = new Size(296, 23);
            txtFindFind.TabIndex = 1;
            // 
            // lblFind
            // 
            lblFind.AutoSize = true;
            lblFind.Location = new Point(12, 18);
            lblFind.Name = "lblFind";
            lblFind.Size = new Size(62, 15);
            lblFind.TabIndex = 0;
            lblFind.Text = "Find what:";
            // 
            // tabPageReplace
            // 
            tabPageReplace.Controls.Add(btnReplaceAll);
            tabPageReplace.Controls.Add(btnReplaceReplace);
            tabPageReplace.Controls.Add(btnReplaceFindNext);
            tabPageReplace.Controls.Add(chkReplaceWholeWord);
            tabPageReplace.Controls.Add(chkReplaceMatchCase);
            tabPageReplace.Controls.Add(txtReplaceWith);
            tabPageReplace.Controls.Add(lblReplaceWith);
            tabPageReplace.Controls.Add(txtReplaceFind);
            tabPageReplace.Controls.Add(lblReplaceFind);
            tabPageReplace.Location = new Point(4, 24);
            tabPageReplace.Name = "tabPageReplace";
            tabPageReplace.Padding = new Padding(6);
            tabPageReplace.Size = new Size(496, 112);
            tabPageReplace.TabIndex = 1;
            tabPageReplace.Text = "Replace";
            tabPageReplace.UseVisualStyleBackColor = true;
            // 
            // btnReplaceAll
            // 
            btnReplaceAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnReplaceAll.Location = new Point(393, 76);
            btnReplaceAll.Name = "btnReplaceAll";
            btnReplaceAll.Size = new Size(90, 23);
            btnReplaceAll.TabIndex = 8;
            btnReplaceAll.Text = "Replace All";
            btnReplaceAll.UseVisualStyleBackColor = true;
            btnReplaceAll.Click += btnReplaceAll_Click;
            // 
            // btnReplaceReplace
            // 
            btnReplaceReplace.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnReplaceReplace.Location = new Point(307, 76);
            btnReplaceReplace.Name = "btnReplaceReplace";
            btnReplaceReplace.Size = new Size(80, 23);
            btnReplaceReplace.TabIndex = 7;
            btnReplaceReplace.Text = "Replace";
            btnReplaceReplace.UseVisualStyleBackColor = true;
            btnReplaceReplace.Click += btnReplaceReplace_Click;
            // 
            // btnReplaceFindNext
            // 
            btnReplaceFindNext.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnReplaceFindNext.Location = new Point(211, 76);
            btnReplaceFindNext.Name = "btnReplaceFindNext";
            btnReplaceFindNext.Size = new Size(90, 23);
            btnReplaceFindNext.TabIndex = 6;
            btnReplaceFindNext.Text = "Find Next";
            btnReplaceFindNext.UseVisualStyleBackColor = true;
            btnReplaceFindNext.Click += btnReplaceFindNext_Click;
            // 
            // chkReplaceWholeWord
            // 
            chkReplaceWholeWord.AutoSize = true;
            chkReplaceWholeWord.Location = new Point(110, 80);
            chkReplaceWholeWord.Name = "chkReplaceWholeWord";
            chkReplaceWholeWord.Size = new Size(90, 19);
            chkReplaceWholeWord.TabIndex = 5;
            chkReplaceWholeWord.Text = "Whole word";
            chkReplaceWholeWord.UseVisualStyleBackColor = true;
            // 
            // chkReplaceMatchCase
            // 
            chkReplaceMatchCase.AutoSize = true;
            chkReplaceMatchCase.Location = new Point(12, 80);
            chkReplaceMatchCase.Name = "chkReplaceMatchCase";
            chkReplaceMatchCase.Size = new Size(86, 19);
            chkReplaceMatchCase.TabIndex = 4;
            chkReplaceMatchCase.Text = "Match case";
            chkReplaceMatchCase.UseVisualStyleBackColor = true;
            // 
            // txtReplaceWith
            // 
            txtReplaceWith.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtReplaceWith.Location = new Point(90, 45);
            txtReplaceWith.Name = "txtReplaceWith";
            txtReplaceWith.Size = new Size(296, 23);
            txtReplaceWith.TabIndex = 3;
            // 
            // lblReplaceWith
            // 
            lblReplaceWith.AutoSize = true;
            lblReplaceWith.Location = new Point(12, 48);
            lblReplaceWith.Name = "lblReplaceWith";
            lblReplaceWith.Size = new Size(77, 15);
            lblReplaceWith.TabIndex = 2;
            lblReplaceWith.Text = "Replace with:";
            // 
            // txtReplaceFind
            // 
            txtReplaceFind.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtReplaceFind.Location = new Point(90, 15);
            txtReplaceFind.Name = "txtReplaceFind";
            txtReplaceFind.Size = new Size(296, 23);
            txtReplaceFind.TabIndex = 1;
            // 
            // lblReplaceFind
            // 
            lblReplaceFind.AutoSize = true;
            lblReplaceFind.Location = new Point(12, 18);
            lblReplaceFind.Name = "lblReplaceFind";
            lblReplaceFind.Size = new Size(62, 15);
            lblReplaceFind.TabIndex = 0;
            lblReplaceFind.Text = "Find what:";
            // 
            // FormFindReplace
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(504, 140);
            Controls.Add(tabControl);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormFindReplace";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Find / Replace";
            tabControl.ResumeLayout(false);
            tabPageFind.ResumeLayout(false);
            tabPageFind.PerformLayout();
            tabPageReplace.ResumeLayout(false);
            tabPageReplace.PerformLayout();
            ResumeLayout(false);
        }
    }
}
