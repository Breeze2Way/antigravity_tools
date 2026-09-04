using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace AntigravityUsageWidget.Data;

/// <summary>
/// Reads the weekly remaining percentage shown by the Codex desktop app.
/// </summary>
public sealed class OfficialUsageReader
{
    private const string ProcessName = "ChatGPT";
    private const string ProfileMenuName = "打开个人资料菜单";
    private const string RemainingUsageName = "剩余用量";
    internal const int UiReadAttempts = 4;
    internal static readonly TimeSpan UiPollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly Regex StandalonePercentageRegex = new(
        @"^\s*(?<value>\d{1,3}(?:[.,]\d+)?)\s*%\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex UsageMenuPercentageRegex = new(
        @"^\s*(?:使用情况|usage)\s*[:：]?\s*(?:剩余|remaining)\s*(?<value>\d{1,3}(?:[.,]\d+)?)\s*%\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex WeeklyUsageLabelRegex = new(
        @"^\s*\d+(?:[.,]\d+)?\s*(?:周|weeks?)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex FiveHourUsageLabelRegex = new(
        @"^\s*\d+(?:[.,]\d+)?\s*(?:小时|小時|时|時|hours?|hrs?|h)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ResetDurationPartRegex = new(
        @"(?<value>\d+(?:[.,]\d+)?)\s*(?<unit>天|d|day|days|小时|小時|时|時|h|hr|hrs|hour|hours|分钟|分鐘|分|m|min|mins|minute|minutes)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ResetDateRegex = new(
        @"(?<month>\d{1,2})\s*月\s*(?<day>\d{1,2})\s*日?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public double? ReadRemainingPercent()
    {
        return ReadUsage()?.RemainingPercent;
    }

    public OfficialUsageSnapshot? ReadUsage()
    {
        foreach (var process in GetChatGptProcesses())
        {
            try
            {
                var usage = ReadFromProcess(process);
                if (usage is not null && usage.RemainingPercent.HasValue)
                {
                    return usage;
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
            match = UsageMenuPercentageRegex.Match(text);
        }
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

    internal static bool WaitUntil(Func<bool> condition, int maxAttempts, Action wait)
    {
        if (maxAttempts <= 0)
        {
            return false;
        }

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (condition())
            {
                return true;
            }

            if (attempt + 1 < maxAttempts)
            {
                wait();
            }
        }

        return false;
    }

    internal static bool ShouldInspectProcess(IntPtr mainWindowHandle)
    {
        return mainWindowHandle != IntPtr.Zero;
    }

    private static IEnumerable<Process> GetChatGptProcesses()
    {
        try
        {
            var inspectableProcesses = new List<Process>();
            foreach (var process in Process.GetProcessesByName(ProcessName))
            {
                try
                {
                    if (ShouldInspectProcess(process.MainWindowHandle))
                    {
                        inspectableProcesses.Add(process);
                    }
                    else
                    {
                        process.Dispose();
                    }
                }
                catch
                {
                    process.Dispose();
                }
            }

            return inspectableProcesses;
        }
        catch
        {
            return [];
        }
    }

    private static OfficialUsageSnapshot? ReadFromProcess(Process process)
    {
        var root = AutomationElement.RootElement;
        var processCondition = new PropertyCondition(
            AutomationElement.ProcessIdProperty,
            process.Id);
        var windows = root.FindAll(TreeScope.Children, processCondition);

        for (var index = 0; index < windows.Count; index++)
        {
            var window = windows[index];
            AutomationElement? profileButton = null;
            WaitUntil(
                () => (profileButton = FindDescendantByName(window, ProfileMenuName)) is not null,
                UiReadAttempts,
                () => Thread.Sleep(UiPollInterval));
            if (profileButton is null)
            {
                continue;
            }

            Expand(profileButton);

            try
            {
                OfficialUsagePercentages? percentages = null;
                WaitUntil(
                    () => (percentages = FindPercentages(window)) is not null,
                    UiReadAttempts,
                    () => Thread.Sleep(UiPollInterval));

                if (!percentages.HasValue)
                {
                    AutomationElement? remainingUsage = null;
                    WaitUntil(
                        () => (remainingUsage = FindVisibleDescendantByName(window, RemainingUsageName)) is not null,
                        UiReadAttempts,
                        () => Thread.Sleep(UiPollInterval));

                    if (remainingUsage is null)
                    {
                        TryReopen(window, profileButton);
                        WaitUntil(
                            () => (remainingUsage = FindVisibleDescendantByName(window, RemainingUsageName)) is not null,
                            UiReadAttempts,
                            () => Thread.Sleep(UiPollInterval));
                    }

                    if (remainingUsage is not null &&
                        remainingUsage.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
                    {
                        ((InvokePattern)invokePattern).Invoke();
                        WaitUntil(
                            () => (percentages = FindPercentages(window)) is not null,
                            UiReadAttempts,
                            () => Thread.Sleep(UiPollInterval));
                    }
                }

                if (!percentages.HasValue || !percentages.Value.Weekly.HasValue)
                {
                    continue;
                }

                return new OfficialUsageSnapshot(
                    percentages.Value.Weekly,
                    FindResetAfter(window, DateTimeOffset.Now),
                    percentages.Value.FiveHour);
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
        try
        {
            var menus = window.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Menu));

            for (var index = 0; index < menus.Count; index++)
            {
                var menu = menus[index];
                if (!menu.Current.IsOffscreen &&
                    menu.Current.Name.Contains(ProfileMenuName, StringComparison.Ordinal))
                {
                    return menu;
                }
            }
        }
        catch (Exception exception) when (IsExpectedUiAutomationException(exception))
        {
        }

        return null;
    }

    public static bool TryParseResetAfter(string? text, out TimeSpan resetAfter)
    {
        resetAfter = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var days = 0d;
        var hours = 0d;
        var minutes = 0d;
        var foundPart = false;
        foreach (Match match in ResetDurationPartRegex.Matches(text))
        {
            foundPart = true;
            var valueText = match.Groups["value"].Value.Replace(',', '.');
            if (!double.TryParse(valueText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            var unit = match.Groups["unit"].Value.ToLowerInvariant();
            if (unit is "天" or "d" or "day" or "days")
            {
                days += value;
            }
            else if (unit is "小时" or "小時" or "时" or "時" or "h" or "hr" or "hrs" or "hour" or "hours")
            {
                hours += value;
            }
            else
            {
                minutes += value;
            }
        }

        if (!foundPart)
        {
            return false;
        }

        if (days <= 0 && hours <= 0 && minutes <= 0)
        {
            return false;
        }

        resetAfter = TimeSpan.FromMinutes(days * 1_440 + hours * 60 + minutes);
        return resetAfter >= TimeSpan.Zero;
    }

    public static bool TryParseResetAfter(string? text, DateTimeOffset now, out TimeSpan resetAfter)
    {
        if (TryParseResetAfter(text, out resetAfter))
        {
            return true;
        }

        var match = ResetDateRegex.Match(text ?? string.Empty);
        if (!match.Success ||
            !int.TryParse(match.Groups["month"].Value, out var month) ||
            !int.TryParse(match.Groups["day"].Value, out var day))
        {
            resetAfter = default;
            return false;
        }

        DateTimeOffset resetAt;
        try
        {
            resetAt = new DateTimeOffset(
                now.Year,
                month,
                day,
                0,
                0,
                0,
                now.Offset);
            if (resetAt <= now)
            {
                resetAt = resetAt.AddYears(1);
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            resetAfter = default;
            return false;
        }

        resetAfter = resetAt - now;
        return resetAfter > TimeSpan.Zero;
    }

    private static OfficialUsagePercentages? FindPercentages(AutomationElement window)
    {
        var menu = FindUsageMenu(window);
        if (menu is null)
        {
            return null;
        }

        try
        {
            var descendants = menu.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            var percentages = new List<PercentageCandidate>();
            var labels = new List<UsageLabelCandidate>();
            for (var index = 0; index < descendants.Count; index++)
            {
                var element = descendants[index];
                var name = element.Current.Name;
                var bounds = element.Current.BoundingRectangle;
                if (TryParsePercentage(name, out var percentage))
                {
                    percentages.Add(new PercentageCandidate(percentage, bounds.Y, bounds.Height));
                }
                else if (IsWeeklyUsageLabel(name) || IsFiveHourUsageLabel(name))
                {
                    labels.Add(new UsageLabelCandidate(name, bounds.Y, bounds.Height));
                }
            }

            var weekly = SelectWeeklyPercentage(percentages, labels);
            var fiveHour = SelectFiveHourPercentage(percentages, labels);
            if (!weekly.HasValue && percentages.Count > 0)
            {
                weekly = percentages[0].Value;
            }

            return weekly.HasValue || fiveHour.HasValue
                ? new OfficialUsagePercentages(weekly, fiveHour)
                : null;
        }
        catch (Exception exception) when (IsExpectedUiAutomationException(exception))
        {
        }

        return null;
    }

    internal readonly record struct PercentageCandidate(double Value, double Top, double Height);

    internal readonly record struct UsageLabelCandidate(string Name, double Top, double Height);

    private readonly record struct OfficialUsagePercentages(
        double? Weekly,
        double? FiveHour);

    internal static double? SelectWeeklyPercentage(
        IReadOnlyList<PercentageCandidate> percentages,
        IReadOnlyList<UsageLabelCandidate> labels)
    {
        return SelectPercentageForLabels(percentages, labels, IsWeeklyUsageLabel);
    }

    internal static double? SelectFiveHourPercentage(
        IReadOnlyList<PercentageCandidate> percentages,
        IReadOnlyList<UsageLabelCandidate> labels)
    {
        return SelectPercentageForLabels(percentages, labels, IsFiveHourUsageLabel);
    }

    private static double? SelectPercentageForLabels(
        IReadOnlyList<PercentageCandidate> percentages,
        IReadOnlyList<UsageLabelCandidate> labels,
        Func<string?, bool> labelSelector)
    {
        var matchingLabels = labels
            .Where(label => labelSelector(label.Name))
            .ToArray();
        if (matchingLabels.Length == 0 || percentages.Count == 0)
        {
            return null;
        }

        return percentages
            .Select(percentage => new
            {
                percentage.Value,
                Distance = matchingLabels.Min(label => Math.Abs(
                    (label.Top + label.Height / 2) -
                    (percentage.Top + percentage.Height / 2)))
            })
            .OrderBy(candidate => candidate.Distance)
            .First()
            .Value;
    }

    private static bool IsWeeklyUsageLabel(string? text)
    {
        return !string.IsNullOrWhiteSpace(text) && WeeklyUsageLabelRegex.IsMatch(text);
    }

    private static bool IsFiveHourUsageLabel(string? text)
    {
        return !string.IsNullOrWhiteSpace(text) && FiveHourUsageLabelRegex.IsMatch(text);
    }

    private static TimeSpan? FindResetAfter(AutomationElement window, DateTimeOffset now)
    {
        var menu = FindUsageMenu(window);
        if (menu is null)
        {
            return null;
        }

        try
        {
            var descendants = menu.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            for (var index = 0; index < descendants.Count; index++)
            {
                if (TryParseResetAfter(descendants[index].Current.Name, now, out var resetAfter))
                {
                    return resetAfter;
                }
            }
        }
        catch (Exception exception) when (IsExpectedUiAutomationException(exception))
        {
        }

        return null;
    }

    private static AutomationElement? FindDescendantByName(AutomationElement root, string name)
    {
        try
        {
            return root.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, name));
        }
        catch (Exception exception) when (IsExpectedUiAutomationException(exception))
        {
            return null;
        }
    }

    private static AutomationElement? FindVisibleDescendantByName(AutomationElement root, string name)
    {
        var element = FindDescendantByName(root, name);
        if (element is null)
        {
            return null;
        }

        try
        {
            return element.Current.IsOffscreen ? null : element;
        }
        catch (Exception exception) when (IsExpectedUiAutomationException(exception))
        {
            return null;
        }
    }

    private static bool TryInvoke(AutomationElement element)
    {
        try
        {
            if (!element.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
            {
                return false;
            }

            ((InvokePattern)invokePattern).Invoke();
            return true;
        }
        catch (Exception exception) when (IsExpectedUiAutomationException(exception))
        {
            return false;
        }
    }

    private static bool TryReopen(AutomationElement window, AutomationElement profileButton)
    {
        try
        {
            TryCollapse(window, profileButton);
            Expand(profileButton);
            return true;
        }
        catch (Exception exception) when (IsExpectedUiAutomationException(exception))
        {
            return false;
        }
    }

    private static void TryCollapse(AutomationElement window, AutomationElement profileButton)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (FindUsageMenu(window) is null)
                {
                    return;
                }

                if (!profileButton.TryGetCurrentPattern(
                        ExpandCollapsePattern.Pattern,
                        out var expandCollapsePattern))
                {
                    return;
                }

                var pattern = (ExpandCollapsePattern)expandCollapsePattern;
                try
                {
                    if (pattern.Current.ExpandCollapseState != ExpandCollapseState.Collapsed)
                    {
                        pattern.Collapse();
                    }
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

            if (FindUsageMenu(window) is null)
            {
                return;
            }
        }

        // Chromium sometimes reports ExpandCollapsePattern.Collapse as
        // successful without actually dismissing the web menu. The element's
        // clickable point is obtained from UI Automation, so this fallback
        // sends the close click directly to the owning window without moving
        // the user's mouse.
        TryClickProfileButton(window, profileButton);
        WaitUntil(
            () => FindUsageMenu(window) is null,
            UiReadAttempts,
            () => Thread.Sleep(UiPollInterval));
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
