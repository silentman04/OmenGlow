<div align="center">

<img src="app.png" alt="OmenGlow Logo" width="140" height="140" style="border-radius: 24px;" />

# OmenGlow
### Ultra-Lightweight 4-Zone RGB Lighting Utility for HP OMEN 16 (`am0279tx`)

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Platform: Windows 10 | 11](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg?logo=windows&logoColor=white)](https://microsoft.com/windows)
[![Runtime: .NET 6.0 | 8.0](https://img.shields.io/badge/.NET-6.0%20%7C%208.0-512BD4.svg?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![RAM Footprint](https://img.shields.io/badge/RAM%20Footprint-0.9%20MB-brightgreen.svg)](#efficiency--resource-footprint)
[![Thermal Safety](https://img.shields.io/badge/Thermal%20Safety-100%25%20Isolated-success.svg)](#scope-isolation--thermal-safety)
[![dGPU Sleep Safe](https://img.shields.io/badge/dGPU-0--Watt%20Sleep%20Locked-green.svg)](#efficiency--resource-footprint)

*A clean, zero-bloat, standalone keyboard lighting manager created specifically for personal use on the HP OMEN 16 (`am0279tx`). Controls 4-zone RGB backlighting with zero fan interference, 0.9 MB RAM footprint, live battery wattage telemetry, and safe coexistence alongside official HP software.*

[Key Features](#key-features) • [Personal Use & Disclaimer](#-personal-project--hardware-liability-disclaimer) • [Efficiency](#efficiency--resource-footprint) • [Installation](#installation--quick-start) • [Attribution](#upstream-attribution--lineage) • [Brand & Logo Rights](#brand--logo-assets-notice)

</div>

---

## ⚠️ Personal Project & Hardware Liability Disclaimer

> [!CAUTION]
> **READ BEFORE USING:**
> 1. **Personal Project**: This software was designed and engineered by the author **strictly for personal use** on their specific laptop model: **HP OMEN 16-am0279tx**.
> 2. **No Warranty**: This software is provided *"as is"*, in the hope that it may be useful or interesting for reference, but **WITHOUT ANY WARRANTY OF ANY KIND**, express or implied, including but not limited to fitness for a particular purpose or non-infringement.
> 3. **Zero Liability**: The author assumes **zero liability and zero responsibility** for any hardware damage, motherboard issues, LED behavior, firmware glitches, loss of data, system instability, or voided manufacturer warranties that may occur from downloading, compiling, or executing this code on your machine.
> 4. **Hardware Specificity**: Different laptop revisions and motherboard batches can vary. **If you run this application on your laptop, you do so entirely at your own risk.**

---

## Overview

Most gaming laptop software suites are bloated: background analytics services, telemetry daemons, multi-hundred-megabyte RAM footprints, and intrusive fan overrides that can conflict with factory thermal safeguards.

**OmenGlow** was built with a single, clear architectural objective: **Pure Lighting Control with Zero Thermal Interference**.

It communicates directly with the HP WMI BIOS interface (`root\wmi:hpqBIntM`) to command the 4-zone RGB keyboard hardware. It completely omits all fan, voltage, undervolting, and power-limit code. You can run your laptop in complete peace—letting HP's official cooling profiles or Windows power sliders handle thermals while OmenGlow provides instantaneous, beautiful lighting control.

---

## Key Features

- **4-Zone RGB Backlight Control**:
  - **Zone 1**: WASD Cluster
  - **Zone 2**: Left Keyboard Area
  - **Zone 3**: Middle Keyboard Area
  - **Zone 4**: Right Keyboard Area & Numpad
  - **Sync Mode**: 1-click synchronous color application across all zones
- **Dynamic Lighting Effects**:
  - **Static**: Custom solid colors per zone
  - **Breathing**: Smooth pulsing animation with configurable speeds
  - **Color Wave**: Dynamic traveling wave propagating across the keyboard zones
  - **Spectrum Cycle**: Smooth 360° chromatic rainbow rotation
  - **Off**: 1-click complete backlight blackout
- **True-to-Scale Vector Visualizer**:
  - Real-time vector-drawn keyboard mirroring your physical keys, active colors, and animation transitions directly on screen.
- **Precision Color Picker**:
  - Quick-access preset palette + dual-strip continuous Hue and Shade sliders + manual Hex code input (`#RRGGBB`).
- **1-Click Factory Reset (`↺ Reset to Defaults`)**:
  - Instantly restores the signature landing palette (WASD `#FF0055`, Left `#9900FF`, Middle `#00EEFF`, Right `#0099FF`), Static mode, and 100% brightness.
- **Dual Theme Engine**:
  - 1-click toggle between crisp **Light Mode** and deep **Dark (OMEN) Mode** with full vector contrast adaptation.
- **System Tray Integration with Live Power Telemetry**:
  - Minimizes silently to the Windows taskbar notification area.
  - Context menu displays real-time battery discharge rates in **Watts (W)**, remaining capacity in **Watt-hours (Wh)**, and exact memory usage.
- **Persistent State**:
  - Automatically saves and restores your colors, modes, brightness, animation speed, and theme across reboots in `omenglow.state`.

---

## Why OmenGlow?

| Feature | HP OMEN Gaming Hub | Upstream Ohman | **OmenGlow** |
| :--- | :---: | :---: | :---: |
| **RAM Consumption** | ~350 MB – 800 MB | ~50 MB – 100 MB | **~0.9 MB** *(Active Trim)* |
| **Background Processes** | 6–10 background services | 1 process | **1 standalone process** |
| **Fan & Thermal Alteration** | Factory default curves | Overrides fans & curves | **0% interference (Untouched)** |
| **CPU / GPU Power Modding** | Overclocking & TGP limits | Undervolt / MSR / PL limits | **None (100% Thermal Safe)** |
| **Third-Party Kernel Drivers** | Proprietary driver stack | Requires `pawnio.sys` | **None (Pure Windows WMI)** |
| **dGPU Sleep Protection** | Frequent wakeups | Varies | **Locked to iGPU (0W D3Cold)** |
| **Live Battery Telemetry** | Basic percentage | System sensors | **Live Watts (W) & Watt-hours (Wh)** |
| **Requires Admin Privileges** | Runs as SYSTEM service | Yes (Kernel driver) | **Yes (WMI BIOS write only)** |
| **Windows Dynamic Lighting Sync**| Inconsistent | Manual | **Automatic coordination** |

---

## Efficiency & Resource Footprint

OmenGlow has been aggressively optimized for zero system impact, making it ideal for battery longevity and intensive gaming sessions:

1. **0.9 MB Working Set Memory Trimming**:
   - Built-in native Windows memory optimization using `psapi.dll!EmptyWorkingSet`.
   - When running or minimized in the system tray, memory drops from typical WPF baselines down to just **~0.9 MB RAM**.
2. **dGPU Sleep Lock (`GpuPreference=1`)**:
   - Automatically registers Windows DirectX User GPU Preferences to strictly bind OmenGlow to the low-power Integrated GPU (`iGPU`).
   - Prevents your dedicated NVIDIA GeForce RTX graphics card from waking up from its zero-watt `D3Cold` sleep state just to service keyboard lighting.
3. **Zero-Allocation Animation Buffers**:
   - The animation engine utilizes pre-allocated, pinned byte arrays (`fxFrameBuffer` and `fxScaledBuffer`).
   - Eliminates heap allocations during 120ms animation ticks, avoiding Gen-0 .NET garbage collection pauses.
4. **Adaptive Battery Power Throttling**:
   - Detects AC mains vs DC battery power state via `GetSystemPowerStatus`.
   - On battery power, the internal timer throttles from 120ms to 200ms while normalizing animation phase deltas. This cuts ACPI firmware wakeups by 40% with zero perceived drop in animation smoothness.
5. **Real-Time Battery & Power Telemetry**:
   - Directly queries `root\WMI:BatteryStatus` for live discharge rate in milliwatts (`mW`) and remaining capacity in milliwatt-hours (`mWh`).
   - Right-click the system tray icon anytime to view:  
     `⚡ 0.9 MB RAM • iGPU • Battery (35.4W) • 59.2Wh left`

---

## Scope Isolation & Thermal Safety

> [!IMPORTANT]
> **OmenGlow will never alter your fans, thermals, or power envelopes.**

Many third-party laptop utilities bundle fan control, acoustic curve overrides, and CPU undervolting together with RGB lighting. While well-intentioned, overriding factory fan control can lead to unexpected thermal throttling, fan ramping bugs, or hardware instability.

OmenGlow strictly enforces an architectural boundary:
- **Zero Fan Modification**: Never reads or writes fan registers, tachometer sensors, or thermal trip points.
- **Zero Power Manipulation**: Does not touch Intel/AMD MSRs, PL1/PL2 thresholds, TGP limits, or core voltages.
- **No Third-Party Kernel Drivers**: The upstream `pawnio.sys` driver loader has been completely removed. Communication is strictly performed through standard, documented Windows WMI BIOS calls (`hpqBIntM`).
- **Coexists with HP OMEN Gaming Hub**: You can safely leave HP OMEN Gaming Hub installed to manage your performance profiles, fan curves, and eco modes without any conflict.

---

## Compatibility & Hardware Scope

> [!IMPORTANT]
> **OmenGlow is tailored specifically for the HP OMEN 16 (`am0279tx`).**

- **Verified & Supported Model**: **HP OMEN 16-am0279tx** (Intel Core i7, NVIDIA GeForce RTX, 4-Zone RGB keyboard).
- **Operating System**: Windows 10 (64-bit) or Windows 11 (64-bit).
- **Other Models**: This software was developed solely for the author's specific laptop and is **not** supported as a universal tool for other HP models. While other 4-zone OMEN laptops may share similar WMI BIOS calls, running this software on unverified models is done entirely at your own risk.

---

## Installation & Quick Start

### Option 1: Download Release Binary
1. Download the latest `OmenGlow.exe` from the [Releases](https://github.com/manuv/OmenGlow/releases) page.
2. Right-click `OmenGlow.exe` and select **Run as administrator**.
   *(Admin access is required by Windows to grant WMI BIOS write access to the motherboard firmware).*

### Option 2: Build from Source

#### Prerequisites
- Windows 10 / 11 (x64)
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

#### 1-Click Build
Clone the repository and run the included batch build script:
```cmd
git clone https://github.com/manuv/OmenGlow.git
cd OmenGlow
.\build.cmd
```
The compiled executable will be placed in `bin\Release\OmenGlow.exe`.

---

## Usage & Command-Line Arguments

| Flag | Description |
| :--- | :--- |
| *(no arguments)* | Standard mode: connects to HP WMI BIOS, loads user preferences, and displays the UI window. |
| `--hidden` | Starts silently minimized to the Windows system tray (perfect for Windows Startup / Task Scheduler). |
| `--demo` | Preview mode: runs the visualizer and UI without making any physical WMI BIOS hardware calls (no administrator privilege required). |
| `--test` | Diagnostic self-test: initializes sub-engines, validates buffers, and exits with code `0` on success. |

### Running on Windows Startup
To launch OmenGlow automatically on boot minimized to tray:
1. Press `Win + R`, type `shell:startup`, and press Enter.
2. Create a shortcut to `OmenGlow.exe`.
3. Right-click the shortcut -> **Properties** -> in the **Target** field, append ` --hidden` (e.g. `C:\Tools\OmenGlow.exe --hidden`).
4. Under **Compatibility** tab, check **Run this program as an administrator**.

---

## Upstream Attribution & Lineage

OmenGlow is derived from and builds upon the open-source hardware reverse-engineering work in **[Ohman](https://github.com/P4R1H/ohman)** created by **[P4R1H](https://github.com/P4R1H)** and the Ohman contributors.

We express our sincere respect and gratitude to P4R1H and the upstream authors:
- Upstream *Ohman* established the reverse-engineered HP BIOS ACPI WMI communication protocol (`root\wmi:hpqBIntM`, Command `0x20009` / Opcode `0x2B`, and the 128-byte packet layout).
- **OmenGlow** was created as an intentional fork/divergence to provide a scoped, ultra-lightweight, thermal-safe utility dedicated exclusively to lighting.
- All thermal, fan, undervolt, and kernel-driver (`pawnio.sys`) dependencies present in upstream were permanently removed to safeguard factory cooling profiles.
- For a complete, line-by-line audit of upstream changes, deletions, and additions, please see [CHANGELOG.md](CHANGELOG.md).

---

## Development & AI Transparency Notice

In the spirit of modern open-source transparency:
- The architectural refactoring, scope isolation, native memory trimming routines (`EmptyWorkingSet`), battery telemetry queries (`root\WMI:BatteryStatus`), vector visualizer implementation, and UI polish in **OmenGlow** were developed with the assistance of **Google Antigravity** (an advanced agentic AI coding environment by Google DeepMind).
- All generated code has been rigorously validated, compiled with zero warnings, hardware-tested on physical HP OMEN 16 silicon, and packaged under standard open-source best practices.

---

## Contributing

This is a personal open-source project maintained for the author's setup.
- If you find a bug specific to the application, animations, or UI, feel free to open an issue using the [Bug Report Template](.github/ISSUE_TEMPLATE/bug_report.md).
- Please note that pull requests introducing fan control, power/voltage overrides, or thermal changes will not be accepted, as scope isolation is the project's primary design pillar.

---

## Security & Hardware Safety

Please see [SECURITY.md](SECURITY.md) for full details on hardware safety, the Windows WMI BIOS security model, and our personal use liability disclaimers.

---

## License

This project's source code is licensed under the **GNU General Public License v3.0 or later** ([GNU GPLv3](LICENSE)).

In accordance with the GPL-3.0 license:
- You are free to run, study, modify, and redistribute this software.
- Any derivative works or distributions containing code from this project must also remain licensed under GNU GPL v3.0 or later and provide complete source code access.

---

## Brand & Logo Assets Notice

The **OmenGlow** mascot owl logo, emblem, and application icons (`app.ico`, `app.png`) represent the personal identity and creator branding of the repository author (used across personal media and YouTube).

- **Code vs. Brand Asset Separation**: While all underlying source code in this repository is licensed under the [GNU General Public License v3.0](LICENSE), the visual branding, owl character, logo artwork, and avatar files are **reserved exclusively by the author**.
- **Usage Restrictions**: You may not use the author's logo, avatar, or brand assets for commercial merchandise, external channel branding, or unrelated third-party applications without explicit written permission.

---

## Trademark Notice & Disclaimer

**OMEN®** is a registered trademark of **HP Inc. (Hewlett-Packard Development Company, L.P.)**.

**OmenGlow** is an independent, unofficial community open-source project. It is **not** manufactured, approved, sponsored, endorsed, or affiliated with HP Inc. or the HP OMEN brand in any way. All trademarks, service marks, model numbers, and brand names referenced in this repository are the property of their respective owners and are used strictly for hardware compatibility, descriptive reference, and identification purposes under **nominative fair use**.
