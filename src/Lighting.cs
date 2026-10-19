// SPDX-License-Identifier: GPL-3.0-or-later
// OMEN 4-Zone RGB: keyboard lighting backend.
// HP laptops expose the keyboard backlight through the BIOS mailbox (hpqBIntM) under command id 0x20009.
// Zone colours sit in a 128-byte table at offset 25 (3 bytes per zone: R, G, B).
// Brightness/backlight is in a byte whose bit 7 (0x80) is the "on" flag.
// Layout from HP OMEN 4-zone specifications:
// Zone 0 = Right / Numpad, Zone 1 = Middle, Zone 2 = Left, Zone 3 = WASD.
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Win32;

namespace Ohman {

    public enum LightKind { None = 0, Zones = 1, PerKey = 2 }

    public struct Rgb {
        public byte R, G, B;
        public Rgb(byte r, byte g, byte b) { R = r; G = g; B = b; }
        public string Hex { get { return R.ToString("X2") + G.ToString("X2") + B.ToString("X2"); } }

        public static bool TryParse(string s, out Rgb c) {
            c = new Rgb();
            int v;
            if (s == null) return false;
            s = s.Trim().TrimStart('#');
            if (s.Length != 6 || !int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return false;
            c = new Rgb((byte)(v >> 16), (byte)(v >> 8), (byte)v);
            return true;
        }

        /// <summary>Hue 0..360 at full saturation and value.</summary>
        public static Rgb FromHue(double h) {
            h = ((h % 360) + 360) % 360;
            double x = 1 - Math.Abs((h / 60) % 2 - 1);
            double r = 0, g = 0, b = 0;
            if (h < 60) { r = 1; g = x; }
            else if (h < 120) { r = x; g = 1; }
            else if (h < 180) { g = 1; b = x; }
            else if (h < 240) { g = x; b = 1; }
            else if (h < 300) { r = x; b = 1; }
            else { r = 1; b = x; }
            return new Rgb((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255));
        }

        public static Rgb FromHsv(double h, double sat, double val) {
            h = ((h % 360) + 360) % 360;
            sat = Math.Max(0, Math.Min(1, sat));
            val = Math.Max(0, Math.Min(1, val));
            double c = val * sat, x = c * (1 - Math.Abs((h / 60) % 2 - 1)), m = val - c, r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return new Rgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }

        public void ToHsv(out double h, out double sat, out double val) {
            double r = R / 255.0, g = G / 255.0, b = B / 255.0, max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
            val = max;
            sat = max <= 0 ? 0 : d / max;
            h = Hue;
        }

        public Rgb Scale(double f) {
            f = Math.Max(0, Math.Min(1, f));
            return new Rgb((byte)Math.Round(R * f), (byte)Math.Round(G * f), (byte)Math.Round(B * f));
        }

        public double Hue {
            get {
                double r = R / 255.0, g = G / 255.0, b = B / 255.0, max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
                if (d < 1e-6) return 0;
                double h = max == r ? ((g - b) / d) % 6 : max == g ? (b - r) / d + 2 : (r - g) / d + 4;
                return ((h * 60) + 360) % 360;
            }
        }
    }

    public interface ILighting {
        LightKind Kind { get; }
        int Zones { get; }
        bool Inert { get; }
        bool Numpad { get; }
        string Describe { get; }
        Rgb[] GetColors();
        void SetColors(Rgb[] zones);
        int GetBacklight();
        void SetBacklight(bool on, int level);
    }

    /// <summary>The firmware path: 0x20008/0x2B for keyboard type, 0x20009 for colours and backlight.</summary>
    public sealed class BiosLighting : ILighting {
        public const uint CMD = 0x20009;                        // WMI_CMD_LED_LIGHTING_CONTROL in OGH
        public const uint OP_KBD_TYPE = 0x2B;                   // under 0x20008: out4 [0] = keyboard type
        public const uint OP_PLATFORM_INFO = 0x01;              // out128 [0] bit 0 = lighting supported
        public const uint OP_COLOR_GET = 0x02, OP_COLOR_SET = 0x03, OP_LIGHT_GET = 0x04, OP_LIGHT_SET = 0x05;
        public const int COLOR_OFFSET = 25;                     // zone i = bytes 25+3i .. 27+3i (R, G, B)
        public const int ON_FLAG = 0x80;

