// SPDX-License-Identifier: GPL-3.0-or-later
// OmenGlow: Standalone 4-Zone RGB Keyboard Lighting utility for HP Omen laptops.
using System.Reflection;

[assembly: AssemblyTitle("OmenGlow")]
[assembly: AssemblyProduct("OmenGlow")]
[assembly: AssemblyDescription("Lightweight, standalone 4-Zone RGB Keyboard Lighting utility for HP Omen laptops.")]
[assembly: AssemblyCompany("OmenGlow")]
[assembly: AssemblyCopyright("Copyright (C) 2026 - GPL-3.0-or-later")]
[assembly: AssemblyVersion(Ohman.Meta.Version)]
[assembly: AssemblyFileVersion(Ohman.Meta.Version)]
[assembly: AssemblyInformationalVersion(Ohman.Meta.Version)]

namespace Ohman {
    public static class Meta {
        public const string Name = "OmenGlow";
        public const string DisplayName = "OmenGlow - 4-Zone RGB Lighting";
        public const string Version = "1.0.0";
    }
}
