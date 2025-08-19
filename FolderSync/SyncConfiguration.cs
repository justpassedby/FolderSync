namespace FolderSync;

public class SyncConfiguration
{
    public required string SourcePath { get; set; }
    public required string ReplicaPath { get; set; }
    public int SyncPeriodSeconds { get; set; }

    // Synchronization is performed based on the file size and last modification time by default. This behavior can be changed to hash comparison.
    public SyncMode SyncMode { get; set; } = SyncMode.SizeTime;

    // Assumed that some files in the Replica folder may be read-only, and the program should not have permissions to modify them - these files will be deleted or modified explicitely by human only. This is the default behavior.
    // If the AllowReadonlyModify property is set to true, the program will attempt to remove the read-only attribute, and modify or delete read-only files in the Replica folder.
    public bool AllowReadonlyModify { get; set; } = false;
}
