// SPDX-License-Identifier: GPL-3.0-or-later
// OMEN 4-Zone RGB: Standalone lighting engine.
// Strictly isolated to 4-zone keyboard lighting.
// Thermal, fan, and power controls are completely excluded.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Ohman {

    public sealed class Settings {
        public int Light = 1;                         // 0 = Off, 1 = On (Static or Effect)
        public int LightEffect = 0;                   // 0 = Static, 1 = Breathing, 2 = Rainbow/Cycle, 3 = Wave
        public int LightSpeed = 3;                    // 1..5
        public int LightLevel = 100;                  // 0..100 %
        public string LightColors = "";               // Comma-separated hex colours for zones
        public bool StartHidden = false;
        public bool TookWinLighting = false;

        static readonly string StateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "omenrgb.state");

        public static Settings Load() {
            var s = new Settings();
            try {
                if (!File.Exists(StateFile)) return s;
                foreach (string line in File.ReadAllLines(StateFile)) {
                    string l = line.Trim();
                    int eq = l.IndexOf('=');
                    if (l.Length == 0 || l[0] == '#' || eq < 1) continue;
                    string k = l.Substring(0, eq).Trim();
                    string v = l.Substring(eq + 1).Trim();
                    int n;
                    bool b;
                    switch (k) {
                        case "Light": if (int.TryParse(v, out n)) s.Light = Math.Max(0, Math.Min(1, n)); break;
                        case "LightEffect": if (int.TryParse(v, out n)) s.LightEffect = Math.Max(0, Math.Min(3, n)); break;
                        case "LightSpeed": if (int.TryParse(v, out n)) s.LightSpeed = Math.Max(1, Math.Min(5, n)); break;
                        case "LightLevel": if (int.TryParse(v, out n)) s.LightLevel = Math.Max(0, Math.Min(100, n)); break;
                        case "LightColors": s.LightColors = v; break;
                        case "StartHidden": if (bool.TryParse(v, out b)) s.StartHidden = b; break;
                        case "TookWinLighting": if (bool.TryParse(v, out b)) s.TookWinLighting = b; break;
                    }
                }
            } catch (Exception ex) {
                Log.Write("settings load error: " + ex.Message);
            }
            return s;
        }

        public void Save() {
            try {
                var sb = new StringBuilder();
                sb.AppendLine("# OMEN 4-Zone RGB State");
                sb.AppendLine("Light=" + Light);
                sb.AppendLine("LightEffect=" + LightEffect);
                sb.AppendLine("LightSpeed=" + LightSpeed);
                sb.AppendLine("LightLevel=" + LightLevel);
                sb.AppendLine("LightColors=" + LightColors);
                sb.AppendLine("StartHidden=" + StartHidden);
                sb.AppendLine("TookWinLighting=" + TookWinLighting);
                File.WriteAllText(StateFile, sb.ToString());
            } catch (Exception ex) {
                Log.Write("settings save error: " + ex.Message);
            }
        }
    }

    public sealed class Engine {
        public readonly IHardware Hw;
        public readonly Settings S;

        public ILighting Light { get; private set; }
        public Rgb[] LightColors = new Rgb[4];

        public event Action<Rgb[]> FrameChanged;
        public event Action Changed;

        readonly object applySync = new object();
        System.Threading.Timer fxTimer;
        double fxPhase;
        int fxFailures;

        public Engine(IHardware hw, Settings s) {
            Hw = hw;
            S = s;
        }

        public void Init() {
            try {
                if (Hw.IsDemo) {
                    Light = new DemoLighting();
                } else {
                    Light = BiosLighting.Detect();
                    if (Light == null) {
                        Log.Write("Firmware lighting not detected; using DemoLighting for preview.");
                        Light = new DemoLighting();
                    }
                }

                int zoneCount = Math.Max(1, Light != null ? Light.Zones : 4);
                Rgb[] fwColors = null;
                try { if (Light != null) fwColors = Light.GetColors(); } catch { }

                LightColors = ParseColors(S.LightColors, zoneCount);
                if (LightColors == null) {
                    if (fwColors != null && fwColors.Length == zoneCount) {
                        LightColors = (Rgb[])fwColors.Clone();
                    } else {
                        // Default vivid palette: Zone 0 (Right: Blue), Zone 1 (Middle: Cyan), Zone 2 (Left: Purple), Zone 3 (WASD: Red)
                        LightColors = new Rgb[] {
                            new Rgb(0x00, 0x99, 0xFF),
                            new Rgb(0x00, 0xEE, 0xFF),
                            new Rgb(0x99, 0x00, 0xFF),
                            new Rgb(0xFF, 0x00, 0x55)
                        };
                    }
                    S.LightColors = JoinColors(LightColors);
                }

                // If Windows Dynamic Lighting is present and controlling the HP device, take control while app runs
                if (!Hw.IsDemo && WinLighting.HasControl) {
                    S.TookWinLighting = true;
                    S.Save();
                    WinLighting.SetControl(false);
                }

                ApplyLightCore();
            } catch (Exception ex) {
                Log.Write("engine init error: " + ex.Message);
            }
        }

        public void Shutdown() {
            StopEffect();
            try {
                if (S.TookWinLighting) {
                    WinLighting.SetControl(true);
                }
            } catch { }
            S.Save();
        }

        static Rgb[] ParseColors(string s, int n) {
            if (string.IsNullOrEmpty(s)) return null;
            var parts = s.Split(',');
            var r = new Rgb[n];
            for (int i = 0; i < n; i++) {
                Rgb c;
                if (i < parts.Length && Rgb.TryParse(parts[i].Trim(), out c)) r[i] = c;
                else return null;
            }
            return r;
        }

        static string JoinColors(Rgb[] c) {
            var s = new List<string>();
            foreach (var x in c) s.Add(x.Hex);
            return string.Join(",", s.ToArray());
        }

        Rgb[] Scaled(Rgb[] c) {
            var r = new Rgb[c.Length];
            double f = Math.Max(0.05, S.LightLevel / 100.0);
            for (int i = 0; i < c.Length; i++) r[i] = c[i].Scale(f);
            return r;
        }

        void ApplyLightCore() {
            if (Light == null) return;
            StopEffect();

            lock (applySync) {
                try {
                    bool lit = S.Light == 1;
                    if (lit) {
                        Light.SetColors(Scaled(LightColors));
                        Light.SetBacklight(true, S.LightLevel);
                    } else {
                        Light.SetBacklight(false, 0);
                        var black = new Rgb[LightColors.Length];
                        Light.SetColors(black);
                    }
                } catch (Exception ex) {
                    Log.Write("ApplyLightCore error: " + ex.Message);
                }
            }

            if (S.Light == 1 && S.LightEffect != 0) {
                StartEffect();
            }
        }

        public void SetLightModeAndEffect(int mode, int effect) {
            S.Light = Math.Max(0, Math.Min(1, mode));
            S.LightEffect = Math.Max(0, Math.Min(3, effect));
            S.Save();
            ApplyLightCore();
            OnChanged();
        }

        public void SetZoneColor(int zoneIndex, Rgb c) {
            if (zoneIndex >= 0 && zoneIndex < LightColors.Length) {
                LightColors[zoneIndex] = c;
                S.LightColors = JoinColors(LightColors);
                S.Save();
                if (S.Light == 1 && S.LightEffect == 0) {
                    lock (applySync) {
                        try { Light.SetColors(Scaled(LightColors)); } catch { }
                    }
                }
                OnChanged();
            }
        }

        public void SetAllZonesColor(Rgb c) {
            for (int i = 0; i < LightColors.Length; i++)
                LightColors[i] = c;
            S.LightColors = JoinColors(LightColors);
            S.Save();
            if (S.Light == 1 && S.LightEffect == 0) {
                lock (applySync) {
                    try { Light.SetColors(Scaled(LightColors)); } catch { }
                }
            }
            OnChanged();
        }

        public void SetSpeed(int speed) {
            S.LightSpeed = Math.Max(1, Math.Min(5, speed));
            S.Save();
            OnChanged();
        }

        public void SetBrightness(int level) {
            S.LightLevel = Math.Max(0, Math.Min(100, level));
            S.Save();
            if (S.Light == 1 && S.LightEffect == 0) {
                lock (applySync) {
                    try {
                        Light.SetColors(Scaled(LightColors));
                        Light.SetBacklight(true, S.LightLevel);
                    } catch { }
                }
            }
            OnChanged();
        }

        public static double SpeedFactor(int speed) {
            return new[] { 0.35, 0.6, 1.0, 1.6, 2.4 }[Math.Max(0, Math.Min(4, speed - 1))];
        }

        /// <summary>Where a zone sits in the wave animation.</summary>
        static double WaveOffset(int zones, int i) {
            if (zones == 4) {
                double pos = i == KeyboardLayouts.ZoneLeft ? 0 : i == KeyboardLayouts.ZoneWasd ? 0.35 : i == KeyboardLayouts.ZoneMiddle ? 1 : 2;
                return -pos * 70;
            }
            return i * (360.0 / Math.Max(1, zones));
        }

        /// <summary>Calculates colour of zone i at a given phase for an effect.</summary>
        public static Rgb EffectFrame(int effect, double phase, Rgb[] baseColors, int i) {
            switch (effect) {
                case 1: // Breathing: pulses base colours
                    double pulse = 0.15 + 0.85 * (0.5 + 0.5 * Math.Sin(phase * 1.6));
                    return baseColors[i].Scale(pulse);
                case 2: // Cycle / Rainbow: all zones through spectrum
                    return Rgb.FromHue(phase * 25);
                case 3: // Wave: zones offset across spectrum
                    return Rgb.FromHue(phase * 25 + WaveOffset(baseColors.Length, i));
                default:
                    return baseColors[i];
            }
        }

        void StartEffect() {
            fxFailures = 0;
            if (fxTimer == null)
                fxTimer = new System.Threading.Timer(delegate { EffectTick(); }, null, 120, 120);
            else
                fxTimer.Change(120, 120);
        }

        void StopEffect() {
            if (fxTimer != null)
                fxTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        void EffectTick() {
            if (Light == null || S.Light != 1 || S.LightEffect == 0) return;
            if (!Monitor.TryEnter(applySync, 50)) return;
            try {
                fxPhase += 0.12 * SpeedFactor(S.LightSpeed);
                var frame = new Rgb[LightColors.Length];
                for (int i = 0; i < frame.Length; i++)
                    frame[i] = EffectFrame(S.LightEffect, fxPhase, LightColors, i);

                try {
                    var scaled = Scaled(frame);
                    Light.SetColors(scaled);
                    fxFailures = 0;
                    var fh = FrameChanged;
                    if (fh != null) fh(scaled);
                } catch (Exception ex) {
                    if (++fxFailures >= 5) {
                        Log.Write("effect halted due to consecutive errors: " + ex.Message);
                        StopEffect();
                    }
                }
            } finally {
                Monitor.Exit(applySync);
            }
        }

        void OnChanged() {
            var h = Changed;
            if (h != null) h();
        }
    }
}
