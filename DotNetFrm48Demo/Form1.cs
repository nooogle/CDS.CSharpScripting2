using System;
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

            var env =
                CDS.CSScripting.Env
                .Default
                .WithAdditionalNamespaceForType<OpenCvSharp.Mat>()
                .WithAdditionalReferenceForType<OpenCvSharp.Mat>();

            CDS.CSScripting.ScriptManager s = await CDS.CSScripting.ScriptManager.CreateAsync(env);
            await s.CompileAsync();
            var como = await s.GetCompilationOutputAsync();
            s = s.ApplyScript("int x = 10;");
            var d = await s.GetDiagnosticsAsync();

            try
            {
                await s.RunAsync();
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            Console.Write(d.Length);

        }
    }
}
