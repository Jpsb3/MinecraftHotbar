using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MinecraftHotbar
{
    /// <summary>
    /// Lee el estado del PC para el HUD: RAM (corazones), batería o disco (comida) y CPU (nivel).
    /// Todo sale de funciones de Windows muy livianas; se llama cada 2 segundos.
    /// </summary>
    internal class SystemStats
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out long idleTime, out long kernelTime, out long userTime);

        private long _lastIdle, _lastKernel, _lastUser;
        private bool _hasLast;

        private DateTime _lastDiskRead = DateTime.MinValue;
        private double _diskFreePct = 100, _diskFreeGB, _diskTotalGB;

        public object Read()
        {
            // RAM
            var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            GlobalMemoryStatusEx(ref mem);
            double totalGB = mem.ullTotalPhys / 1073741824.0;
            double usedGB = (mem.ullTotalPhys - mem.ullAvailPhys) / 1073741824.0;

            // CPU: porcentaje de tiempo NO ocioso desde la lectura anterior
            int cpu = 0;
            if (GetSystemTimes(out long idle, out long kernel, out long user))
            {
                if (_hasLast)
                {
                    long dIdle = idle - _lastIdle;
                    long dTotal = (kernel - _lastKernel) + (user - _lastUser); // kernel ya incluye el ocio
                    if (dTotal > 0) cpu = (int)Math.Round(100.0 * (dTotal - dIdle) / dTotal);
                }
                _lastIdle = idle; _lastKernel = kernel; _lastUser = user; _hasLast = true;
            }

            // Batería (null si es un PC de escritorio)
            int? battery = null;
            bool charging = false;
            var ps = SystemInformation.PowerStatus;
            bool noBattery = ps.BatteryChargeStatus.HasFlag(BatteryChargeStatus.NoSystemBattery)
                             || ps.BatteryChargeStatus == BatteryChargeStatus.Unknown
                             || ps.BatteryLifePercent > 1f;
            if (!noBattery)
            {
                battery = (int)Math.Round(ps.BatteryLifePercent * 100);
                charging = ps.PowerLineStatus == PowerLineStatus.Online;
            }

            // Disco C: (se lee cada 30 s, cambia poco)
            if ((DateTime.Now - _lastDiskRead).TotalSeconds > 30)
            {
                try
                {
                    var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\");
                    _diskTotalGB = drive.TotalSize / 1073741824.0;
                    _diskFreeGB = drive.TotalFreeSpace / 1073741824.0;
                    _diskFreePct = 100.0 * drive.TotalFreeSpace / drive.TotalSize;
                }
                catch { }
                _lastDiskRead = DateTime.Now;
            }

            return new
            {
                action = "stats",
                ram = (int)mem.dwMemoryLoad,          // % de RAM en uso
                ramUsedGB = Math.Round(usedGB, 1),
                ramTotalGB = Math.Round(totalGB, 1),
                cpu = Math.Clamp(cpu, 0, 100),        // % de CPU en uso
                battery,                               // % o null
                charging,
                diskFree = (int)Math.Round(_diskFreePct),
                diskFreeGB = Math.Round(_diskFreeGB, 0),
                diskTotalGB = Math.Round(_diskTotalGB, 0)
            };
        }
    }
}
