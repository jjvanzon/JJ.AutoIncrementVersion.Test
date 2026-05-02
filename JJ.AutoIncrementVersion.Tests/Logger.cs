namespace JJ.AutoIncrementVersion.Tests;

internal static class Logger
{
    // Logging

    /// <summary>
    /// Logs a line to the Debug or Console output.
    /// </summary>
    internal static void Log(string message = "") => Trace.WriteLine(message);

    internal static void LogTitle(string title = "")
    {
        Log();
        Log(title);
        string line = "-".Repeat(title.Length);
        Log(line);
    }
}
