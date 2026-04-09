# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

PcMeter is a Windows system tray application that reads CPU and memory utilization and sends the data over serial to an Arduino-based physical meter device (analog gauges with LEDs). There is also Arduino firmware for the device itself.

## Projects

There are two Windows tray applications in this repo. The modern one (`PcMeter/`) supersedes the legacy one (`PcMeter (legacy)/`).

### PcMeter — .NET 8 WPF (current)

```
dotnet build PcMeter/PcMeter.csproj
dotnet run --project PcMeter/PcMeter.csproj
```

## Architecture — PcMeter (.NET 8 WPF)

Entry point is `App.xaml.cs` (`App : Application`). There is no main window; the app is tray-only with `ShutdownMode = OnExplicitShutdown`.

**App.xaml.cs** — central orchestrator. Owns the single-instance `Mutex`, creates the three services, and builds the `TaskbarIcon` and `ContextMenu` entirely in code via `CreateTrayIcon()`. Runs a 500ms `DispatcherTimer` that reads metrics and sends serial data each tick. All menu click handlers are private `OnXxxMenuClick()` methods here. `App.xaml` is minimal — no resources or XAML-defined UI.

**Services/**
- `AppSettings` — POCO with `ComPort` property. `Load()` / `Save()` read and write `%APPDATA%\PcMeter\settings.json` via `System.Text.Json`. Default port is `COM20`.
- `MetricsService` — CPU% via `PerformanceCounter("Processor", "% Processor Time", "_Total")`; Memory% via P/Invoke through `PsApiWrapper`. Formula: `100 - (available / total * 100)`. Implements `IDisposable`.
- `SerialService` — wraps `SerialPort` at 9600 baud. `Connect()` / `Disconnect()` / `TrySend(cpu, mem)`. Fires `ErrorOccurred(message)` for unexpected errors and `ConnectionLost` for silent mid-session drops (unplug, sleep/resume); both events are marshaled to the UI thread via the captured `Dispatcher`. The timer in `App.xaml.cs` auto-reconnects each tick when disconnected, and polls `SerialPort.GetPortNames()` after each send to catch silent unplugs (USB drivers buffer writes, so no exception is thrown on device removal).
- `PsApiWrapper` — static helper; P/Invokes `psapi.dll!GetPerformanceInfo` and returns `(available, total)` page counts.

**Views/SettingsWindow** — lists available COM ports in a `ComboBox`, pre-selects the saved port, and calls `_settings.Save()` on OK.

**Views/AboutWindow** — shows app info, MIT license text, and a clickable URL opened via `Process.Start` with `UseShellExecute = true`.

NuGet packages: `H.NotifyIcon.Wpf` (tray icon), `System.IO.Ports` (serial), `System.Diagnostics.PerformanceCounter` (CPU stats).

## Serial Protocol

The Windows app sends to the Arduino:
- `C{0-100}\r` — CPU percentage
- `M{0-100}\r` — memory percentage

Messages are sent together every 500ms: `C{cpu}\rM{mem}\r`

**Arduino firmware** (`arduino/pcmeter.ino`) — runs on an Arduino Leonardo at 9600 baud. Smooths values using a rolling average of 20 readings, drives two analog meters via PWM, and controls green/red LEDs (red zone ≥80%). Includes a screensaver mode when no serial data is received for 2 seconds.
