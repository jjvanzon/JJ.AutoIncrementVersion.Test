namespace JJ.AutoIncrementVersion.TestSuite.Helpers;

public record CommandLineResult(int ExitCode, string Output, string Error)
{
    public void Assert()
    {
        if (HasError)
        {
            throw new Exception($"Error: Exit code {ExitCode}. Output: {Output} {Error}");
        }
    }

    // TODO: This is still not enough. It could be exit code 0 and no error text? But error in the output?
    public bool HasError => ExitCode != 0 || !string.IsNullOrWhiteSpace(Error);
}