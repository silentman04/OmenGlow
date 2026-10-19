// SPDX-License-Identifier: GPL-3.0-or-later
// OMEN 4-Zone RGB: Product metadata and assembly information.
using System.Reflection;

[assembly: AssemblyTitle("OMEN 4-Zone RGB")]
[assembly: AssemblyProduct("OMEN 4-Zone RGB")]
[assembly: AssemblyDescription("Lightweight, standalone 4-Zone RGB Keyboard Lighting utility for HP Omen laptops.")]
[assembly: AssemblyCompany("OMEN RGB")]
[assembly: AssemblyCopyright("Copyright (C) 2026 - GPL-3.0-or-later")]
[assembly: AssemblyVersion(Ohman.Meta.Version)]
[assembly: AssemblyFileVersion(Ohman.Meta.Version)]
[assembly: AssemblyInformationalVersion(Ohman.Meta.Version)]

namespace Ohman {
    public static class Meta {
        public const string Version = "2.0.0";
    }
}
