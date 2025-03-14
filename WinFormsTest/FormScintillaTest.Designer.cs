namespace WinFormsTest
{
    partial class FormScintillaTest
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormScintillaTest));
            scintilla = new ScintillaNET.Scintilla();
            timerAutoComplete = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // scintilla
            // 
            scintilla.AutocompleteListSelectedBackColor = Color.FromArgb(0, 120, 215);
            scintilla.Font = (Font)resources.GetObject("scintilla.Font");
            scintilla.LexerName = null;
            scintilla.Location = new Point(12, 34);
            scintilla.Name = "scintilla";
            scintilla.ScrollWidth = 49;
            scintilla.Size = new Size(776, 404);
            scintilla.TabIndex = 0;
            scintilla.AutoCCancelled += scintilla_AutoCCancelled;
            scintilla.AutoCCharDeleted += scintilla_AutoCCharDeleted;
            scintilla.AutoCCompleted += scintilla_AutoCCompleted;
            scintilla.AutoCSelection += scintilla_AutoCSelection;
            scintilla.AutoCSelectionChange += scintilla_AutoCSelectionChange;
            scintilla.CharAdded += scintilla_CharAdded;
            scintilla.KeyDown += scintilla_KeyDown;
            // 
            // timerAutoComplete
            // 
            timerAutoComplete.Interval = 200;
            timerAutoComplete.Tick += timerAutoComplete_Tick;
            // 
            // FormScintillaTest
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(scintilla);
            Name = "FormScintillaTest";
            Text = "FormScintillaTest";
            ResumeLayout(false);
        }

        #endregion

        private ScintillaNET.Scintilla scintilla;
        private System.Windows.Forms.Timer timerAutoComplete;
    }
}