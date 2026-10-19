// SPDX-License-Identifier: GPL-3.0-or-later
// OMEN 4-Zone RGB: Standalone window implementation.
// Pure 4-Zone RGB Keyboard Lighting interface for HP Omen 16 (am0279tx).
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ohman {

    public partial class MainWindow : Window {
        public readonly Engine E;
        readonly KeyboardView kbdView;
        readonly Seg modeSeg;
        readonly StripPicker hueBar;
        readonly StripPicker shadeBar;

        int selectedZone = KeyboardLayouts.ZoneWasd; // default selection is WASD
        bool allZonesSelected = false;
        Rgb curColor = new Rgb(0xFF, 0x00, 0x55);
        bool syncing;

        static readonly Rgb[] PresetColors = new[] {
            new Rgb(0xFF, 0x00, 0x44), // OMEN Red / Crimson
            new Rgb(0xFF, 0x55, 0x00), // Orange
            new Rgb(0xFF, 0xCC, 0x00), // Amber / Yellow
            new Rgb(0x00, 0xE6, 0x76), // Green
            new Rgb(0x00, 0xEE, 0xFF), // Cyan
            new Rgb(0x29, 0x79, 0xFF), // Blue
            new Rgb(0x99, 0x00, 0xFF), // Purple
            new Rgb(0xFF, 0x00, 0xCC), // Magenta / Pink
            new Rgb(0xFF, 0xFF, 0xFF)  // Pure White
        };

        public MainWindow(Engine engine) {
            E = engine;
            InitializeComponent();

            // Set up Keyboard visualizer
            kbdView = new KeyboardView();
            var layout = KeyboardLayouts.Build(true, 4);
            kbdView.SetLayout(layout);
            kbdView.SetColors(E.LightColors, false);
            KbdHost.Child = kbdView;

            kbdView.KeyClicked += delegate(KeyDef k) {
                if (k != null) {
                    SelectZone(k.Zone);
                }
            };

            // Set up Mode segmented control
            modeSeg = new Seg(Choice.Modes);
            ModeSegHost.Child = modeSeg;
            modeSeg.Picked += delegate(int idx) {
                int lightMode, effect;
                Choice.ToEngine(idx, out lightMode, out effect);
                E.SetLightModeAndEffect(lightMode, effect);
                UpdateModeUi();
            };

            // Set up Color Strips
            hueBar = new StripPicker { Cells = 36 };
            HueHost.Child = hueBar;
            shadeBar = new StripPicker { Cells = 30, Shade = true };
            ShadeHost.Child = shadeBar;

            hueBar.Picked += delegate(Rgb c) { ApplyColor(c, false); };
            shadeBar.Picked += delegate(Rgb c) { ApplyColor(c, false); };

            // Build Preset Palette
            foreach (var pc in PresetColors) {
                var c = pc;
                var chip = new Border {
                    Width = 22,
                    Height = 22,
                    CornerRadius = new CornerRadius(11),
                    Background = Ui.Brush(c),
                    BorderBrush = Ui.LineHi,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(3, 0, 3, 0),
                    Cursor = Cursors.Hand
                };
                chip.MouseLeftButtonUp += delegate { ApplyColor(c, false); };
                PresetPalette.Children.Add(chip);
            }

            // Hex Input
            TxtHexInput.KeyDown += delegate(object s, KeyEventArgs e) {
                if (e.Key == Key.Enter) {
                    Rgb c;
                    if (Rgb.TryParse(TxtHexInput.Text, out c)) {
                        ApplyColor(c, true);
                    }
                }
            };
            TxtHexInput.LostFocus += delegate {
                Rgb c;
                if (Rgb.TryParse(TxtHexInput.Text, out c)) {
                    ApplyColor(c, true);
                }
            };

            // Zone Selection Cards
            BtnZoneWasd.MouseLeftButtonUp += delegate { SelectZone(KeyboardLayouts.ZoneWasd); };
            BtnZoneLeft.MouseLeftButtonUp += delegate { SelectZone(KeyboardLayouts.ZoneLeft); };
            BtnZoneMiddle.MouseLeftButtonUp += delegate { SelectZone(KeyboardLayouts.ZoneMiddle); };
            BtnZoneRight.MouseLeftButtonUp += delegate { SelectZone(KeyboardLayouts.ZoneRight); };
            BtnZoneAll.MouseLeftButtonUp += delegate { SelectAllZones(); };

            // Sliders
            SlBrightness.ValueChanged += delegate {
                if (syncing) return;
                int val = (int)SlBrightness.Value;
                TxtBrightnessVal.Text = val + "%";
                E.SetBrightness(val);
                kbdView.Level = val / 100.0;
                kbdView.Repaint();
            };

            SlSpeed.ValueChanged += delegate {
                if (syncing) return;
                int val = (int)SlSpeed.Value;
                TxtSpeedVal.Text = val.ToString(CultureInfo.InvariantCulture);
                E.SetSpeed(val);
            };

            // Buttons
            BtnMinimizeTrayTop.Click += delegate { HideToTray(); };
            BtnMinimizeTray.Click += delegate { HideToTray(); };
            BtnOffToggle.Click += delegate { TogglePower(); };

            // Engine Frame Changed (Live Animation)
            E.FrameChanged += delegate(Rgb[] frame) {
                Dispatcher.BeginInvoke((Action)delegate {
                    if (IsVisible) kbdView.SetColors(frame, true);
                });
            };

            E.Changed += delegate {
                Dispatcher.BeginInvoke((Action)delegate {
                    RefreshAll();
                });
            };

            // Status indication
            if (E.Hw.IsDemo) {
                DotStatus.Fill = Ui.Brush(Ui.Warn);
                TxtStatus.Text = "Simulated Hardware (Preview Mode)";
            } else {
                DotStatus.Fill = Ui.Brush(Ui.Ok);
                TxtStatus.Text = "Connected to HP Firmware (4 Zones)";
            }

            SelectZone(KeyboardLayouts.ZoneWasd);
            RefreshAll();
        }

        void SelectZone(int zone) {
            allZonesSelected = false;
            selectedZone = Math.Max(0, Math.Min(3, zone));

            // Highlight corresponding zone keys on keyboard view
            kbdView.Selected.Clear();
            foreach (var k in kbdView.Keys) {
                if (k.Zone == selectedZone)
                    kbdView.Selected.Add(k.Index);
            }
            kbdView.Repaint();

            UpdateZoneCardHighlights();

            TxtSelectedInfo.Text = "Selected: " + KeyboardLayouts.ZoneName(4, selectedZone);

            if (selectedZone < E.LightColors.Length) {
                curColor = E.LightColors[selectedZone];
                SyncColorControls(false);
            }
        }

        void SelectAllZones() {
            allZonesSelected = true;

            kbdView.Selected.Clear();
            foreach (var k in kbdView.Keys)
                kbdView.Selected.Add(k.Index);
            kbdView.Repaint();

            UpdateZoneCardHighlights();
            TxtSelectedInfo.Text = "Selected: All 4 Zones";
        }

        void UpdateZoneCardHighlights() {
            var activeBrush = Ui.Accent;
            var inactiveBrush = Ui.Line;

            BtnZoneWasd.BorderBrush = (!allZonesSelected && selectedZone == KeyboardLayouts.ZoneWasd) ? activeBrush : inactiveBrush;
            BtnZoneLeft.BorderBrush = (!allZonesSelected && selectedZone == KeyboardLayouts.ZoneLeft) ? activeBrush : inactiveBrush;
            BtnZoneMiddle.BorderBrush = (!allZonesSelected && selectedZone == KeyboardLayouts.ZoneMiddle) ? activeBrush : inactiveBrush;
            BtnZoneRight.BorderBrush = (!allZonesSelected && selectedZone == KeyboardLayouts.ZoneRight) ? activeBrush : inactiveBrush;
            BtnZoneAll.BorderBrush = allZonesSelected ? activeBrush : inactiveBrush;

            BtnZoneWasd.Background = (!allZonesSelected && selectedZone == KeyboardLayouts.ZoneWasd) ? Ui.Pill : Ui.Card;
            BtnZoneLeft.Background = (!allZonesSelected && selectedZone == KeyboardLayouts.ZoneLeft) ? Ui.Pill : Ui.Card;
            BtnZoneMiddle.Background = (!allZonesSelected && selectedZone == KeyboardLayouts.ZoneMiddle) ? Ui.Pill : Ui.Card;
            BtnZoneRight.Background = (!allZonesSelected && selectedZone == KeyboardLayouts.ZoneRight) ? Ui.Pill : Ui.Card;
            BtnZoneAll.Background = allZonesSelected ? Ui.Pill : Ui.Card;
        }

        void ApplyColor(Rgb c, bool fromHex) {
            curColor = c;
            if (allZonesSelected) {
                E.SetAllZonesColor(c);
            } else {
                E.SetZoneColor(selectedZone, c);
            }

            SyncColorControls(fromHex);
            UpdateZoneSwatches();
            if (E.S.Light == 1 && E.S.LightEffect == 0) {
                kbdView.SetColors(E.LightColors, false);
            }
        }

        void SyncColorControls(bool fromHex) {
            HexChip.Background = Ui.Brush(curColor);
            if (!fromHex)
                TxtHexInput.Text = "#" + curColor.Hex;

            hueBar.Current = curColor;
            hueBar.HasCurrent = true;
            hueBar.Repaint();

            double h, s, v;
            curColor.ToHsv(out h, out s, out v);
            shadeBar.Hue = h;
            shadeBar.Current = curColor;
            shadeBar.HasCurrent = true;
            shadeBar.Repaint();
        }

        void UpdateZoneSwatches() {
            if (E.LightColors.Length >= 4) {
                SwatchRight.Background = Ui.Brush(E.LightColors[KeyboardLayouts.ZoneRight]);
                TxtHexRight.Text = "#" + E.LightColors[KeyboardLayouts.ZoneRight].Hex;

                SwatchMiddle.Background = Ui.Brush(E.LightColors[KeyboardLayouts.ZoneMiddle]);
                TxtHexMiddle.Text = "#" + E.LightColors[KeyboardLayouts.ZoneMiddle].Hex;

                SwatchLeft.Background = Ui.Brush(E.LightColors[KeyboardLayouts.ZoneLeft]);
                TxtHexLeft.Text = "#" + E.LightColors[KeyboardLayouts.ZoneLeft].Hex;

                SwatchWasd.Background = Ui.Brush(E.LightColors[KeyboardLayouts.ZoneWasd]);
                TxtHexWasd.Text = "#" + E.LightColors[KeyboardLayouts.ZoneWasd].Hex;
            }
        }

        void UpdateModeUi() {
            int uiMode = Choice.FromEngine(E.S.Light, E.S.LightEffect);
            modeSeg.Select(uiMode, false);

            bool isLit = E.S.Light == 1;
            bool isCustomColorMode = isLit && (E.S.LightEffect == 0 || E.S.LightEffect == 1); // Static or Breathe
            bool hasSpeed = isLit && E.S.LightEffect != 0;                                    // Breathe, Wave, Rainbow

            ColorCard.Opacity = isCustomColorMode ? 1.0 : 0.35;
            ColorCard.IsEnabled = isCustomColorMode;

            SlSpeed.IsEnabled = hasSpeed;
            SpeedHeader.Opacity = hasSpeed ? 1.0 : 0.45;
            TxtSpeedHint.Visibility = hasSpeed ? Visibility.Visible : Visibility.Hidden;

            kbdView.Off = !isLit;
            kbdView.Smooth = (E.S.LightEffect == 3); // Smooth wave blend across zones
            kbdView.Repaint();

            BtnOffToggle.Content = isLit ? "Turn Off Backlight" : "Turn On Backlight";
        }

        void TogglePower() {
            if (E.S.Light == 1) {
                E.SetLightModeAndEffect(0, 0);
            } else {
                E.SetLightModeAndEffect(1, 0); // Restore static
            }
            UpdateModeUi();
        }

        public void RefreshAll() {
            syncing = true;
            try {
                SlBrightness.Value = E.S.LightLevel;
                TxtBrightnessVal.Text = E.S.LightLevel + "%";
                kbdView.Level = E.S.LightLevel / 100.0;

                SlSpeed.Value = E.S.LightSpeed;
                TxtSpeedVal.Text = E.S.LightSpeed.ToString(CultureInfo.InvariantCulture);

                UpdateZoneSwatches();
                UpdateModeUi();

                if (E.S.Light == 1 && E.S.LightEffect == 0) {
                    kbdView.SetColors(E.LightColors, false);
                }
            } finally {
                syncing = false;
            }
        }

        public void HideToTray() {
            Hide();
            Program.NotifyHiddenToTray();
        }

        public void ShowFromTray() {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        protected override void OnStateChanged(EventArgs e) {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized) {
                HideToTray();
            }
        }

        protected override void OnClosing(CancelEventArgs e) {
            // When closing window, minimize to tray rather than killing background lighting
            e.Cancel = true;
            HideToTray();
        }
    }
}
