using Microsoft.Extensions.Logging;

namespace FolderSync;

public class ReplicaSynchronizer(IFileSystem fileSystem, SyncConfiguration configuration, IFileComparer comparer, ILogger<ReplicaSynchronizer> logger)
{
    private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));   
    private readonly SyncConfiguration _configuration = configuration; 
    private readonly ILogger<ReplicaSynchronizer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFileComparer _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer)); 

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
                        // comparing file sizes and last write times for difference
                        sourceInfo.Length == targetInfo.Length && sourceInfo.LastWriteTimeUtc == targetInfo.LastWriteTimeUtc :
                        // comparing file hashes for content difference
                        comparer.AreFilesEqual(sourceInfo.FullName, targetInfo.FullName);
                    return !areEqual;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error checking equality of {source} and {target} files: {ex.Message}");
                    return false;
                }
            },
            (source, target) =>
            {
                if (_fileSystem.IsFileReadonly(target))
                {
                    // Program configuration does not allow to modify read-only files in the replica folder.
                    if (!_configuration.AllowReadonlyModify)
                        throw new InvalidOperationException($"Cannot modify readonly file {target}");

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
        _logger.LogInformation($"Starting to delete files in replica folder {replicaFolder} that do not exist in source folder {sourceFolder}");
        RunSyncOperation(
            _fileSystem.GetFiles(replicaFolder), // files in the replica folder
            target => _fileSystem.Combine(sourceFolder, _fileSystem.GetFileName(target)), // corresponding source file path
            (target, source) => !_fileSystem.FileExists(source),
            (target, source) =>
            {
                if (_fileSystem.IsFileReadonly(target))
                {
                    // Program configuration does not allow to modify read-only files in the replica folder.
                    if (!_configuration.AllowReadonlyModify)
                        throw new InvalidOperationException($"Cannot delete readonly file {target}");

                    // Program configuration allows to modify read-only files in the replica folder.
                    _fileSystem.ClearReadonlyAttribute(target);
                }
                _logger.LogInformation($"Deleting file {target}");
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
        string logMessage)
    {
        foreach (var source in sourcePaths)
        {
            string target = getTargetPath(source);
            if (condition(source, target))
            {
                try
                {
                    action(source, target);
                    _logger.LogInformation($"{logMessage} {target} - success");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"{logMessage} {target} - failed: {ex.Message}");
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
