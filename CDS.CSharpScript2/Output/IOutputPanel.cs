namespace CDS.CSharpScript2.Output;


/// <summary>
/// Interface for output panels.
/// </summary>
public interface IOutputPanel
{
    /// <summary>
    /// Clears the output panel.
    /// </summary>
    void Clear();


    /// <summary>
    /// Appends text to the output panel.
    /// </summary>
    /// <param name="text">
    /// The text to append.
    /// </param>
    void Append(string text);


    /// <summary>
    /// Appends a line of text to the output panel.
    /// </summary>
    /// <param name="text">
    /// The text to append.
    /// </param>
    void AppendLine(string text);
}
