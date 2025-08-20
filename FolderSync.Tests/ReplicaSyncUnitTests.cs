using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FolderSync.Tests;

public class ReplicaSyncUnitTests
{
    private readonly Mock<IFileSystem> _fileSystemMock = new();
    private readonly Mock<IFileComparer> _fileComparerMock = new();
    private readonly Mock<ILogger<ReplicaSynchronizer>> _loggerMock = new();
    private readonly SyncConfiguration _config = new()
    {
        SourcePath = "source",
        ReplicaPath = "replica",
        SyncMode = SyncMode.SizeTime,
        AllowReadonlyModify = true
    };
    private ReplicaSynchronizer _synchronizer => new(_fileSystemMock.Object, _config, _fileComparerMock.Object, _loggerMock.Object);

    [Fact]
    public void Synchronize_SourceFolderDoesNotExist_DirectoryNotFoundException()
    {
        // Arrange
        _fileSystemMock.Setup(x => x.DirectoryExists(_config.SourcePath)).Returns(false);

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => _synchronizer.Synchronize(_config.SourcePath, _config.ReplicaPath));
    }

    [Fact]
    public void Synchronize_ReplicaFolderDoesNotExist_DirectoryNotFoundException()
    {
        // Arrange
        _fileSystemMock.Setup(x => x.DirectoryExists(_config.SourcePath)).Returns(true);
        _fileSystemMock.Setup(x => x.DirectoryExists(_config.ReplicaPath)).Returns(false);

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => _synchronizer.Synchronize(_config.SourcePath, _config.ReplicaPath));
    }

    [Fact]
    public void CreateReplicaFiles_CopiesNewFile()
    {
        // Arrange
        var newFileName = "file1.txt";
        var source = "source/file1.txt";
        var target = "replica/file1.txt";

        _fileSystemMock.Setup(x => x.DirectoryExists(_config.SourcePath)).Returns(true);
        _fileSystemMock.Setup(x => x.DirectoryExists(_config.ReplicaPath)).Returns(true);
        _fileSystemMock.Setup(x => x.GetFiles(_config.SourcePath)).Returns([source]);
        _fileSystemMock.Setup(x => x.Combine(_config.ReplicaPath, newFileName)).Returns(target);
        _fileSystemMock.Setup(x => x.GetFileName(source)).Returns(newFileName);
        _fileSystemMock.Setup(x => x.FileExists(target)).Returns(false);

        // Act
        _synchronizer.Synchronize(_config.SourcePath, _config.ReplicaPath);

        // Assert
        _fileSystemMock.Verify(x => x.CopyFile(source, target, It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public void DeleteReplicaFiles_RemovesExtraFile()
    {
        // Arrange
        var source = "source";
        var replica = "replica";
        var replicaFile = "replica/file1.txt";
        var fileName = "file1.txt";

        _fileSystemMock.Setup(x => x.DirectoryExists(source)).Returns(true);
        _fileSystemMock.Setup(x => x.DirectoryExists(replica)).Returns(true);
        _fileSystemMock.Setup(x => x.GetFiles(source)).Returns([]);
        _fileSystemMock.Setup(x => x.GetFiles(replica)).Returns([replicaFile]);
        _fileSystemMock.Setup(x => x.GetFileName(replicaFile)).Returns(fileName);
        _fileSystemMock.Setup(x => x.Combine(replica, fileName)).Returns(replicaFile);

        // Act
        _synchronizer.Synchronize(source, replica);

        // Assert
        _fileSystemMock.Verify(x => x.DeleteFile(replicaFile), Times.Once);
    }

}