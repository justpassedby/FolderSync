namespace FolderSync;

public enum SyncMode
{
    /// <summary>
    /// Synchronization based on file size and last modification time.
    /// </summary>
    SizeTime,
    /// <summary>
    /// Synchronization based on file hash calculated with MD5 algorithm.
    /// </summary>
    // The MD5 algorithm is selected for demonstration purposes only because it is fast and widely supported.
    MD5Hash
}
