namespace FolderSync;

public class SyncConfiguration
{
    public required string SourcePath { get; set; }
    public required string ReplicaPath { get; set; }
    public int SyncPeriodSeconds { get; set; }

    /// <summary>
    /// Synchronization is performed based on the file size and last modification time by default. This behavior can be changed to hash comparison <see cref="SyncMode"/>. 
    /// </summary>
    public SyncMode SyncMode { get; set; } = SyncMode.SizeTime;

    /// <summary>
    /// Some files in the Replica folder may be read-only with no permissions to modify them - the files can be deleted or modified explicitely by a human only. This is the default behavior. 
    /// If the AllowReadonlyModify property is set to true, the program will attempt to remove the read-only attribute, and modify or delete read-only files in the Replica folder.
    /// </summary>
    public bool AllowReadonlyModify { get; set; } = false;
}
