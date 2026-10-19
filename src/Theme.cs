// SPDX-License-Identifier: GPL-3.0-or-later
// OMEN 4-Zone RGB: styling, palette, animation curves and brushes.
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ohman {

    static class Ui {
        public static SolidColorBrush Brush(string hex) { var b = new SolidColorBrush(Col(hex)); b.Freeze(); return b; }
        public static SolidColorBrush Brush(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
        public static SolidColorBrush Brush(Rgb c) { return Brush(Color.FromRgb(c.R, c.G, c.B)); }
        public static Color Col(string hex) { return (Color)ColorConverter.ConvertFromString(hex); }

        public static readonly Color OmenRed = Col("#FF0055"), AccentColor = Col("#FF2D60"), NeonBlue = Col("#00E5FF");
        public static readonly Color Warn = Col("#F3821D"), Danger = Col("#FF5C5C"), Ok = Col("#4AC06C");

        public static Color Mix(Color c, Color towards, double t) {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromRgb((byte)(c.R + (towards.R - c.R) * t), (byte)(c.G + (towards.G - c.G) * t), (byte)(c.B + (towards.B - c.B) * t));
        }

        public static FontFamily UiFont = new FontFamily("Segoe UI, IBM Plex Sans, Arial");
        public static FontFamily MonoFont = new FontFamily("Consolas, IBM Plex Mono, Courier New");

        public static readonly Brush Card = Brush("#161311"), Sunken = Brush("#0D0B0A"), Pill = Brush("#201C19"), Line = Brush("#2C2825"),
            LineHi = Brush("#3D3733"),
            TextB = Brush("#EDEAE8"), TextHi = Brush("#F4F1EF"), SegText = Brush("#A8A3A0"), Hex = Brush("#A29D99"),
            Sub = Brush("#96918D"), Desc = Brush("#908B87"), Status = Brush("#8A8581"), Axis = Brush("#847F7B"),
            Foot = Brush("#7E7976"), Section = Brush("#787370");

        public static SolidColorBrush Accent = new SolidColorBrush(AccentColor);

        public static void Glide(DependencyObject o, DependencyProperty p, double to, int ms, bool spring) {
            var a = (IAnimatable)o;
            double from = (double)o.GetValue(p);
            if (double.IsNaN(from) || Math.Abs(from - to) < 0.5) { a.BeginAnimation(p, null); o.SetValue(p, to); return; }
            o.SetValue(p, to);
            var an = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(ms)) { FillBehavior = FillBehavior.Stop };
            an.EasingFunction = spring ? (IEasingFunction)new SpringEase() : new CubicEase { EasingMode = EasingMode.EaseOut };
            a.BeginAnimation(p, an);
        }

        public static void Fade(UIElement e, double to, int ms) {
            e.BeginAnimation(UIElement.OpacityProperty, null);
            double from = e.Opacity;
            e.Opacity = to;
            if (from == to) return;
            e.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(ms)) { FillBehavior = FillBehavior.Stop, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }

        public static Rgb Hsl(double h, double s, double l) {
            h = ((h % 360) + 360) % 360;
            double c = (1 - Math.Abs(2 * l - 1)) * s, x = c * (1 - Math.Abs((h / 60) % 2 - 1)), m = l - c / 2, r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return new Rgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }
    }

    /// <summary>Lighting mode choices.</summary>
    static class Choice {
        // Modes: Static, Breathing, Wave, Cycle/Rainbow, Off
        public static readonly string[] Modes = { "Static", "Breathing", "Wave", "Rainbow", "Off" };

        public const int ModeStatic = 0;
        public const int ModeBreathing = 1;
        public const int ModeWave = 2;
        public const int ModeRainbow = 3;
        public const int ModeOff = 4;

        /// <summary>Converts UI mode index (0..4) to Engine mode (0=off, 1=on) and effect (0=static, 1=breathe, 2=cycle, 3=wave).</summary>
        public static void ToEngine(int uiMode, out int lightMode, out int effect) {
            switch (uiMode) {
                case ModeStatic:
                    lightMode = 1; effect = 0; break;
                case ModeBreathing:
                    lightMode = 1; effect = 1; break;
                case ModeWave:
                    lightMode = 1; effect = 3; break;
                case ModeRainbow:
                    lightMode = 1; effect = 2; break;
                case ModeOff:
                default:
                    lightMode = 0; effect = 0; break;
            }
        }

        /// <summary>Converts Engine light mode and effect to UI mode index (0..4).</summary>
        public static int FromEngine(int lightMode, int effect) {
            if (lightMode <= 0) return ModeOff;
            switch (effect) {
                case 1: return ModeBreathing;
                case 2: return ModeRainbow;
                case 3: return ModeWave;
                case 0:
                default: return ModeStatic;
            }
        }
    }

    sealed class SpringEase : EasingFunctionBase {
        protected override Freezable CreateInstanceCore() { return new SpringEase(); }
        protected override double EaseInCore(double t) {
            // under-damped spring
            const double a = 6.5, b = 5.5;
            return 1 - Math.Exp(-a * t) * (Math.Cos(b * t) + a / b * Math.Sin(b * t));
        }
    }

    public static class ThemeManager {
        public static bool IsDark { get; private set; } = true;

        public static void Apply(Window win, KeyboardView kbd, Seg seg, bool dark) {
            IsDark = dark;
            var res = win.Resources;

            if (dark) {
                // Dark OMEN Palette
                res["WindowBg"] = Ui.Brush("#161311");
                res["Card"] = Ui.Brush("#1C1816");
                res["Sunken"] = Ui.Brush("#0D0B0A");
                res["Pill"] = Ui.Brush("#25201D");
                res["Line"] = Ui.Brush("#2A2522");
                res["LineHi"] = Ui.Brush("#3D3733");
                res["Text"] = Ui.Brush("#EDEAE8");
                res["TextHi"] = Ui.Brush("#FFFFFF");
                res["Sub"] = Ui.Brush("#96918D");
                res["Desc"] = Ui.Brush("#807B77");
                res["SegText"] = Ui.Brush("#A8A3A0");
                res["Accent"] = Ui.Brush("#FF0055");
                res["AccentHi"] = Ui.Brush("#FF2D6E");
            } else {
                // Light Modern Palette
                res["WindowBg"] = Ui.Brush("#F4F3F6");
                res["Card"] = Ui.Brush("#FFFFFF");
                res["Sunken"] = Ui.Brush("#E8E6ED");
                res["Pill"] = Ui.Brush("#F9F8FA");
                res["Line"] = Ui.Brush("#D6D3DC");
                res["LineHi"] = Ui.Brush("#B8B4C2");
                res["Text"] = Ui.Brush("#1C1A21");
                res["TextHi"] = Ui.Brush("#0C0A10");
                res["Sub"] = Ui.Brush("#5E5A69");
                res["Desc"] = Ui.Brush("#726E7E");
                res["SegText"] = Ui.Brush("#555260");
                res["Accent"] = Ui.Brush("#E6004C");
                res["AccentHi"] = Ui.Brush("#FF1A66");
            }

            if (kbd != null) kbd.SetTheme(dark);
            if (seg != null) seg.SetTheme(dark);
        }
    }
}