        public readonly int KbdType;
        readonly LightKind kind;
        readonly int zones;

        public LightKind Kind { get { return kind; } }
        public int Zones { get { return zones; } }
        public string Describe { get { return WhiteOnly ? "white backlight" : kind == LightKind.PerKey ? "per-key" : zones == 1 ? "1 zone" : zones + " zones"; } }
        public bool Numpad { get { return KbdType == 1 || KbdType == 4; } }
        public bool Inert { get { return kind == LightKind.PerKey; } }
        public bool WhiteOnly { get; private set; }

        BiosLighting(int kbdType, LightKind k, int z) {
            KbdType = kbdType;
            kind = k;
            zones = z;
        }

        /// <summary>Detects HP keyboard lighting via WMI BIOS mailbox. Read-only probe.</summary>
        public static BiosLighting Detect() {
            bool declared = false;
            int sup0 = -1, sup1 = -1;
            try {
                var d = Bios.Call(CMD, OP_PLATFORM_INFO, new byte[0], 128);
                if (d.Length > 0) { declared = (d[0] & 1) != 0; sup0 = d[0]; }
                if (d.Length > 1) sup1 = d[1];
            } catch (Exception ex) {
                Log.Write("lighting support probe: " + ex.Message);
            }

            int type = 0;
            bool typeAnswered = false;
            try {
                var d = Bios.Call(Bios.CMD_DEFAULT, OP_KBD_TYPE, new byte[0], 4);
                if (d.Length > 0) { type = (sbyte)d[0]; typeAnswered = true; }
            } catch (Exception ex) {
                Log.Write("keyboard type query: " + ex.Message + "; assuming standard layout");
            }
            if (type < 0) type = 0;

            LightKind k;
            int z;
            if (type == 3) { k = LightKind.PerKey; z = 4; }
            else if (type == 4 || type == 5) { k = LightKind.Zones; z = 1; }
            else { k = LightKind.Zones; z = 4; } // 0 standard, 1 numpad, 2 tenkeyless: all 4-zone boards

            var l = new BiosLighting(type, k, z);

            try {
                var c = l.GetColors();
                int b = l.GetBacklight();

                try {
                    bool black = true;
                    foreach (var x in c) if (x.R != 0 || x.G != 0 || x.B != 0) black = false;
                    int t0 = l.Table()[0];
                    l.WhiteOnly = typeAnswered && type == 0 && ((sup0 == 0x01 && sup1 == 0 && t0 == 0 && black) || sup0 == 0x06);
                } catch { }

                Log.Write("keyboard lighting detected: type=" + type + " (" + l.Describe + "), colours=" + Join(c) + ", backlight=0x" + b.ToString("X2"));
            } catch (Exception ex) {
                Log.Write("keyboard lighting: none (colour table error: " + ex.Message + ")");
                return null;
            }

            return l;
        }

        static string Join(Rgb[] c) {
            var s = new List<string>();
            foreach (var x in c) s.Add(x.Hex);
            return string.Join(",", s.ToArray());
        }

        byte[] Table() {
            var d = Bios.Call(CMD, OP_COLOR_GET, new byte[] { 0 }, 128);
            if (d.Length < COLOR_OFFSET + 3 * zones)
                throw new InvalidOperationException("short colour table (" + d.Length + " bytes)");
            return d;
        }

        public Rgb[] GetColors() {
            var d = Table();
            var c = new Rgb[zones];
            for (int i = 0; i < zones; i++)
                c[i] = new Rgb(d[COLOR_OFFSET + 3 * i], d[COLOR_OFFSET + 3 * i + 1], d[COLOR_OFFSET + 3 * i + 2]);
            return c;
        }

