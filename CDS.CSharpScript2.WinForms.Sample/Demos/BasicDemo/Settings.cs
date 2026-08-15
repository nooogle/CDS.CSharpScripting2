namespace CDS.CSharpScript2.WinForms.Sample.Demos.BasicDemo;

/// <summary>
/// Settings for the demo
/// </summary>
public class Settings
{
    /// <summary>
    /// The script to run
    /// </summary>
    public string Script { get; set; } = "Console.WriteLine(\"Hello world, from the script!\")";

    /// <summary>
    /// The 0-based line numbers of the folds that were collapsed when the script was last saved.
    /// </summary>
    public List<int> CollapsedFoldLines { get; set; } = [];
}
