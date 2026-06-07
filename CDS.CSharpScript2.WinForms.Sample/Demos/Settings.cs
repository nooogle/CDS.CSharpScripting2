namespace CDS.CSharpScript2.WinForms.Sample.Demos;

/// <summary>
/// Settings for the demos
/// </summary>
class Settings
{
    public BasicDemo.Settings BasicDemo { get; set; } = new BasicDemo.Settings();

    public GlobalsDemo.Settings GlobalsDemo { get; set; } = new GlobalsDemo.Settings();

    public OpenCvSharpStaticDemo.Settings OpenCvSharpStaticDemo { get; set; } = new OpenCvSharpStaticDemo.Settings();


    public SyntaxTreeViewDemo.Settings TreeViewDemo { get; set; } = new ();


    public ClassifiedSpansDemo.Settings ClassifiedSpansDemo { get; set; } = new ();
}
