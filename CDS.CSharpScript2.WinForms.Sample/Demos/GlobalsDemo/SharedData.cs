namespace CDS.CSharpScript2.WinForms.Sample.Demos.GlobalsDemo;


/// <summary>
/// Shared data for the demo
/// </summary>
public class SharedData
{
    /// <summary>
    /// An animal
    /// </summary>
    public string Animal { get; set; } = "Cat";


    /// <summary>
    /// A list of countries
    /// </summary>
    public List<string> Countries { get; } = ["USA", "UK", "France"];
}
