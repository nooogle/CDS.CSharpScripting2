namespace CDS.CSharpScript2.Output;


/// <summary>
/// Defines a simple text-oriented output surface used by the scripting UI.
/// </summary>
public interface IOutputPanel
{
    /// <summary>
    /// Clears all content from the output panel.
    /// </summary>
    void Clear();


    /// <summary>
    /// Appends text to the output panel without automatically adding a line terminator.
    /// </summary>
    /// <param name="text">The text to append.</param>
    void Append(string text);


    /// <summary>
    /// Appends text to the output panel followed by the environment newline sequence.
    /// </summary>
    /// <param name="text">The text to append.</param>
    void AppendLine(string text);
}
