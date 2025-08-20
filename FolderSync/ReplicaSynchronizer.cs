using Microsoft.Extensions.Logging;

namespace FolderSync;

public class ReplicaSynchronizer(IFileSystem fileSystem, SyncConfiguration configuration, IFileComparer comparer, ILogger<ReplicaSynchronizer> logger)
{
    private readonly IFileSystem _fileSystem = fileSystem;   
    private readonly SyncConfiguration _configuration = configuration; 
    private readonly ILogger<ReplicaSynchronizer> _logger = logger;
    private readonly IFileComparer _comparer = comparer; 

    public void Synchronize(string sourceFolder, string replicaFolder)
    {
        if (!_fileSystem.DirectoryExists(sourceFolder))
            throw new DirectoryNotFoundException($"Source folder {sourceFolder} does not exist.");

        if (!_fileSystem.DirectoryExists(replicaFolder))
            throw new DirectoryNotFoundException($"Replica folder {replicaFolder} does not exist.");

        CreateReplicaFolders(sourceFolder, replicaFolder);
        CreateReplicaFiles(sourceFolder, replicaFolder);
        UpdateReplicaFiles(sourceFolder, replicaFolder);
        DeleteReplicaFiles(sourceFolder, replicaFolder);
        DeleteReplicaFolders(sourceFolder, replicaFolder);
    }

    private void CreateReplicaFolders(string sourceFolder, string replicaFolder)
    {
        RunSyncOperation(
            _fileSystem.GetDirectories(sourceFolder),
            source => _fileSystem.Combine(replicaFolder, _fileSystem.GetFileName(source)),
            (source, target) => !_fileSystem.DirectoryExists(target),
            (source, target) => _fileSystem.CreateDirectory(target),
            "Create folder");

        RecursiveAction(sourceFolder, replicaFolder, CreateReplicaFolders);
    }

    private void CreateReplicaFiles(string sourceFolder, string replicaFolder)
    {
        RunSyncOperation(
            _fileSystem.GetFiles(sourceFolder),
            source => _fileSystem.Combine(replicaFolder, _fileSystem.GetFileName(source)),
            (source, target) => !_fileSystem.FileExists(target),
            (source, target) => _fileSystem.CopyFile(source, target),
            "Create file");

        RecursiveAction(sourceFolder, replicaFolder, CreateReplicaFiles);
    }

    private void UpdateReplicaFiles(string sourceFolder, string replicaFolder)
    {
        RunSyncOperation(
            _fileSystem.GetFiles(sourceFolder),
            source => _fileSystem.Combine(replicaFolder, _fileSystem.GetFileName(source)),
            (source, target) =>
            {
                try
                {
                    var sourceInfo = _fileSystem.GetFileInfo(source);
                    var targetInfo = _fileSystem.GetFileInfo(target);
                    bool areEqual = _configuration.SyncMode == SyncMode.SizeTime ?
                        // Comparing file sizes and last write times for difference.
                        sourceInfo.Length == targetInfo.Length && sourceInfo.LastWriteTimeUtc == targetInfo.LastWriteTimeUtc :
                        // Comparing file hashes for content difference.
                        _comparer.AreFilesEqual(sourceInfo.FullName, targetInfo.FullName);
                    return !areEqual;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking equality of {Source} and {Target} files.", source, target);
                    return false;
                }
            },
            (source, target) =>
            {
                if (_fileSystem.IsFileReadonly(target))
                {
                    // Program configuration does not allow to modify read-only files in the replica folder.
                    if (!_configuration.AllowReadonlyModify)
                    {
                        _logger.LogError("Cannot update readonly file {Target}", target);
                        return;
                    }

                    // Program configuration allows to modify read-only files in the replica folder.
                    _fileSystem.ClearReadonlyAttribute(target);
                }
                _fileSystem.CopyFile(source, target, true);
            },
            "Update file");

        RecursiveAction(sourceFolder, replicaFolder, UpdateReplicaFiles);
    }

    private void DeleteReplicaFiles(string sourceFolder, string replicaFolder)
    {
        RunSyncOperation(
            _fileSystem.GetFiles(replicaFolder),
            target => _fileSystem.Combine(sourceFolder, _fileSystem.GetFileName(target)), 
            (target, source) => !_fileSystem.FileExists(source),
            (target, source) =>
            {
                if (_fileSystem.IsFileReadonly(target))
                {
                    // Program configuration does not allow to modify read-only files in the replica folder.
                    if (!_configuration.AllowReadonlyModify)
                    {
                        _logger.LogError("Cannot update readonly file {Target}", target);
                        return;
                    }

                    // Program configuration allows to modify read-only files in the replica folder.
                    _fileSystem.ClearReadonlyAttribute(target);
                }
                _fileSystem.DeleteFile(target);
            },
            "Delete file");

        RecursiveAction(sourceFolder, replicaFolder, DeleteReplicaFiles);
    }

    private void DeleteReplicaFolders(string sourceFolder, string replicaFolder)
    {
        RunSyncOperation(
            _fileSystem.GetDirectories(replicaFolder),
            target => _fileSystem.Combine(sourceFolder, _fileSystem.GetFileName(target)),
            (target, source) => !_fileSystem.DirectoryExists(source),
            (target, source) => _fileSystem.DeleteDirectory(target, true),
            "Delete folder");

        RecursiveAction(sourceFolder, replicaFolder, DeleteReplicaFolders);
    }

    private void RunSyncOperation(
        IEnumerable<string> sourcePaths,
        Func<string, string> getTargetPath,
        Func<string, string, bool> condition,
        Action<string, string> action,
        string actionName)
    {
        foreach (var source in sourcePaths)
        {
            string target = getTargetPath(source);
            if (condition(source, target))
            {
                try
                {
                    action(source, target);
                    _logger.LogInformation("{ActionName} {Target} - success", actionName, target);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{ActionName} {Target} - failed", actionName, target);
                }
            }
        }
    }

    private void RecursiveAction(string sourceFolder, string replicaFolder, Action<string, string> recursiveAction)
    {
        foreach (var sourceDirectory in _fileSystem.GetDirectories(sourceFolder))
        {
            string sourceDirectoryName = _fileSystem.GetFileName(sourceDirectory);
            string replicaDirectoryName = _fileSystem.Combine(replicaFolder, sourceDirectoryName);

            recursiveAction(sourceDirectory, replicaDirectoryName);
        }
    }
}
