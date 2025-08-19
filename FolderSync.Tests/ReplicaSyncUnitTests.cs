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

    private ReplicaSynchronizer CreateSynchronizer() => new(_fileSystemMock.Object, _config, _fileComparerMock.Object, _loggerMock.Object);

    [Fact]
    public void Synchronize_SourceFolderDoesNotExist_DirectoryNotFoundException()
    {
        _fileSystemMock.Setup(x => x.DirectoryExists("source")).Returns(false);
        var synchronizer = CreateSynchronizer();
        Assert.Throws<DirectoryNotFoundException>(() => synchronizer.Synchronize("source", "replica"));
    }

    [Fact]
    public void Synchronize_ReplicaFolderDoesNotExist_DirectoryNotFoundException()
    {
        _fileSystemMock.Setup(x => x.DirectoryExists("source")).Returns(true);
        _fileSystemMock.Setup(x => x.DirectoryExists("replica")).Returns(false);
        var synchronizer = CreateSynchronizer();
        Assert.Throws<DirectoryNotFoundException>(() => synchronizer.Synchronize("source", "replica"));
    }

    [Fact]
    public void CreateReplicaFiles_CopiesNewFile()
    {
        var source = "source/file1.txt";
        var target = "replica/file1.txt";

        _fileSystemMock.Setup(x => x.DirectoryExists("source")).Returns(true);
        _fileSystemMock.Setup(x => x.DirectoryExists("replica")).Returns(true);
        _fileSystemMock.Setup(x => x.GetFiles("source")).Returns([source]);
        _fileSystemMock.Setup(x => x.Combine("replica", "file1.txt")).Returns("replica/file1.txt");
        _fileSystemMock.Setup(x => x.GetFileName("source/file1.txt")).Returns("file1.txt");
        _fileSystemMock.Setup(x => x.FileExists(target)).Returns(false);

        var synchronizer = CreateSynchronizer();
        synchronizer.Synchronize("source", "replica");

        _fileSystemMock.Verify(x => x.CopyFile(source, target, It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public void DeleteReplicaFiles_RemovesExtraFile()
    {
        var source = "source";
        var replica = "replica";
        var replicaFile = "replica/file1.txt";
        var fileName = "file1.txt";

        _fileSystemMock.Setup(x => x.DirectoryExists(source)).Returns(true);
        _fileSystemMock.Setup(x => x.DirectoryExists(replica)).Returns(true);

        // Source has no files
        _fileSystemMock.Setup(x => x.GetFiles(source)).Returns(Array.Empty<string>());

        // Replica has one file that shouldn't exist
        _fileSystemMock.Setup(x => x.GetFiles(replica)).Returns([replicaFile]);

        _fileSystemMock.Setup(x => x.GetFileName(replicaFile)).Returns(fileName);
        _fileSystemMock.Setup(x => x.Combine(replica, fileName)).Returns(replicaFile);

        var synchronizer = CreateSynchronizer();
        synchronizer.Synchronize(source, replica);

        _fileSystemMock.Verify(x => x.DeleteFile(replicaFile), Times.Once);
    }

}