// SPDX-License-Identifier: GPL-3.0-or-later
// OMEN 4-Zone RGB: hardware communication layer.
// HP OMEN BIOS lighting control through the WMI class root\wmi:hpqBIntM (requires elevation).
// Scope Isolation: All fan speed, thermal telemetry, and power limit opcodes have been intentionally
// excluded to prevent any interference with HP OMEN Gaming Hub's thermal/fan management.
using System;
using System.Globalization;
using System.IO;
using System.Management;
using System.Text;

namespace Ohman {

    public static class Log {
        static readonly object Sync = new object();
        public static string Path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "omenrgb.log");

        public static void Write(string s) {
            try {
                lock (Sync) {
                    var fi = new FileInfo(Path);
                    if (fi.Exists && fi.Length > 1024 * 1024) {
                        string old = Path + ".1";
                        try { if (File.Exists(old)) File.Delete(old); fi.MoveTo(old); } catch { }
                    }
                    File.AppendAllText(Path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "  " + s + Environment.NewLine);
                }
            } catch { }
        }
    }

    public class BiosException : Exception {
        public readonly uint CommandType, ReturnCode;
        public BiosException(uint cmd, uint rc)
            : base(string.Format(CultureInfo.InvariantCulture, "WMI BIOS call failed (cmd 0x{0:X2}, rc {1})", cmd, rc)) {
            CommandType = cmd;
            ReturnCode = rc;
        }
    }

    public interface IHardware {
        bool IsDemo { get; }
    }

    /// <summary>Real BIOS access via HP WMI class root\wmi:hpqBIntM. Thread-safe.</summary>
    public sealed class Bios : IHardware {
        public const uint CMD_DEFAULT = 0x20008;                              // HPWMI_GM default command class
        static readonly byte[] SIGN = new byte[] { 0x53, 0x45, 0x43, 0x55 }; // "SECU"
        static readonly object Sync = new object();

        public bool IsDemo { get { return false; } }

        static ManagementObject cached;                       // cached hpqBIntM instance
        static ManagementObject Interface() {
            if (cached != null) return cached;
            using (var s = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM hpqBIntM")) {
                foreach (ManagementObject mo in s.Get()) { cached = mo; return mo; }
            }
            throw new InvalidOperationException("hpqBIntM WMI class has no instance (not an HP OMEN, or not elevated)");
        }

        public static string Hex(byte[] b, int max) {
            if (b == null) return "(null)";
            var sb = new StringBuilder();
            int n = Math.Min(b.Length, max);
            for (int i = 0; i < n; i++) { if (i > 0) sb.Append(' '); sb.Append(b[i].ToString("X2")); }
            if (b.Length > n) sb.Append(" ..");
            return sb.ToString();
        }

        /// <summary>Raw BIOS call using CMD_DEFAULT (0x20008).</summary>
        public static byte[] Call(uint commandType, byte[] data, int outSize) {
            return Call(CMD_DEFAULT, commandType, data, outSize);
        }

        /// <summary>Raw BIOS call to hpqBIntM. outSize must be 0, 4, 128, 1024 or 4096. Throws on rwReturnCode != 0.</summary>
        public static byte[] Call(uint command, uint commandType, byte[] data, int outSize) {
            if (data == null) data = new byte[0];
            lock (Sync) {
                try {
                    return CallLocked(command, commandType, data, outSize);
                } catch (ManagementException) {
                    try { if (cached != null) cached.Dispose(); } catch { }
                    cached = null;
                    throw;
                }
            }
        }

        static byte[] CallLocked(uint command, uint commandType, byte[] data, int outSize) {
            ManagementObject intf = Interface();
            using (var cls = new ManagementClass("root\\wmi", "hpqBDataIn", null))
            using (ManagementBaseObject din = cls.CreateInstance()) {
                din["Sign"] = SIGN;
                din["Command"] = command;
                din["CommandType"] = commandType;
                din["Size"] = (uint)data.Length;
                din["hpqBData"] = data;
                string method = "hpqBIOSInt" + outSize.ToString(CultureInfo.InvariantCulture);
                using (ManagementBaseObject inP = intf.GetMethodParameters(method)) {
                    inP["InData"] = din;
                    using (ManagementBaseObject outP = intf.InvokeMethod(method, inP, null))
                    using (var outD = (ManagementBaseObject)outP["OutData"]) {
                        uint rc = Convert.ToUInt32(outD["rwReturnCode"], CultureInfo.InvariantCulture);
                        if (rc != 0) throw new BiosException(commandType, rc);
                        if (outSize == 0) return new byte[0];
                        byte[] o = outD["Data"] as byte[];
                        return o ?? new byte[0];
                    }
                }
            }
        }

        public static string ReadBoard() {
            try {
                using (var s = new ManagementObjectSearcher("SELECT Product FROM Win32_BaseBoard"))
                    foreach (ManagementObject mo in s.Get()) return (mo["Product"] ?? "").ToString().Trim();
            } catch { }
            return "";
        }

        public static string ReadModel() {
            try {
                using (var s = new ManagementObjectSearcher("SELECT Model FROM Win32_ComputerSystem"))
                    foreach (ManagementObject mo in s.Get()) return (mo["Model"] ?? "").ToString().Trim();
            } catch { }
            return "";
        }

        public static bool IsDesktop() {
            try {
                using (var s = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure"))
                    foreach (ManagementObject mo in s.Get()) {
                        var types = mo["ChassisTypes"] as ushort[];
                        if (types != null && types.Length > 0) {
                            ushort t = types[0];
                            if (t == 3 || t == 4 || t == 5 || t == 6 || t == 7 || t == 13) return true;
                        }
                    }
            } catch { }
            return false;
        }
    }

    /// <summary>Simulated hardware for preview and testing without administrator elevation.</summary>
    public sealed class DemoHardware : IHardware {
        public bool IsDemo { get { return true; } }
    }
}
