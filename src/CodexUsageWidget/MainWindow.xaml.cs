using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using CodexUsageWidget.Controls;
using CodexUsageWidget.Data;
using CodexUsageWidget.Models;
using CodexUsageWidget.Services;

namespace CodexUsageWidget;

public partial class MainWindow : Window
{
    private const double BallWindowSize = 68;
    private const double WaterBallSize = 62;
    private static readonly TimeSpan LocalRefreshDebounce = TimeSpan.FromMilliseconds(400);
    private const double SettingsWindowWidth = 420;
    private const double SettingsWindowHeight = 480;
    private const double SettingsWindowGap = 12;
    private const string OfficialUsageUrl = "https://chatgpt.com";
    private const string StartupValueName = "CodexUsageWidget";

    private readonly SettingsStore settingsStore = new();
    private readonly WaterBallControl waterBall = new();
    private readonly System.Windows.Controls.TextBlock ballDetailsText = CreateDetailsTextBlock();
    private readonly System.Windows.Controls.TextBlock hostDetailsText = CreateDetailsTextBlock();
    private readonly UsageRefreshService refreshService;
    private readonly DispatcherTimer refreshTimer;
    private readonly DispatcherTimer localRefreshTimer;
    private readonly DispatcherTimer resetCountdownTimer;
    private readonly DispatcherTimer officialRetryTimer;
    private readonly UsageFileWatcher usageFileWatcher;
    private readonly UserActivityMonitor userActivityMonitor = new();
    private Forms.NotifyIcon trayIcon = null!;
    private Forms.ToolStripMenuItem trayRefreshMenuItem = null!;
    private Forms.ToolStripMenuItem traySettingsMenuItem = null!;
    private Forms.ToolStripMenuItem trayOfficialUsageMenuItem = null!;
    private Forms.ToolStripMenuItem trayLanguageMenuItem = null!;
    private Forms.ToolStripMenuItem trayExitMenuItem = null!;
    private System.Drawing.Icon? applicationIcon;
    private WidgetSettings settings;
    private bool isRefreshing;
    private bool localRefreshPending;
    private WidgetViewState? lastState;
    private string? lastDetails;
    private System.Windows.Point? dashboardPosition;

