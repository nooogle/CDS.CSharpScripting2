using System;
using System.Diagnostics;

namespace ConsoleTest;

/// <summary>
/// Writes messages to the console with the elapsed time in milliseconds.
/// </summary>
public class TimedConsoleLogger
{
    private readonly Stopwatch stopwatch;
    private long lastElapsedMilliseconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimedConsoleLogger"/> class.
    /// </summary>
    public TimedConsoleLogger()
    {
        stopwatch = Stopwatch.StartNew();
        lastElapsedMilliseconds = 0;
    }

    /// <summary>
    /// Writes a message to the console with the elapsed time in milliseconds.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public void Log(string message)
    {
        var currentElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        var elapsedSinceLastMessage = currentElapsedMilliseconds - lastElapsedMilliseconds;
        lastElapsedMilliseconds = currentElapsedMilliseconds;

        Console.WriteLine($"{currentElapsedMilliseconds.ToString().PadLeft(6)} ms (+{elapsedSinceLastMessage.ToString().PadLeft(6)} ms): {message}");
    }
}