        public void SetColors(Rgb[] c) {
            var d = Table(); // Read-modify-write as OGH does; bytes 0..24 are untouched
            var buf = new byte[128];
            Array.Copy(d, buf, Math.Min(d.Length, 128));
            for (int i = 0; i < zones && i < c.Length; i++) {
                buf[COLOR_OFFSET + 3 * i] = c[i].R;
                buf[COLOR_OFFSET + 3 * i + 1] = c[i].G;
                buf[COLOR_OFFSET + 3 * i + 2] = c[i].B;
            }
            Bios.Call(CMD, OP_COLOR_SET, buf, 4);
        }

        public int GetBacklight() {
            var d = Bios.Call(CMD, OP_LIGHT_GET, new byte[] { 0 }, 128);
            return d.Length > 0 ? d[0] : -1;
        }

        public void SetBacklight(bool on, int level) {
            int v = Math.Max(0, Math.Min(100, level)) | (on ? ON_FLAG : 0);
            Bios.Call(CMD, OP_LIGHT_SET, new byte[] { (byte)v, 0, 0, 0 }, 4);
        }
    }

    /// <summary>Simulated 4-zone keyboard for preview and test runs.</summary>
    public sealed class DemoLighting : ILighting {
        // Order: Zone 0: Right, Zone 1: Middle, Zone 2: Left, Zone 3: WASD
        Rgb[] colors = { new Rgb(0x2E, 0x6B, 0xFF), new Rgb(0x33, 0xA5, 0xE6), new Rgb(0x5F, 0xCB, 0xE0), new Rgb(0xFF, 0x00, 0x55) };
        int light = 0xE4;

        public LightKind Kind { get { return LightKind.Zones; } }
        public bool Inert { get { return false; } }
        public int Zones { get { return 4; } }
        public string Describe { get { return "4 zones (demo)"; } }
        public bool Numpad { get { return true; } }

        public Rgb[] GetColors() { return (Rgb[])colors.Clone(); }
        public void SetColors(Rgb[] c) { for (int i = 0; i < 4 && i < c.Length; i++) colors[i] = c[i]; }
        public int GetBacklight() { return light; }
        public void SetBacklight(bool on, int level) { light = Math.Max(0, Math.Min(100, level)) | (on ? 0x80 : 0); }
    }

    /// <summary>Windows Dynamic Lighting coordination for HP VHF lighting devices.</summary>
    public static class WinLighting {
        const string Root = @"Software\Microsoft\Lighting";

        static List<string> AllVhfKeys() {
            var all = new List<string>();
            try {
                using (var k = Registry.CurrentUser.OpenSubKey(Root + @"\Devices")) {
                    if (k != null) {
                        foreach (string n in k.GetSubKeyNames()) {
                            if (n.IndexOf("HID_DEVICE_SYSTEM_VHF", StringComparison.OrdinalIgnoreCase) >= 0)
                                all.Add(n);
                        }
                    }
                }
            } catch { }
            return all;
        }

        public static bool Present { get { return AllVhfKeys().Count > 0; } }

        public static bool HasControl {
            get {
                try {
                    using (var g = Registry.CurrentUser.OpenSubKey(Root)) {
                        if (g != null && Convert.ToInt32(g.GetValue("AmbientLightingEnabled", 1)) == 0)
                            return false;
                    }
                    foreach (string n in AllVhfKeys()) {
                        using (var k = Registry.CurrentUser.OpenSubKey(Root + @"\Devices\" + n)) {
                            if (k != null && Convert.ToInt32(k.GetValue("AmbientLightingEnabled", 1)) != 0)
                                return true;
                        }
                    }
                } catch { }
                return false;
            }
        }

        public static void SetControl(bool windows) {
            foreach (string n in AllVhfKeys()) {
                try {
                    using (var k = Registry.CurrentUser.CreateSubKey(Root + @"\Devices\" + n)) {
                        k.SetValue("AmbientLightingEnabled", windows ? 1 : 0, RegistryValueKind.DWord);
                    }
                } catch (Exception ex) {
                    Log.Write("dynamic lighting switch: " + ex.Message);
                }
            }
        }
    }
}
