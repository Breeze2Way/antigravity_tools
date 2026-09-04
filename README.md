# Antigravity Usage Widget / Antigravity 配额悬浮球

A compact Windows floating widget that visualizes official Antigravity model quotas as a liquid neon energy ball.

一个小巧的 Windows 悬浮工具，用液态霓虹能量球显示 Antigravity 官方模型配额。

## Features / 功能

- The inner water ball shows the lowest remaining short-period quota; the outer ring shows the lowest remaining weekly quota.
  内圈水球显示所有短周期配额中的最低剩余值；外圈显示所有周配额中的最低剩余值。
- Missing quota periods remain visually muted instead of showing invented values.
  缺失的额度周期保持低亮度，不显示伪造数值。
- Hover details show the Antigravity plan, every model/group quota, remaining percentages, reset times, and refresh time.
  鼠标悬停显示 Antigravity 套餐、每个模型/模型组配额、剩余百分比、重置时间和更新时间。
- Neon glass shell, adaptive glow, animated water waves, and customizable weekly ring colors.
  提供霓虹玻璃球壳、自适应光晕、动态水波和可自定义的周额度外圈颜色。
- Click the ball to refresh immediately; left-drag moves it; right-click opens settings, Antigravity, language switching, and exit actions.
  点击小球立即刷新；左键拖动；右键打开设置、Antigravity、语言切换和退出菜单。
- Automatic official reads wait for five seconds of user inactivity and use a separate ten-minute cooldown.
  自动读取会等待用户连续五秒无操作，并设有独立的十分钟冷却。
- Failed reads keep the last successful snapshot and show a non-disruptive bilingual status.
  读取失败时保留上次成功快照，并显示非打扰式双语状态。

## Requirements / 运行要求

- Windows 10/11 x64
- .NET 9 Desktop Runtime
- Antigravity IDE running and signed in for live quota reads

## Build and Run / 构建与运行

```powershell
dotnet test tests\AntigravityUsageWidget.Tests\AntigravityUsageWidget.Tests.csproj
dotnet build src\AntigravityUsageWidget\AntigravityUsageWidget.csproj -c Release
.\publish.ps1
.\publish\AntigravityUsageWidget.exe
```

## Data and Privacy / 数据与隐私

The widget reads official quota data from the local Antigravity language server in read-only mode:

程序以只读方式从本机 Antigravity language server 读取官方配额：

- Finds the Antigravity-scoped `language_server.exe` process.
  查找属于 Antigravity 的 `language_server.exe` 进程。
- Extracts the temporary CSRF token from that process in memory and sends Connect-RPC requests only to `127.0.0.1`.
  在内存中提取临时 CSRF token，并且只向 `127.0.0.1` 发送 Connect-RPC 请求。
- Tries `RetrieveUserQuotaSummary`, then `GetUserStatus`, then `GetCommandModelConfigs`.
  依次尝试 `RetrieveUserQuotaSummary`、`GetUserStatus` 和 `GetCommandModelConfigs`。
- Does not read or persist Google passwords, cookies, OAuth tokens, or remote credentials.
  不读取或持久化 Google 密码、Cookie、OAuth token 或远程凭据。

The IDE must be running because the language-server port and CSRF token are temporary. The widget does not scrape the Antigravity UI or simulate clicks.

由于语言服务器端口和 CSRF token 是临时的，必须保持 IDE 运行。程序不会抓取 Antigravity UI，也不会模拟点击。

## Project Structure / 项目结构

- `src/AntigravityUsageWidget`: desktop application / 桌面程序
- `tests/AntigravityUsageWidget.Tests`: automated tests / 自动化测试
- `publish.ps1`: publish script / 发布脚本

## Releases / 发布版本

Windows packages can be published on GitHub Releases with bilingual release notes.

Windows 安装包可以通过 GitHub Releases 发布，并提供中英双语版本说明。
