namespace CDS.CSharpScript2.ScintillaEditor
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
            if (disposing)
            {
                _disposed = true;
                _editorStateVersion++;
                timerChangeMonitor?.Stop();
                timerSyntacticColour?.Stop();
                timerCompletion?.Stop();
                CancelPendingAsyncOperations();
                _manager?.Dispose();
                _manager = null;
                _apiInfoForm.Dispose();
                _findReplaceForm?.Dispose();
                components?.Dispose();
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
            timerSyntacticColour = new System.Windows.Forms.Timer(components);
            timerCompletion = new System.Windows.Forms.Timer(components);
            toolTip = new ToolTip(components);
            SuspendLayout();
            // 
            // scintilla
            // 
            scintilla.AutocompleteListSelectedBackColor = Color.FromArgb(0, 120, 212);
            scintilla.Dock = DockStyle.Fill;
            scintilla.Font = new Font("Cascadia Mono", 10F);
            scintilla.LexerName = null;
            scintilla.Location = new Point(0, 0);
            scintilla.Name = "scintilla";
            scintilla.Size = new Size(336, 261);
            scintilla.TabIndex = 0;
            scintilla.AutoCCancelled += scintilla_AutoCCancelled;
            scintilla.AutoCCharDeleted += scintilla_AutoCCharDeleted;
            scintilla.AutoCCompleted += scintilla_AutoCCompleted;
            scintilla.CharAdded += scintilla_CharAdded;
            scintilla.Insert += scintilla_Insert;
            scintilla.Delete += scintilla_Delete;
            scintilla.DwellStart += scintilla_DwellStart;
            scintilla.DwellEnd += scintilla_DwellEnd;
            scintilla.CallTipClick += scintilla_CallTipClick;
            scintilla.KeyDown += scintilla_KeyDown;
            scintilla.MouseMove += scintilla_MouseMove;
            scintilla.UpdateUI += scintilla_UpdateUI;
            // 
            // timerChangeMonitor
            // 
            timerChangeMonitor.Interval = 500;
            timerChangeMonitor.Tick += timerChangeMonitor_Tick;
            //
            // timerSyntacticColour
            //
            // Short enough that colour lands within the "feels live" budget, long enough that a
            // burst of typing coalesces into one pass instead of one per character.
            timerSyntacticColour.Interval = 60;
            timerSyntacticColour.Tick += timerSyntacticColour_Tick;
            //
            // timerCompletion
            //
            // Debounces the completion request so a burst of typing issues one Roslyn
            // request per word. A timer rather than a cancelled delay: see StartCompletionSession.
            timerCompletion.Interval = 150;
            timerCompletion.Tick += timerCompletion_Tick;
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
        private System.Windows.Forms.Timer timerSyntacticColour;
        private System.Windows.Forms.Timer timerCompletion;
        private ToolTip toolTip;
    }
}
