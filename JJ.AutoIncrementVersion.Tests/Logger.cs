namespace JJ.AutoIncrementVersion.Tests;

internal static class Logger
{
    // Logging

    internal static void Log(string message = "") => Trace.WriteLine(message);

    internal static void LogTitle(string title = "")
    {
        Log();
        Log(title);
        string line = "-".Repeat(title.Length);
        Log(line);
    }
}
