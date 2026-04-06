# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

PcMeter is a Windows system tray application that reads CPU and memory utilization and sends the data over serial to an Arduino-based physical meter device (analog gauges with LEDs). There is also Arduino firmware for the device itself.

## Projects

There are two Windows tray applications in this repo. The modern one (`PcMeter/`) supersedes the legacy one (`PcMeterSln/`).

### PcMeter — .NET 8 WPF (current)

```
dotnet build PcMeter/PcMeter.csproj
dotnet run --project PcMeter/PcMeter.csproj
```

### PcMeterSln — .NET Framework 4.8 WinForms (legacy)

```
msbuild PcMeterSln\PcMeterSln.sln /p:Configuration=Debug /p:Platform="Mixed Platforms"
```

There are no automated tests in this project.

## Architecture — PcMeter (.NET 8 WPF)

Entry point is `App.xaml.cs` (`App : Application`). There is no main window; the app is tray-only with `ShutdownMode = OnExplicitShutdown`.

**App.xaml.cs** — central orchestrator. Owns the single-instance `Mutex`, creates the three services, retrieves the `TaskbarIcon` and named `MenuItem` references from XAML resources, and runs a 500ms `DispatcherTimer` that reads metrics and sends serial data each tick. All menu click handlers route through public `OnXxxMenuClick()` methods here.

**Services/**
- `AppSettings` — POCO with `ComPort` property. `Load()` / `Save()` read and write `%APPDATA%\PcMeter\settings.json` via `System.Text.Json`. Default port is `COM20`.
- `MetricsService` — CPU% via `PerformanceCounter("Processor", "% Processor Time", "_Total")`; Memory% via P/Invoke to `psapi.dll!GetPerformanceInfo` (inner `PsApiWrapper` class), formula `100 - (available / total * 100)`. Implements `IDisposable`.
- `SerialService` — wraps `SerialPort` at 9600 baud. `Connect()` / `Disconnect()` / `TrySend(cpu, mem)`. Fires `ErrorOccurred(message, isSleepResumeError)` event, marshaled to the UI thread via the captured `Dispatcher`.

**TrayIcon/TrayContextMenu.xaml** — `ResourceDictionary` with `x:Class` code-behind. Declares the `TaskbarIcon` (key `"TrayIcon"`) with a WPF `ContextMenu` containing named items (`CpuMenuItem`, `MemMenuItem`, `ConnectMenuItem`, `SettingsMenuItem`). Click handlers in the code-behind delegate to `App`.

**Views/SettingsWindow** — lists available COM ports in a `ComboBox`, pre-selects the saved port, and calls `_settings.Save()` on OK.

**Views/AboutWindow** — shows app info, MIT license text, and a clickable URL opened via `Process.Start` with `UseShellExecute = true`.

NuGet packages: `H.NotifyIcon.Wpf` (tray icon), `System.IO.Ports` (serial), `System.Diagnostics.PerformanceCounter` (CPU stats).

## Architecture — PcMeterSln (.NET Framework 4.8 WinForms, legacy)

Two projects: **MutexManager** (single-instance helper library) and **PcMeter** (WinForms tray app). Entry point is `Program.cs`; `CustomApplicationContext : ApplicationContext` drives the app. Same serial protocol and metrics approach as the modern app.

## Serial Protocol

The Windows app sends to the Arduino:
- `C{0-100}\r` — CPU percentage
- `M{0-100}\r` — memory percentage

Messages are sent together every 500ms: `C{cpu}\rM{mem}\r`

**Arduino firmware** (`arduino/pcmeter2/pcmeter2.ino`) — runs on an Arduino Leonardo at 9600 baud. Smooths values using a rolling average of 20 readings, drives two analog meters via PWM, and controls green/red LEDs (red zone ≥80%). Includes a screensaver mode when no serial data is received for 2 seconds.
