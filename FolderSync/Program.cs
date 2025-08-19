using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FolderSync;

public class Program
{
    // The application is implemented in a synchronous manner.
    public static void Main(string[] args)
    {
        // Parsing command line arguments
        if (args.Length < 4)
        {
            Console.WriteLine("ERROR: Incorrect program input parameters. The parameters must be: <source> folder full path, <replica> folder full path, synchronization period in seconds, log file full path.");
            return;
        }
        if (!Directory.Exists(args[0]))
        {
            Console.WriteLine($"ERROR: Source folder '{args[0]}' does not exist.");
            return;
        }
        if (!Directory.Exists(args[1]))
        {
            Console.WriteLine($"ERROR: Replica folder '{args[1]}' does not exist.");
            return;
        }
        if (!int.TryParse(args[2], out var syncPeriod) || syncPeriod <= 0)
        {
            Console.WriteLine($"ERROR: Synchronization period '{args[2]}' is not a valid positive integer.");
            return;
        }
        if (string.IsNullOrWhiteSpace(args[3]) || !Path.IsPathRooted(args[3]))
        {
            Console.WriteLine($"ERROR: Log file path '{args[3]}' is not a valid absolute path.");
            return;
        }

        SyncConfiguration config = new()
        {
            SourcePath = args[0],
            ReplicaPath = args[1],
            SyncPeriodSeconds = int.Parse(args[2])
        };

        // Use Serilog as ILogger provider
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(args[3])
            .CreateLogger();

        IFileComparer fileComparer = new Md5FileComparer(); // Using MD5 for file comparison

        // Set up Dependency Injection container
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

        // Run synchronization
        while (true)
        {
            try
            {
                synchronizer.Synchronize(config.SourcePath, config.ReplicaPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

            // The simpliest way to wait for the next synchronization period. May be replaced with a more sophisticated scheduling mechanism if needed (System.Timers.Timer, Stopwatch, Task.Delay).
            Thread.Sleep(config.SyncPeriodSeconds * 1000);
        }
    }
}