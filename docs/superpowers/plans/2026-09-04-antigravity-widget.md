# Antigravity Widget Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an independent .NET 9 Windows widget that preserves the Codex widget experience while reading official Antigravity quotas from its local language server.

**Architecture:** Clone the existing WPF application into a separate repository and rename the application namespace, project, solution, tests, published executable, and user-facing copy. Keep the existing rendering and interaction code, and replace the Codex data adapter with a small Antigravity process-discovery/Connect-RPC reader plus a quota parser and conservative short/weekly aggregation layer.

**Tech Stack:** .NET 9 WPF/Windows Forms, C#, `System.Text.Json`, `System.Management` for Windows process command-line discovery, `HttpClient`, xUnit, Microsoft.Data.Sqlite only for retained shared code where required.

**Spec:** `docs/superpowers/specs/2026-09-04-antigravity-widget-design.md`

## Global Constraints

- Target framework remains `net9.0-windows`; runtime support is Windows 10/11 x64.
- Official quota requests are read-only and limited to Antigravity's loopback language server.
- Never log, display, persist, or upload CSRF tokens or Google authentication material.
- Inner display uses the lowest short-period remaining quota; outer ring uses the lowest weekly remaining quota; missing periods stay muted.
- Preserve existing widget interactions and bilingual settings unless they are Codex-specific.
- Run tests before claiming completion, then build Release and run the publish script.

---

### Task 1: Establish the independent application shell

**Files:**
- Modify: `src/CodexUsageWidget/CodexUsageWidget.csproj` → renamed project `src/AntigravityUsageWidget/AntigravityUsageWidget.csproj`
- Modify: `src/CodexUsageWidget/CodexUsageWidget.sln` → renamed solution `src/AntigravityUsageWidget/AntigravityUsageWidget.sln`
- Modify: all tracked files under `src/CodexUsageWidget`, `tests/CodexUsageWidget.Tests`, `README.md`, and `publish.ps1` to use Antigravity application names and paths.
- Test: existing migrated test project as the shell baseline.

**Interfaces:** Produces the independent `AntigravityUsageWidget` project and test project while retaining the existing behavior until the data adapter is replaced.

- [ ] Copy/rename tracked source and test directories, excluding `bin`, `obj`, `publish`, and zip artifacts.
- [ ] Update namespaces, project references, assembly names, solution entries, application icon metadata, publish output name, registry startup value, window/tray/automation labels, README paths, and bilingual product copy.
- [ ] Run `dotnet test tests\\AntigravityUsageWidget.Tests\\AntigravityUsageWidget.Tests.csproj` to establish the renamed baseline.
- [ ] Commit with `chore: scaffold Antigravity widget project`.

### Task 2: Define and parse Antigravity official quota data

**Files:**
- Create: `src/AntigravityUsageWidget/Data/AntigravityQuotaSnapshot.cs`
- Create: `src/AntigravityUsageWidget/Data/AntigravityQuotaParser.cs`
- Create: `tests/AntigravityUsageWidget.Tests/AntigravityQuotaParserTests.cs`

**Interfaces:** `AntigravityQuotaParser.Parse(string json) -> AntigravityQuotaSnapshot`; snapshot contains plan name and immutable quota rows with `Label`, `RemainingPercent`, `ResetAt`, and `Period` (`Short`, `Weekly`, or `Unknown`).

- [ ] Write a failing test for `RetrieveUserQuotaSummary`-shaped JSON containing Gemini and Claude/GPT short and weekly rows; assert percentages are converted from fractions and timestamps are parsed.
- [ ] Run the focused test and verify it fails because the parser/snapshot does not exist.
- [ ] Write a failing test for invalid fractions, empty labels, internal models, and malformed rows; assert invalid rows are skipped without failing the whole response.
- [ ] Run the focused tests and verify the expected failures.
- [ ] Implement the minimal immutable snapshot types and parser supporting both summary groups and `GetUserStatus` model rows.
- [ ] Run the focused tests and verify all parser tests pass.
- [ ] Commit with `feat: add Antigravity quota parser`.

### Task 3: Discover the local language server and call Connect-RPC

**Files:**
- Create: `src/AntigravityUsageWidget/Data/AntigravityLanguageServerDiscovery.cs`
- Create: `src/AntigravityUsageWidget/Data/AntigravityStatusReader.cs`
- Create: `tests/AntigravityUsageWidget.Tests/AntigravityLanguageServerDiscoveryTests.cs`
- Create: `tests/AntigravityUsageWidget.Tests/AntigravityStatusReaderTests.cs`
- Modify: `src/AntigravityUsageWidget/AntigravityUsageWidget.csproj` to add the Windows-only process discovery dependency if required by the implementation.

**Interfaces:** `AntigravityLanguageServerDiscovery.TryFind(out AntigravityServerEndpoint endpoint) -> bool`; endpoint contains loopback ports and a redacted internal token value. `AntigravityStatusReader.ReadUsage() -> AntigravityQuotaSnapshot?`.

