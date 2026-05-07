using System.Text;

namespace CDS.CSharpScript2;

/// <summary>
/// Hooks the console output and redirects strings to a client-supplied
/// handler.
/// </summary>
public class ConsoleOutputHook : TextWriter
{
    private readonly Action<string?> _writeString;
    private readonly TextWriter _defaultConsoleOut;

    /// <summary>
    /// Initialise
    /// </summary>
    /// <param name="writeString">Callback to handle a new string</param>
    public ConsoleOutputHook(Action<string?> writeString)
    {
        _writeString = writeString ?? throw new ArgumentNullException(nameof(writeString));
        _defaultConsoleOut = Console.Out;
        Console.SetOut(this);
    }

    ///<inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Console.SetOut(_defaultConsoleOut);
        }

        base.Dispose(disposing);
    }

    ///<inheritdoc/>
    public override void Write(char value)
    {
        _writeString(value.ToString());
    }

    ///<inheritdoc/>
    public override void Write(string? value)
    {
        _writeString(value);
    }

    ///<inheritdoc/>
    public override void Write(char[]? buffer, int index, int count)
    {
        if (buffer != null)
        {
            _writeString(new string(buffer, index, count));
        }
    }

    /// <inheritdoc/>
    public override Encoding Encoding => Encoding.Unicode;
}
