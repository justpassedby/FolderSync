using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FolderSync.Tests;

public class ReplicaSyncIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<ReplicaSynchronizer>> _loggerMock;
    private readonly SyncConfiguration _config;
    private readonly Md5FileComparer _fileComparer;
    private readonly ReplicaSynchronizer _synchronizer;

    public ReplicaSyncIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<ReplicaSynchronizer>>();
        _config = new SyncConfiguration
        {
            SourcePath = "Source",
            ReplicaPath = "Replica",
            SyncMode = SyncMode.SizeTime,
            AllowReadonlyModify = true
        };
        Directory.CreateDirectory(_config.SourcePath);
        Directory.CreateDirectory(_config.ReplicaPath);
        _fileComparer = new Md5FileComparer();
        _synchronizer = new ReplicaSynchronizer(new FileSystem(), _config, _fileComparer, _loggerMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_config.SourcePath))
            Directory.Delete(_config.SourcePath, true);
        if (Directory.Exists(_config.ReplicaPath))
            Directory.Delete(_config.ReplicaPath, true);
    }

    [Fact]
    public void DirectoryMissingInReplica_CreatesMissingDirectory()
    {
        // arrange
        const string subfolderName = "Subfolder";
        var sourceDirectory = Path.Combine(_config.SourcePath, subfolderName);
        Directory.CreateDirectory(sourceDirectory);

        // act
        _synchronizer.Synchronize(_config.SourcePath, _config.ReplicaPath);

        // assert
        Assert.True(Directory.Exists(Path.Combine(_config.ReplicaPath, subfolderName)));
    }

    [Fact]
    public void FileMissingInReplica_CreatesMissingFile()
    {
        // arrange
        const string newFileName = "file1.txt";
        const string newFileContent = "Test content";
        var newFilePath = Path.Combine(_config.SourcePath, newFileName);
        File.WriteAllText(newFilePath, newFileContent);

        // act
        _synchronizer.Synchronize(_config.SourcePath, _config.ReplicaPath);

        // assert
        var replicaFile = Path.Combine(_config.ReplicaPath, newFileName);
        Assert.True(File.Exists(replicaFile));
        Assert.Equal(newFileContent, File.ReadAllText(replicaFile));
    }

    [Fact]
    public void ReplicaFileIsDifferent_UpdatesDifferentFile()
    {
        // arrange
        const string fileName = "file1.txt";
        const string newFileContent = "Test content";
        const string oldFileContent = "Old content";

        var sourceFilePath = Path.Combine(_config.SourcePath, fileName);
        File.WriteAllText(sourceFilePath, newFileContent);

        var replicaFile = Path.Combine(_config.ReplicaPath, fileName);
        File.WriteAllText(replicaFile, oldFileContent);

        // act
        _synchronizer.Synchronize(_config.SourcePath, _config.ReplicaPath);

        // assert
        Assert.Equal(newFileContent, File.ReadAllText(replicaFile));
    }

    [Fact]
    public void FileExistsInReplicaOnly_DeletesExtraFile()
    {
        // arrange
        const string fileName = "file1.txt";
        var replicaFile = Path.Combine(_config.ReplicaPath, fileName);

        // act
        _synchronizer.Synchronize(_config.SourcePath, _config.ReplicaPath);

        // assert
        Assert.False(File.Exists(replicaFile));
    }

    [Fact]
    public void DirectoryExistsInReplicaOnly_DeletesExtraDirectory()
    {
        // arrange
        const string subfolderName = "Subfolder";
        var replicaDirectory = Path.Combine(_config.ReplicaPath, subfolderName);
        Directory.CreateDirectory(replicaDirectory);

        // act
        _synchronizer.Synchronize(_config.SourcePath, _config.ReplicaPath);

        // assert
        Assert.False(Directory.Exists(replicaDirectory));
    }
}
