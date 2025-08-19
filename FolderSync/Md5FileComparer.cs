using System.Collections;
using System.Security.Cryptography;

namespace FolderSync;

public class Md5FileComparer : IFileComparer
{
    public bool AreFilesEqual(string filePath1, string filePath2)
    {
        using var md5 = MD5.Create();

        byte[] hash1 = ComputeMD5Hash(md5, filePath1);
        byte[] hash2 = ComputeMD5Hash(md5, filePath2);

        return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
    }

    private byte[] ComputeMD5Hash(MD5 md5, string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return md5.ComputeHash(stream);
    }
}
