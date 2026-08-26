# Codex Usage Widget / Codex 用量悬浮球

A compact Windows floating widget that visualizes Codex five-hour and weekly remaining percentages as a liquid neon energy ball.

一个小巧的 Windows 悬浮工具，用液态霓虹能量球直观显示 Codex 五小时和周剩余百分比。

## Features / 功能

- The ball center automatically prefers the five-hour remaining percentage and falls back to weekly remaining when no five-hour limit is available.
  圆球中心优先显示五小时剩余百分比，没有五小时限制时自动回退到周剩余。
- A thin outer ring shows weekly remaining; the inner water ball shows five-hour remaining.
  外窄环显示周剩余，内圈水球显示五小时剩余。
- Missing limit windows remain visually muted instead of showing misleading values.
  缺失的额度窗口保持低亮度，不显示误导性数值。
- Neon glass shell, adaptive glow, animated water waves, and a weekly progress ring.
  提供霓虹玻璃球壳、自适应光晕、动态水波和周额度进度环。
- The weekly ring uses the inverted RGB color of the inner water ball for stronger contrast.
  周用量外环使用内层水球的 RGB 反色，提升两层之间的对比度。
- Ring animation speed and glow respond to recent Token consumption.
  进度环动画速度和光晕会根据近期 Token 消耗速度响应。
- Hover card shows five-hour remaining, weekly remaining, both reset times, today/yesterday usage, local 7-day/30-day totals, and update time.
  鼠标悬停显示五小时剩余、周剩余、两者重置时间、当日/昨日用量、近 7 天/30 天本地统计和更新时间。
- The reset line is highlighted in red and uses compact integer hours such as `132h`.
  重置行使用红色强调，并以 `132h` 这样的整数小时显示。
- Low remaining usage gets a soft red alert ring instead of a disruptive flashing window.
  剩余量较低时显示柔和红色警戒环，不会用闪烁窗口打断工作。
- Five-hour and weekly limits are identified by their window length (`300` and `10080` minutes), not by whether they appear as `primary` or `secondary`.
  五小时和周额度按窗口长度（`300` 和 `10080` 分钟）自动识别，不依赖它们出现在 `primary` 还是 `secondary`。
- File watching and a low-frequency timer keep the local value current; the last valid value is cached if a read temporarily fails.
  文件监听和低频定时器会自动更新本地数值，读取暂时失败时继续显示上一次有效缓存。
- Local file changes refresh the 7-day/30-day statistics, while the legacy official reader remains available only as an optional fallback.
  本地文件变化会刷新 7 天/30 天统计，旧版官方读取器仅保留为可选备用方式。
- Left-drag moves the widget; right-click opens refresh, settings, official usage, and exit actions.
  左键拖动窗口，右键打开刷新、设置、官方用量和退出菜单。

## Performance / 性能

The animation uses an adaptive background-priority timer instead of repainting on every screen frame:

动画使用自适应的后台优先级定时器，不再跟随屏幕每一帧重绘：

- No official percentage: approximately 360 ms per frame.
  没有官方百分比：约每 360ms 更新一次。
- Normal state: approximately 240 ms per frame.
  普通状态：约每 240ms 更新一次。
- Hover or fast Token consumption: approximately 160 ms per frame.
  鼠标悬停或 Token 消耗较快：约每 160ms 更新一次。
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
