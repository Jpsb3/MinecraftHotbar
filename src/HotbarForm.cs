using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MinecraftHotbar
{
    internal class HotbarForm : Form
    {
        private const int HOTBAR_WIDTH = 600;
        private const int HOTBAR_HEIGHT = 260; // espacio para el tooltip y HUD (vida, comida, xp)
        private const int MODAL_WIDTH = 900;   // espacio para el inventario completo (440px) y cajón de recetas lateral
        private const int MODAL_HEIGHT = 620;

        private readonly WebView2 _webView;
        private InputHooks _hooks;
        private System.Windows.Forms.Timer _fsTimer;
        private System.Windows.Forms.Timer _statsTimer;
        private readonly SystemStats _stats = new SystemStats();
        private System.Windows.Forms.Timer _runningTimer;
        private readonly RunningApps _running = new RunningApps();
        private bool _isModalOpen = false;
        private bool _isManuallyHidden = false;
        private bool _hasLeftDesktopAfterEsc = false;

        public bool IsModalOpen => _isModalOpen;
        public bool IsManuallyHidden => _isManuallyHidden;

        public HotbarForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            // Antes: BackColor/TransparencyKey = Lime. Con WebView2 eso hacía que TODA la ventana
            // fuera "invisible" también para el mouse: los clics atravesaban la hotbar.
            // Ahora la ventana toma la forma exacta de lo que se ve (ver SetHitRegions).
            BackColor = Color.FromArgb(28, 28, 28);
            Region = new Region(Rectangle.Empty); // invisible hasta que el HTML diga qué se ve
            Text = "Minecraft Desktop Hotbar";

            Width = HOTBAR_WIDTH;
            Height = HOTBAR_HEIGHT;
            RepositionWindow();

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.Transparent
            };
            Controls.Add(_webView);

            InitFullscreenWatcher();
            InitStatsTimer();
            InitRunningTimer();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= (int)Native.WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Log.Write("HotbarForm OnLoad iniciado.");
            await InitWebViewAsync();
            try
            {
                _hooks = new InputHooks(this);
            }
            catch (Exception ex)
            {
                Log.Write($"Error inicializando InputHooks: {ex.Message}");
            }
            Log.Write("OnLoad completado con éxito.");
        }

        private async Task InitWebViewAsync()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string appDir = baseDir;

                if (File.Exists(Path.Combine(baseDir, "hotbar.html")))
                {
                    appDir = baseDir;
                }
                else if (Directory.Exists(Path.Combine(baseDir, "app")))
                {
                    appDir = Path.Combine(baseDir, "app");
                }
                else if (Directory.Exists(Path.Combine(baseDir, "..", "app")))
                {
                    appDir = Path.GetFullPath(Path.Combine(baseDir, "..", "app"));
                }

                Log.Write($"Carpeta de recursos web: {appDir}");

                string userData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MinecraftHotbar",
                    "WebView2"
                );

                Log.Write($"Carpeta UserData WebView2: {userData}");

                var env = await CoreWebView2Environment.CreateAsync(
                    null,
                    userData,
                    new CoreWebView2EnvironmentOptions(
                        "--autoplay-policy=no-user-gesture-required " +
                        "--disable-features=msSmartScreenProtection,CalculateNativeWinOcclusion " +
                        "--disable-backgrounding-occluded-windows"
                    )
                );

                Log.Write("Inicializando CoreWebView2 con EnsureCoreWebView2Async...");
                await _webView.EnsureCoreWebView2Async(env);
                Log.Write("CoreWebView2 inicializado.");

                _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

                _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "hotbar.local",
                    appDir,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                _webView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    Log.Write($"NavigationCompleted: Success={e.IsSuccess}, Error={e.WebErrorStatus}");
                    SendStats();
                };
                _webView.CoreWebView2.ProcessFailed += (s, e) =>
                {
                    Log.Write($"ProcessFailed: ExitCode={e.ExitCode}, Kind={e.ProcessFailedKind}");
                };

                _webView.CoreWebView2.Navigate("https://hotbar.local/hotbar.html");
                Log.Write("Navegando a https://hotbar.local/hotbar.html");
            }
            catch (Exception ex)
            {
                Log.Write($"EXCEPCIÓN en InitWebViewAsync: {ex}");
                MessageBox.Show(
                    $"Error al inicializar la Hotbar:\n\n{ex.Message}",
                    "Minecraft Desktop Hotbar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using var doc = JsonDocument.Parse(e.WebMessageAsJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("action", out var actionProp)) return;
                string action = actionProp.GetString();

                switch (action)
                {
                    case "launch":
                        string target = root.GetProperty("target").GetString();
                        string name = root.TryGetProperty("name", out var n) ? n.GetString() : "";
                        Log.Write($"Lanzando '{name}': {target}");
                        ProcessLauncher.Launch(target, name);
                        // Revisar pronto para que el brillo de encantamiento aparezca rápido
                        Task.Delay(1500).ContinueWith(_ => { try { BeginInvoke(new Action(SendRunning)); } catch { } });
                        Task.Delay(5000).ContinueWith(_ => { try { BeginInvoke(new Action(SendRunning)); } catch { } });
                        break;

                    case "watchTargets":
                        // El HTML avisa qué apps hay en las casillas (hotbar + inventario)
                        var targets = new System.Collections.Generic.List<string>();
                        foreach (var t in root.GetProperty("targets").EnumerateArray()) targets.Add(t.GetString());
                        _running.SetTargets(targets);
                        SendRunning();
                        break;

                    case "dropFiles":
                        // Archivos arrastrados desde el Explorador o el escritorio a una casilla.
                        // El navegador no conoce la ruta real; WebView2 se la da a C# en AdditionalObjects.
                        var paths = new System.Collections.Generic.List<string>();
                        if (e.AdditionalObjects != null)
                        {
                            foreach (var obj in e.AdditionalObjects)
                                if (obj is CoreWebView2File f && !string.IsNullOrEmpty(f.Path)) paths.Add(f.Path);
                        }
                        Log.Write($"Archivo soltado en casilla: {string.Join(", ", paths)}");
                        if (paths.Count > 0)
                        {
                            string p = paths[0];
                            bool isDir = Directory.Exists(p);
                            string reply = JsonSerializer.Serialize(new
                            {
                                action = "fileDropped",
                                group = root.GetProperty("group").GetString(),
                                idx = root.GetProperty("idx").GetInt32(),
                                path = p,
                                name = isDir ? new DirectoryInfo(p).Name : Path.GetFileNameWithoutExtension(p),
                                isFolder = isDir
                            });
                            _webView.CoreWebView2.PostWebMessageAsJson(reply);
                        }
                        break;

                    case "saveConfig":
                        if (root.TryGetProperty("config", out var configEl))
                        {
                            ConfigManager.SaveRawConfig(configEl.GetRawText());
                        }
                        break;

                    case "modalState":
                        bool open = root.GetProperty("open").GetBoolean();
                        SetModalState(open);
                        break;

                    case "hideHotbar":
                        HideHotbar();
                        break;

                    case "browseFile":
                        ShowFileBrowser();
                        break;

                    case "regions":
                        SetHitRegions(root);
                        break;

                    case "log":
                        Log.Write("[HTML] " + (root.TryGetProperty("msg", out var m) ? m.GetString() : ""));
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnWebMessageReceived: {ex.Message}");
            }
        }

        public void SetModalState(bool open)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetModalState(open)));
                return;
            }

            _isModalOpen = open;
            RepositionWindow();
            if (open)
            {
                Activate();
                _webView.Focus();
            }
        }

        private void ShowFileBrowser()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ShowFileBrowser));
                return;
            }

            using var ofd = new OpenFileDialog
            {
                Title = "Seleccionar programa o acceso directo",
                Filter = "Ejecutables y accesos directos (*.exe;*.lnk;*.bat)|*.exe;*.lnk;*.bat|Todos los archivos (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Programs)
            };

            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                string json = JsonSerializer.Serialize(new
                {
                    action = "fileSelected",
                    path = ofd.FileName
                });

                _webView.CoreWebView2.PostWebMessageAsJson(json);
            }
        }

        public void RepositionWindow()
        {
            // El HTML se dibuja más grande cuando Windows tiene escala 125%, 150%, etc.
            // Por eso el tamaño de la ventana se multiplica por esa escala:
            // antes medía 520 px fijos y la hotbar salía cortada a los lados.
            float scale = (IsHandleCreated ? DeviceDpi : 96) / 96f;
            var work = Screen.PrimaryScreen.WorkingArea;

            int w = (int)Math.Ceiling((_isModalOpen ? MODAL_WIDTH : HOTBAR_WIDTH) * scale);
            int h = _isModalOpen ? work.Height : (int)Math.Ceiling(HOTBAR_HEIGHT * scale);

            int x = work.Left + (work.Width - w) / 2;
            int y = work.Bottom - h;
            Bounds = new Rectangle(x, y, w, h);

            if (IsHandleCreated)
            {
                uint flags = Native.SWP_NOACTIVATE | (Visible ? Native.SWP_SHOWWINDOW : 0);
                Native.SetWindowPos(Handle, Native.HWND_TOPMOST, x, y, w, h, flags);
            }
        }

        /// <summary>
        /// El HTML manda los rectángulos de lo que se ve (barra, tooltip, inventario) en píxeles CSS.
        /// La ventana se recorta a esa forma: ahí recibe clics, y fuera de ella los clics van al escritorio.
        /// </summary>
        private void SetHitRegions(System.Text.Json.JsonElement root)
        {
            if (InvokeRequired) { var copy = root.Clone(); BeginInvoke(new Action(() => SetHitRegions(copy))); return; }

            double dpr = root.TryGetProperty("dpr", out var d) ? d.GetDouble() : DeviceDpi / 96.0;
            var region = new Region(Rectangle.Empty);
            foreach (var r in root.GetProperty("rects").EnumerateArray())
            {
                int x = (int)Math.Floor(r.GetProperty("x").GetDouble() * dpr);
                int y = (int)Math.Floor(r.GetProperty("y").GetDouble() * dpr);
                int w = (int)Math.Ceiling(r.GetProperty("w").GetDouble() * dpr);
                int h = (int)Math.Ceiling(r.GetProperty("h").GetDouble() * dpr);
                region.Union(new Rectangle(x, y, w, h));
            }
            var old = Region;
            Region = region;
            old?.Dispose();
        }

        /// <summary>true si el punto (coordenadas de pantalla) cae sobre algo visible de la hotbar.</summary>
        public bool ContainsScreenPoint(Point screenPt)
        {
            if (!Bounds.Contains(screenPt)) return false;
            var reg = Region;
            return reg == null || reg.IsVisible(PointToClient(screenPt));
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RepositionWindow();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            RepositionWindow();
        }

        public void OnMouseWheelScroll(int delta)
        {
            if (IsDisposed || Disposing) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnMouseWheelScroll(delta))); } catch { }
                return;
            }
            if (_webView?.CoreWebView2 == null) return;
            string json = $"{{\"action\":\"scroll\",\"delta\":{delta}}}";
            _webView.CoreWebView2.PostWebMessageAsJson(json);
        }

        public void OnSelectSlotKey(int slotIndex)
        {
            if (IsDisposed || Disposing) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnSelectSlotKey(slotIndex))); } catch { }
                return;
            }
            if (_webView?.CoreWebView2 == null) return;
            string json = $"{{\"action\":\"select\",\"slot\":{slotIndex + 1}}}";
            _webView.CoreWebView2.PostWebMessageAsJson(json);
        }

        public void OnToggleInventoryKey()
        {
            if (IsDisposed || Disposing) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(OnToggleInventoryKey)); } catch { }
                return;
            }
            if (_webView?.CoreWebView2 == null) return;
            _webView.CoreWebView2.PostWebMessageAsJson("{\"action\":\"toggleInventory\"}");
        }

        /// <summary>
        /// Cada 2 s manda RAM, CPU, batería y disco al HTML para los corazones, la comida y el nivel.
        /// Si la hotbar está escondida (juego en pantalla completa) no hace nada.
        /// </summary>
        private void InitStatsTimer()
        {
            _statsTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _statsTimer.Tick += (s, e) => SendStats();
            _statsTimer.Start();
        }

        /// <summary>
        /// Cada 3 s revisa qué apps de las casillas están abiertas y cuántas ventanas tienen.
        /// Manda { action: "running", counts: { target: ventanas } } al HTML.
        /// </summary>
        private void InitRunningTimer()
        {
            _runningTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _runningTimer.Tick += (s, e) => SendRunning();
            _runningTimer.Start();
        }

        private void SendRunning()
        {
            try
            {
                if (!Visible || _webView?.CoreWebView2 == null) return;
                string json = JsonSerializer.Serialize(new { action = "running", counts = _running.Count() });
                _webView.CoreWebView2.PostWebMessageAsJson(json);
            }
            catch (Exception ex)
            {
                Log.Write($"Error en SendRunning: {ex.Message}");
            }
        }

        private void SendStats()
        {
            try
            {
                if (!Visible || _webView?.CoreWebView2 == null) return;
                string json = JsonSerializer.Serialize(_stats.Read());
                _webView.CoreWebView2.PostWebMessageAsJson(json);
            }
            catch (Exception ex)
            {
                Log.Write($"Error en SendStats: {ex.Message}");
            }
        }

        private void InitFullscreenWatcher()
        {
            _fsTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _fsTimer.Tick += (s, e) =>
            {
                try
                {
                    // Si el inventario está abierto o nuestra propia ventana está en foco, no ocultar
                    if (_isModalOpen || Native.IsOwnWindowForeground())
                        return;

                    IntPtr fg = Native.GetForegroundWindow();
                    if (fg == IntPtr.Zero) return;
                    if (Native.IsOwnWindow(fg)) return;

                    bool isDesktop = Native.IsDesktopWindow(fg);
                    bool isFs = Native.IsForegroundWindowFullscreen();

                    if (isDesktop && !isFs)
                    {
                        // Si se había ocultado con Esc y el usuario fue a otra ventana y regresó al escritorio, se reactiva
                        if (_isManuallyHidden && _hasLeftDesktopAfterEsc)
                        {
                            _isManuallyHidden = false;
                            _hasLeftDesktopAfterEsc = false;
                        }

                        if (!_isManuallyHidden && !Visible)
                        {
                            Visible = true;
                            RepositionWindow();
                            SendStats();
                            SendRunning();
                            Log.Write("Escritorio detectado. Mostrando Hotbar.");
                        }
                    }
                    else
                    {
                        // Otra aplicación de ventana o juego está en primer plano
                        if (_isManuallyHidden)
                        {
                            _hasLeftDesktopAfterEsc = true;
                        }

                        if (Visible)
                        {
                            Visible = false;
                            Log.Write($"Ventana externa activa ({Native.ClassOf(fg)}). Ocultando Hotbar.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Write($"Error en _fsTimer: {ex.Message}");
                }
            };
            _fsTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Log.Write($"HotbarForm OnFormClosing. CloseReason: {e.CloseReason}, Cancel: {e.Cancel}");
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Log.Write($"HotbarForm OnFormClosed. CloseReason: {e.CloseReason}");
            base.OnFormClosed(e);
        }

        public void HideHotbar()
        {
            if (InvokeRequired) { BeginInvoke(new Action(HideHotbar)); return; }
            if (_isModalOpen)
            {
                SetModalState(false);
                _webView?.CoreWebView2?.PostWebMessageAsJson("{\"action\":\"toggleInventory\"}");
                return;
            }
            _isManuallyHidden = true;
            _hasLeftDesktopAfterEsc = false;
            Visible = false;
            Log.Write("Hotbar ocultada con tecla Esc.");
        }

        public void ShowHotbar()
        {
            if (InvokeRequired) { BeginInvoke(new Action(ShowHotbar)); return; }
            _isManuallyHidden = false;
            _hasLeftDesktopAfterEsc = false;
            Visible = true;
            RepositionWindow();
            SendStats();
            SendRunning();
            Log.Write("Hotbar restaurada.");
        }

        public void ToggleHotbar()
        {
            if (Visible && !_isManuallyHidden) HideHotbar();
            else ShowHotbar();
        }

        public void ToggleVisibility()
        {
            ToggleHotbar();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hooks?.Dispose();
                _fsTimer?.Dispose();
                _statsTimer?.Dispose();
                _runningTimer?.Dispose();
                _webView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
