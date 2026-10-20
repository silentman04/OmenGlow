## Description
<!-- Please include a summary of the change and which issue it fixes, if applicable. -->

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds new functionality, e.g. animation, UI, visualizer)
- [ ] Performance optimization (e.g. memory trimming, zero-allocation buffers)
- [ ] Documentation update (e.g. README, compatibility notes)

## Architectural & Safety Verification (Mandatory)
- [ ] **Zero Fan/Thermal Touch**: This change does **NOT** add or touch fan curves, fan RPM, fan tachometers, or thermal registers.
- [ ] **Zero Voltage/Power Touch**: This change does **NOT** add or touch undervolting, MSRs, PL1/PL2 limits, or TGP overrides.
- [ ] **Kernel Driver Free**: No third-party `.sys` kernel drivers are added or used.

## Testing & Quality Checklist
- [ ] `build.cmd` builds cleanly with **0 errors** and **0 warnings**.
- [ ] `OmenGlow.exe --test` passes all diagnostic checks.
- [ ] Tested live on physical HP hardware or in `--demo` mode.
- [ ] Follows existing C# conventions and avoids heap allocations in the 120ms animation tick loop.
- [ ] My contribution is licensed under GNU General Public License v3.0 or later.
