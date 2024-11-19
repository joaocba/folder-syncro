
# FolderSyncro

FolderSyncro is a simple folder synchronization tool written in C#. It ensures that a replica folder remains an exact copy of a source folder by synchronizing file and folder content periodically.

This tool is particularly useful for scenarios where a one-way synchronization is required, ensuring that any changes in the source folder are mirrored in the replica folder.

---

## Features

- **One-way synchronization:** Keeps the replica folder identical to the source folder, with file hashing validation.
- **Periodic updates:** Synchronization occurs at user-defined intervals.
- **Logging:** Logs all actions (file creation, copying, and removal) to a log file and the console.
- **Flexible configuration:** Supports configuration via command-line arguments or a JSON file.

---

## How to Use

### Command-Line Arguments
Run the application using the following syntax:
```bash
dotnet output/FolderSync <source_folder_path> <replica_folder_path> <interval_in_seconds> <log_file_path>
```

### Example:
```bash
dotnet output/FolderSync "C:\path\to\source" "C:\path\to\replica" 10 "C:\path\to\logfile.log"
```

### Using a Configuration File
Alternatively, provide a configuration file as input:
```bash
dotnet output/FolderSync <config_file_path>
```

### Example Configuration File (config.json):
```json
{
    "SourceFolder": "C:\\path\\to\\source",
    "ReplicaFolder": "C:\\path\\to\\replica",
    "Interval": 10,
    "LogFilePath": "C:\\path\\to\\logfile.log"
}
```

If no arguments are provided, the application defaults to a `config.json` file in the current directory.

---

## Installation and Running

1. **Build the project:**
    - Use the .NET CLI:
      ```bash
      dotnet build
      ```

2. **Run the application:**
    - With command-line arguments or a configuration file as shown above.

3. **Stopping the application:**
    - Press `Ctrl+C` to gracefully cancel synchronization.

---

## Requirements

- .NET SDK (version compatible with C# 10 or later)
- Windows, Linux, or macOS

---

## Logging

- All synchronization actions (file creation, updates, and deletion) are logged to both the console and the specified log file.
- Logs include timestamps for all actions.

---

## Unit Testing

The project includes unit tests implemented with NUnit. These tests validate:

1. **Copying new files**: Ensures new files in the source are copied to the replica.
2. **Removing deleted files**: Ensures files deleted in the source are also removed from the replica.

### Running Tests:
Run the following command to execute the tests:
```bash
dotnet test
```

---

## Example Scenarios

### Synchronizing a Folder:
```bash
dotnet output/FolderSync "C:\Documents\Source" "D:\Backup\Replica" 30 "C:\Logs\sync.log"
```
- Synchronizes every 30 seconds.
- Logs actions to `C:\Logs\sync.log`.

### Using a JSON Configuration:
```bash
dotnet output/FolderSync config.json
```
- Reads the configuration from `config.json` in the current directory.

---

## Notes

- Ensure the source folder exists before running the application.
- The replica folder will be created automatically if it doesn’t exist.
- The program uses MD5 checksums to detect file changes.

