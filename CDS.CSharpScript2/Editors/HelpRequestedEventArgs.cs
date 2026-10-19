namespace CDS.CSharpScript2.Editors;

/// <summary>
/// Provides the script position a user asked for help on.
/// </summary>
public sealed class HelpRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HelpRequestedEventArgs"/> class.
    /// </summary>
    /// <param name="position">The character offset in the script that help was requested at.</param>
    public HelpRequestedEventArgs(int position)
    {
        Position = position;
    }

    /// <summary>
    /// Gets the character offset in the script that help was requested at.
    /// </summary>
    public int Position { get; }
}
