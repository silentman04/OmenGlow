# OmenGlow - Architectural Changelog & Modification Record

This document records all modifications, deletions, and additions made when refactoring upstream *Ohman* (by P4R1H and the Ohman authors) into **OmenGlow**, a dedicated, standalone 4-Zone RGB Keyboard Lighting utility for HP Omen 16 (am0279tx) and compatible HP laptops.

Date of Refactoring: October 2026  
License: GNU General Public License v3 or later (GPL-3.0-or-later)

---

## 1. Scope Isolation & Safety Boundary (Why changes were made)

The primary design requirement is **absolute thermal, fan, and power safety**:
- **Zero Fan Interference**: The official HP OMEN Gaming Hub (OGH) manages fan curves, quiet/balanced/performance modes, and thermal throttling. OmenGlow never touches fan registers or fan RPM commands.
- **Zero Power Interference**: All CPU undervolting, PL1/PL2 power limits, and GPU TGP overrides have been eradicated.
- **Zero Background Watchdogs**: Heartbeat watchdogs, EC read loops, and process monitors that previously suspended or killed OMEN Gaming Hub tasks were eliminated.

---

## 2. Complete Inventory of Deletions

### A. Hardware & Thermal Engine Modules
- `src/Cpu.cs`: Removed Intel/AMD CPU voltage offset registers, PL1/PL2 power limit MSRs, and clock multiplier controls.
- `src/Ec.cs`: Removed direct Embedded Controller (EC) read/write routines that intercepted motherboard thermal registers and forced fan speeds.
- `src/Curve.cs`: Removed custom fan curve algorithms, RPM interpolation tables, and acoustic step profiles.
- `src/Sensors.cs`: Removed continuous hardware polling loops for temperature probes, fan tachometers, and wattage sensors.
- `src/Driver.cs`: Removed loader for the `pawnio.sys` third-party kernel driver.
- `third_party/`: Completely deleted the third-party kernel driver binary (`pawnio.sys`).
- `src/Platform.cs`: Removed platform detection code related to desktop models, OGH service suspension, and fan table queries.
- `src/McuKeyboard.cs`: Removed per-key MCU serial protocol code (irrelevant for 4-zone laptops).

### B. UI Panels, Tabs, and Extraneous Features
- `src/Bench.cs` & `src/BenchUi.cs`: Removed the game benchmark harness, PresentMon integration, and FPS tracking graphs.
- `src/Display.cs`: Removed display refresh rate (60Hz/144Hz/165Hz/240Hz) and display panel brightness toggles.
- `src/Hotkeys.cs`: Removed global keyboard hotkey hooks.
- `src/Support.cs`: Removed diagnostic system info dumper and debug telemetry exporters.
- `src/Update.cs`: Removed auto-updater and external GitHub network polling.
- `KillOgh()` & `SetOghTasks()`: Removed functions that previously killed or suspended the HP OMEN Gaming Hub background tasks.

### C. Unused Assets & Legacy Project Noise
- `tools/`: Excluded all loose PowerShell/CMD test scripts (`check-fans.ps1`, `fantest.ps1`, `powertest.ps1`, etc.).
- `docs/`: Excluded legacy documentation, screenshots of fan curves, and web pages.
- `fonts/`: Excluded external IBM Plex TTF font files; UI standardizes on Windows built-in system fonts (`Segoe UI` and `Consolas`).
- `brand/`: Excluded upstream brand assets.
- `.github/`: Excluded upstream GitHub issue and pull request templates.
- `app.demo.manifest`: Excluded duplicate manifest file.

---

## 3. Preserved Hardware Functionality

The proven HP BIOS WMI lighting communication pathway is preserved with exact byte fidelity:
- **WMI Method**: `root\wmi:hpqBIntM` BIOS invocation via `Bios.Call` in `src/Hardware.cs`.
- **Command Codes**: Preserved Command `0x20009` (Set Lighting) and `0x20008` (Get Lighting) with Opcode `0x2B`.
- **Packet Structure**: Exact 128-byte read-modify-write ACPI buffer structure.
- **Zone Offsets**: Preserved 3 bytes per zone starting at buffer offset 25:
  - Zone 0: Right / Numpad (Bytes 25, 26, 27)
  - Zone 1: Middle (Bytes 28, 29, 30)
  - Zone 2: Left (Bytes 31, 32, 33)
  - Zone 3: WASD (Bytes 34, 35, 36)
- **Windows Dynamic Lighting**: Preserved registry coordination (`HKCU\Software\Microsoft\Lighting`) to prevent OS-level lighting collisions.

---

## 4. Complete Inventory of Additions & New Modules

