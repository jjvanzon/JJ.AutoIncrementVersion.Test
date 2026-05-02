namespace JJ.AutoIncrementVersion.Tests;

public partial class TestBase
{
    // File Helpers

    internal bool   BuildNumXmlExists()              => Exists(BuildNumXmlFilePath);
    internal bool   DirPropsExists()                 => Exists(DirPropsFilePath);
    internal string ReadBuildNumXml()                => ReadAllText(BuildNumXmlFilePath);
    internal string ReadDirProps()                   => ReadAllText(DirPropsFilePath);
    internal void   WriteBuildNumXml(string content) => Save(BuildNumXmlFilePath, content);
    internal void   WriteDirProps(string content)    => Save(DirPropsFilePath, content);
    internal void   DeleteBuildNumXml()              => Delete(BuildNumXmlFilePath);
    internal void   DeleteDirProps()                 => Delete(DirPropsFilePath);

    private bool Exists(string filePath)
    {
        string fileName = Path.GetFileName(filePath);

        bool exists = File.Exists(filePath);
        if (!exists)
        {
            Log($"{fileName} missing");
            return false;
        }
    
        long length = new FileInfo(filePath).Length;
        // ncrunch: no coverage start
        if (length == 0)
        {
            Log($"{fileName} empty");
            return false;
        }
        // ncrunch: no coverage end

        Log($"{fileName} exists");

        return true;
    }

    private void Save(string filePath, string content)
    {
        string fileName = Path.GetFileName(filePath);
        Log("Save " + fileName);
        WriteAllText(filePath, content);
    }

    private void Delete(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        Log("Deleting " + fileName);
        File.Delete(filePath);
    }
}
