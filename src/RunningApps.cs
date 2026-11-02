using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace MinecraftHotbar
{
    /// <summary>
    /// Sabe qué apps de las casillas están abiertas y cuántas ventanas tiene cada una.
    /// - El brillo de encantamiento aparece si la app tiene al menos una ventana.
    /// - El número de "stack" es la cantidad de ventanas (como una pila de ítems).
    ///
    /// Cada casilla tiene un "target" (ruta .exe, acceso directo .lnk, URL, protocolo como spotify:, carpeta...).
    /// Aquí se traduce cada target al nombre del proceso que lo ejecuta (chrome.exe, spotify.exe, explorer.exe...).
    /// </summary>
    internal class RunningApps
    {
        // ---------- Win32 ----------
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hWnd, uint cmd);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hWnd, StringBuilder sb, int max);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);
        [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out int value, int size);
        [DllImport("kernel32.dll")] private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern bool QueryFullProcessImageName(IntPtr h, int flags, StringBuilder sb, ref int size);

        private const uint GW_OWNER = 4;
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TOOLWINDOW = 0x80;
        private const int DWMWA_CLOAKED = 14;
        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        private static readonly HashSet<string> IgnoredClasses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd"
        };

        // target -> nombre de proceso (minúsculas), o null si no se puede saber
        private readonly Dictionary<string, string> _targetToExe = new();
        private readonly Dictionary<uint, string> _pidToExe = new();
        private DateTime _pidCacheTime = DateTime.Now;

        public void SetTargets(IEnumerable<string> targets)
        {
            var wanted = new HashSet<string>(targets.Where(t => !string.IsNullOrWhiteSpace(t)));
            foreach (var old in _targetToExe.Keys.Where(k => !wanted.Contains(k)).ToList()) _targetToExe.Remove(old);
            foreach (var t in wanted)
            {
                if (_targetToExe.ContainsKey(t)) continue;
                string exe = null;
                try { exe = ResolveExe(t, 0); } catch (Exception ex) { Log.Write($"No se pudo resolver '{t}': {ex.Message}"); }
                _targetToExe[t] = exe;
                Log.Write($"Casilla '{t}' -> proceso: {exe ?? "(desconocido)"}");
            }
        }

        /// <summary>Devuelve { target: número de ventanas } solo para las casillas con la app abierta.</summary>
        public Dictionary<string, int> Count()
        {
            // Cada minuto se limpia la caché de procesos (los PID se reutilizan)
            if ((DateTime.Now - _pidCacheTime).TotalSeconds > 60) { _pidToExe.Clear(); _pidCacheTime = DateTime.Now; }

            var windowsPerExe = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            uint ownPid = (uint)Environment.ProcessId;
            var sb = new StringBuilder(256);

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                if (GetWindow(hWnd, GW_OWNER) != IntPtr.Zero) return true;          // diálogos y ventanas hijas
                if (GetWindowTextLength(hWnd) == 0) return true;                      // ventanas sin título
                if ((GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64() & WS_EX_TOOLWINDOW) != 0) return true;
                if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, 4) == 0 && cloaked != 0) return true; // en otro escritorio virtual
                sb.Clear(); GetClassName(hWnd, sb, sb.Capacity);
                if (IgnoredClasses.Contains(sb.ToString())) return true;

                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == ownPid) return true;
                string exe = ExeOfPid(pid);
                if (exe == null) return true;
                windowsPerExe[exe] = windowsPerExe.TryGetValue(exe, out int c) ? c + 1 : 1;
                return true;
            }, IntPtr.Zero);

            var result = new Dictionary<string, int>();
            foreach (var (target, exe) in _targetToExe)
            {
                if (exe != null && windowsPerExe.TryGetValue(exe, out int n)) result[target] = n;
            }
            return result;
        }

        private string ExeOfPid(uint pid)
        {
            if (_pidToExe.TryGetValue(pid, out var cached)) return cached;
            string exe = null;
            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h != IntPtr.Zero)
            {
                var sb = new StringBuilder(1024);
                int size = sb.Capacity;
                if (QueryFullProcessImageName(h, 0, sb, ref size)) exe = Path.GetFileName(sb.ToString()).ToLowerInvariant();
                CloseHandle(h);
            }
            _pidToExe[pid] = exe;
            return exe;
        }

        // ---------- Traducir un target al proceso que lo ejecuta ----------
        private static string ResolveExe(string target, int depth)
        {
            if (depth > 3) return null;
            string t = Environment.ExpandEnvironmentVariables(target.Trim().Trim('"'));
            if (t.Length == 0 || t.Equals("inventory", StringComparison.OrdinalIgnoreCase)) return null;

            // Carpetas y accesos del Explorador
            if (t.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) || Directory.Exists(t)) return "explorer.exe";

            string ext = Path.GetExtension(t).ToLowerInvariant();

            // Acceso directo .lnk -> a dónde apunta
            if (ext == ".lnk" && File.Exists(t))
            {
                string linked = ReadShortcutTarget(t);
                return string.IsNullOrEmpty(linked) ? null : ResolveExe(linked, depth + 1);
            }

            // Acceso a internet .url -> la URL que contiene
            if (ext == ".url" && File.Exists(t))
            {
                string url = File.ReadAllLines(t).FirstOrDefault(l => l.StartsWith("URL=", StringComparison.OrdinalIgnoreCase));
                return url == null ? null : ResolveExe(url.Substring(4), depth + 1);
            }

            // Protocolos: https:, spotify:, discord:, steam:... (pero no "C:\...")
            int colon = t.IndexOf(':');
            if (colon > 1 && !t.Contains('\\'))
            {
                string scheme = t.Substring(0, colon).ToLowerInvariant();
                if (scheme == "http" || scheme == "https") return DefaultBrowserExe();
                return ExeFromCommand(Registry.ClassesRoot.OpenSubKey($@"{scheme}\shell\open\command")?.GetValue("") as string);
            }

            if (ext == ".exe") return Path.GetFileName(t).ToLowerInvariant();

            // "powershell.exe -NoExit" o "code ." -> primer palabra
            if (!File.Exists(t) && t.Contains(' ')) return ResolveExe(t.Substring(0, t.IndexOf(' ')), depth + 1);

            // Comando sin extensión: "code" -> code.exe
            if (ext == "") return Path.GetFileName(t).ToLowerInvariant() + ".exe";

            return null; // .bat, .cmd, documentos, etc.
        }

        private static string DefaultBrowserExe()
        {
            string progId = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice")?.GetValue("ProgId") as string;
            if (progId == null) return "msedge.exe";
            return ExeFromCommand(Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command")?.GetValue("") as string) ?? "msedge.exe";
        }

        /// <summary>Saca el .exe de un comando del registro, ej: "C:\...\Spotify.exe" --protocol-uri="%1"</summary>
        private static string ExeFromCommand(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return null;
            // Discord y otros usan Update.exe --processStart Discord.exe
            int ps = cmd.IndexOf("--processStart", StringComparison.OrdinalIgnoreCase);
            if (ps >= 0)
            {
                var rest = cmd.Substring(ps + "--processStart".Length).Trim().Split(' ');
                if (rest.Length > 0 && rest[0].Trim('"').EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    return rest[0].Trim('"').ToLowerInvariant();
            }
            string path = cmd.TrimStart().StartsWith("\"")
                ? cmd.TrimStart().Substring(1).Split('"')[0]
                : cmd.Trim().Split(' ')[0];
            return path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? Path.GetFileName(path).ToLowerInvariant() : null;
        }

        private static string ReadShortcutTarget(string lnkPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return null;
                object shell = Activator.CreateInstance(shellType);
                object link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
                string target = link.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, link, null) as string;
                Marshal.FinalReleaseComObject(link);
                Marshal.FinalReleaseComObject(shell);
                return target;
            }
            catch { return null; }
        }
    }
}
