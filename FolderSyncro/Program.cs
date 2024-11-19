using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections;

class Program
{
    private static string logFilePath;
    private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    static async Task Main(string[] args)
    {
        string sourceFolder;
        string replicaFolder;
        int interval;

        // Check if a configuration file is provided or if no arguments are given
        if (args.Length == 1 && File.Exists(args[0]))
        {
            var config = await LoadConfigAsync(args[0]);
            sourceFolder = config.SourceFolder;
            replicaFolder = config.ReplicaFolder;
            interval = config.Interval * 1000; // Convert seconds to milliseconds
            logFilePath = config.LogFilePath;
        }
        else if (args.Length == 0)
        {
            // Default to looking for a config file named "config.json"
            if (File.Exists("config.json"))
            {
                var config = await LoadConfigAsync("config.json");
                sourceFolder = config.SourceFolder;
                replicaFolder = config.ReplicaFolder;
                interval = config.Interval * 1000; // Convert seconds to milliseconds
                logFilePath = config.LogFilePath;
            }
            else
            {
                // Prompt user for input if no config file is found
                sourceFolder = PromptUser("Source folder path: ");
                replicaFolder = PromptUser("Replica folder path: ");
                interval = int.Parse(PromptUser("Interval in seconds: ")) * 1000; // Convert seconds to milliseconds
                logFilePath = PromptUser("Log file path: ");
            }
        }
        else if (args.Length == 4)
        {
            sourceFolder = args[0];
            replicaFolder = args[1];
            interval = int.Parse(args[2]) * 1000; // Convert seconds to milliseconds
            logFilePath = args[3];
        }
        else
        {
            Console.WriteLine("Invalid arguments. Please provide a valid configuration or command-line arguments.");
            return;
        }

        // Verify if the source folder exists
        if (!Directory.Exists(sourceFolder))
        {
            Console.WriteLine("Source folder does not exist. Exiting.");
            return;
        }

        // Start the synchronization loop
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent the process from terminating.
            _cancellationTokenSource.Cancel();
        };

        try
        {
            while (true)
            {
                await FolderSyncService.SyncFoldersAsync(sourceFolder, replicaFolder, logFilePath, _cancellationTokenSource.Token);
                await Task.Delay(interval, _cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Synchronization canceled.");
        }
        catch (Exception ex)
        {
            await LogAsync($"Error: {ex.Message}", logFilePath);
        }
    }

    private static string PromptUser(string message)
    {
        Console.Write(message);
        return Console.ReadLine();
    }

    private static async Task<Config> LoadConfigAsync(string configFilePath)
    {
        using (var stream = File.OpenRead(configFilePath))
        {
            return await JsonSerializer.DeserializeAsync<Config>(stream);
        }
    }

    public static async Task LogAsync(string message, string logFilePath)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss .fff");
        string logMessage = $"{timestamp}: {message}";
        Console.WriteLine(logMessage);
        try
        {
            await File.AppendAllTextAsync(logFilePath, $"{logMessage}{Environment.NewLine}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Error writing to log file: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
        }
    }
}

public class Config
{
    public string SourceFolder { get; set; }
    public string ReplicaFolder { get; set; }
    public int Interval { get; set; }
    public string LogFilePath { get; set; }
}

public static class FolderSyncService
{
    public static async Task SyncFoldersAsync(string source, string replica, string logFilePath, CancellationToken cancellationToken)
    {
        // Create replica folder if it doesn't exist
        if (!Directory.Exists(replica))
        {
            Directory.CreateDirectory(replica);
            await Program.LogAsync($"Created directory: {Path.GetFileName(replica)} in {Path.GetDirectoryName(replica)}", logFilePath);
        }

        var logMessages = new List<string>();
        bool anyFilesSynced = false; // Flag to track if any files were synced

        // Sync files and directories from source to replica
        foreach (var sourceItem in Directory.GetFileSystemEntries(source))
        {
            cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation

            string itemName = Path.GetFileName(sourceItem);
            string replicaItem = Path.Combine(replica, itemName);

            if (Directory.Exists(sourceItem))
            {
                // Recursively sync directories
                await SyncFoldersAsync(sourceItem, replicaItem, logFilePath, cancellationToken);
            }
            else
            {
                // Check if file needs to be copied or updated
                if (!File.Exists(replicaItem))
                {
                    await CopyFileAsync(sourceItem, replicaItem, logFilePath, logMessages);
                    anyFilesSynced = true; // Mark that a file was synced
                }
                else if (!FilesAreEqual(sourceItem, replicaItem))
                {
                    await CopyFileAsync(sourceItem, replicaItem, logFilePath, logMessages);
                    anyFilesSynced = true; // Mark that a file was synced
                }
            }
        }

        // Log a single message if no files were copied or updated
        if (!anyFilesSynced)
        {
            logMessages.Add("All files are synchronized, job is running.");
        }

        // Remove files/directories from replica that are not in source
        foreach (var replicaItem in Directory.GetFileSystemEntries(replica))
        {
            cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation

            string itemName = Path.GetFileName(replicaItem);
            string sourceItem = Path.Combine(source, itemName);

            if (!File.Exists(sourceItem) && !Directory.Exists(sourceItem))
            {
                if (Directory.Exists(replicaItem))
                {
                    Directory.Delete(replicaItem, true);
                    logMessages.Add($"Removed directory: {itemName} from {Path.GetFileName(replica)}");
                }
                else
                {
                    File.Delete(replicaItem);
                    logMessages.Add($"Removed file: {itemName} from {Path.GetFileName(replica)}");
                }
            }
        }

        // Log all messages at once
        foreach (var message in logMessages)
        {
            await Program.LogAsync(message, logFilePath);
        }
    }

    private static async Task CopyFileAsync(string sourceFile, string destinationFile, string logFilePath, List<string> logMessages)
    {
        using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.None))
        using (var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(destinationStream);
        }
        logMessages.Add($"Copied: {Path.GetFileName(sourceFile)} to {Path.GetFileName(destinationFile)}");
    }

    private static bool FilesAreEqual(string file1, string file2)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream1 = File.OpenRead(file1))
            using (var stream2 = File.OpenRead(file2))
            {
                byte[] hash1 = md5.ComputeHash(stream1);
                byte[] hash2 = md5.ComputeHash(stream2);
                return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
            }
        }
    }
}