- [ ] Write failing tests for selecting only `language_server.exe` command lines that contain the Antigravity app-data marker, extracting a CSRF token, and rejecting missing token/port candidates.
- [ ] Run the focused tests and verify they fail for the missing discovery implementation.
- [ ] Implement process enumeration with command-line inspection, token extraction without logging, and per-process listening-port enumeration.
- [ ] Run discovery tests and verify they pass.
- [ ] Write a failing reader test using an injected HTTP transport to assert required headers, local endpoint selection, and summary → status → model-config fallback order.
- [ ] Run the focused reader test and verify it fails before the reader exists.
- [ ] Implement loopback HTTP/HTTPS transport with short timeout, local certificate acceptance only for loopback, endpoint probing, fallback calls, and exception-to-null conversion.
- [ ] Run reader tests and verify all pass.
- [ ] Commit with `feat: read official Antigravity quotas from language server`.

### Task 4: Adapt refresh state and display formatting

**Files:**
- Create: `src/AntigravityUsageWidget/Services/AntigravityQuotaAggregator.cs`
- Create: `src/AntigravityUsageWidget/Services/AntigravityUsageDisplayFormatter.cs`
- Create: `tests/AntigravityUsageWidget.Tests/AntigravityQuotaAggregatorTests.cs`
- Create: `tests/AntigravityUsageWidget.Tests/AntigravityUsageDisplayFormatterTests.cs`
- Modify: `src/AntigravityUsageWidget/Services/UsageRefreshService.cs` and related models to consume official quota snapshots instead of Codex local token data.

**Interfaces:** `AntigravityQuotaAggregator.Aggregate(AntigravityQuotaSnapshot snapshot) -> AntigravityDisplayQuota`; display quota exposes nullable `ShortRemainingPercent`, `WeeklyRemainingPercent`, corresponding reset times, plan, and rows. `AntigravityUsageDisplayFormatter.FormatTooltipDetails(...) -> string`.

- [ ] Write a failing test asserting the minimum short-period and minimum weekly percentages are selected and missing periods remain null.
- [ ] Run the focused test and verify it fails.
- [ ] Implement the minimal aggregator and verify the focused test passes.
- [ ] Write a failing formatter test asserting plan/model rows, percentages, reset times, and bilingual labels are included.
- [ ] Run the focused test and verify it fails.
- [ ] Implement the formatter and verify focused tests pass.
- [ ] Update refresh state retention, local-refresh behavior, alert coloring, and animation inputs so failed reads retain the last successful snapshot and no token-estimate text remains.
- [ ] Run the complete test suite and fix only regressions caused by the provider replacement.
- [ ] Commit with `feat: map Antigravity quotas to widget state`.

### Task 5: Wire the UI, settings, tray, and official usage action

**Files:**
- Modify: `src/AntigravityUsageWidget/MainWindow.xaml.cs`
- Modify: `src/AntigravityUsageWidget/MainWindow.xaml`
- Modify: `src/AntigravityUsageWidget/Data/UsageFileWatcher.cs` or remove its Codex-only wiring if no longer needed.
- Modify: `tests/AntigravityUsageWidget.Tests/MainWindowCompositionTests.cs`, `SettingsWindowLayoutTests.cs`, and `WidgetLanguageTests.cs`.

**Interfaces:** `MainWindow.CreateRefreshService(...)` creates the Antigravity reader and refresh service; `OpenOfficialUsage()` activates Antigravity or starts the installed executable; all existing menu event handlers remain available.

- [ ] Write failing composition/language tests for Antigravity labels, official quota detail placeholders, and removal of Codex-specific data fields.
- [ ] Run the focused tests and verify the expected failures.
- [ ] Replace Codex data-path/watcher construction with Antigravity reader construction, preserving timers and manual refresh behavior.
- [ ] Change tooltip, title, tray, automation, settings, and error strings to Antigravity bilingual copy.
- [ ] Wire official usage action to the running Antigravity window or installed executable without UI scraping.
- [ ] Run all tests and verify they pass.
- [ ] Commit with `feat: wire Antigravity widget UI`.

### Task 6: Documentation, package, verification, and GitHub push

**Files:**
- Modify: `README.md`
- Modify: `publish.ps1`
- Create: `docs/superpowers/specs/2026-09-04-antigravity-widget-design.md`
- Create: `docs/superpowers/plans/2026-09-04-antigravity-widget.md`

- [ ] Update README with Antigravity prerequisites, loopback language-server data source, supported fallback endpoints, privacy behavior, build/test commands, and the fact that the IDE must be running for live quota reads.
- [ ] Run `dotnet test tests\\AntigravityUsageWidget.Tests\\AntigravityUsageWidget.Tests.csproj` and record the complete pass count.
- [ ] Run `dotnet build src\\AntigravityUsageWidget\\AntigravityUsageWidget.csproj -c Release` and verify exit code 0.
- [ ] Run `powershell -ExecutionPolicy Bypass -File .\\publish.ps1` and verify the published executable exists.
- [ ] Inspect `git diff --check`, `git status`, and the staged diff to ensure no auth data, build output, or unrelated source changes are included.
- [ ] Commit with `feat: add Antigravity usage widget`.
- [ ] Verify the remote URL is `https://github.com/Breeze2Way/antigravity_tools.git`; push `main` and report the exact result. If the remote repository is absent, report that concrete GitHub error instead of claiming the push succeeded.
