namespace JJ.AutoIncrementVersion.Tests;

public partial class TestBase
{
    // Inspect/Write Values

    internal int GetBuildNumFromXml()
    {
        int value = GetBuildNumFromXml(nolog);
        Log($"BuildNum.xml = {value}");
        return value;
    }

    // ReSharper disable once UnusedParameter.Global
    internal int GetBuildNumFromXml(NoLog nolog)
    {
        var doc = XDocument.Load(BuildNumXmlFilePath);
        var elements = doc.Descendants("BuildNum").ToArray();
        AreEqual(1, elements.Length);
        IsNotNull(elements[0]);
        string str = elements[0].Value;
        var value = int.Parse(str);
        return value;
    }

    internal void SetBuildNumInXml(int num)
    {
        Log("Set BuildNum.xml to " + num);
        var doc = XDocument.Load(BuildNumXmlFilePath);
        var el = doc.Descendants("BuildNum").First();
        el.Value = num.ToString();
        // Write back as single-line XML (matching original format)
        Save(BuildNumXmlFilePath, doc.Declaration?.ToString() ?? "");
        using var writer = new System.Xml.XmlTextWriter(BuildNumXmlFilePath, Encoding.UTF8);
        writer.Formatting = System.Xml.Formatting.None;
        doc.WriteTo(writer);
    }

    /// <summary>
    /// Replaces the <c>&lt;Version&gt;</c> value in the csproj.
    /// </summary>
    internal void SetCsprojVersion(string version)
    {
        Log($"Set ver = {version}");
        string text = ReadAllText(CsprojFilePath);
        text = Regex.Replace(text, @"<Version>[^<]*</Version>", $"<Version>{version}</Version>");
        Save(CsprojFilePath, text);
    }

    /// <summary>
    /// Extracts major.minor from the csproj Version element.
    /// E.g. "4.3.0" → "4.3", "4.3.$(BuildNum)" → "4.3"
    /// </summary>
    internal string GetCsprojMajorMinor()
    {
        string text = ReadAllText(CsprojFilePath);
        Match versionMatch = Match(text, @"<Version>\s*(\d+\.\d+)", IgnoreCase);
        
        // ncrunch: no coverage start
        if (!versionMatch.Success)
        {
            throw new InvalidOperationException("Could not extract major.minor from csproj Version element.");
        }
        // ncrunch: no coverage end

        return versionMatch.Groups[1].Value;
    }

    /// <summary>
    /// Sets csproj Version to &lt;major&gt;.&lt;minor&gt;.0,
    /// extracting major.minor from the current csproj Version value.
    /// </summary>
    internal void SetProjPatchNum(string patch)
    {
        string majorMinor = GetCsprojMajorMinor();
        SetCsprojVersion($"{majorMinor}.{patch}"); // Logs
    }

    /// <summary>
    /// Checks whether the csproj currently references the package.
    /// </summary>
    internal bool CsprojHasPackageReference()
    {
        string text = ReadAllText(CsprojFilePath);
        var hasRef = text.Contains($"Include=\"{PackageId}\"", OrdinalIgnoreCase);
        string csprojFileName = Path.GetFileName(CsprojFilePath);

        if (hasRef)
        {
            Log($"{PackageId} ref exists in {csprojFileName}");
        }
        else
        {
            Log($"{PackageId} ref missing from {csprojFileName}");
        }
           
        return hasRef;
    }

    /// <summary>
    /// Removes the JJ.AutoIncrementVersion PackageReference from the csproj
    /// by editing the file directly (no dotnet CLI needed).
    /// </summary>
    private void RemovePackageReferenceFromCsproj()
    {
        Log("Remove package reference");
        string text = ReadAllText(CsprojFilePath);
        const string pattern = @"\s*<PackageReference\s+Include=""JJ\.AutoIncrementVersion""[^/]*/>\s*";
        text = Regex.Replace(text, pattern, "\n");
        Save(CsprojFilePath, text);
    }

    /// <summary>
    /// Extracts the nupkg file name (e.g. "JJ.AutoIncrementVersion.Dummy.4.3.5.nupkg")
    /// from build output.
    /// </summary>
    internal string ExtractPackageFileName(string output)
    {
        Match match = Match(output, @"(JJ\.AutoIncrementVersion\.Dummy\.\S+\.nupkg)");
        string packageFileName = match.Success ? match.Groups[1].Value : "";

        // ncrunch: no coverage start
        if (IsNullOrWhiteSpace(packageFileName))
        {
            throw new Exception($"Package '{TestProjectName}*.nupkg' not found in output: " + output);
        }
        // ncrunch: no coverage end

        Log($"Package name = {packageFileName}");

        return packageFileName;
    }

    /// <summary>
    /// Ensures Directory.Build.props imports BuildNum.xml only for Release configuration.
    /// If the Release condition is missing, it is inserted.
    /// </summary>
    internal void EnsureDirPropsReleaseCondition()
    {
        string content = ReadDirProps();

        // ncrunch: no coverage start
        if (content.Contains("$(Configuration)=='Release'", OrdinalIgnoreCase))
        {
            Log("Directory.Build.props contains condition: $(Configuration)=='Release'");
            return;
        }
        // ncrunch: no coverage end

        Log("Adding condition to Directory.Build.props: $(Configuration)=='Release'");

        const string pattern = "Condition\\s*=\\s*\"Exists\\('BuildNum\\.xml'\\)\"";
        const string replacement = "Condition=\"Exists('BuildNum.xml') And $(Configuration)=='Release'\"";

        string updated = Replace(content, pattern, replacement, IgnoreCase);

        // ncrunch: no coverage start
        if (updated == content)
        {
            throw new InvalidOperationException("Could not inject Release condition into Directory.Build.props.");
        }
        // ncrunch: no coverage end

        WriteDirProps(updated);
    }

    internal string GetEmbeddedPackageVersion()
    {
        string content = GetResource(TestProjectName + ".csproj");

        Match match = Match(content, @"<PackageReference\s+Include=""JJ\.AutoIncrementVersion""\s+Version=""([^""]+)""", IgnoreCase);

        // ncrunch: no coverage start
        if (!match.Success)
        {
            throw new InvalidOperationException(
                "Could not extract JJ.AutoIncrementVersion package version from embedded csproj.");
        }
        // ncrunch: no coverage end

        var packageVersion = match.Groups[1].Value;

        Log($"Package version = {packageVersion}");

        return packageVersion;
    }
}