    public MainWindow()
    {
        InitializeComponent();
        Icon = LoadWindowIcon();
        waterBall.Width = WaterBallSize;
        waterBall.Height = WaterBallSize;
        waterBall.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        waterBall.VerticalAlignment = VerticalAlignment.Center;
        waterBall.SnapsToDevicePixels = true;
        AutomationProperties.SetName(waterBall, "Codex 剩余用量");
        ToolTipService.SetInitialShowDelay(waterBall, 150);
        ToolTipService.SetShowDuration(waterBall, 60000);
        ToolTipService.SetBetweenShowDelay(waterBall, 100);
        waterBall.ToolTip = ballDetailsText;
        WaterBallHost.ToolTip = hostDetailsText;
        WaterBallHost.Children.Add(waterBall);
        settings = settingsStore.Load();
        ApplyWeeklyRingSettings();
        var dataPaths = CodexDataPaths.ForCurrentUser();
        refreshService = new UsageRefreshService(
            new CodexDataReader(),
            dataPaths);
        refreshTimer = new DispatcherTimer();
        refreshTimer.Tick += RefreshTimer_Tick;
        localRefreshTimer = new DispatcherTimer
        {
            Interval = LocalRefreshDebounce
        };
        localRefreshTimer.Tick += LocalRefreshTimer_Tick;
        resetCountdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        resetCountdownTimer.Tick += ResetCountdownTimer_Tick;
        officialRetryTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        officialRetryTimer.Tick += OfficialRetryTimer_Tick;
        usageFileWatcher = new UsageFileWatcher(dataPaths);
        usageFileWatcher.Changed += UsageFileWatcher_Changed;
        ConfigureWindow();
        ConfigureTrayIcon();
        ApplyLanguage();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        ConfigureRefreshTimer();
        resetCountdownTimer.Start();
        if (OfficialRefreshPolicy.ShouldReadAutomatically(userActivityMonitor.IsUserActive()))
        {
            RefreshAsync();
        }
        else
        {
            RefreshAsync(refreshOfficial: false);
            officialRetryTimer.Start();
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
        localRefreshTimer.Stop();
        resetCountdownTimer.Stop();
        officialRetryTimer.Stop();
        usageFileWatcher.Dispose();
        var savedPosition = dashboardPosition ?? new System.Windows.Point(Left, Top);
        settingsStore.Save(settings with { Left = savedPosition.X, Top = savedPosition.Y });
        trayIcon.Visible = false;
        trayIcon.Dispose();
        applicationIcon?.Dispose();
    }

    private void ConfigureWindow()
    {
        Topmost = settings.Topmost;
        Opacity = settings.Opacity;
    }

    private void RestoreWindow()
    {
        WindowState = WindowRestorePolicy.GetRestoredState(WindowState);
        if (!IsVisible)
        {
            Show();
        }

        Activate();
    }

    private void PositionWindow()
    {
        var workArea = GetCurrentWorkArea();
        var windowSize = new System.Windows.Size(BallWindowSize, BallWindowSize);
        if (double.IsFinite(settings.Left) && double.IsFinite(settings.Top))
        {
            var savedPosition = WindowPlacementCalculator.ClampInsideWorkArea(
                new System.Windows.Point(settings.Left, settings.Top),
                windowSize,
                workArea);
            Left = savedPosition.X;
            Top = savedPosition.Y;
            return;
        }

        var defaultPosition = WindowPlacementCalculator.ClampInsideWorkArea(
            new System.Windows.Point(workArea.Right - BallWindowSize - 24, workArea.Top + 24),
            windowSize,
            workArea);
        Left = defaultPosition.X;
        Top = defaultPosition.Y;
    }

    private void ConfigureRefreshTimer()
    {
        refreshTimer.Interval = TimeSpan.FromSeconds(settings.RefreshSeconds);
        refreshTimer.Start();
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (!OfficialRefreshPolicy.ShouldReadAutomatically(userActivityMonitor.IsUserActive()))
        {
            officialRetryTimer.Start();
            return;
        }

        RefreshAsync(refreshOfficial: true);
    }

    private void OfficialRetryTimer_Tick(object? sender, EventArgs e)
    {
        if (!OfficialRefreshPolicy.ShouldReadAutomatically(userActivityMonitor.IsUserActive()))
        {
            return;
        }

        officialRetryTimer.Stop();
        RefreshAsync(refreshOfficial: true);
    }

    private void UsageFileWatcher_Changed(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            localRefreshTimer.Stop();
            localRefreshTimer.Start();
        });
    }

    private void LocalRefreshTimer_Tick(object? sender, EventArgs e)
    {
        localRefreshTimer.Stop();
        RefreshAsync(refreshOfficial: false);
    }

    private async void RefreshAsync(bool refreshOfficial = true)
    {
        if (isRefreshing)
        {
            if (!refreshOfficial)
            {
                localRefreshPending = true;
            }

            return;
        }

        isRefreshing = true;
        try
        {
            var snapshot = await Task.Run(() => refreshService.Refresh(
                DateTimeOffset.UtcNow,
                settings,
                refreshOfficial));
            ApplySnapshot(snapshot);
        }
        catch (Exception exception)
        {
            SetDetails($"刷新失败：{exception.Message}");
        }
        finally
        {
            isRefreshing = false;
            if (localRefreshPending)
            {
                localRefreshPending = false;
                RefreshAsync(refreshOfficial: false);
            }
        }
    }

    private void ApplySnapshot(WidgetViewState state)
    {
        lastState = state;
        var fiveHourText = WaterBallDisplay.FormatCenterText(state.FiveHourRemainingPercent);
        var weeklyText = WaterBallDisplay.FormatCenterText(state.OfficialRemainingPercent);
        var centerPercent = state.FiveHourRemainingPercent ?? state.OfficialRemainingPercent;
        var centerText = WaterBallDisplay.FormatCenterText(centerPercent);
        waterBall.FiveHourRemainingPercent = state.FiveHourRemainingPercent;
        waterBall.WeeklyRemainingPercent = state.OfficialRemainingPercent;
        waterBall.RemainingPercent = centerPercent;
        waterBall.TokensPerMinute = state.RecentTokensPerMinute;
        waterBall.CenterText = centerText;
        SetDetails(UsageDisplayFormatter.FormatTooltipDetails(
            fiveHourText,
            weeklyText,
            state.TodayTokens,
            state.YesterdayTokens,
            state.SevenDay.Usage.TotalTokens,
            state.ThirtyDay.Usage.TotalTokens,
            state.RefreshedAt));
    }

    private void SetDetails(string details)
    {
        lastDetails = details;
        ApplyTooltipDetails(
            details,
            UsageDisplayFormatter.FormatResetDetails(
                lastState?.FiveHourResetAt,
                lastState?.WeeklyResetAt ?? lastState?.ResetAt,
                DateTimeOffset.Now));
    }

    private void ResetCountdownTimer_Tick(object? sender, EventArgs e)
    {
        if (lastDetails is not null)
        {
            ApplyTooltipDetails(
                lastDetails,
                UsageDisplayFormatter.FormatResetDetails(
                    lastState?.FiveHourResetAt,
                    lastState?.WeeklyResetAt ?? lastState?.ResetAt,
                    DateTimeOffset.Now));
        }
    }

    private void ApplyTooltipDetails(string details, string? resetDetails)
    {
        foreach (var textBlock in new[] { ballDetailsText, hostDetailsText })
        {
            textBlock.Inlines.Clear();
            textBlock.Inlines.Add(new Run(details));
            if (resetDetails is not null)
            {
                textBlock.Inlines.Add(new Run(Environment.NewLine + resetDetails)
                {
                    Foreground = System.Windows.Media.Brushes.IndianRed,
                    FontWeight = System.Windows.FontWeights.SemiBold
                });
            }
        }
    }

    private static System.Windows.Controls.TextBlock CreateDetailsTextBlock()
    {
        return new System.Windows.Controls.TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 280,
            Margin = new Thickness(0)
        };
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                // The window can close while a drag starts.
            }
        }
    }

    private void SettingsHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Header_MouseLeftButtonDown(sender, e);
    }

    private System.Windows.Rect GetCurrentWorkArea()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return SystemParameters.WorkArea;
        }

        var workArea = Forms.Screen.FromHandle(handle).WorkingArea;
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
        {
            return new System.Windows.Rect(
                workArea.Left,
                workArea.Top,
                workArea.Width,
                workArea.Height);
        }

        var transform = source.CompositionTarget.TransformFromDevice;
        var topLeft = transform.Transform(new System.Windows.Point(workArea.Left, workArea.Top));
        var bottomRight = transform.Transform(new System.Windows.Point(workArea.Right, workArea.Bottom));
        return new System.Windows.Rect(topLeft, bottomRight);
    }

    private void ConfigureTrayIcon()
    {
        var iconPath = AppIcon.GetPath(AppContext.BaseDirectory);
        if (File.Exists(iconPath))
        {
            applicationIcon = new System.Drawing.Icon(iconPath);
        }

        trayIcon = new Forms.NotifyIcon
        {
            Icon = applicationIcon ?? System.Drawing.SystemIcons.Application,
            Text = "Codex 剩余用量",
            Visible = true
        };

        var menu = new Forms.ContextMenuStrip
        {
            AutoSize = true,
            Padding = new Forms.Padding(4),
            ShowCheckMargin = false,
            ShowImageMargin = false,
            Font = new System.Drawing.Font("Microsoft YaHei UI", 9F)
        };
        trayRefreshMenuItem = AddTrayMenuItem(menu, "刷新", (_, _) => Dispatcher.Invoke(RefreshAsync));
        traySettingsMenuItem = AddTrayMenuItem(menu, "设置", (_, _) => Dispatcher.Invoke(ShowSettings));
        trayOfficialUsageMenuItem = AddTrayMenuItem(menu, "打开官方用量", (_, _) => Dispatcher.Invoke(OpenOfficialUsage));
        trayLanguageMenuItem = AddTrayMenuItem(menu, "English", (_, _) => Dispatcher.Invoke(ToggleLanguage));
        trayExitMenuItem = AddTrayMenuItem(menu, "退出", (_, _) => Dispatcher.Invoke(Close));
        trayIcon.ContextMenuStrip = menu;
        trayIcon.MouseClick += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left)
            {
                Dispatcher.Invoke(ShowDashboard);
            }
        };
    }

    private static BitmapImage? LoadWindowIcon()
    {
        var iconPath = AppIcon.GetPath(AppContext.BaseDirectory);
        return File.Exists(iconPath)
            ? new BitmapImage(new Uri(iconPath, UriKind.Absolute))
            : null;
    }

    private static Forms.ToolStripMenuItem AddTrayMenuItem(
        Forms.ContextMenuStrip menu,
        string text,
        EventHandler click)
    {
        var item = new Forms.ToolStripMenuItem(text)
        {
            Padding = new Forms.Padding(8, 4, 8, 4)
        };
        item.Click += click;
        menu.Items.Add(item);
        return item;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshAsync();

    private void Settings_Click(object sender, RoutedEventArgs e) => ShowSettings();

    private void ShowSettings()
    {
        RestoreWindow();
        if (SettingsPanel.Visibility != Visibility.Visible)
        {
            dashboardPosition = new System.Windows.Point(Left, Top);
        }

        var anchorBounds = new System.Windows.Rect(
            dashboardPosition?.X ?? Left,
            dashboardPosition?.Y ?? Top,
            BallWindowSize,
            BallWindowSize);
        var settingsPosition = WindowPlacementCalculator.CalculateSettingsPosition(
            anchorBounds,
            new System.Windows.Size(SettingsWindowWidth, SettingsWindowHeight),
            GetCurrentWorkArea(),
            SettingsWindowGap);

        Width = SettingsWindowWidth;
        Height = SettingsWindowHeight;
        Left = settingsPosition.X;
        Top = settingsPosition.Y;
        WeeklyBudgetBox.Text = settings.WeeklyBudgetConfigured
            ? settings.WeeklyBudgetTokens.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
        RefreshSecondsBox.Text = settings.RefreshSeconds.ToString(CultureInfo.InvariantCulture);
        OpacitySlider.Value = settings.Opacity * 100;
        TopmostBox.IsChecked = settings.Topmost;
        AutoStartBox.IsChecked = settings.AutoStart;
        WeeklyRingModeBox.SelectedIndex = settings.WeeklyRingGradientEnabled ? 1 : 0;
        WeeklyRingStartColorBox.Text = settings.WeeklyRingColor;
        WeeklyRingEndColorBox.Text = settings.WeeklyRingGradientColor;
        UpdateRingColorInputs();
        DashboardPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Visible;
    }

    private void ShowDashboard()
    {
        RestoreWindow();
        SettingsPanel.Visibility = Visibility.Collapsed;
        DashboardPanel.Visibility = Visibility.Visible;
        Width = BallWindowSize;
        Height = BallWindowSize;
        if (dashboardPosition is { } position)
        {
            Left = position.X;
            Top = position.Y;
            dashboardPosition = null;
        }
    }

    private void SettingsCancel_Click(object sender, RoutedEventArgs e) => ShowDashboard();

    private void SettingsSave_Click(object sender, RoutedEventArgs e)
    {
        var budget = 0L;
        var hasBudget = !string.IsNullOrWhiteSpace(WeeklyBudgetBox.Text);
        if ((hasBudget &&
            (!long.TryParse(WeeklyBudgetBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out budget) || budget <= 0)) ||
            !int.TryParse(RefreshSecondsBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var refreshSeconds))
        {
            ShowLocalizedMessage(
                "周额度和刷新间隔必须是有效数字。",
                "Weekly budget and refresh interval must be valid numbers.",
                "设置无效",
                "Invalid settings");
            return;
        }

        if (!ColorParser.TryParseHex(WeeklyRingStartColorBox.Text, out var startColor) ||
            !ColorParser.TryParseHex(WeeklyRingEndColorBox.Text, out var endColor))
        {
            ShowLocalizedMessage(
                "外圈颜色必须是有效的十六进制颜色，例如 #58B7E8。",
                "Ring colors must be valid hexadecimal colors, for example #58B7E8.",
                "设置无效",
                "Invalid settings");
            return;
        }

        var newSettings = SettingsStore.Normalize(settings with
        {
            WeeklyBudgetTokens = budget,
            WeeklyBudgetConfigured = hasBudget,
            RefreshSeconds = refreshSeconds,
            Opacity = OpacitySlider.Value / 100,
            Topmost = TopmostBox.IsChecked == true,
            AutoStart = AutoStartBox.IsChecked == true,
            WeeklyRingColor = ColorParser.ToHex(startColor),
            WeeklyRingGradientColor = ColorParser.ToHex(endColor),
            WeeklyRingGradientEnabled = WeeklyRingModeBox.SelectedIndex == 1
        });

        settingsStore.Save(newSettings);
        settings = newSettings;
        ApplyWeeklyRingSettings();
        Topmost = settings.Topmost;
        Opacity = settings.Opacity;
        ConfigureRefreshTimer();
        SetAutoStart(settings.AutoStart);
        ShowDashboard();
        RefreshAsync();
    }

    private void WeeklyRingMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateRingColorInputs();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValueText is not null)
        {
            OpacityValueText.Text = SettingsDisplayFormatter.FormatOpacityPercent(e.NewValue);
        }
    }

    private void RingColor_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateRingColorInputs();
    }

    private void PickStartColor_Click(object sender, RoutedEventArgs e)
    {
        PickRingColor(WeeklyRingStartColorBox);
    }

    private void PickEndColor_Click(object sender, RoutedEventArgs e)
    {
        PickRingColor(WeeklyRingEndColorBox);
    }

    private void PickRingColor(System.Windows.Controls.TextBox textBox)
    {
        using var dialog = new Forms.ColorDialog
        {
            AllowFullOpen = true,
            AnyColor = true,
            FullOpen = true,
            SolidColorOnly = true
        };
        if (ColorParser.TryParseHex(textBox.Text, out var currentColor))
        {
            dialog.Color = System.Drawing.Color.FromArgb(
                currentColor.Red,
                currentColor.Green,
                currentColor.Blue);
        }

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            textBox.Text = ColorParser.ToHex(ColorParser.FromDrawingColor(dialog.Color));
        }
    }

    private void UpdateRingColorInputs()
    {
        if (WeeklyRingEndColorBox is null)
        {
            return;
        }

        WeeklyRingEndColorBox.IsEnabled = WeeklyRingModeBox.SelectedIndex == 1;
        UpdateColorPreview(WeeklyRingStartColorBox, WeeklyRingStartPreview);
        UpdateColorPreview(WeeklyRingEndColorBox, WeeklyRingEndPreview);
    }

    private static void UpdateColorPreview(System.Windows.Controls.TextBox textBox, Border preview)
    {
        preview.Background = ColorParser.TryParseHex(textBox.Text, out var color)
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.Red, color.Green, color.Blue))
            : System.Windows.Media.Brushes.Transparent;
        preview.BorderBrush = ColorParser.TryParseHex(textBox.Text, out _)
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225))
            : System.Windows.Media.Brushes.IndianRed;
    }

    private void ApplyWeeklyRingSettings()
    {
        var startColor = ColorParser.TryParseHex(settings.WeeklyRingColor, out var parsedStart)
            ? parsedStart
            : new WaterBallColor(88, 183, 232);
        var endColor = ColorParser.TryParseHex(settings.WeeklyRingGradientColor, out var parsedEnd)
            ? parsedEnd
            : new WaterBallColor(139, 220, 245);
        waterBall.WeeklyRingStartColor = startColor;
        waterBall.WeeklyRingEndColor = endColor;
        waterBall.WeeklyRingGradientEnabled = settings.WeeklyRingGradientEnabled;
    }

    private void Language_Click(object sender, RoutedEventArgs e)
    {
        ToggleLanguage();
    }

    private void ToggleLanguage()
    {
        settings = SettingsStore.Normalize(settings with
        {
            Language = WidgetLanguage.IsEnglish(settings.Language)
                ? WidgetLanguage.Chinese
                : WidgetLanguage.English
        });
        settingsStore.Save(settings);
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        var isEnglish = WidgetLanguage.IsEnglish(settings.Language);
        Title = isEnglish ? "Codex Usage" : "Codex 剩余用量";

        RefreshMenuItem.Header = isEnglish ? "Refresh" : "刷新";
        SettingsMenuItem.Header = isEnglish ? "Settings" : "设置";
        OfficialUsageMenuItem.Header = isEnglish ? "Open official usage" : "打开官方用量";
        LanguageMenuItem.Header = isEnglish ? "切换到中文" : "Switch to English";
        ExitMenuItem.Header = isEnglish ? "Exit" : "退出";

        SettingsTitleText.Text = isEnglish ? "Widget settings" : "小工具设置";
        SettingsSubtitleText.Text = isEnglish
            ? "Adjust refresh and ring appearance"
            : "调整数据刷新与外圈显示样式";
        DataRefreshSectionText.Text = isEnglish ? "Data and refresh" : "数据与刷新";
        WeeklyBudgetLabel.Text = isEnglish ? "Weekly budget (optional)" : "周额度（可选 token）";
        RefreshIntervalLabel.Text = isEnglish ? "Refresh interval (seconds)" : "刷新间隔（秒）";
        OpacityLabel.Text = isEnglish ? "Window opacity" : "窗口不透明度";
        RingStyleSectionText.Text = isEnglish ? "Ring style" : "外圈样式";
        RingModeLabel.Text = isEnglish ? "Color mode" : "颜色模式";
        SolidModeItem.Content = isEnglish ? "Solid" : "纯色";
        GradientModeItem.Content = isEnglish ? "Gradient" : "渐变色";
        StartColorLabel.Text = isEnglish ? "Start color" : "起始颜色";
        EndColorLabel.Text = isEnglish ? "End color" : "结束颜色";
        ColorHintText.Text = isEnglish
            ? "Use #RRGGBB, for example #58B7E8. Solid mode uses the start color."
            : "支持 #RRGGBB，例如 #58B7E8；纯色模式只使用起始颜色。";
        TopmostBox.Content = isEnglish ? "Always on top" : "窗口置顶";
        AutoStartBox.Content = isEnglish ? "Start with Windows" : "开机启动";
        CancelButton.Content = isEnglish ? "Cancel" : "取消";
        SaveButton.Content = isEnglish ? "Save settings" : "保存设置";
        WeeklyBudgetBox.ToolTip = isEnglish
            ? "Leave blank to avoid setting a manual weekly budget."
            : "留空表示不手动设置周额度";
        RefreshSecondsBox.ToolTip = isEnglish
            ? "Official usage query interval, from 10 to 600 seconds."
            : "官方用量查询间隔，范围 10–600 秒";

        if (trayRefreshMenuItem is not null)
        {
            trayRefreshMenuItem.Text = isEnglish ? "Refresh" : "刷新";
            traySettingsMenuItem.Text = isEnglish ? "Settings" : "设置";
            trayOfficialUsageMenuItem.Text = isEnglish ? "Open official usage" : "打开官方用量";
            trayLanguageMenuItem.Text = isEnglish ? "切换到中文" : "Switch to English";
            trayExitMenuItem.Text = isEnglish ? "Exit" : "退出";
            trayIcon.Text = isEnglish ? "Codex Usage" : "Codex 剩余用量";
        }
    }

    private void ShowLocalizedMessage(
        string chineseMessage,
        string englishMessage,
        string chineseTitle,
        string englishTitle)
    {
        var isEnglish = WidgetLanguage.IsEnglish(settings.Language);
        System.Windows.MessageBox.Show(
            isEnglish ? englishMessage : chineseMessage,
            isEnglish ? englishTitle : chineseTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private static void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            writable: true);
        if (key is null)
        {
            return;
        }

        if (enabled && !string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            key.SetValue(StartupValueName, $"\"{Environment.ProcessPath}\"");
        }
        else
        {
            key.DeleteValue(StartupValueName, throwOnMissingValue: false);
        }
    }

    private static void OpenOfficialUsage()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = OfficialUsageUrl,
            UseShellExecute = true
        });
    }

    private void OfficialUsage_Click(object sender, RoutedEventArgs e) => OpenOfficialUsage();

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
