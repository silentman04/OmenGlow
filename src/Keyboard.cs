// SPDX-License-Identifier: GPL-3.0-or-later
// OMEN 4-Zone RGB: keyboard rendering and layout definition.
// Flat coloured keys mapped to HP's 4-zone layout:
// Zone 0 = Right / Numpad, Zone 1 = Middle, Zone 2 = Left, Zone 3 = WASD.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ohman {

    public sealed class KeyDef {
        public string Label;
        public double X, Y, W, H = 1;
        public int Zone;
        public int Index;
        public bool OnNumpad;
        public int Row { get { return (int)Y; } }
    }

    public static class KeyboardLayouts {
        public const int ZoneRight = 0, ZoneMiddle = 1, ZoneLeft = 2, ZoneWasd = 3;

        /// <summary>Order of display: Zone 1 (WASD), Zone 2 (Left), Zone 3 (Middle), Zone 4 (Right/Numpad).</summary>
        public static int[] DisplayOrder(int zones) {
            return zones == 4 ? new[] { ZoneWasd, ZoneLeft, ZoneMiddle, ZoneRight } : new[] { 0 };
        }

        public static string ZoneName(int zones, int zone) {
            if (zones != 4) return "Keyboard";
            switch (zone) {
                case ZoneWasd: return "WASD (Zone 1)";
                case ZoneLeft: return "Left (Zone 2)";
                case ZoneMiddle: return "Middle (Zone 3)";
                case ZoneRight: return "Right / Numpad (Zone 4)";
                default: return "Zone " + zone;
            }
        }

        public static string ShortZoneName(int zone) {
            switch (zone) {
                case ZoneWasd: return "WASD";
                case ZoneLeft: return "Left";
                case ZoneMiddle: return "Middle";
                case ZoneRight: return "Right";
                default: return "Z" + zone;
            }
        }

        /// <summary>Builds the key layout for 4-zone keyboard.</summary>
        public static List<KeyDef> Build(bool numpad, int zones) {
            var keys = new List<KeyDef>();
            string[][] rows = {
                new[] { "Esc", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "Del" },
                new[] { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Bksp:2.0" },
                new[] { "Tab:1.5", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\:1.5" },
                new[] { "Caps:1.8", "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter:2.2" },
                new[] { "Shift:2.3", "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "Shift:2.7" },
                new[] { "Ctrl:1.3", "Fn", "Win", "Alt:1.3", " :6.0", "Alt:1.3", "Ctrl:1.3", "◀", "▲▼", "▶" }
            };

            double y = 0;
            int idx = 0;
            foreach (var row in rows) {
                double x = 0;
                foreach (string spec in row) {
                    string label = spec;
                    double w = 1;
                    int c = spec.LastIndexOf(':');
                    if (c > 0) {
                        label = spec.Substring(0, c);
                        w = double.Parse(spec.Substring(c + 1), CultureInfo.InvariantCulture);
                    }
                    keys.Add(new KeyDef { Label = label.Trim(), X = x, Y = y, W = w, H = 1, Index = idx++ });
                    x += w;
                }
                y += 1;
            }

            if (numpad) {
                double nx = 15.3;
                string[][] np = {
                    new[] { "Num", "/", "*", "-" },
                    new[] { "7", "8", "9", "+" },
                    new[] { "4", "5", "6", "" },
                    new[] { "1", "2", "3", "Ent" },
                    new[] { "0:2", ".", "" }
                };
                double ny = 1;
                foreach (var row in np) {
                    double x = nx;
                    foreach (string spec in row) {
                        string label = spec;
                        double w = 1;
                        int c = spec.LastIndexOf(':');
                        if (c > 0) {
                            label = spec.Substring(0, c);
                            w = double.Parse(spec.Substring(c + 1), CultureInfo.InvariantCulture);
                        }
                        if (label.Length > 0)
                            keys.Add(new KeyDef { Label = label, X = x, Y = ny, W = w, H = 1, Index = idx++, OnNumpad = true });
                        x += w;
                    }
                    ny += 1;
                }
            }

            foreach (var k in keys) {
                if (zones != 4) { k.Zone = 0; continue; }
                double mid = k.X + k.W / 2;
                bool wasd = k.Label == "W" || k.Label == "A" || k.Label == "S" || k.Label == "D";
                double rightFrom = k.Row == 5 ? 11.4 : 13.25;
                k.Zone = wasd ? ZoneWasd : mid < 5.9 ? ZoneLeft : mid < rightFrom ? ZoneMiddle : ZoneRight;
            }
            return keys;
        }
    }

    /// <summary>Visualizer control rendering flat keys in zone colours with live animation support.</summary>
    public sealed class KeyboardView : FrameworkElement {
        public List<KeyDef> Keys = new List<KeyDef>();
        public bool Interactive = true, Selectable = true, Off;
        public bool Smooth;                                       // Wave effect: blend between zone colours
        public double Level = 1.0;                                // Brightness scale
        public double Gap = 4, RowPitch = 34;
        public readonly HashSet<int> Selected = new HashSet<int>();
        public readonly HashSet<int> Hover = new HashSet<int>();

        public event Action<KeyDef> KeyClicked;
        public event Action<KeyDef> KeyHovered;

        Rgb[] shown = new Rgb[0], target = new Rgb[0];
        bool animating;
        int zones = 4;
        double unitsW = 19.5, unitsH = 6;
        static Typeface face;
        static readonly Dictionary<uint, SolidColorBrush> Brushes_ = new Dictionary<uint, SolidColorBrush>();
        static readonly Color OffCap = Color.FromRgb(0x1F, 0x1C, 0x1A), OffLegend = Color.FromRgb(0x84, 0x7F, 0x7B),
            Card = Color.FromRgb(0x16, 0x13, 0x11);

        public void SetColors(Rgb[] c, bool animate) {
            if (c == null) c = new Rgb[0];
            if (!animate || shown.Length != c.Length) {
                shown = (Rgb[])c.Clone();
                target = (Rgb[])c.Clone();
                StopAnim();
                InvalidateVisual();
                return;
            }
            target = (Rgb[])c.Clone();
            if (!animating) {
                animating = true;
                CompositionTarget.Rendering += Tick;
            }
        }

        void Tick(object o, EventArgs e) {
            bool done = true;
            for (int i = 0; i < shown.Length; i++) {
                var a = shown[i];
                var b = target[i];
                shown[i] = new Rgb(Ease(a.R, b.R), Ease(a.G, b.G), Ease(a.B, b.B));
                if (shown[i].R != b.R || shown[i].G != b.G || shown[i].B != b.B) done = false;
            }
            InvalidateVisual();
            if (done) StopAnim();
        }

        static byte Ease(byte a, byte b) {
            int d = b - a;
            if (Math.Abs(d) <= 1) return b;
            return (byte)(a + d * 0.35);
        }

        void StopAnim() {
            if (animating) {
                animating = false;
                CompositionTarget.Rendering -= Tick;
            }
        }

        public void SetLayout(List<KeyDef> keys) {
            Keys = keys;
            unitsW = 0;
            unitsH = 0;
            zones = 1;
            foreach (var k in keys) {
                unitsW = Math.Max(unitsW, k.X + k.W);
                unitsH = Math.Max(unitsH, k.Y + k.H);
                zones = Math.Max(zones, k.Zone + 1);
            }
            InvalidateMeasure();
            InvalidateVisual();
        }

        public void Repaint() { InvalidateVisual(); }

        protected override Size MeasureOverride(Size a) {
            double w = double.IsInfinity(a.Width) ? 640 : a.Width;
            return new Size(w, unitsH * RowPitch - Gap);
        }

        double Unit { get { return (ActualWidth + Gap) / (unitsW > 0 ? unitsW : 19.5); } }
        Rect KeyRect(KeyDef k) {
            double u = Unit;
            return new Rect(k.X * u, k.Y * RowPitch, Math.Max(1, k.W * u - Gap), Math.Max(1, k.H * RowPitch - Gap));
        }

        static SolidColorBrush B(Color c) {
            uint key = (uint)(c.A << 24 | c.R << 16 | c.G << 8 | c.B);
            SolidColorBrush b;
            if (Brushes_.TryGetValue(key, out b)) return b;
            if (Brushes_.Count > 1024) Brushes_.Clear();
            b = new SolidColorBrush(c);
            b.Freeze();
            Brushes_[key] = b;
            return b;
        }

        static Color WithA(Color c, double a) { return Color.FromArgb((byte)Math.Round(Math.Max(0, Math.Min(1, a)) * 255), c.R, c.G, c.B); }
        static Color Scale(Color c, double f) { f = Math.Max(0, Math.Min(1, f)); return Color.FromRgb((byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f)); }
        static Color ToColor(Rgb c) { return Color.FromRgb(c.R, c.G, c.B); }
        static Color Mix(Color a, Color b, double t) {
            t = Math.Max(0, Math.Min(1, t));
            return Color.FromRgb((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));
        }

        Color ZoneColor(KeyDef k) {
            if (shown.Length == 0) return OffCap;
            if (Smooth && shown.Length >= 4) return SmoothColor(k);
            return ToColor(shown[Math.Min(shown.Length - 1, Math.Max(0, k.Zone))]);
        }

        Color SmoothColor(KeyDef k) {
            if (zones != 4 || shown.Length < 4) return ToColor(shown[Math.Min(shown.Length - 1, Math.Max(0, k.Zone))]);
            if (k.Zone == KeyboardLayouts.ZoneWasd) return ToColor(shown[KeyboardLayouts.ZoneWasd]);
            int[] order = { KeyboardLayouts.ZoneLeft, KeyboardLayouts.ZoneMiddle, KeyboardLayouts.ZoneRight };
            const int n = 3;
            double t = (k.X + k.W / 2) / unitsW * n - 0.5;
            int i0 = (int)Math.Floor(t), i1 = i0 + 1;
            double f = t - i0;
            i0 = Math.Max(0, Math.Min(n - 1, i0));
            i1 = Math.Max(0, Math.Min(n - 1, i1));
            return Mix(ToColor(shown[order[i0]]), ToColor(shown[order[i1]]), f);
        }

        static double Luma(Color c) { return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0; }

        protected override void OnRender(DrawingContext dc) {
            if (face == null) face = new Typeface(new FontFamily("Segoe UI, Consolas"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
            bool lit = !Off && shown.Length > 0;
            double lv = 0.35 + 0.65 * Math.Max(0, Math.Min(1, Level));
            double radius = 4;
            bool anyHover = Interactive && Hover.Count > 0, anySel = Interactive && Selected.Count > 0;

            foreach (var k in Keys) {
                var r = KeyRect(k);
                bool sel = anySel && Selected.Contains(k.Index), hov = anyHover && Hover.Contains(k.Index);
                Color fill, legend;
                if (lit) {
                    fill = Scale(ZoneColor(k), lv);
                    legend = Luma(fill) > 0.62 ? Color.FromArgb(0x80, 0, 0, 0) : Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF);
                } else {
                    fill = OffCap;
                    legend = OffLegend;
                }

                if (anyHover && !hov && !sel) {
                    fill = Mix(Card, fill, 0.32);
                    legend = WithA(legend, 0.45);
                }

                dc.DrawRoundedRectangle(B(fill), null, r, radius, radius);

                if (sel)
                    dc.DrawRoundedRectangle(null, new Pen(B(Color.FromRgb(0xFC, 0xFA, 0xF9)), 2), new Rect(r.X + 1, r.Y + 1, Math.Max(0, r.Width - 2), Math.Max(0, r.Height - 2)), radius, radius);
                else if (hov)
                    dc.DrawRoundedRectangle(null, new Pen(B(WithA(Colors.White, 0.5)), 2), new Rect(r.X + 1, r.Y + 1, Math.Max(0, r.Width - 2), Math.Max(0, r.Height - 2)), radius, radius);
                else
                    dc.DrawRoundedRectangle(null, new Pen(B(WithA(Colors.Black, 0.22)), 1), new Rect(r.X + 0.5, r.Y + 0.5, Math.Max(0, r.Width - 1), Math.Max(0, r.Height - 1)), radius, radius);

                string label = k.Label.Trim();
                if (label.Length > 0 && r.Width >= 14) {
                    var ft = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, 9.5, B(legend), 1.0);
                    dc.DrawText(ft, new Point(r.X + (r.Width - ft.Width) / 2, r.Y + (r.Height - ft.Height) / 2));
                }
            }
        }

        const double Snap = 11;

        KeyDef Hit(Point p) {
            KeyDef best = null;
            double bestD = double.MaxValue;
            foreach (var k in Keys) {
                Rect r = KeyRect(k);
                if (r.Contains(p)) return k;
                double dx = p.X < r.Left ? r.Left - p.X : p.X > r.Right ? p.X - r.Right : 0;
                double dy = p.Y < r.Top ? r.Top - p.Y : p.Y > r.Bottom ? p.Y - r.Bottom : 0;
                double d = dx * dx + dy * dy;
                if (d < bestD) { bestD = d; best = k; }
            }
            return bestD <= Snap * Snap ? best : null;
        }

        KeyDef hovered;
        protected override void OnMouseMove(MouseEventArgs e) {
            if (!Interactive || !Selectable) return;
            var k = Hit(e.GetPosition(this));
            if (k == hovered) return;
            hovered = k;
            Cursor = k == null ? Cursors.Arrow : Cursors.Hand;
            var h = KeyHovered;
            if (h != null) h(k);
        }

        protected override void OnMouseLeave(MouseEventArgs e) {
            if (!Interactive || hovered == null) return;
            hovered = null;
            var h = KeyHovered;
            if (h != null) h(null);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            if (!Interactive || !Selectable) return;
            var k = Hit(e.GetPosition(this));
            var h = KeyClicked;
            if (h != null) h(k);
        }
    }
}
