# Antigravity Widget Design

**Goal:** Create an independent Windows floating widget at `D:\VS\antigravity_tools` with the same interaction and visual behavior as `codex_tools`, but displaying official Antigravity model quotas.

## Scope

- Preserve the water-ball UI, weekly ring styling, drag/click/right-click behavior, tray menu, bilingual UI, settings, startup registration, topmost/opacity controls, idle refresh policy, and snapshot caching.
- Replace Codex-specific data sources with the local Antigravity language server.
- Do not modify or depend on the `codex_tools` checkout at runtime.
- Do not read, persist, log, or transmit Google passwords, cookies, or OAuth tokens.

## Data source and flow

`AntigravityStatusReader` discovers an Antigravity-scoped `language_server.exe`, reads its command-line CSRF token, finds its loopback listening ports, and sends read-only Connect-RPC requests to `127.0.0.1`. The reader probes the local endpoint, prefers `RetrieveUserQuotaSummary`, then falls back to `GetUserStatus`, then `GetCommandModelConfigs`. Requests use JSON, `Connect-Protocol-Version: 1`, and `X-Codeium-Csrf-Token`; HTTPS with a local self-signed certificate and HTTP fallback are supported.

The parser returns plan information and user-facing model or model-group quota rows. Each row carries a label, remaining percentage, optional reset time, and an optional short/weekly period. Internal models, empty labels, invalid percentages, and malformed rows are ignored. A successful snapshot is retained when a later process discovery or request fails.

## Display mapping

The existing two-value ball is retained:

- Inner water level: the lowest remaining percentage among available short-period model quotas.
- Weekly outer ring: the lowest remaining percentage among available weekly model quotas.
- If a period is absent, its visual is muted and no invented percentage is shown.
- Hover details show the Antigravity plan, every parsed model/group row, remaining percentages, reset countdowns, data source state, and refresh time.

The original local Codex token totals are removed from the Antigravity-facing details because Antigravity official quotas are not equivalent to Codex token events. The details area instead exposes the official model breakdown.

## Interaction changes

- Ball click performs an immediate official Antigravity quota refresh.
- Automatic refresh keeps the existing five-second idle requirement and ten-minute official-read cooldown.
- “Open official usage” activates the running Antigravity window or starts the installed Antigravity executable when no window is running.
- Error states remain non-disruptive: the prior successful quota snapshot remains visible and the bilingual status explains whether Antigravity is not running, the endpoint is unavailable, or the response is invalid.

## Testing and verification

Add tests for response parsing, model filtering, short/weekly aggregation, reset timestamps, malformed responses, fallback endpoint selection, process candidate filtering, and no-server behavior. Migrate the existing widget tests under the renamed application namespace. Verify with the complete test suite, Release build, and `publish.ps1`.

