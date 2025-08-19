namespace FolderSync;

public interface IFileComparer
{
    bool AreFilesEqual(string filePath1, string filePath2);
}
