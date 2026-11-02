using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace MinecraftHotbar
{
    internal static class Native
    {
        public const int GWL_EXSTYLE = -20;
        public const uint WS_EX_TOOLWINDOW = 0x00000080;
        public const uint WS_EX_NOACTIVATE = 0x08000000;
        public const uint WS_EX_LAYERED = 0x00080000;
        public const uint WS_EX_TRANSPARENT = 0x00000020;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_MOUSEWHEEL = 0x020A;

        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, dwNewLong);
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public static string ClassOf(IntPtr hWnd)
        {
            var sb = new StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        /// <summary>
        /// true si la ventana dada corresponde al entorno del escritorio o la barra de tareas de Windows.
        /// </summary>
        public static bool IsDesktopWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;

            IntPtr desktop = GetDesktopWindow();
            IntPtr shell = GetShellWindow();
            if (hWnd == desktop || hWnd == shell) return true;

            string cls = ClassOf(hWnd);
            return cls == "WorkerW" || cls == "Progman"
                || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd"
                || cls == "Windows.UI.Core.CoreWindow" || cls == "XamlExplorerHostIslandWindow";
        }

        /// <summary>
        /// true si la ventana dada pertenece a este mismo proceso.
        /// </summary>
        public static bool IsOwnWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            GetWindowThreadProcessId(hWnd, out uint pid);
            return pid == (uint)Environment.ProcessId;
        }

        public static bool IsForegroundWindowFullscreen()
        {
            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;

            IntPtr desktop = GetDesktopWindow();
            IntPtr shell = GetShellWindow();
            if (fg == desktop || fg == shell) return false;

            string cls = ClassOf(fg);
            if (cls == "WorkerW" || cls == "Progman" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd"
                || cls == "Windows.UI.Core.CoreWindow" || cls == "XamlExplorerHostIslandWindow")
                return false;

            if (!GetWindowRect(fg, out RECT rect)) return false;

            var screen = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            return rect.Left <= screen.Left &&
                   rect.Top <= screen.Top &&
                   rect.Right >= screen.Right &&
                   rect.Bottom >= screen.Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public uint flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [DllImport("user32.dll")]
        public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        private const int VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12, VK_LWIN = 0x5B, VK_RWIN = 0x5C;

        /// <summary>
        /// true si hay Ctrl, Alt, Shift o Windows presionado (atajos como Ctrl+E no se tocan).
        /// </summary>
        public static bool AnyModifierDown()
        {
            return (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0
                || (GetAsyncKeyState(VK_MENU) & 0x8000) != 0
                || (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0
                || (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0
                || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;
        }

        /// <summary>
        /// true solo si el escritorio (fondo con los íconos) está en primer plano
        /// y NO se está escribiendo nada en él (por ejemplo, renombrando un archivo con F2).
        /// La barra de tareas y "ninguna ventana" ya no cuentan como escritorio.
        /// </summary>
        public static bool IsDesktopFocusedWithoutTyping()
        {
            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;

            string cls = ClassOf(fg);
            if (cls != "WorkerW" && cls != "Progman") return false;

            uint thread = GetWindowThreadProcessId(fg, out _);
            var info = new GUITHREADINFO { cbSize = Marshal.SizeOf<GUITHREADINFO>() };
            if (GetGUIThreadInfo(thread, ref info))
            {
                // Hay un cursor de texto parpadeando: el usuario está escribiendo
                if (info.hwndCaret != IntPtr.Zero) return false;
                // El foco está en un cuadro de texto (renombrar archivo)
                if (info.hwndFocus != IntPtr.Zero && ClassOf(info.hwndFocus) == "Edit") return false;
            }
            return true;
        }

        /// <summary>
        /// true si la ventana en primer plano es de este mismo programa (la hotbar o el inventario).
        /// </summary>
        public static bool IsOwnWindowForeground()
        {
            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return false;
            GetWindowThreadProcessId(fg, out uint pid);
            return pid == (uint)Environment.ProcessId;
        }
    }
}
