namespace ConsoleTest;


/// <summary>
/// Interface for a demo class
/// </summary>
public interface IDemo
{
    /// <summary>
    /// Name of the demo
    /// </summary>
    string Name { get; }


    /// <summary>
    /// Description of the demo
    /// </summary>
    string Description { get; }


    /// <summary>
    /// Run the demo
    /// </summary>
    Task Run();
}
