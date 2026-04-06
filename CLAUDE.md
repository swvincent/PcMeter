# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

PcMeter is a Windows system tray application that reads CPU and memory utilization and sends the data over serial to an Arduino-based physical meter device (analog gauges with LEDs). There is also Arduino firmware for the device itself.

## Build

Open and build using Visual Studio:

```
PcMeterSln\PcMeterSln.sln
```

Or build from the command line with MSBuild:

```
msbuild PcMeterSln\PcMeterSln.sln /p:Configuration=Debug /p:Platform="Mixed Platforms"
```

- Target framework: .NET Framework 4.8
- PcMeter project builds as x86 (`Debug|x86` / `Release|x86`)
- MutexManager project builds as Any CPU

There are no automated tests in this project.

## Architecture

The solution has two C# projects and one Arduino sketch:

**MutexManager** — a helper library that enforces single-instance behavior using a named mutex (GUID-based). `SingleInstance.Start()` / `Stop()` are called from `Program.cs`.

**PcMeter** — the main WinForms tray application. Entry point is `Program.cs`, which uses `CustomApplicationContext` (derives from `ApplicationContext`) instead of a main form. Key responsibilities in `CustomApplicationContext`:
- **System tray** — `NotifyIcon` with a context menu showing live CPU% and Memory% labels, connect/disconnect toggle, settings, about, and exit.
- **Performance counters** — CPU% via `PerformanceCounter("Processor", "% Processor Time", "_Total")`. Memory% via P/Invoke to `psapi.dll!GetPerformanceInfo` (wrapped in `PsApiWrapper` / `PerfomanceInfoData` in `PsApiPerformanceInfo.cs`), calculated as `100 - (availableBytes / totalBytes * 100)`.
- **Serial communication** — sends data to the meter device every 500ms via `SerialPort`. Format: `C{cpu%}\rM{mem%}\r` (carriage-return delimited, prefixed with `C` for CPU or `M` for memory). COM port is stored in user settings (`Settings.Default.MeterComPort`).
- **Settings** — `SettingsForm` lets the user pick a COM port from available ports; saved via `Properties.Settings`.

**Arduino firmware** (`arduino/pcmeter2/pcmeter2.ino`) — runs on an Arduino Leonardo at 9600 baud. Parses the `C{n}\r` / `M{n}\r` serial protocol, smooths values using a rolling average of 20 readings, drives two analog meters via PWM (`analogWrite`), and controls green/red LEDs (red zone at ≥80%). Includes a screensaver mode that sweeps needles back and forth when no serial data is received for 2 seconds.

## Serial Protocol

The Windows app sends to the Arduino:
- `C{0-100}\r` — CPU percentage
- `M{0-100}\r` — memory percentage

Messages are sent together every 500ms: `C{cpu}\rM{mem}\r`

## User Settings

The only persisted setting is `MeterComPort` (the selected COM port name). Settings are saved using the standard .NET `Properties.Settings` mechanism (stored in the user's AppData).
