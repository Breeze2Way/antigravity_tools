# Codex Usage Widget / Codex 用量悬浮球

A compact Windows floating widget that visualizes the official Codex weekly remaining percentage as a liquid neon energy ball.

一个小巧的 Windows 悬浮工具，用液态霓虹能量球直观显示 Codex 官方周剩余百分比。

## Features / 功能

- Official weekly remaining percentage in the center of the ball.
  圆球中心显示官方周剩余百分比。
- Water level, shell tint, glow, and ring color follow the remaining percentage.
  水位、球体底色、光晕和外圈颜色随剩余百分比变化。
- Neon glass reflection, dynamic waterline, and floating bubbles.
  提供霓虹玻璃反光、动态水线和上浮气泡效果。
- Animation speed and glow respond to recent Token consumption.
  动画速度和光晕会根据近期 Token 消耗速度响应。
- Hover card shows weekly remaining, local 7-day/30-day totals, update time, and reset countdown.
  鼠标悬停显示周剩余、近 7 天/30 天本地统计、更新时间和重置倒计时。
- The reset line is highlighted in red and uses compact integer hours such as `132h`.
  重置行使用红色强调，并以 `132h` 这样的整数小时显示。
- Low remaining usage gets a soft red alert ring instead of a disruptive flashing window.
  剩余量较低时显示柔和红色警戒环，不会用闪烁窗口打断工作。
- The weekly remaining percentage is read in the background from local Codex session metadata (`rate_limits.primary`), without clicking or interrupting the desktop app.
  周剩余百分比在后台直接读取本地 Codex 会话元数据（`rate_limits.primary`），不会点击或打断桌面端操作。
- File watching and a low-frequency timer keep the local value current; the last valid value is cached if a read temporarily fails.
  文件监听和低频定时器会自动更新本地数值，读取暂时失败时继续显示上一次有效缓存。
- Local file changes refresh the 7-day/30-day statistics, while the legacy official reader remains available only as an optional fallback.
  本地文件变化会刷新 7 天/30 天统计，旧版官方读取器仅保留为可选备用方式。
- Left-drag moves the widget; right-click opens refresh, settings, official usage, and exit actions.
  左键拖动窗口，右键打开刷新、设置、官方用量和退出菜单。

## Performance / 性能

The animation uses an adaptive background-priority timer instead of repainting on every screen frame:

动画使用自适应的后台优先级定时器，不再跟随屏幕每一帧重绘：

- No official percentage: approximately 160 ms per frame.
  没有官方百分比：约每 160ms 更新一次。
- Normal state: approximately 100 ms per frame.
  普通状态：约每 100ms 更新一次。
- Hover or fast Token consumption: approximately 50 ms per frame.
  鼠标悬停或 Token 消耗较快：约每 50ms 更新一次。
- Invisible controls skip rendering work.
  控件不可见时跳过绘制工作。

## Requirements / 运行要求

- Windows 10/11 x64
- .NET 9 Desktop Runtime

## Build and Run / 构建与运行

```powershell
dotnet test tests\CodexUsageWidget.Tests\CodexUsageWidget.Tests.csproj
dotnet build src\CodexUsageWidget\CodexUsageWidget.csproj -c Release
.\publish.ps1
.\publish\CodexUsageWidget.exe
```

## Data and Privacy / 数据与隐私

The widget reads local Codex data in read-only mode:

程序以只读方式读取本机 Codex 数据：

- `%USERPROFILE%\.codex\state_5.sqlite`
- `%USERPROFILE%\.codex\sessions\**\rollout-*.jsonl`

It does not read, display, or upload `auth.json`, API keys, passwords, or other authentication data.

程序不会读取、显示或上传 `auth.json`、API key、密码或其他认证信息，也不会模拟鼠标点击桌面端用量页面。

## Project Structure / 项目结构

- `src/CodexUsageWidget`: desktop application / 桌面程序
- `tests/CodexUsageWidget.Tests`: automated tests / 自动化测试
- `publish.ps1`: publish script / 发布脚本

## Releases / 发布版本

Windows packages are published on GitHub Releases with bilingual release notes.

Windows 安装包通过 GitHub Releases 发布，并提供中英双语版本说明。
