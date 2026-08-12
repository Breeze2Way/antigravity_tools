# Cyber Liquid Ball Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade the 68×68 Codex usage widget into a neon liquid energy ball with responsive glow, alert effects, bubbles, and a holographic hover card while preserving all current data and interaction behavior.

**Architecture:** Keep usage reading, caching, refresh timing, dragging, and menus unchanged. Add a pure `WaterBallEffects` service for testable visual-state calculations, then let `WaterBallControl` consume that state during its existing WPF `DrawingContext` render loop. Style the existing WPF `ToolTip` in XAML so the current red reset line and tooltip content remain intact while gaining the holographic appearance.

**Tech Stack:** .NET 9, WPF, `DrawingContext`, `SolidColorBrush`, `RadialGradientBrush`, `DropShadowEffect`, xUnit, existing `WaterBallDisplay` and `WaterWaveDisplay` services.

## Global Constraints

- Keep the widget window at 68×68 and preserve the current transparent, topmost, draggable window behavior.
- Keep the existing official percentage source, cached reset date, local 7-day/30-day file watcher, refresh interval, user-activity pause, and right-click menu behavior unchanged.
- Use the existing green → blue → yellow → red remaining-percentage gradient for water, shell tint, ring, glow, and tooltip accent.
- Use gray-blue visuals when the official percentage is unavailable; do not invent a colored percentage state.
- Animation may only redraw the widget; it must never trigger an official usage read or interrupt keyboard/mouse input.
- Do not introduce a graphics runtime, external service, image asset, or new NuGet dependency.
- Every implementation change must be covered by tests where the behavior is pure; finish with the full test suite, Release publish, one GitHub commit, one version tag, and one matching GitHub Release with the Windows zip.

---

### Task 1: Add the pure visual-effect state model

**Files:**
- Create: `src/CodexUsageWidget/Services/WaterBallEffects.cs`
- Create: `tests/CodexUsageWidget.Tests/WaterBallEffectsTests.cs`

**Interfaces:**
- Consumes: `WaterBallDisplay.GetColor(double?)`, `WaterWaveDisplay.GetAmplitude(double, double)`, and the current `WaterBallControl` inputs `remainingPercent`, `tokensPerMinute`, and hover state.
- Produces: `WaterBallEffects.GetGlowOpacity(double?, double, bool)`, `WaterBallEffects.GetAlertPulse(double?, double)`, `WaterBallEffects.GetBubbleVisibility(double?, double, int, double)`, and `WaterBallEffects.GetShellOpacity(double?, bool)`.

- [ ] **Step 1: Write the failing tests**

```csharp
namespace CodexUsageWidget.Tests;

public sealed class WaterBallEffectsTests
{
    [Fact]
    public void FasterUsageAndHoverIncreaseGlow()
    {
        var calm = WaterBallEffects.GetGlowOpacity(60, 0, false);
        var fast = WaterBallEffects.GetGlowOpacity(60, 220_000, false);
        var hovered = WaterBallEffects.GetGlowOpacity(60, 0, true);

        Assert.True(fast > calm);
        Assert.True(hovered > calm);
        Assert.InRange(fast, 0, 1);
        Assert.InRange(hovered, 0, 1);
    }

    [Fact]
    public void LowRemainingProducesAlertPulseOnlyBelowTwentyPercent()
    {
        Assert.Equal(0, WaterBallEffects.GetAlertPulse(25, 0.5), precision: 6);
        Assert.NotEqual(0, WaterBallEffects.GetAlertPulse(10, 0.5));
    }

    [Fact]
    public void BubbleVisibilityUsesUsageAndRemainingState()
    {
        var calm = WaterBallEffects.GetBubbleVisibility(80, 0, 2, 0.4);
        var fast = WaterBallEffects.GetBubbleVisibility(80, 220_000, 2, 0.4);
        var empty = WaterBallEffects.GetBubbleVisibility(null, 220_000, 2, 0.4);

        Assert.True(fast > calm);
        Assert.Equal(0, empty, precision: 6);
        Assert.InRange(fast, 0, 1);
    }
}
```

- [ ] **Step 2: Run the focused test and verify the expected missing-type failure**

Run:

```powershell
dotnet test tests\CodexUsageWidget.Tests\CodexUsageWidget.Tests.csproj --filter FullyQualifiedName~WaterBallEffectsTests --no-restore
```

Expected: compilation fails because `WaterBallEffects` does not exist yet.

