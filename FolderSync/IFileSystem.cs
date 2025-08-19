namespace FolderSync;

public interface IFileSystem
{
    string[] GetDirectories(string path);
    string[] GetFiles(string path);
    bool DirectoryExists(string path);
    bool FileExists(string path);
    FileInfo GetFileInfo(string path);
    string GetFileName(string path);
    string Combine(string path1, string path2);
    void CreateDirectory(string path);
    void CopyFile(string source, string dest, bool overwrite = false);
    void DeleteFile(string path);
    void DeleteDirectory(string path, bool recursive);
    bool IsFileReadonly(string path);
    void ClearReadonlyAttribute(string path);
}
