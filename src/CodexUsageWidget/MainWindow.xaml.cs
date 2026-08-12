using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
    private const double SettingsWindowWidth = 360;
    private const double SettingsWindowHeight = 245;
    private const double SettingsWindowGap = 12;
    private const string OfficialUsageUrl = "https://chatgpt.com";
    private const string StartupValueName = "CodexUsageWidget";

    private readonly SettingsStore settingsStore = new();
    private readonly OfficialUsageReader officialUsageReader = new();
    private readonly UserActivityMonitor userActivityMonitor = new();
    private readonly WaterBallControl waterBall = new();
    private readonly System.Windows.Controls.TextBlock ballDetailsText = CreateDetailsTextBlock();
    private readonly System.Windows.Controls.TextBlock hostDetailsText = CreateDetailsTextBlock();
    private readonly UsageRefreshService refreshService;
    private readonly DispatcherTimer refreshTimer;
    private readonly DispatcherTimer localRefreshTimer;
    private readonly UsageFileWatcher usageFileWatcher;
    private readonly CancellationTokenSource shutdownCancellation = new();
    private Forms.NotifyIcon trayIcon = null!;
    private WidgetSettings settings;
    private bool isRefreshing;
    private bool localRefreshPending;
    private System.Windows.Point? dashboardPosition;
    private volatile bool officialRefreshSuspended;

    public MainWindow()
    {
        InitializeComponent();
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
        var dataPaths = CodexDataPaths.ForCurrentUser();
        refreshService = new UsageRefreshService(
            new CodexDataReader(),
            dataPaths,
            ReadOfficialUsageIfSafe);
        refreshTimer = new DispatcherTimer();
        refreshTimer.Tick += RefreshTimer_Tick;
        localRefreshTimer = new DispatcherTimer
        {
            Interval = LocalRefreshDebounce
        };
        localRefreshTimer.Tick += LocalRefreshTimer_Tick;
        usageFileWatcher = new UsageFileWatcher(dataPaths);
        usageFileWatcher.Changed += UsageFileWatcher_Changed;
        ConfigureWindow();
        ConfigureTrayIcon();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        ConfigureRefreshTimer();
        RefreshAsync();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
        localRefreshTimer.Stop();
        shutdownCancellation.Cancel();
        usageFileWatcher.Dispose();
        var savedPosition = dashboardPosition ?? new System.Windows.Point(Left, Top);
        settingsStore.Save(settings with { Left = savedPosition.X, Top = savedPosition.Y });
        trayIcon.Visible = false;
        trayIcon.Dispose();
    }

    private void ConfigureWindow()
    {
        Topmost = settings.Topmost;
        Opacity = settings.Opacity;
    }

    private void PositionWindow()
    {
        if (double.IsFinite(settings.Left) && double.IsFinite(settings.Top))
        {
            Left = settings.Left;
            Top = settings.Top;
            return;
        }

        Left = SystemParameters.WorkArea.Right - Width - 24;
        Top = SystemParameters.WorkArea.Top + 24;
    }

    private void ConfigureRefreshTimer()
    {
        refreshTimer.Interval = TimeSpan.FromSeconds(settings.RefreshSeconds);
        refreshTimer.Start();
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e) => RefreshAsync(refreshOfficial: true);

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
        var officialText = WaterBallDisplay.FormatCenterText(state.OfficialRemainingPercent);
        waterBall.RemainingPercent = state.OfficialRemainingPercent;
        waterBall.CenterText = officialText;
        SetDetails(string.Join(
            Environment.NewLine,
            $"周剩余：{officialText}",
            $"近 7 天总量：{UsageDisplayFormatter.FormatMillions(state.SevenDay.Usage.TotalTokens)}",
            $"近 30 天总量：{UsageDisplayFormatter.FormatMillions(state.ThirtyDay.Usage.TotalTokens)}",
            $"状态：{state.Status}",
            $"更新时间：{state.RefreshedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}"));
    }

    private void SetDetails(string details)
    {
        ballDetailsText.Text = details;
        hostDetailsText.Text = details;
    }

    private static System.Windows.Controls.TextBlock CreateDetailsTextBlock()
    {
        return new System.Windows.Controls.TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 260
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

    private double? ReadOfficialUsageIfSafe()
    {
        if (officialRefreshSuspended ||
            !userActivityMonitor.WaitForQuietPeriod(shutdownCancellation.Token) ||
            officialRefreshSuspended ||
            userActivityMonitor.IsUserActive())
        {
            return null;
        }

        return officialUsageReader.ReadRemainingPercent();
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
        trayIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
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
        AddTrayMenuItem(menu, "刷新", (_, _) => Dispatcher.Invoke(RefreshAsync));
        AddTrayMenuItem(menu, "设置", (_, _) => Dispatcher.Invoke(ShowSettings));
        AddTrayMenuItem(menu, "打开官方用量", (_, _) => Dispatcher.Invoke(OpenOfficialUsage));
        AddTrayMenuItem(menu, "退出", (_, _) => Dispatcher.Invoke(Close));
        trayIcon.ContextMenuStrip = menu;
        trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowDashboard);
    }

    private static void AddTrayMenuItem(
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
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshAsync();

    private void Settings_Click(object sender, RoutedEventArgs e) => ShowSettings();

    private void ShowSettings()
    {
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
        officialRefreshSuspended = true;
        WeeklyBudgetBox.Text = settings.WeeklyBudgetConfigured
            ? settings.WeeklyBudgetTokens.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
        RefreshSecondsBox.Text = settings.RefreshSeconds.ToString(CultureInfo.InvariantCulture);
        OpacitySlider.Value = settings.Opacity * 100;
        TopmostBox.IsChecked = settings.Topmost;
        AutoStartBox.IsChecked = settings.AutoStart;
        DashboardPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Visible;
    }

    private void ShowDashboard()
    {
        officialRefreshSuspended = false;
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
            System.Windows.MessageBox.Show("周额度和刷新间隔必须是有效数字。", "设置无效", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var newSettings = SettingsStore.Normalize(settings with
        {
            WeeklyBudgetTokens = budget,
            WeeklyBudgetConfigured = hasBudget,
            RefreshSeconds = refreshSeconds,
            Opacity = OpacitySlider.Value / 100,
            Topmost = TopmostBox.IsChecked == true,
            AutoStart = AutoStartBox.IsChecked == true
        });

        settingsStore.Save(newSettings);
        settings = newSettings;
        Topmost = settings.Topmost;
        Opacity = settings.Opacity;
        ConfigureRefreshTimer();
        SetAutoStart(settings.AutoStart);
        ShowDashboard();
        RefreshAsync();
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
