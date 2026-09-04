using System.IO;

namespace AntigravityUsageWidget.Data;

public sealed class UsageFileWatcher : IDisposable
{
    private readonly List<FileSystemWatcher> watchers = [];
    private bool disposed;

    public UsageFileWatcher(CodexDataPaths paths)
    {
        AddSessionsWatcher(paths.SessionsDirectory);
        AddStateWatcher(paths.StateDatabasePath);
    }

    public event EventHandler? Changed;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (var watcher in watchers)
        {
            watcher.Dispose();
        }

        watchers.Clear();
    }

    private void AddSessionsWatcher(string sessionsDirectory)
    {
        if (!Directory.Exists(sessionsDirectory))
        {
            return;
        }

        var watcher = new FileSystemWatcher(sessionsDirectory, "*.jsonl")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        watcher.Changed += OnSessionsChanged;
        watcher.Created += OnSessionsChanged;
        watcher.Deleted += OnSessionsChanged;
        watcher.Renamed += OnSessionsRenamed;
        watchers.Add(watcher);
    }

    private void AddStateWatcher(string stateDatabasePath)
    {
        var directory = Path.GetDirectoryName(stateDatabasePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        var watcher = new FileSystemWatcher(directory)
        {
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };
        watcher.Changed += (sender, args) => OnStateChanged(sender, args, stateDatabasePath);
        watcher.Created += (sender, args) => OnStateChanged(sender, args, stateDatabasePath);
        watcher.Deleted += (sender, args) => OnStateChanged(sender, args, stateDatabasePath);
        watcher.Renamed += (sender, args) => OnStateChanged(sender, args, stateDatabasePath);
        watchers.Add(watcher);
    }

    private void OnSessionsChanged(object? sender, FileSystemEventArgs args)
    {
        RaiseChanged();
    }

    private void OnSessionsRenamed(object? sender, RenamedEventArgs args)
    {
        RaiseChanged();
    }

    private void OnStateChanged(object? sender, FileSystemEventArgs args, string stateDatabasePath)
    {
        var expectedPrefix = Path.GetFileName(stateDatabasePath);
        if (args.Name is not null &&
            args.Name.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            RaiseChanged();
        }
    }

    private void RaiseChanged()
    {
        if (!disposed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
