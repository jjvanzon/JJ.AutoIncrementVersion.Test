
    private string RunDotNet(string args)
    {
        string output = RunDotNetNoLog(args);

        if (string.Equals(VERBOSITY, "Diagnostic", OrdinalIgnoreCase) ||
            string.Equals(VERBOSITY, "Detailed", OrdinalIgnoreCase))
        // ncrunch: no coverage start
        {
            Log(output);
        }
        // ncrunch: no coverage end

        return output;
    }

    private string RunDotNetNoLog(string args)
    {
        const string fileName = "dotnet";

        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = ProjectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;

        var outputSB = new StringBuilder();
        var errorSB = new StringBuilder();
        process.OutputDataReceived += (_, e) => outputSB.AppendLine(e.Data ?? "");
        process.ErrorDataReceived += (_, e) => errorSB.AppendLine(e.Data ?? "");
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        string timeOutMessage = "";
        if (!process.WaitForExit(CmdTimeOutSeconds * 1000))
        // ncrunch: no coverage start
        {
            // TODO: Add Shim to JJ.Framework.
            #if !NET5_0_OR_GREATER
            process.Kill();
            #else
            process.Kill(entireProcessTree: true);
            #endif
            timeOutMessage = $"{fileName} {args} timed out after {CmdTimeOutSeconds}s";
        }
        // ncrunch: no coverage end

        // .NET may flush async after WaitForExit(int); call the parameterless overload.
        process.WaitForExit();

        var output = outputSB.ToString().TrimEnd();
        var error = errorSB.ToString().Trim();

        bool hasExitCode = process.ExitCode != 0;
        bool hasErrorText = !IsNullOrWhiteSpace(error);
        bool hasOutput = !IsNullOrWhiteSpace(output);
        bool hasErrorInOutput = output.Contains("[error]");
        bool hasTimeOut = !IsNullOrWhiteSpace(timeOutMessage);
        bool hasError = hasExitCode || hasErrorInOutput; // Don't consider error text, which has welcome messages and such in it these days.

        if (hasError)
        {
            throw new Exception(
                $"{fileName} {args} failed " +
                $"{new { hasExitCode, hasErrorText, hasErrorInOutput, hasTimeOut }}: " +
                $"{timeOutMessage} " +
                $"Exit code {process.ExitCode} {error} {output}"); // ncrunch: no coverage
        }

        //string result = $"{error} {output}";
        string result = 
            Join(NewLine,
                 hasExitCode  ? $"Exit Code = {process.ExitCode}" : "",
                 hasErrorText ? $"Error = {error}" : "",
                 hasOutput    ? $"Output = {output}" : "");

        return result;
    }