- [ ] **Step 3: Implement the minimal pure effect calculations**

Create a static service with clamped outputs:

```csharp
namespace CodexUsageWidget.Services;

public static class WaterBallEffects
{
    public static double GetGlowOpacity(double? remainingPercent, double tokensPerMinute, bool isHovered)
    {
        if (!remainingPercent.HasValue || !double.IsFinite(remainingPercent.Value))
            return isHovered ? 0.18 : 0.10;

        var usageBoost = Math.Clamp(tokensPerMinute / 250_000d, 0, 1) * 0.18;
        var hoverBoost = isHovered ? 0.12 : 0;
        var alertBoost = remainingPercent.Value <= 20 ? 0.08 : 0;
        return Math.Clamp(0.10 + usageBoost + hoverBoost + alertBoost, 0, 0.45);
    }

    public static double GetAlertPulse(double? remainingPercent, double phase)
    {
        if (!remainingPercent.HasValue || !double.IsFinite(remainingPercent.Value) || remainingPercent.Value > 20)
            return 0;

        return 0.5 + 0.5 * Math.Sin(phase * Math.PI * 2);
    }

    public static double GetAlertRingThickness(double? remainingPercent, double phase)
    {
        return 1.5 + GetAlertPulse(remainingPercent, phase) * 1.5;
    }

    public static double GetBubbleVisibility(double? remainingPercent, double tokensPerMinute, int bubbleIndex, double phase)
    {
        if (!remainingPercent.HasValue || !double.IsFinite(remainingPercent.Value) || bubbleIndex < 0)
            return 0;

        var usageFactor = Math.Clamp(tokensPerMinute / 220_000d, 0, 1);
        var bubblePhase = phase * (0.35 + usageFactor * 0.65) + bubbleIndex * 1.7;
        var motion = 0.5 + 0.5 * Math.Sin(bubblePhase);
        return Math.Clamp((0.12 + usageFactor * 0.70) * motion, 0, 1);
    }

    public static double GetShellOpacity(double? remainingPercent, bool isHovered)
    {
        var baseOpacity = remainingPercent.HasValue && double.IsFinite(remainingPercent.Value) ? 0.22 : 0.12;
        return Math.Clamp(baseOpacity + (isHovered ? 0.08 : 0), 0, 1);
    }
}
```

- [ ] **Step 4: Run the focused tests and then the full suite**

Run:

```powershell
dotnet test tests\CodexUsageWidget.Tests\CodexUsageWidget.Tests.csproj --filter FullyQualifiedName~WaterBallEffectsTests --no-restore
dotnet test tests\CodexUsageWidget.Tests\CodexUsageWidget.Tests.csproj --no-restore
```

Expected: the focused tests and all existing tests pass.

### Task 2: Add neon shell, glow, ring, reflection, and responsive alert rendering

**Files:**
- Modify: `src/CodexUsageWidget/Controls/WaterBallControl.cs:12-280`
- Modify: `tests/CodexUsageWidget.Tests/WaterBallEffectsTests.cs`

**Interfaces:**
- Consumes: `WaterBallEffects` methods from Task 1, existing `WaterBallDisplay.GetColor`, `GetBackgroundColor`, `GetFillRatio`, and `WaterWaveDisplay` methods.
- Produces: a layered ball renderer with a hover-sensitive visual state and low-percentage alert pulse; public data and interaction APIs remain unchanged.

- [ ] **Step 1: Add a failing test for the alert-ring thickness calculation**

```csharp
[Fact]
public void LowRemainingAddsAlertRingThickness()
{
    var normal = WaterBallEffects.GetAlertRingThickness(60, 0.5);
    var alert = WaterBallEffects.GetAlertRingThickness(10, 0.5);

    Assert.Equal(1.5, normal, precision: 6);
    Assert.True(alert > normal);
}
```

- [ ] **Step 2: Run the test and verify the boundary failure**

Run:

```powershell
dotnet test tests\CodexUsageWidget.Tests\CodexUsageWidget.Tests.csproj --filter FullyQualifiedName~LowRemainingAddsAlertRingThickness --no-restore
```

Expected: compilation fails because `GetAlertRingThickness` does not exist yet.

- [ ] **Step 3: Add hover tracking and animation time without changing refresh behavior**

In `WaterBallControl`:

