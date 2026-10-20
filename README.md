# OmenGlow - 4-Zone RGB Keyboard Lighting

**OmenGlow** is a lightweight, standalone 4-Zone RGB Keyboard Lighting utility designed specifically for HP OMEN 16 laptops (including the `am0279tx` and compatible models).

It provides immediate, fine-grained control over your keyboard's backlighting zones and animation effects through a clean, compact dark-mode interface—**without** interfering with your laptop's fan speeds, power profiles, or official HP software.

---

## Key Features

- **4-Zone RGB Lighting Control**:
  - **Zone 1**: WASD cluster
  - **Zone 2**: Left keyboard area
  - **Zone 3**: Middle keyboard area
  - **Zone 4**: Right keyboard area & Numpad
  - **All Zones**: 1-click synchronous color application
- **Lighting Effects**:
  - **Static**: Solid custom colors per zone
  - **Breathing**: Smooth pulsing animation with configurable speed
  - **Color Wave**: Dynamic color wave propagating across the keyboard zones
  - **Cycle / Rainbow**: Smooth spectrum cycling
  - **Off**: 1-click backlight toggle
- **Interactive Visualizer**: Real-time vector keyboard on screen mimicking your laptop's actual lighting state and animation timing.
- **Precision Color Picker**: Quick preset palette + Dual-strip continuous Hue and Shade sliders + Hex code input.
- **Factory Reset**: 1-click `↺ Reset to Defaults` button restoring the signature landing palette (WASD `#FF0055`, Left `#9900FF`, Middle `#00EEFF`, Right `#0099FF`), Static mode, and 100% brightness.
- **Dual Theme Support**: 1-click toggle between crisp **Light** mode and dark **OMEN** mode, with full dynamic theme persistence across sessions.
- **System Tray Integration**: Minimizes unobtrusively to the Windows Taskbar Notification Area with a quick context menu.
- **Automatic State Persistence**: Remembers your chosen colors, modes, speed, brightness, and active theme in `omenglow.state` across reboots.

---

## Scope Isolation & Thermal Safety

Unlike general hardware-monitoring utilities, **OmenGlow deliberately omits all fan and thermal controls**:
- **Zero Fan Interference**: OmenGlow never reads or writes fan RPM, acoustic curves, or thermal trip points.
- **Zero Power Interference**: Does not manipulate CPU/GPU wattage, PL1/PL2 limits, TGP overrides, or undervolting.
- **OMEN Gaming Hub Compatible**: You can run the official **HP OMEN Gaming Hub** for performance modes and cooling without any conflict or service disruption.

---

## How to Build from Source

### Prerequisites
- Windows 10 or Windows 11 (64-bit)
- .NET 6.0 Desktop Runtime or .NET SDK (installed by default or via dotnet.microsoft.com)

### 1-Click Build
Double-click `build.cmd` in this folder, or run in PowerShell / Command Prompt:
```cmd
.\build.cmd
```
The compiled executable will be placed in `bin\Release\OmenGlow.exe`.

---

## How to Run

### Standard Usage (Hardware Lighting)
Right-click `bin\Release\OmenGlow.exe` and select **Run as administrator** (or run from an elevated prompt):
```powershell
.\bin\Release\OmenGlow.exe
```
*Note: Administrator privilege is required by Windows to grant WMI BIOS write access to the HP motherboard firmware.*

### Demo / Preview Mode (No Admin Required)
To explore the UI, test animations, and use the color pickers without applying changes to physical keyboard hardware:
```powershell
.\bin\Release\OmenGlow.exe --demo
```

### Command-Line Options
| Option | Description |
| :--- | :--- |
| *(no arguments)* | Standard mode: connects to HP WMI BIOS and loads the main window. |
| `--demo` | Preview mode: runs the visualizer without sending hardware WMI calls. |
| `--hidden` | Starts minimized directly to the Windows system tray. |
| `--test` | Runs automated self-test diagnostics and exits with code `0` on success. |

---

## Project Structure

```text
OmenGlow/
├── OmenGlow.csproj    # .NET project configuration
├── app.manifest       # DPI awareness & execution privileges
├── build.cmd          # Automated 1-click build script
├── .gitignore         # Clean Git rules
├── LICENSE            # GNU General Public License v3
├── README.md          # User guide & documentation
├── CHANGELOG.md       # Full record of modifications & architectural decisions
└── src/
    ├── Program.cs     # Main entry point & system tray coordinator
    ├── Hardware.cs    # HP WMI BIOS communication (hpqBIntM)
    ├── Lighting.cs    # 4-zone packet layout & Windows Dynamic Lighting sync
    ├── Keyboard.cs    # 4-zone layout mapping & vector keyboard visualizer
    ├── Engine.cs      # 120ms tick animation loop & state persistence
    ├── Controls.cs    # Dual-strip Hue/Shade picker & custom controls
    ├── Theme.cs       # Dark theme styles & easing functions
    ├── Ui.xaml        # Clean, modern WPF interface layout
    ├── Ui.cs          # UI interaction event handlers
    └── Meta.cs        # Product information & versioning
```

---

## License & Attribution

- **License**: Released under the [GNU General Public License v3.0 or later](LICENSE).
- **Attribution**: Based on the open-source hardware reverse-engineering work from *Ohman* by P4R1H and the Ohman authors (https://github.com/P4R1H/ohman). Refactored into a focused, standalone lighting utility with all thermal/fan modules removed.

---

## Trademark Notice & Disclaimer

**OMEN®** is a registered trademark of **HP Inc. (Hewlett-Packard)**.

**OmenGlow** is an independent, unofficial community open-source project. It is **not** developed, sponsored, endorsed, or affiliated with HP Inc. or OMEN in any way. All trademarks, service marks, and trade names referenced herein belong to their respective owners and are used strictly for hardware compatibility and identification purposes under nominative fair use.
