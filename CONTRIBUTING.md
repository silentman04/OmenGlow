# Contributing to OmenGlow

Thank you for your interest in contributing to **OmenGlow**!

Whether you are testing hardware compatibility across different HP OMEN laptop models, submitting bug fixes, designing new lighting animations, or improving documentation, your help is warmly welcomed.

---

## 1. Core Architectural Boundary (Please Read First)

OmenGlow has one foundational design principle that sets it apart from other laptop utilities:

> **OmenGlow is dedicated exclusively to 4-Zone RGB Lighting. It will NEVER modify fan curves, thermal trip points, power limits, or CPU/GPU voltages.**

- Pull requests that introduce fan RPM overrides, thermal throttling tampering, undervolting, or third-party kernel drivers (`.sys` files) **will be rejected**.
- We intentionally leave all thermal and power management to HP's factory BIOS and the official **HP OMEN Gaming Hub**. This guarantees complete safety for users' laptops.

If you are developing a new feature or optimization, please ensure it strictly stays within the lighting, UI, visualizer, efficiency, or telemetry domains.

---

## 2. Ways to Contribute

### A. Hardware Compatibility Reports (Most Valuable!)
We want to verify every HP OMEN model that works with OmenGlow. If you own an HP OMEN laptop:
1. Run `OmenGlow.exe` as Administrator.
2. Test static colors, brightness, and animations.
3. Open an issue using our [Hardware Compatibility Report Template](.github/ISSUE_TEMPLATE/hardware_compatibility.md) and tell us:
   - Your laptop model number (e.g., `HP Omen 16-am0279tx`, `HP Omen 17-ck0xxx`)
   - CPU and GPU specifications
   - BIOS version (run `wmic bios get smbiosbiosversion` or `Get-CimInstance Win32_BIOS | Select-Object SMBIOSBIOSVersion`)
   - Did all 4 zones respond correctly?

### B. Bug Reports
If something isn't working:
1. Check the [Existing Issues](https://github.com/manuv/OmenGlow/issues) to see if it has already been reported.
2. If not, open a new issue using the [Bug Report Template](.github/ISSUE_TEMPLATE/bug_report.md).
3. Include your Windows version, laptop model, and the output of running `OmenGlow.exe --test`.

### C. Feature Requests
Have an idea for a cool animation effect, UI enhancement, or palette preset?
- Open an issue using the [Feature Request Template](.github/ISSUE_TEMPLATE/feature_request.md).

### D. Code & Optimization PRs
We welcome improvements to:
- Lighting animation algorithms in `src/Engine.cs`.
- Vector keyboard visualizer styling in `src/Keyboard.cs`.
- Color picker controls in `src/Controls.cs`.
- Memory and battery efficiency improvements.

---

## 3. Local Development & Building

### Prerequisites
- Windows 10 or Windows 11 (64-bit)
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git for Windows

### Build Instructions
1. Clone your fork:
   ```cmd
   git clone https://github.com/<your-username>/OmenGlow.git
   cd OmenGlow
   ```
2. Build the project:
   ```cmd
   .\build.cmd
   ```
3. Run diagnostic self-tests:
   ```cmd
   .\bin\Release\OmenGlow.exe --test
   ```
4. Run in UI preview mode (no admin or HP hardware required):
   ```cmd
   .\bin\Release\OmenGlow.exe --demo
   ```

---

## 4. Coding & Performance Guidelines

1. **Zero-Allocation Hot Paths**:
   - The animation loop in `src/Engine.cs` ticks every 120ms (200ms on battery). Avoid allocating new objects, closures, or arrays inside the per-tick calculation. Use pre-allocated buffers (`fxFrameBuffer`).
2. **Resource & Memory Awareness**:
   - OmenGlow achieves a **~0.9 MB RAM** resident working set when running in the background. Keep memory footprint minimal.
3. **dGPU Sleep Protection**:
   - Do not invoke APIs or 3D rendering pipelines that trigger Windows to wake the discrete GPU (e.g. NVIDIA RTX) from its zero-watt `D3Cold` state.
4. **Clean Code Style**:
   - Follow standard modern C# naming conventions (PascalCase for methods/properties, camelCase for local variables).
   - Keep files focused and cleanly structured.

---

## 5. Submitting a Pull Request

1. Create a feature branch:
   ```cmd
   git checkout -b feature/my-cool-effect
   ```
2. Make your changes and test thoroughly:
   - Ensure `build.cmd` compiles with **0 warnings** and **0 errors**.
   - Verify `OmenGlow.exe --test` passes.
3. Commit with clear, descriptive commit messages.
4. Push your branch to GitHub and open a Pull Request against `main`.
5. Fill out our [Pull Request Template](.github/pull_request_template.md).

---

## 6. Licensing & Legal Agreement

By submitting code, documentation, or other content to the OmenGlow repository, you agree that your contribution is provided under the terms of the **GNU General Public License v3.0 or later** ([GPL-3.0-or-later](LICENSE)).

You also certify that:
- You authored the contribution or have the legal right to submit it under the GPLv3 license.
- Your contribution does not violate any third-party proprietary rights or nondisclosure agreements.