```csharp
private double animationTime;
private bool isHovered;

public WaterBallControl()
{
    Loaded += Control_Loaded;
    Unloaded += Control_Unloaded;
    MouseEnter += (_, _) => { isHovered = true; InvalidateVisual(); };
    MouseLeave += (_, _) => { isHovered = false; InvalidateVisual(); };
}
```

Increment `animationTime` in `CompositionTarget_Rendering` using the existing elapsed-time calculation. Keep `TokensPerMinute` as the only speed input and do not call any refresh service from the renderer.

- [ ] **Step 4: Draw the layers in the existing `OnRender` method**

Use the current color and radius to draw in this order:

```csharp
var glowOpacity = WaterBallEffects.GetGlowOpacity(remainingPercent, tokensPerMinute, isHovered);
var alertPulse = WaterBallEffects.GetAlertPulse(remainingPercent, animationTime * 0.35);
var glowColor = ToMediaColor(color, glowOpacity + alertPulse * 0.08);

drawingContext.DrawEllipse(
    new SolidColorBrush(glowColor),
    null,
    center,
    radius + 2.5,
    radius + 2.5);

    drawingContext.DrawEllipse(
        new SolidColorBrush(ToMediaColor(
            WaterBallDisplay.GetBackgroundColor(remainingPercent),
            WaterBallEffects.GetShellOpacity(remainingPercent, isHovered))),
    new MediaPen(Lighten(color, 0.55), WaterBallEffects.GetAlertRingThickness(remainingPercent, animationTime * 0.35)),
    center,
    radius,
    radius);
```

Add the alpha-aware color helper beside the existing RGB conversion:

```csharp
private static MediaColor ToMediaColor(WaterBallColor color, double opacity)
{
    return MediaColor.FromArgb(
        (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255, MidpointRounding.AwayFromZero),
        color.Red,
        color.Green,
        color.Blue);
}
```

Then retain the existing clipped water and wave rendering, add a translucent highlight ellipse near the upper-left shell, and draw a second thin ring after the center text. Keep all new brushes local to `OnRender` so their state cannot leak between frames.

- [ ] **Step 5: Run the full test suite and Release build**

Run:

```powershell
dotnet test tests\CodexUsageWidget.Tests\CodexUsageWidget.Tests.csproj --no-restore
dotnet build src\CodexUsageWidget\CodexUsageWidget.csproj -c Release --no-restore
```

Expected: all tests pass and the Release build exits with code 0.

### Task 3: Add water highlights and usage-responsive bubbles

**Files:**
- Modify: `src/CodexUsageWidget/Controls/WaterBallControl.cs:95-145`
- Modify: `tests/CodexUsageWidget.Tests/WaterBallEffectsTests.cs`

**Interfaces:**
- Consumes: `WaterBallEffects.GetBubbleVisibility`, current fill ratio, current wave phase, and the existing clip geometry.
- Produces: subtle bubbles and a second transparent water highlight layer that become more visible as recent token usage increases.

- [ ] **Step 1: Add deterministic bubble-position data**

Add a fixed array near the control fields so the bubbles do not randomly jump between frames:

```csharp
private static readonly (double X, double Y, double Size)[] BubbleLayout =
[
    (-0.28, 0.18, 1.2),
    (0.24, 0.28, 1.8),
    (-0.10, 0.42, 1.0),
    (0.35, 0.55, 1.3),
    (-0.34, 0.64, 1.5)
];
```

- [ ] **Step 2: Draw bubbles inside the existing water clip**

For each layout entry, compute the bubble center relative to the ball center, call `GetBubbleVisibility`, and draw a small ellipse with a white brush whose opacity is the returned visibility multiplied by `0.55`. Do not draw bubbles when `fillRatio` is null or zero. Use the same `PushClip(bucketGeometry)` scope as the water body.

- [ ] **Step 3: Add a thin moving waterline highlight**

Draw a non-filled wave geometry using a lighter color and a `MediaPen` width between `0.7` and `1.3`, with `amplitudeScale: 0.30`. Keep the current main water wave unchanged so the new effect is additive and easy to disable.

- [ ] **Step 4: Run tests and verify visual build output**

Run:

```powershell
dotnet test tests\CodexUsageWidget.Tests\CodexUsageWidget.Tests.csproj --no-restore
dotnet build src\CodexUsageWidget\CodexUsageWidget.csproj -c Release --no-restore
```

Expected: all tests pass and the ball remains a 68×68 transparent window.

### Task 4: Style the hover tooltip as a holographic card

