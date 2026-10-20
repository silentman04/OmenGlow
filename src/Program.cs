// SPDX-License-Identifier: GPL-3.0-or-later
// OMEN 4-Zone RGB: Application entry point and system tray coordinator.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using WF = System.Windows.Forms;

namespace Ohman {

    public static class Program {
        public const string AppName = "OmenGlow";
        public const string DisplayName = "OmenGlow - 4-Zone RGB";
        public static readonly string Version = Meta.Version;

        static Mutex singleMutex;
        static EventWaitHandle showEvent;
        static WF.NotifyIcon trayIcon;
        static MainWindow mainWindow;
        static Engine engine;

        [STAThread]
        public static int Main(string[] args) {
            EnsurePowerSavingGpuPreference();

            bool demo = false;
            bool hidden = false;
            bool test = false;
            for (int i = 0; i < args.Length; i++) {
                string a = args[i].ToLowerInvariant();
                if (a == "--demo") demo = true;
                else if (a == "--hidden") hidden = true;
                else if (a == "--test") { test = true; demo = true; }
            }

            // Single instance protection
            bool createdNew;
            string mutexName = AppName + (demo ? "_Demo" : "") + "_Mutex";
            singleMutex = new Mutex(true, mutexName, out createdNew);

            string showEventName = AppName + (demo ? "_Demo" : "") + "_Show";
            if (!createdNew && !test) {
                try {
                    EventWaitHandle.OpenExisting(showEventName).Set();
                } catch { }
                return 0;
            }

            try {
                showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, showEventName);
                new Thread(WaitForShowSignal) { IsBackground = true }.Start();
            } catch { }

            bool elevated = false;
            try {
                elevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            } catch { }

            IHardware hw = (demo || !elevated) ? (IHardware)new DemoHardware() : new Bios();
            Log.Write(DisplayName + " " + Version + " starting (elevated=" + elevated + ", hardware=" + (hw.IsDemo ? "demo" : "bios") + ")");

            var settings = Settings.Load();
            if (settings.StartHidden) hidden = true;

            engine = new Engine(hw, settings);
            engine.Init();

            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.DispatcherUnhandledException += delegate(object o, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
                Log.Write("UI Exception: " + e.Exception);
                e.Handled = true;
            };

            mainWindow = new MainWindow(engine);

            if (test) {
                Log.Write("Self-test check succeeded. Exiting test run.");
                Console.WriteLine("Self-test check succeeded.");
                Cleanup();
                Environment.Exit(0);
                return 0;
            }

            // Initialize System Tray Icon
            SetupTrayIcon();

            if (!hidden) {
                mainWindow.Show();
            } else {
                MainWindow.TrimWorkingSet();
            }

            app.Run();

            Cleanup();
            return 0;
        }

        static void WaitForShowSignal() {
            while (true) {
                try {
                    if (showEvent.WaitOne()) {
                        Application.Current.Dispatcher.BeginInvoke((Action)delegate {
                            if (mainWindow != null) mainWindow.ShowFromTray();
                        });
                    }
                } catch {
                    break;
                }
            }
        }

