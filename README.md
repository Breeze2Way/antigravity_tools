# Codex Usage Widget

一个小巧的 Windows 悬浮圆球，用水位直观显示 Codex 桌面端的官方周剩余百分比。

## 功能

- 圆球中心显示官方周剩余百分比。
- 水位和颜色随剩余百分比连续变化。
- 鼠标悬停显示周剩余、近 7 天和近 30 天 token 总量（M）、状态及更新时间。
- 统计只包含用户线程，会排除 Codex 子代理线程。
- 左键拖动窗口，右键打开刷新、设置、官方用量页面和退出菜单。

## 运行要求

- Windows 10/11 x64
- .NET 9 Desktop Runtime

## 构建与运行

```powershell
dotnet test tests\CodexUsageWidget.Tests\CodexUsageWidget.Tests.csproj
dotnet build src\CodexUsageWidget\CodexUsageWidget.csproj -c Release
.\publish.ps1
.\publish\CodexUsageWidget.exe
```

## 数据与隐私

程序以只读方式读取本机 Codex 数据和桌面端显示的剩余百分比：

- `%USERPROFILE%\.codex\state_5.sqlite`
- `%USERPROFILE%\.codex\sessions\**\rollout-*.jsonl`

程序不会读取、显示或上传 `auth.json`、API key 或其他认证信息。

## 项目结构

- `src/CodexUsageWidget`：桌面程序
- `tests/CodexUsageWidget.Tests`：自动化测试
- `publish.ps1`：发布脚本