| Component | File | Description |
| :--- | :--- | :--- |
| **Project Definition** | `OmenGlow.csproj` | Clean .NET 6.0-windows SDK configuration with WPF, Windows Forms (for tray icon), and `System.Management`. Targets the machine's installed desktop runtime. |
| **Application Manifest** | `app.manifest` | Configured with `asInvoker` execution level and Per-Monitor V2 DPI awareness. Allows instant launch without forced UAC prompts, while seamlessly obtaining WMI write permissions when run as Administrator. |
| **Build Script** | `build.cmd` | 1-click batch build script with automatic .NET SDK path detection. Outputs release binaries into `bin\Release\`. |
| **Animation Engine** | `src/Engine.cs` | Standalone, lightweight 120ms tick animation loop. Implements Static, Breathing, Color Wave, Rainbow Cycle, and Off modes. Persists settings to `omenglow.state`. |
| **Vector Visualizer** | `src/Keyboard.cs` | Custom WPF visual element (`KeyboardView`) drawing a true-to-scale vector keyboard with real-time zone backlighting and smooth cubic color transitions. |
| **Custom Controls** | `src/Controls.cs` | Dual-strip Hue & Shade gradient color picker (`StripPicker`) and segmented option buttons (`Seg`) designed specifically for dark desktop themes. |
| **Theme & Brushes** | `src/Theme.cs` | High-contrast OMEN dark palette, accent colors, easing curves, and XAML value converters. |
| **Dedicated UI** | `src/Ui.xaml` & `src/Ui.cs` | Compact, single-window WPF interface with 5-zone selector, 5 effect modes, quick preset palette, brightness (0–100%) and animation speed (1–5) sliders. |
| **Tray Coordinator** | `src/Program.cs` | Application entry point with single-instance named Mutex (`OmenGlow_Mutex`), command-line parser (`--demo`, `--hidden`, `--test`), and Windows System Tray integration with context menu. |
| **Product Metadata** | `src/Meta.cs` | Clean assembly metadata (Product: `OmenGlow`, Version `2.0.0`). |
| **Housekeeping** | `.gitignore`, `LICENSE`, `README.md` | Clean Git ignore rules, GPL-3.0 attribution license, and comprehensive user guide. |

---

## 5. Quality of Life Additions (v2.1.0)

- **Factory Reset (`Reset to Defaults`)**:
  - Added dedicated `↺ Reset to Defaults` button directly in the header bar.
  - Instantly resets all 4 zones to the signature vibrant palette (WASD `#FF0055`, Left `#9900FF`, Middle `#00EEFF`, Right `#0099FF`), sets Mode to Static, Speed to 3, and Brightness to 100%.
  - Persists the reset state directly to `omenglow.state` and sends immediate updates to both hardware and screen visualizer.
- **Dual Theme Engine (Dark & Light Modes)**:
  - Added a one-click `☀ Light` / `🌙 Dark` theme toggle button in the header bar.
  - Implemented dynamic resource brushes for window background, cards, sunken panels, dividers, and typography.
  - `KeyboardView`, `Seg`, and `StripPicker` automatically adapt key caps, legends, selection rings, and hover outlines to guarantee crisp readability in both lighting conditions.
  - Remembers the user's theme preference across application restarts.

---

## 6. Optimization & Telemetry Additions (v2.2.0)

- **Native Working Set Memory Trimming (`0.9 MB RAM`)**:
  - Implemented unmanaged `psapi.dll!EmptyWorkingSet` invocation in `src/Ui.cs` (`TrimWorkingSet()`) combined with full garbage collection sweeps (`GC.Collect(2)`).
  - Automatically invoked whenever the window is hidden, minimized to the system tray, or after layout updates.
  - Reduces the application's resident physical memory footprint from standard WPF baselines (~70–218 MB) down to **0.9 MB RAM** (confirmed via live Task Manager telemetry).
- **Live Battery & Power Telemetry**:
  - Implemented `GetPowerStats()` in `src/Program.cs` querying the ACPI battery class `root\WMI:BatteryStatus`.
  - Calculates real-time instantaneous battery discharge rate in **Watts (W)** (`DischargeRate / 1000.0`) and remaining capacity in **Watt-hours (Wh)** (`RemainingCapacity / 1000.0`).
  - System tray context menu displays live, updating power telemetry:  
    `⚡ 0.9 MB RAM • iGPU • Battery (35.4W) • 59.2Wh left`
- **dGPU Sleep Lock (`GpuPreference=1`)**:
  - Enforces low-power iGPU execution by registering `GpuPreference=1` under `HKCU\Software\Microsoft\DirectX\UserGpuPreferences` in `src/Program.cs`.
  - Guarantees that Windows and the NVIDIA graphics driver never wake the discrete GPU from its zero-watt `D3Cold` sleep state just to render keyboard lighting.
- **Zero-Allocation Animation Buffers**:
  - Pre-allocated `fxFrameBuffer` (12 bytes) and `fxScaledBuffer` (12 bytes) in `src/Engine.cs`.
  - Completely eliminates heap allocation and garbage collection churn during the continuous 120ms animation tick loop.
- **Adaptive Battery Throttling**:
  - Dynamic tick rate shifting: 120ms tick rate when plugged into AC power; automatically throttles to 200ms when running on DC battery power.
  - Phase advancement normalization preserves visual animation speed while reducing WMI ACPI wakeups by 40%.
- **Custom High-DPI Cyberpunk Icon**:
  - Created a unique combined neon geometric owl mascot and hexagon "V" emblem icon (`app.ico` and `app.png`).
  - Built multi-resolution icon payload (256x256, 128x128, 64x64, 48x48, 32x32, 16x16) integrated into project metadata, window headers, and taskbar tray icon.

---

## 7. Verification & Testing History

1. **Clean Compilation**: Verified via `dotnet build -c Release` with **0 Warnings** and **0 Errors**.
2. **Self-Test Validation**: Verified via `OmenGlow.exe --test` exiting with code `0`.
3. **Demo Preview Execution**: Verified via `OmenGlow.exe --demo` showing full interactive keyboard rendering, real-time animation math, color picker reactivity, and live theme switching without requiring hardware WMI access.
4. **Administrator Execution**: WMI BIOS communication verified to write to `root\wmi:hpqBIntM` without disturbing any HP OMEN Gaming Hub background services.
5. **Memory Footprint Validation**: Verified live on Windows 11 taskbar tray showing working set of **0.9 MB RAM**.
6. **Power Telemetry Validation**: Verified live reading of battery discharge wattage (e.g. 35.4W) and capacity (e.g. 59.2Wh) directly from `root\WMI:BatteryStatus`.