**Files:**
- Modify: `src/CodexUsageWidget/MainWindow.xaml:1-45`
- Modify: `src/CodexUsageWidget/MainWindow.xaml.cs:60-66`

**Interfaces:**
- Consumes: the existing `ballDetailsText`, `hostDetailsText`, red reset `Run`, and WPF tooltip service delays.
- Produces: a dark translucent rounded tooltip with border glow, drop shadow, padding, wrapping, and an accent color that can follow the current ball color without changing tooltip content.

- [ ] **Step 1: Add a WPF `ToolTip` style in `Window.Resources`**

Use a `ControlTemplate` with a `Border` and `ContentPresenter`:

```xml
<Window.Resources>
    <Style TargetType="ToolTip">
        <Setter Property="Background" Value="#E60B1728" />
        <Setter Property="Foreground" Value="#F5FAFF" />
        <Setter Property="BorderBrush" Value="#8059C7FF" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="Padding" Value="10,8" />
        <Setter Property="HasDropShadow" Value="True" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ToolTip">
                    <Border Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}"
                            CornerRadius="10"
                            Padding="{TemplateBinding Padding}">
                        <Border.Effect>
                            <DropShadowEffect BlurRadius="18" ShadowDepth="0" Opacity="0.65" Color="#3AB8FF" />
                        </Border.Effect>
                        <ContentPresenter />
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</Window.Resources>
```

- [ ] **Step 2: Set tooltip placement and width constraints**

In the constructor, keep `ToolTipService.SetInitialShowDelay(waterBall, 150)` and set both tooltip text blocks to `MaxWidth = 280`, `Margin = new Thickness(0)`, and `TextWrapping = TextWrapping.Wrap`. Set `ToolTipService.Placement` to `PlacementMode.Mouse` only if the default placement causes the card to cover the ball; otherwise retain the current placement so edge positioning remains safe.

- [ ] **Step 3: Verify tooltip content and existing interactions**

Manually check:

1. Hovering shows the weekly percentage, 7-day total, 30-day total, update time, and red reset line.
2. The status line remains absent.
3. Left-drag still moves the ball.
4. Right-click still opens the menu.
5. Moving the pointer away hides the card without changing the refresh interval.

### Task 5: Final visual verification, packaging, and release

**Files:**
- Modify: `README.md` with the new visual feature description and screenshot-free usage notes.
- Create: `D:\VS\CodexUsageWidget-v0.2.0-win-x64.zip`

**Interfaces:**
- Consumes: all completed visual layers and the existing release script.
- Produces: a verified packaged release with one implementation commit and one matching GitHub Release.

- [ ] **Step 1: Run the complete verification commands**

```powershell
dotnet test tests\CodexUsageWidget.Tests\CodexUsageWidget.Tests.csproj --no-restore
dotnet build src\CodexUsageWidget\CodexUsageWidget.csproj -c Release --no-restore
.\publish.ps1
```

Expected: all tests pass, Release build succeeds, and `D:\VS\codex_tools\publish\CodexUsageWidget.exe` is produced.

- [ ] **Step 2: Stop only the running widget executable and create the package**

Confirm the target process path is exactly `D:\VS\codex_tools\publish\CodexUsageWidget.exe`, stop that process if present, then run:

```powershell
Compress-Archive -Path .\publish\* -DestinationPath D:\VS\CodexUsageWidget-v0.2.0-win-x64.zip -Force
```

- [ ] **Step 3: Review the diff and create the single implementation commit**

```powershell
git diff --check
git status --short
git add src tests README.md
git diff --cached --check
git commit -m "feat: add neon liquid ball visual effects"
git tag -a v0.2.0 -m "v0.2.0"
git push origin main
git push origin v0.2.0
```

- [ ] **Step 4: Create the matching GitHub Release and upload the zip**

Create the GitHub Release for tag `v0.2.0` with release notes describing the neon shell, responsive glow, bubbles, alert ring, and holographic tooltip, then upload `D:\VS\CodexUsageWidget-v0.2.0-win-x64.zip`.

- [ ] **Step 5: Start the published program and verify final state**

```powershell
Start-Process D:\VS\codex_tools\publish\CodexUsageWidget.exe
Start-Sleep -Seconds 2
git status --short --branch
```

Confirm the process is alive, the working tree is clean, `origin/main` contains the implementation commit, the `v0.2.0` tag exists remotely, and the Release has exactly one uploaded Windows zip.
