namespace CDS.CSharpScriptUtils.Editors.ScintillaEditor
{
    partial class ScintillaScriptEditor
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
            scintilla = new ScintillaNET.Scintilla();
            timerChangeMonitor = new System.Windows.Forms.Timer(components);
            toolTip = new ToolTip(components);
            SuspendLayout();
            // 
            // scintilla
            // 
            scintilla.AutocompleteListSelectedBackColor = Color.FromArgb(0, 120, 215);
            scintilla.Dock = DockStyle.Fill;
            scintilla.Font = new Font("Cascadia Code", 10.12F);
            scintilla.LexerName = null;
            scintilla.Location = new Point(0, 0);
            scintilla.Name = "scintilla";
            scintilla.ScrollWidth = 182;
            scintilla.Size = new Size(336, 261);
            scintilla.TabIndex = 0;
            scintilla.AutoCCancelled += scintilla_AutoCCancelled;
            scintilla.AutoCCharDeleted += scintilla_AutoCCharDeleted;
            scintilla.AutoCCompleted += scintilla_AutoCCompleted;
            scintilla.CharAdded += scintilla_CharAdded;
            scintilla.Delete += scintilla_Delete;
            scintilla.KeyDown += scintilla_KeyDown;
            scintilla.MouseMove += scintilla_MouseMove;
            // 
            // timerChangeMonitor
            // 
            timerChangeMonitor.Interval = 500;
            timerChangeMonitor.Tick += timerChangeMonitor_Tick;
            // 
            // ScintillaScriptEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(scintilla);
            Name = "ScintillaScriptEditor";
            Size = new Size(336, 261);
            ResumeLayout(false);
        }

        #endregion

        private ScintillaNET.Scintilla scintilla;
        private System.Windows.Forms.Timer timerChangeMonitor;
        private ToolTip toolTip;
    }
}
