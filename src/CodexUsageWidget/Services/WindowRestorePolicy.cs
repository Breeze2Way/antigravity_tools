using System.Windows;

namespace CodexUsageWidget.Services;

public static class WindowRestorePolicy
{
    public static WindowState GetRestoredState(WindowState currentState)
    {
        return currentState == WindowState.Minimized
            ? WindowState.Normal
            : currentState;
    }
}
