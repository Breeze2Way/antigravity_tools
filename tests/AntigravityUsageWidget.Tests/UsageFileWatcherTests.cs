namespace AntigravityUsageWidget.Tests;

public sealed class UsageFileWatcherTests
{
    [Fact]
    public async Task ReportsSessionFileChanges()
    {
        using var temp = new TemporaryDirectory();
        var sessions = Directory.CreateDirectory(Path.Combine(temp.Path, "sessions")).FullName;
        var paths = new CodexDataPaths(Path.Combine(temp.Path, "state_5.sqlite"), sessions);
        using var watcher = new UsageFileWatcher(paths);
        var changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        watcher.Changed += (_, _) => changed.TrySetResult(true);

        File.WriteAllText(Path.Combine(sessions, "rollout.jsonl"), "change");

        var completed = await Task.WhenAny(changed.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(changed.Task, completed);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory() => Path = Directory.CreateTempSubdirectory("AntigravityUsageWidgetWatcherTests").FullName;

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