        static void SetupTrayIcon() {
            try {
                trayIcon = new WF.NotifyIcon {
                    Text = DisplayName,
                    Visible = true,
                    Icon = CreateAppIcon()
                };

                var menu = new WF.ContextMenuStrip();

                var itemOpen = new WF.ToolStripMenuItem("Open " + DisplayName);
                itemOpen.Font = new Font(itemOpen.Font, System.Drawing.FontStyle.Bold);
                itemOpen.Click += delegate { mainWindow.ShowFromTray(); };
                menu.Items.Add(itemOpen);

                menu.Items.Add(new WF.ToolStripSeparator());

                // Mode submenu
                var modeMenu = new WF.ToolStripMenuItem("Lighting Mode");
                string[] modeNames = { "Static", "Breathing", "Wave", "Rainbow", "Off" };
                for (int i = 0; i < modeNames.Length; i++) {
                    int modeIdx = i;
                    var item = new WF.ToolStripMenuItem(modeNames[i]);
                    item.Click += delegate {
                        int lightMode, effect;
                        Choice.ToEngine(modeIdx, out lightMode, out effect);
                        engine.SetLightModeAndEffect(lightMode, effect);
                    };
                    modeMenu.DropDownItems.Add(item);
                }
                menu.Items.Add(modeMenu);

                // Brightness submenu
                var brightMenu = new WF.ToolStripMenuItem("Brightness");
                int[] levels = { 100, 75, 50, 25, 0 };
                foreach (int lv in levels) {
                    int bVal = lv;
                    var item = new WF.ToolStripMenuItem(bVal == 0 ? "Off (0%)" : bVal + "%");
                    item.Click += delegate { engine.SetBrightness(bVal); };
                    brightMenu.DropDownItems.Add(item);
                }
                menu.Items.Add(brightMenu);

                menu.Items.Add(new WF.ToolStripSeparator());

                var itemHide = new WF.ToolStripMenuItem("Minimize to Tray");
                itemHide.Click += delegate { mainWindow.HideToTray(); };
                menu.Items.Add(itemHide);

                menu.Items.Add(new WF.ToolStripSeparator());

                var itemStats = new WF.ToolStripMenuItem();
                itemStats.Enabled = false;
                itemStats.Font = new Font(itemStats.Font.FontFamily, 8.5f, System.Drawing.FontStyle.Regular);
                menu.Items.Add(itemStats);

                menu.Opening += delegate {
                    try {
                        if (mainWindow != null && !mainWindow.IsVisible) {
                            MainWindow.TrimWorkingSet();
                        }
                        var proc = System.Diagnostics.Process.GetCurrentProcess();
                        proc.Refresh();
                        double ramMb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 1);
                        string pwr = GetPowerStats();
                        itemStats.Text = $"⚡ {ramMb} MB RAM • iGPU • {pwr}";
                    } catch { }
                };

                var itemExit = new WF.ToolStripMenuItem("Exit");
                itemExit.Click += delegate { ExitApp(); };
                menu.Items.Add(itemExit);

                trayIcon.ContextMenuStrip = menu;

                trayIcon.DoubleClick += delegate { mainWindow.ShowFromTray(); };
                trayIcon.Click += delegate(object s, EventArgs e) {
                    var me = e as WF.MouseEventArgs;
                    if (me != null && me.Button == WF.MouseButtons.Left) {
                        mainWindow.ShowFromTray();
                    }
                };
            } catch (Exception ex) {
                Log.Write("SetupTrayIcon error: " + ex.Message);
            }
        }

        static Icon CreateAppIcon() {
            try {
                string icoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(icoPath)) {
                    return new Icon(icoPath, 32, 32);
                }
            } catch { }

            using (var bmp = new Bitmap(32, 32)) {
                using (var g = Graphics.FromImage(bmp)) {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    // Outer diamond (OMEN accent)
                    using (var b = new SolidBrush(Color.FromArgb(255, 0, 85))) {
                        var pts = new[] {
                            new PointF(16, 2),
                            new PointF(30, 16),
                            new PointF(16, 30),
                            new PointF(2, 16)
                        };
                        g.FillPolygon(b, pts);
                    }

                    // Inner dark diamond
                    using (var b = new SolidBrush(Color.FromArgb(22, 19, 17))) {
                        var pts = new[] {
                            new PointF(16, 9),
                            new PointF(23, 16),
                            new PointF(16, 23),
                            new PointF(9, 16)
                        };
                        g.FillPolygon(b, pts);
                    }
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        public static void NotifyHiddenToTray() {
            try {
                if (trayIcon != null) {
                    trayIcon.ShowBalloonTip(1500, DisplayName, DisplayName + " is active in the system tray.", WF.ToolTipIcon.Info);
                }
            } catch { }
        }

        public static void ExitApp() {
            Cleanup();
            Application.Current.Shutdown();
        }

        static void Cleanup() {
            try {
                if (engine != null) engine.Shutdown();
            } catch { }
            try {
                if (trayIcon != null) {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                    trayIcon = null;
                }
            } catch { }
            try {
                if (singleMutex != null) singleMutex.ReleaseMutex();
            } catch { }
        }

        static void EnsurePowerSavingGpuPreference() {
            try {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences")) {
                    if (key != null) {
                        string val = key.GetValue(exePath) as string;
                        if (string.IsNullOrEmpty(val) || !val.Contains("GpuPreference=1")) {
                            key.SetValue(exePath, "GpuPreference=1;");
                        }
                    }
                }
            } catch { }
        }

        static string GetPowerStats() {
            try {
                using (var searcher = new System.Management.ManagementObjectSearcher(@"root\WMI", "SELECT * FROM BatteryStatus")) {
                    foreach (System.Management.ManagementObject obj in searcher.Get()) {
                        bool online = (bool)obj["PowerOnline"];
                        bool discharging = (bool)obj["Discharging"];
                        bool charging = (bool)obj["Charging"];
                        int dRate = Convert.ToInt32(obj["DischargeRate"]);
                        int cRate = Convert.ToInt32(obj["ChargeRate"]);
                        int rem = Convert.ToInt32(obj["RemainingCapacity"]);

                        double remWh = rem / 1000.0;
                        double rateW = (discharging ? dRate : cRate) / 1000.0;

                        if (online) {
                            if (charging && rateW > 0.1) {
                                return $"AC (+{rateW:F1}W) • {remWh:F1}Wh";
                            }
                            return $"AC • {remWh:F1}Wh";
                        } else {
                            return $"{rateW:F1}W • {remWh:F1}Wh left";
                        }
                    }
                }
            } catch { }
            return "iGPU";
        }
    }
}
