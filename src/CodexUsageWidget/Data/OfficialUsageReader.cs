using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace CodexUsageWidget.Data;

/// <summary>
/// Reads the weekly remaining percentage shown by the Codex desktop app.
/// </summary>
public sealed class OfficialUsageReader
{
    private const string ProcessName = "ChatGPT";
    private const string ProfileMenuName = "打开个人资料菜单";
    private const string RemainingUsageName = "剩余用量";
    private static readonly Regex StandalonePercentageRegex = new(
        @"^\s*(?<value>\d{1,3}(?:[.,]\d+)?)\s*%\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public double? ReadRemainingPercent()
    {
        foreach (var process in GetChatGptProcesses())
        {
            try
            {
                var percent = ReadFromProcess(process);
                if (percent.HasValue)
                {
                    return percent;
                }
            }
            catch (Exception exception) when (IsExpectedUiAutomationException(exception))
            {
                // The desktop app may be starting, closing, or rendering a menu.
            }
            finally
            {
                process.Dispose();
            }
        }

        return null;
    }

    public static bool TryParsePercentage(string? text, out double percentage)
    {
        percentage = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var match = StandalonePercentageRegex.Match(text);
        if (!match.Success)
        {
            return false;
        }

        var normalized = match.Groups["value"].Value.Replace(',', '.');
        if (!double.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var parsed) ||
            !double.IsFinite(parsed) ||
            parsed < 0 ||
            parsed > 100)
        {
            return false;
        }

        percentage = parsed;
        return true;
    }

    private static IEnumerable<Process> GetChatGptProcesses()
    {
        try
        {
            return Process.GetProcessesByName(ProcessName);
        }
        catch
        {
            return [];
        }
    }

    private static double? ReadFromProcess(Process process)
    {
        var root = AutomationElement.RootElement;
        var processCondition = new PropertyCondition(
            AutomationElement.ProcessIdProperty,
            process.Id);
        var windows = root.FindAll(TreeScope.Children, processCondition);

        for (var index = 0; index < windows.Count; index++)
        {
            var window = windows[index];
            var profileButton = FindDescendantByName(window, ProfileMenuName);
            if (profileButton is null)
            {
                continue;
            }

            Expand(profileButton);
            Thread.Sleep(300);

            try
            {
                var remainingUsage = FindDescendantByName(window, RemainingUsageName);
                if (remainingUsage is null ||
                    !remainingUsage.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
                {
                    continue;
                }

                ((InvokePattern)invokePattern).Invoke();
                Thread.Sleep(400);

                var usageMenu = FindUsageMenu(window);
                if (usageMenu is null)
                {
                    continue;
                }

                var percentage = FindPercentage(usageMenu);
                if (percentage.HasValue)
                {
                    return percentage;
                }
            }
            finally
            {
                TryCollapse(window, profileButton);
            }
        }

        return null;
    }

    private static void Expand(AutomationElement profileButton)
    {
        try
        {
            if (profileButton.TryGetCurrentPattern(
                    ExpandCollapsePattern.Pattern,
                    out var expandCollapsePattern))
            {
                var pattern = (ExpandCollapsePattern)expandCollapsePattern;
                if (pattern.Current.ExpandCollapseState != ExpandCollapseState.Expanded)
                {
                    try
                    {
                        pattern.Expand();
                    }
                    catch (InvalidOperationException)
                    {
                        // Chromium can finish the transition asynchronously;
                        // the following lookup will observe the resulting menu.
                    }
                }

                return;
            }

            if (profileButton.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
            {
                ((InvokePattern)invokePattern).Invoke();
            }
        }
        catch (ElementNotAvailableException)
        {
            // The profile button can be replaced while the desktop app renders.
        }
    }

    private static AutomationElement? FindUsageMenu(AutomationElement window)
    {
        var menus = window.FindAll(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Menu));

        for (var index = 0; index < menus.Count; index++)
        {
            var menu = menus[index];
            if (menu.Current.Name.Contains(ProfileMenuName, StringComparison.Ordinal))
            {
                return menu;
            }
        }

        return null;
    }

    private static double? FindPercentage(AutomationElement menu)
    {
        var descendants = menu.FindAll(TreeScope.Descendants, Condition.TrueCondition);
        for (var index = 0; index < descendants.Count; index++)
        {
            if (TryParsePercentage(descendants[index].Current.Name, out var percentage))
            {
                return percentage;
            }
        }

        return null;
    }

    private static AutomationElement? FindDescendantByName(AutomationElement root, string name)
    {
        return root.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, name));
    }

    private static void TryCollapse(AutomationElement window, AutomationElement profileButton)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (!profileButton.TryGetCurrentPattern(
                        ExpandCollapsePattern.Pattern,
                        out var expandCollapsePattern))
                {
                    return;
                }

                var pattern = (ExpandCollapsePattern)expandCollapsePattern;
                if (pattern.Current.ExpandCollapseState == ExpandCollapseState.Collapsed)
                {
                    return;
                }

                try
                {
                    pattern.Collapse();
                }
                catch (InvalidOperationException)
                {
                    // The menu usually closes even when Chromium reports a
                    // transient invalid operation during the transition.
                }
            }
            catch (ElementNotAvailableException)
            {
                return;
            }

            Thread.Sleep(250);
        }

        // Chromium sometimes reports ExpandCollapsePattern.Collapse as
        // successful without actually dismissing the web menu. The element's
        // clickable point is obtained from UI Automation, so this fallback
        // sends the close click directly to the owning window without moving
        // the user's mouse.
        TryClickProfileButton(window, profileButton);
    }

    private static void TryClickProfileButton(AutomationElement window, AutomationElement profileButton)
    {
        try
        {
            if (!profileButton.TryGetClickablePoint(out var screenPoint) ||
                window.Current.NativeWindowHandle == 0)
            {
                return;
            }

            var clientPoint = new NativePoint
            {
                X = (int)Math.Round(screenPoint.X),
                Y = (int)Math.Round(screenPoint.Y)
            };
            var windowHandle = (IntPtr)window.Current.NativeWindowHandle;
            if (!ScreenToClient(windowHandle, ref clientPoint))
            {
                return;
            }

            var packedPoint = (IntPtr)((clientPoint.Y << 16) | (clientPoint.X & 0xffff));
            SendMessage(windowHandle, 0x0201, (IntPtr)1, packedPoint);
            SendMessage(windowHandle, 0x0202, IntPtr.Zero, packedPoint);
        }
        catch (Exception exception) when (IsExpectedUiAutomationException(exception))
        {
            // The desktop window can disappear while the close click is sent.
        }
    }

    private static bool IsExpectedUiAutomationException(Exception exception)
    {
        return exception is ElementNotAvailableException or
            InvalidOperationException or
            UnauthorizedAccessException or
            System.Runtime.InteropServices.COMException;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(
        IntPtr hWnd,
        uint message,
        IntPtr wParam,
        IntPtr lParam);
}
