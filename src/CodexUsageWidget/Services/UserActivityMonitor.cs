using System.Runtime.InteropServices;

namespace CodexUsageWidget.Services;

public sealed class UserActivityMonitor
{
    private const uint DefaultPauseMilliseconds = 2_000;
    private readonly uint pauseMilliseconds;

    public UserActivityMonitor(TimeSpan? pauseWindow = null)
    {
        var milliseconds = (pauseWindow ?? TimeSpan.FromMilliseconds(DefaultPauseMilliseconds)).TotalMilliseconds;
        pauseMilliseconds = (uint)Math.Clamp(milliseconds, 0, uint.MaxValue);
    }

    public bool IsUserActive()
    {
        var lastInputTick = GetLastInputTick();
        return lastInputTick.HasValue &&
            IsWithinPauseWindow(
                unchecked((uint)Environment.TickCount),
                lastInputTick.Value,
                pauseMilliseconds);
    }

    public static bool IsWithinPauseWindow(
        uint currentTick,
        uint lastInputTick,
        uint pauseMilliseconds)
    {
        return unchecked(currentTick - lastInputTick) < pauseMilliseconds;
    }

    private static uint? GetLastInputTick()
    {
        var info = new LastInputInfo
        {
            Size = (uint)Marshal.SizeOf<LastInputInfo>()
        };
        return GetLastInputInfo(ref info) ? info.Time : null;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }
}
