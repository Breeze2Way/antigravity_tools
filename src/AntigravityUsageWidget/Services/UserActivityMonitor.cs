using System.Runtime.InteropServices;

namespace AntigravityUsageWidget.Services;

public sealed class UserActivityMonitor
{
    private const uint DefaultPauseMilliseconds = 5_000;
    private const uint PollMilliseconds = 200;
    private readonly uint pauseMilliseconds;

    public UserActivityMonitor(TimeSpan? pauseWindow = null)
    {
        var milliseconds = (pauseWindow ?? TimeSpan.FromMilliseconds(DefaultPauseMilliseconds)).TotalMilliseconds;
        pauseMilliseconds = (uint)Math.Clamp(milliseconds, 0, uint.MaxValue);
    }

    public bool IsUserActive()
    {
        var lastInputTick = GetLastInputTick();
        return lastInputTick.HasValue && GetRemainingQuietMilliseconds(
            unchecked((uint)Environment.TickCount),
            lastInputTick.Value,
            pauseMilliseconds) > 0;
    }

    public bool WaitForQuietPeriod(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lastInputTick = GetLastInputTick();
            if (!lastInputTick.HasValue)
            {
                return true;
            }

            var remaining = GetRemainingQuietMilliseconds(
                unchecked((uint)Environment.TickCount),
                lastInputTick.Value,
                pauseMilliseconds);
            if (remaining == 0)
            {
                return true;
            }

            var waitMilliseconds = (int)Math.Min(remaining, PollMilliseconds);
            if (cancellationToken.WaitHandle.WaitOne(waitMilliseconds))
            {
                return false;
            }
        }
    }

    public static bool IsWithinPauseWindow(
        uint currentTick,
        uint lastInputTick,
        uint pauseMilliseconds)
    {
        return GetRemainingQuietMilliseconds(currentTick, lastInputTick, pauseMilliseconds) > 0;
    }

    internal static uint GetRemainingQuietMilliseconds(
        uint currentTick,
        uint lastInputTick,
        uint quietMilliseconds)
    {
        var elapsed = unchecked(currentTick - lastInputTick);
        return elapsed >= quietMilliseconds ? 0 : quietMilliseconds - elapsed;
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
