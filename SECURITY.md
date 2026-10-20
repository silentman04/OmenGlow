# Security & Hardware Safety Policy

## 1. Hardware Safety by Architecture

The number one security priority of **OmenGlow** is the physical safety and thermal integrity of your laptop hardware.

### Scope Isolation
Many open-source laptop utilities combine fan control, acoustic curve overrides, and CPU/GPU undervolting together with RGB lighting. While powerful, overriding factory fan control in software introduces severe risks:
- Fan curves failing to ramp under heavy loads if a background app hangs or crashes.
- Unintentional overheating, thermal throttling, or emergency shutdowns.
- Potential conflict with the laptop's embedded controller (EC) or factory firmware.

**OmenGlow completely eliminates this risk by design**:
- **0% Fan Interaction**: OmenGlow contains **zero** code to read or write fan RPM, fan tables, or thermal trip points.
- **0% Power Interaction**: OmenGlow contains **zero** code to modify CPU voltages, PL1/PL2 power limits, or GPU TGPs.
- **No Third-Party Kernel Drivers**: OmenGlow does not load `pawnio.sys` or any other kernel-mode driver. It runs purely in user mode using the standard Windows WMI subsystem.
- **Safe Coexistence**: The factory BIOS and official **HP OMEN Gaming Hub** retain 100% control over cooling and performance profiles without any interference.

---

## 2. Why Administrator Privileges Are Required

When running in standard hardware mode, OmenGlow requires **Run as administrator**.

### Technical Explanation:
- Keyboard backlighting on HP OMEN laptops is controlled via the motherboard's ACPI WMI BIOS interface (`root\wmi:hpqBIntM`).
- The Windows operating system restricts WMI write operations to hardware BIOS namespaces to accounts with elevated administrator privileges (`SeSecurityPrivilege` / UAC elevation).
- OmenGlow uses this elevation **exclusively** to call the HP BIOS lighting command (`0x20009` / `0x2B`). It does not modify system files, install services, or make network connections.

> [!TIP]
> If you wish to test the UI, vector visualizer, and animation effects without granting Administrator privileges, you can launch OmenGlow with the preview flag:
> ```powershell
> .\OmenGlow.exe --demo
> ```

---

## 3. Privacy & Network Security

- **Zero Telemetry**: OmenGlow does not collect, log, or transmit any user data, telemetry, or analytics.
- **Zero Network Activity**: OmenGlow makes zero outbound or inbound network connections. It operates 100% offline.
- **Local State Only**: User preferences (colors, modes, brightness, speed, active theme) are stored exclusively in a single local configuration file (`omenglow.state`) in the application directory.

---

## 4. Reporting a Security Vulnerability

If you discover a security vulnerability or potential hardware issue in OmenGlow:
1. **Do not disclose it publicly** in a public issue or forum.
2. Please report the issue via GitHub's [Private Vulnerability Reporting](https://github.com/manuv/OmenGlow/security/advisories/new) or directly contact the maintainers.
3. Please provide:
   - A description of the issue.
   - Steps to reproduce or proof-of-concept.
   - Affected Windows and laptop model versions.

We take all security and hardware safety concerns seriously and will investigate and patch verified issues promptly.
