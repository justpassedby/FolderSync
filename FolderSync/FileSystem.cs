namespace FolderSync;

public class FileSystem : IFileSystem
{
    public string[] GetDirectories(string path) => Directory.GetDirectories(path);
    public string[] GetFiles(string path) => Directory.GetFiles(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);
    public bool FileExists(string path) => File.Exists(path);

    public FileInfo GetFileInfo(string path) => new(path);
    public string GetFileName(string path) => Path.GetFileName(path);
    public string Combine(string path1, string path2) => Path.Combine(path1, path2);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);

    public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false) =>
        File.Copy(sourceFileName, destFileName, overwrite);
    public void DeleteFile(string path) => File.Delete(path);

    public bool IsFileReadonly(string path) => (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0;

    public void ClearReadonlyAttribute(string path) => File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
}
