// SPDX-License-Identifier: GPL-3.0-or-later
// OMEN 4-Zone RGB: UI controls (Color strip pickers, segmented controls).
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Ohman {

    /// <summary>A strip of colour cells: the hue row, or shades of one hue. Click or drag to pick.</summary>
    public sealed class StripPicker : FrameworkElement {
        public int Cells = 36;
        public bool Shade;
        public double Hue = 345;
        public Rgb Current;
        public bool HasCurrent;
        public event Action<Rgb> Picked;

        public StripPicker() {
            Cursor = Cursors.Cross;
            SnapsToDevicePixels = true;
        }

        protected override Size MeasureOverride(Size a) {
            return new Size(double.IsInfinity(a.Width) ? 200 : a.Width, 26);
        }

        public void Repaint() { InvalidateVisual(); }

        public Rgb ColorAt(int i) {
            if (!Shade) return Ui.Hsl(i * 360.0 / Cells, 0.88, 0.52);
            double t = Cells <= 1 ? 0 : i / (double)(Cells - 1);
            return Ui.Hsl(Hue, 0.28 + t * 0.72, 0.95 - t * 0.78);
        }

        int Nearest() {
            if (!HasCurrent) return -1;
            int best = -1;
            double bd = double.MaxValue;
            for (int i = 0; i < Cells; i++) {
                var c = ColorAt(i);
                double d = (c.R - Current.R) * (c.R - Current.R) + (c.G - Current.G) * (c.G - Current.G) + (c.B - Current.B) * (c.B - Current.B);
                if (d < bd) { bd = d; best = i; }
            }
            return bd <= 1200 ? best : -1;
        }

        protected override void OnRender(DrawingContext dc) {
            double w = ActualWidth / Cells, h = ActualHeight;
            for (int i = 0; i < Cells; i++)
                dc.DrawRectangle(Ui.Brush(ColorAt(i)), null, new Rect(i * w, 0, w + 0.7, h));
            int sel = Nearest();
            if (sel >= 0)
                dc.DrawRectangle(null, new Pen(Ui.Brush("#FCFAF9"), 2), new Rect(sel * w + 1, 1, Math.Max(1, w - 2), h - 2));
        }

        void Pick(Point p) {
            int i = Math.Max(0, Math.Min(Cells - 1, (int)(p.X / (ActualWidth / Cells))));
            var h = Picked;
            if (h != null) h(ColorAt(i));
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            CaptureMouse();
            Pick(e.GetPosition(this));
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed && IsMouseCaptured)
                Pick(e.GetPosition(this));
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
            if (IsMouseCaptured) ReleaseMouseCapture();
        }
    }

    /// <summary>A segmented button row with animated sliding selection indicator.</summary>
    public sealed class Seg : Border {
        readonly Grid grid = new Grid();
        readonly Panel cells;
        readonly Border pill;
        readonly TranslateTransform pillT = new TranslateTransform();
        readonly List<TextBlock> labels = new List<TextBlock>();
        readonly List<Border> items = new List<Border>();
        int sel = -1;
        public event Action<int> Picked;

        public Seg(string[] names) {
            cells = new UniformGrid { Rows = 1 };
            Background = Ui.Sunken;
            CornerRadius = new CornerRadius(8);
            Padding = new Thickness(3);

            pill = new Border {
                CornerRadius = new CornerRadius(6),
                Background = Ui.Accent,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 40,
                Opacity = 0,
                RenderTransform = pillT
            };

            grid.Children.Add(pill);
            grid.Children.Add(cells);
            Child = grid;

            for (int i = 0; i < names.Length; i++) {
                int idx = i;
                var tb = new TextBlock {
                    Text = names[i],
                    FontFamily = Ui.UiFont,
                    FontSize = 12.5,
                    Foreground = Ui.SegText,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var cell = new Border {
                    Background = Brushes.Transparent,
                    Cursor = Cursors.Hand,
                    Padding = new Thickness(10, 6, 10, 6),
                    CornerRadius = new CornerRadius(6),
                    Child = tb
                };
                cell.MouseLeftButtonUp += delegate {
                    if (idx == sel) return;
                    Select(idx, true);
                    var h = Picked;
                    if (h != null) h(idx);
                };
                cell.MouseEnter += delegate { if (idx != sel) tb.Foreground = Ui.TextB; };
                cell.MouseLeave += delegate { if (idx != sel) tb.Foreground = Ui.SegText; };

                labels.Add(tb);
                items.Add(cell);
                cells.Children.Add(cell);
            }
            cells.SizeChanged += delegate { Place(false); };
        }

        public void Select(int i, bool animate) {
            sel = i;
            for (int j = 0; j < labels.Count; j++) {
                labels[j].Foreground = j == i ? Brushes.White : (items[j].IsMouseOver ? Ui.TextB : Ui.SegText);
                labels[j].FontWeight = j == i ? FontWeights.SemiBold : FontWeights.Normal;
            }
            Place(animate);
        }

        void Place(bool animate) {
            if (sel < 0 || cells.ActualWidth <= 0 || items.Count == 0) {
                pill.Opacity = 0;
                return;
            }
            double x = 0, w = 0;
            if (sel < items.Count && items[sel].ActualWidth > 0) {
                x = items[sel].TranslatePoint(new Point(0, 0), cells).X;
                w = items[sel].ActualWidth;
            } else {
                w = cells.ActualWidth / items.Count;
                x = w * sel;
            }
            x = Math.Round(x);
            w = Math.Round(w);

            if (pill.Opacity == 0 || !animate) {
                pill.BeginAnimation(WidthProperty, null);
                pillT.BeginAnimation(TranslateTransform.XProperty, null);
                pill.Width = w;
                pillT.X = x;
                pill.Opacity = 1;
                return;
            }
            Ui.Glide(pill, WidthProperty, w, 220, true);
            Ui.Glide(pillT, TranslateTransform.XProperty, x, 220, true);
        }
    }
}
