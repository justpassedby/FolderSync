using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FolderSync;

public class Program
{
    public static void Main(string[] args)
    {
        SyncConfiguration config = GetProgramConfigFromParameters(args);

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(args[3])
            .CreateLogger();

        IFileComparer fileComparer = new Md5FileComparer();

        var serviceProvider = new ServiceCollection()
            .AddLogging(configure =>
            {
                configure.ClearProviders();
                configure.AddSerilog();
            })
            .AddSingleton<IFileComparer, Md5FileComparer>()
            .AddSingleton(config)
            .AddSingleton<IFileSystem, FileSystem>()
            .AddSingleton<ReplicaSynchronizer>()
            .BuildServiceProvider();

        var synchronizer = serviceProvider.GetRequiredService<ReplicaSynchronizer>();

        while (true)
        {
            try
            {
                synchronizer.Synchronize(config.SourcePath, config.ReplicaPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
            }

            // The simpliest way to wait for the next synchronization period.
            // May be replaced with a more sophisticated scheduling mechanism if needed (System.Timers.Timer, Stopwatch, Task.Delay).
            Thread.Sleep(config.SyncPeriodSeconds * 1000);
        }
    }

    private static SyncConfiguration GetProgramConfigFromParameters(string[] args)
    {
        string errorMessage = string.Empty;
        ArgumentException exception;

        if (args.Length < 4)
            errorMessage = "Incorrect program input parameters. The parameters must be: <source> folder full path, <replica> folder full path, synchronization period in seconds, log file full path.";
        if (!Directory.Exists(args[0]))
            errorMessage = $"Source folder '{args[0]}' does not exist.";            
        if (!Directory.Exists(args[1]))
            errorMessage = $"Replica folder '{args[1]}' does not exist.";
        if (!int.TryParse(args[2], out var syncPeriod) || syncPeriod <= 0)
            errorMessage = $"Synchronization period '{args[2]}' is not a valid positive integer.";
        if (string.IsNullOrWhiteSpace(args[3]) || !Path.IsPathRooted(args[3]))
            errorMessage = $"Log file path '{args[3]}' is not a valid absolute path.";

        if(!string.IsNullOrEmpty(errorMessage))
        {
            exception = new ArgumentException(errorMessage);
            Log.Error(exception, errorMessage);
            throw exception;
        }

        return new SyncConfiguration()
        {
            SourcePath = args[0],
            ReplicaPath = args[1],
            SyncPeriodSeconds = syncPeriod
        };
    }
}