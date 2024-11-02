using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNetFrm48Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            _ = CreateSM();
        }

        private static async Task CreateSM()
        {
            CDS.CSScripting.ScriptManager s = await CDS.CSScripting.ScriptManager.CreateAsync();

            System.Diagnostics.Debug.WriteLine(s);
        }
    }
}
