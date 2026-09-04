# Antigravity Usage Widget / Codex 用量悬浮球

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
- The weekly ring defaults to the current light-green-to-deep-blue gradient and can be customized as a solid color or a two-color gradient.
  周用量外环默认使用当前的浅绿色到深蓝色渐变，也可以在设置中自定义为纯色或双色渐变。
- The settings panel uses grouped white cards, editable `#RRGGBB` values, color previews, and input validation.
  设置面板采用白底分组布局，支持编辑 `#RRGGBB` 色值、颜色预览和输入校验。
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
- File watching refreshes local statistics without opening any window. Official limits are read silently through the local Codex session and are cached after a successful read.
  文件监听会在不打开任何窗口的情况下刷新本地统计，官方额度通过本机 Codex 登录会话静默读取，成功后会缓存最后一次结果。
- Automatic official reads wait for five seconds of user inactivity and have an independent ten-minute cooldown. Manual refresh remains explicit and immediate.
  自动读取会等待用户连续五秒无操作，并设有独立的十分钟冷却；手动刷新仍按用户明确操作立即执行。
- Click the ball to refresh official usage once; left-drag moves the widget, while right-click opens settings, official usage, and exit actions.
  点击小球手动刷新一次官方用量；左键拖动窗口，右键打开设置、官方用量和退出菜单。
- The right-click menu supports hot switching between Chinese and English, including the settings panel and tray menu.
  右键菜单支持中英文热切换，设置面板和托盘菜单会同步更新。
- Hover details and reset countdown text follow the selected language as well.
  鼠标悬停详情和重置倒计时文字也会跟随当前语言切换。

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
dotnet test tests\AntigravityUsageWidget.Tests\AntigravityUsageWidget.Tests.csproj
dotnet build src\AntigravityUsageWidget\AntigravityUsageWidget.csproj -c Release
.\publish.ps1
.\publish\AntigravityUsageWidget.exe
```

## Data and Privacy / 数据与隐私

The widget reads local Codex data and the local Codex login session in read-only mode:

程序以只读方式读取本机 Codex 数据和 Codex 登录会话：

- `%USERPROFILE%\.codex\state_5.sqlite`
- `%USERPROFILE%\.codex\sessions\**\rollout-*.jsonl`
- `%USERPROFILE%\.codex\auth.json` (only the current access token and account ID needed for the official usage request)

Authentication data is never displayed, logged, or uploaded. The widget does not simulate mouse clicks or open the desktop app's usage menu.

认证数据不会被显示、记录或上传，程序不会模拟鼠标点击，也不会打开桌面端用量菜单。

## Project Structure / 项目结构

- `src/AntigravityUsageWidget`: desktop application / 桌面程序
- `tests/AntigravityUsageWidget.Tests`: automated tests / 自动化测试
- `publish.ps1`: publish script / 发布脚本

## Releases / 发布版本

Windows packages are published on GitHub Releases with bilingual release notes.

Windows 安装包通过 GitHub Releases 发布，并提供中英双语版本说明。
