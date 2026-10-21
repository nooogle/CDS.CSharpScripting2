namespace CDS.CSharpScript2.ScintillaEditor
{
    partial class ScriptEditorPanel
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

        #region Component Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.scintillaScriptEditor = new ScintillaScriptEditor();
            this.outputPanel = new RTFOutputPanel();
            this.docsPanel = new DocsPanel();
            this.tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 0F));
            this.tableLayoutPanel.Controls.Add(this.scintillaScriptEditor, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.outputPanel, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.docsPanel, 1, 0);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 2;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 66.66666F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(513, 344);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // scintillaScriptEditor
            // 
            this.scintillaScriptEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scintillaScriptEditor.Location = new System.Drawing.Point(3, 3);
            this.scintillaScriptEditor.Name = "scintillaScriptEditor";
            this.scintillaScriptEditor.Size = new System.Drawing.Size(210, 223);
            this.scintillaScriptEditor.TabIndex = 0;
            // 
            // outputPanel
            // 
            this.outputPanel.AllowClickLinks2 = true;
            this.outputPanel.BackColor = System.Drawing.Color.White;
            this.outputPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.outputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputPanel.ForeColor = System.Drawing.Color.Black;
            this.outputPanel.Location = new System.Drawing.Point(3, 232);
            this.outputPanel.Name = "outputPanel";
            this.outputPanel.Size = new System.Drawing.Size(210, 109);
            this.outputPanel.TabIndex = 1;
            // 
            // docsPanel
            // 
            this.docsPanel.BackColor = System.Drawing.Color.White;
            this.docsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.docsPanel.ForeColor = System.Drawing.Color.Black;
            this.docsPanel.Location = new System.Drawing.Point(219, 3);
            this.docsPanel.Name = "docsPanel";
            this.docsPanel.Padding = new System.Windows.Forms.Padding(1, 0, 0, 0);
            this.tableLayoutPanel.SetRowSpan(this.docsPanel, 2);
            this.docsPanel.Size = new System.Drawing.Size(291, 338);
            this.docsPanel.TabIndex = 2;
            this.docsPanel.Visible = false;
            // 
            // ScriptEditorPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel);
            this.Name = "ScriptEditorPanel";
            this.Size = new System.Drawing.Size(513, 344);
            this.tableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private ScintillaScriptEditor scintillaScriptEditor;
        private RTFOutputPanel outputPanel;
        private DocsPanel docsPanel;
    }
}
