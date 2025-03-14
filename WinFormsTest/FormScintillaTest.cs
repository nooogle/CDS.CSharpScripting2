using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsTest
{
    public partial class FormScintillaTest : Form
    {
        public FormScintillaTest()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            timerAutoComplete.Start();
        }

        private void timerAutoComplete_Tick(object sender, EventArgs e)
        {
        }


        bool isAutoCompleteActive = false;

        private void TestAutoCompete()
        {
            if (isAutoCompleteActive)
            {
                //return;
            }

            scintilla.AutoCCancel();

            isAutoCompleteActive = true;

            System.Diagnostics.Debug.WriteLine("TestAutoCompete");


            // Get the word fragment at the caret position
            int currentPosition = scintilla.CurrentPosition;
            int wordStartPosition = scintilla.WordStartPosition(position: currentPosition, onlyWordCharacters: true);
            int lenEntered = currentPosition - wordStartPosition;
            string word = scintilla.GetTextRange(wordStartPosition, currentPosition);


            // show an autocomplete list
            var list = new List<string> { "WriteLine", "Write" };
            var acList = string.Join(scintilla.AutoCSeparator.ToString(), list);
            scintilla.AutoCShow(word.Length, acList);
        }

        private void scintilla_CharAdded(object sender, ScintillaNET.CharAddedEventArgs e)
        {
        }

        private void scintilla_AutoCCancelled(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("AutoCCancelled");
            isAutoCompleteActive = false;
        }

        private void scintilla_AutoCCharDeleted(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("AutoCCharDeleted");
        }

        private void scintilla_AutoCCompleted(object sender, ScintillaNET.AutoCSelectionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("AutoCCompleted");
            isAutoCompleteActive = false;
        }

        private void scintilla_AutoCSelection(object sender, ScintillaNET.AutoCSelectionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("AutoCSelection");
        }

        private void scintilla_AutoCSelectionChange(object sender, ScintillaNET.AutoCSelectionChangeEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("AutoCSelectionChange");
            isAutoCompleteActive = false;
        }

        private void scintilla_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Space && e.Control)
            {
                TestAutoCompete();
            }
        }
    }
}
