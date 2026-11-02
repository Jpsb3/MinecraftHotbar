using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MinecraftHotbar
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Log.Write($">>> Main() iniciado. Args: {string.Join(" ", args)}");
            using var mutex = new Mutex(true, "MinecraftDesktopHotbar_SingleInstance", out bool isFirst);
            Log.Write($">>> Mutex isFirst: {isFirst}");

            // Al reiniciar desde la bandeja, la copia vieja todavía se está cerrando:
            // esperamos hasta 5 s a que suelte el Mutex en vez de mostrar "ya se está ejecutando".
            if (!isFirst && Array.IndexOf(args, "--restart") >= 0)
            {
                try { isFirst = mutex.WaitOne(5000); }
                catch (AbandonedMutexException) { isFirst = true; }
            }

            if (!isFirst)
            {
                MessageBox.Show(
                    "Minecraft Desktop Hotbar ya se está ejecutando en segundo plano.\n\nRevisa el icono de la barra de tareas (junto al reloj).",
                    "Minecraft Desktop Hotbar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) => Log.Write("Error UI: " + e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Write("Error fatal: " + e.ExceptionObject);

            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal; } catch { }

            Log.Write("Iniciando HotbarApp Context...");
            Application.Run(new HotbarApp());
            Log.Write("HotbarApp finalizado.");
        }
    }

    internal class HotbarApp : ApplicationContext
    {
        private readonly HotbarForm _form;
        private readonly NotifyIcon _tray;

        public HotbarApp()
        {
            Log.Write("HotbarApp constructor...");
            _form = new HotbarForm();
            _tray = CreateTray();
            _form.Show();
            Log.Write("HotbarForm.Show ejecutado.");
        }

        private NotifyIcon CreateTray()
        {
            var tray = new NotifyIcon
            {
                Text = "Minecraft Desktop Hotbar",
                Visible = true
            };

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string iconPath = Path.Combine(baseDir, "icon.ico");
            if (!File.Exists(iconPath))
            {
                string srcIcon = Path.Combine(baseDir, "..", "src", "icon.ico");
                if (File.Exists(srcIcon)) iconPath = Path.GetFullPath(srcIcon);
            }

            if (File.Exists(iconPath))
                tray.Icon = new Icon(iconPath);
            else
                tray.Icon = SystemIcons.Application;

            var menu = new ContextMenuStrip();

            var itemToggle = new ToolStripMenuItem("⚔️ Mostrar / Ocultar Hotbar (Esc)");
            itemToggle.Click += (s, e) => _form.ToggleHotbar();
            tray.DoubleClick += (s, e) => _form.ToggleHotbar();
            menu.Items.Add(itemToggle);

            var itemInv = new ToolStripMenuItem("📦 Abrir Inventario (Personalizar)");
            itemInv.Click += (s, e) =>
            {
                if (!_form.Visible) _form.Visible = true;
                _form.OnToggleInventoryKey();
            };
            menu.Items.Add(itemInv);

            menu.Items.Add(new ToolStripSeparator());

            var itemConfig = new ToolStripMenuItem("⚙️ Editar config.json");
            itemConfig.Click += (s, e) =>
            {
                if (File.Exists(ConfigManager.ConfigPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = ConfigManager.ConfigPath,
                        UseShellExecute = true
                    });
                }
            };
            menu.Items.Add(itemConfig);

            var itemRestart = new ToolStripMenuItem("🔄 Reiniciar Hotbar");
            itemRestart.Click += (s, e) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = "--restart",
                    UseShellExecute = false
                });
                tray.Visible = false;
                ExitThread();
            };
            menu.Items.Add(itemRestart);

            menu.Items.Add(new ToolStripSeparator());

            var itemExit = new ToolStripMenuItem("❌ Salir");
            itemExit.Click += (s, e) =>
            {
                tray.Visible = false;
                ExitThread();
            };
            menu.Items.Add(itemExit);

            tray.ContextMenuStrip = menu;
            tray.DoubleClick += (s, e) => _form.ToggleVisibility();

            return tray;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tray?.Dispose();
                _form?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
