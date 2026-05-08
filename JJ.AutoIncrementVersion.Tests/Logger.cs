namespace JJ.AutoIncrementVersion.Tests;

internal static class Logger
{
    internal static void LogLine() => Log("");
    internal static void Log(string message) => Trace.WriteLine(message);

    internal static void LogTitle(string title = "")
    {
        LogLine();
        Log(title);
        string underLine = "-".Repeat(title.Length);
        Log(underLine);
    }
}
