# Contributing to OmenGlow

**OmenGlow** is an independent, personal open-source project tailored specifically for the **HP OMEN 16 (`am0279tx`)** laptop.

While this project is primarily maintained for the author's own setup, bug fixes, visual enhancements, and animation improvements are welcome.

---

## ⚠️ Important Scope & Liability Notice

1. **Hardware Scope**: This software is tailored and tested strictly for the author's laptop model (**HP OMEN 16-am0279tx**). We do not actively test or maintain support for other laptop models.
2. **Zero Liability**: Running this software on any machine is done strictly at your own risk. The author accepts zero responsibility for any hardware issues, motherboard behavior, or LED anomalies.
3. **Thermal Safety is Non-Negotiable**:
   > **OmenGlow is dedicated exclusively to 4-Zone RGB Lighting. It will NEVER modify fan curves, thermal trip points, power limits, or CPU/GPU voltages.**
   Pull requests that introduce fan RPM overrides, thermal throttling tampering, undervolting, or third-party kernel drivers (`.sys` files) **will be rejected**.

---

## Ways to Contribute

### A. Bug Reports
If you encounter a bug on Windows 10/11:
1. Check the [Existing Issues](https://github.com/silentman04/OmenGlow/issues) to see if it has already been reported.
2. If not, open a new issue using the [Bug Report Template](.github/ISSUE_TEMPLATE/bug_report.md).
3. Include your Windows build and the output of running `OmenGlow.exe --test`.

### B. Feature & Animation Proposals
Have an idea for a creative lighting animation, palette preset, or vector visualizer tweak?
- Open an issue using the [Feature Request Template](.github/ISSUE_TEMPLATE/feature_request.md).

### C. Code & Optimization PRs
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
