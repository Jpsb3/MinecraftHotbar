#!/usr/bin/env python3
"""
Minecraft Desktop Hotbar - versión Linux
La misma hotbar de Windows (app/hotbar.html) dentro de una ventana GTK 3 + WebKit2GTK.

Uso:
  minecraft-hotbar               abre la hotbar (si ya está abierta, la muestra/oculta)
  minecraft-hotbar --inventory   abre/cierra el inventario (ideal para un atajo de teclado del sistema)
  minecraft-hotbar --quit        cierra la hotbar

Autor: Jpsb3 - https://github.com/Jpsb3/MinecraftHotbar
Licencia: GPL-3.0 (ver LICENSE). Las texturas, sonidos y fuente de Minecraft son de Mojang Studios / Microsoft.
"""

import os
import sys
import json
import time
import glob
import shlex
import mimetypes
import shutil
import signal
import subprocess
from pathlib import Path
from urllib.parse import urlparse, unquote

APP_ID = 'minecraft-hotbar'
mimetypes.add_type('audio/ogg', '.ogg')
mimetypes.add_type('font/otf', '.otf')
mimetypes.add_type('text/javascript', '.js')
RUNTIME_DIR = Path(os.environ.get('XDG_RUNTIME_DIR') or f'/tmp/{APP_ID}-{os.getuid()}')
PID_FILE = RUNTIME_DIR / f'{APP_ID}.pid'


# ---------------------------------------------------------------------------
# Una sola copia: si ya está abierta, se le manda una señal y esta copia sale
# ---------------------------------------------------------------------------
def running_pid():
    try:
        pid = int(PID_FILE.read_text().strip())
        os.kill(pid, 0)
        cmd = Path(f'/proc/{pid}/cmdline').read_bytes()
        return pid if b'minecraft-hotbar' in cmd else None
    except Exception:
        return None


def forward_to_running(args):
    pid = running_pid()
    if not pid:
        return False
    if '--quit' in args:
        os.kill(pid, signal.SIGTERM)
    elif '--inventory' in args:
        os.kill(pid, signal.SIGUSR2)
    else:
        os.kill(pid, signal.SIGUSR1)
    return True


if __name__ == '__main__':
    if forward_to_running(sys.argv[1:]):
        sys.exit(0)
    if '--quit' in sys.argv[1:]:
        sys.exit(0)

# GNOME en Wayland no permite colocar ventanas ni dejarlas encima de todo:
# ahí se usa XWayland, que sí lo permite (funciona igual para el usuario).
_desktop = os.environ.get('XDG_CURRENT_DESKTOP', '').upper()
IS_WAYLAND_SESSION = os.environ.get('XDG_SESSION_TYPE') == 'wayland' or bool(os.environ.get('WAYLAND_DISPLAY'))
if IS_WAYLAND_SESSION and ('GNOME' in _desktop or 'UNITY' in _desktop or 'PANTHEON' in _desktop):
    os.environ.setdefault('GDK_BACKEND', 'x11')

try:
    import gi
    gi.require_version('Gtk', '3.0')
    gi.require_version('Gdk', '3.0')
    try:
        gi.require_version('WebKit2', '4.1')
    except ValueError:
        gi.require_version('WebKit2', '4.0')
    from gi.repository import Gtk, Gdk, Gio, GLib, WebKit2
    import cairo
except (ImportError, ValueError) as e:
    print(f'[Error] Faltan dependencias: {e}', file=sys.stderr)
    print('Ejecuta linux/install.sh o instala PyGObject, python3-gi-cairo y WebKit2GTK.', file=sys.stderr)
    sys.exit(1)

# Wayland con layer-shell (KDE Plasma 6, Hyprland, Sway, Wayfire...)
HAS_LAYER_SHELL = False
if Gdk.Display.get_default() and 'Wayland' in type(Gdk.Display.get_default()).__name__:
    try:
        gi.require_version('GtkLayerShell', '0.1')
        from gi.repository import GtkLayerShell
        HAS_LAYER_SHELL = GtkLayerShell.is_supported()
    except Exception:
        HAS_LAYER_SHELL = False

# Wnck (solo X11): saber qué ventana está activa y cuántas ventanas tiene cada app
Wnck = None
if not IS_WAYLAND_SESSION:
    try:
        gi.require_version('Wnck', '3.0')
        from gi.repository import Wnck as _Wnck
        _Wnck.set_client_type(_Wnck.ClientType.PAGER)
        if _Wnck.Screen.get_default() is not None:
            Wnck = _Wnck
    except Exception:
        Wnck = None


# Ícono junto al reloj: AppIndicator (Ubuntu, KDE, la mayoría) o StatusIcon (XFCE, MATE, Cinnamon)
AppIndicator = None
for _name, _ver in (('AyatanaAppIndicator3', '0.1'), ('AppIndicator3', '0.1')):
    try:
        gi.require_version(_name, _ver)
        AppIndicator = getattr(__import__('gi.repository', fromlist=[_name]), _name)
        break
    except Exception:
        continue


def log(msg):
    print(f'[minecraft-hotbar] {msg}', file=sys.stderr)


# ---------------------------------------------------------------------------
# Estado del PC (corazones = RAM, comida = batería o disco, nivel = CPU)
# ---------------------------------------------------------------------------
class SystemStats:
    def __init__(self):
        self._last_idle = self._last_total = 0
        self._has_cpu = False
        self._last_disk = 0
        self._disk = (100, 0, 0)

    def cpu(self):
        try:
            with open('/proc/stat') as f:
                fields = [int(x) for x in f.readline().split()[1:]]
            idle, total = fields[3] + fields[4], sum(fields)
            d_idle, d_total = idle - self._last_idle, total - self._last_total
            first = not self._has_cpu
            self._last_idle, self._last_total, self._has_cpu = idle, total, True
            if not first and d_total > 0:
                return max(0, min(100, round(100.0 * (d_total - d_idle) / d_total)))
        except Exception:
            pass
        return 0

    def ram(self):
        info = {}
        try:
            with open('/proc/meminfo') as f:
                for line in f:
                    k, v = line.split(':', 1)
                    info[k] = int(v.split()[0])
            total, avail = info['MemTotal'], info.get('MemAvailable', info.get('MemFree', 0))
            return round(100.0 * (total - avail) / total), round((total - avail) / 1048576, 1), round(total / 1048576, 1)
        except Exception:
            return 0, 0.0, 0.0

    def battery(self):
        for d in sorted(glob.glob('/sys/class/power_supply/*')):
            try:
                if Path(d, 'type').read_text().strip() != 'Battery':
                    continue
                if Path(d, 'scope').exists() and Path(d, 'scope').read_text().strip() == 'Device':
                    continue  # mouse o audífonos inalámbricos, no la batería del portátil
                cap = int(Path(d, 'capacity').read_text().strip())
                status = Path(d, 'status').read_text().strip().lower() if Path(d, 'status').exists() else ''
                return cap, status in ('charging', 'full')
            except Exception:
                continue
        return None, False

    def disk(self):
        if time.time() - self._last_disk > 30:
            try:
                u = shutil.disk_usage(str(Path.home()))
                self._disk = (round(100.0 * u.free / u.total), round(u.free / 1024 ** 3), round(u.total / 1024 ** 3))
            except Exception:
                pass
            self._last_disk = time.time()
        return self._disk

    def payload(self):
        ram, used, total = self.ram()
        bat, charging = self.battery()
        dfree, dfree_gb, dtotal_gb = self.disk()
        return {'action': 'stats', 'ram': ram, 'ramUsedGB': used, 'ramTotalGB': total, 'cpu': self.cpu(),
                'battery': bat, 'charging': charging, 'diskFree': dfree, 'diskFreeGB': dfree_gb, 'diskTotalGB': dtotal_gb}


# ---------------------------------------------------------------------------
# Apps abiertas (brillo de encantamiento y número de "stack")
# ---------------------------------------------------------------------------
IGNORED_NAMES = {'', 'sh', 'bash', 'env', 'flatpak', 'snap', 'xdg-open', 'gio', 'python3', 'python'}


def _app_names(info):
    """Nombres con los que se puede reconocer la app de un AppInfo (ejecutable, WM_CLASS, id)."""
    names = set()
    if not info:
        return names
    exe = info.get_executable() or ''
    names.add(os.path.basename(exe).lower())
    if isinstance(info, Gio.DesktopAppInfo):
        wm = info.get_startup_wm_class()
        if wm:
            names.add(wm.lower())
        cmd = info.get_commandline() or ''
        # flatpak run com.spotify.Client  ->  spotify
        if 'flatpak' in os.path.basename(exe) and '--command=' in cmd:
            names.add(cmd.split('--command=')[1].split()[0].lower())
    app_id = (info.get_id() or '').lower().replace('.desktop', '')
    if app_id:
        names.add(app_id)
        names.add(app_id.split('.')[-1])        # com.discordapp.Discord -> discord
        if app_id.endswith('.client'):           # com.spotify.Client -> spotify
            names.add(app_id.split('.')[-2])
    return names - IGNORED_NAMES


TERMINALS = ['x-terminal-emulator', 'gnome-terminal', 'kgx', 'ptyxis', 'konsole', 'xfce4-terminal',
             'mate-terminal', 'tilix', 'kitty', 'alacritty', 'wezterm', 'foot', 'xterm']


def find_terminal():
    for t in TERMINALS:
        if shutil.which(t):
            return t
    return None


def special_dir(target):
    t = target.strip()
    mapping = {
        'shell:downloads': GLib.UserDirectory.DIRECTORY_DOWNLOAD,
        'shell:personal': GLib.UserDirectory.DIRECTORY_DOCUMENTS,
        'shell:mypictures': GLib.UserDirectory.DIRECTORY_PICTURES,
        'shell:mymusic': GLib.UserDirectory.DIRECTORY_MUSIC,
        'shell:myvideo': GLib.UserDirectory.DIRECTORY_VIDEOS,
        'shell:desktop': GLib.UserDirectory.DIRECTORY_DESKTOP,
    }
    if t.lower() in mapping:
        return GLib.get_user_special_dir(mapping[t.lower()]) or str(Path.home())
    if t.lower().startswith('shell:'):
        return str(Path.home())
    return None


def is_uri(t):
    p = urlparse(t)
    return bool(p.scheme) and len(p.scheme) > 1 and not t.startswith('/') and p.scheme != 'file'


def resolve_names(target):
    """Traduce el target de una casilla a los nombres de proceso/ventana que lo representan."""
    t = os.path.expandvars(target.strip().strip('"'))
    if not t or t == 'inventory':
        return set()
    if t in ('terminal', 'powershell.exe', 'cmd.exe'):
        term = find_terminal()
        if not term:
            return set()
        names = {os.path.basename(os.path.realpath(shutil.which(term))).lower(), term}
        if term == 'gnome-terminal':
            names.add('gnome-terminal-server')
        return names
    d = special_dir(t)
    if d or os.path.isdir(os.path.expanduser(t)):
        return _app_names(Gio.AppInfo.get_default_for_type('inode/directory', False))
    if is_uri(t):
        scheme = urlparse(t).scheme.lower()
        return _app_names(Gio.AppInfo.get_default_for_uri_scheme('https' if scheme == 'http' else scheme))
    path = os.path.expanduser(t)
    if path.endswith('.desktop') and os.path.isfile(path):
        return _app_names(Gio.DesktopAppInfo.new_from_filename(path))
    if os.path.isfile(path):
        if os.access(path, os.X_OK):
            return {os.path.basename(path).lower()}
        return set()  # un documento: no se puede saber si "está abierto"
    first = shlex.split(t)[0] if t else ''
    names = {os.path.basename(first).lower()}
    try:
        info = Gio.DesktopAppInfo.new(os.path.basename(first) + '.desktop')
        names |= _app_names(info)
    except Exception:
        pass
    return names - IGNORED_NAMES


def proc_names(pid):
    names = set()
    try:
        names.add(Path(f'/proc/{pid}/comm').read_text().strip().lower())
    except Exception:
        pass
    try:
        names.add(os.path.basename(os.readlink(f'/proc/{pid}/exe')).lower())
    except Exception:
        pass
    return names


def matches(candidates, names):
    for c in candidates:
        for n in names:
            if n == c or (len(n) == 15 and c.startswith(n)):  # comm se corta a 15 letras
                return True
    return False


class RunningApps:
    def __init__(self):
        self.targets = {}

    def set_targets(self, targets):
        wanted = [t for t in (targets or []) if t]
        self.targets = {t: (self.targets.get(t) if t in self.targets else resolve_names(t)) for t in wanted}

    def count(self):
        result = {}
        if not self.targets:
            return result
        if Wnck:  # X11: contar ventanas reales, como en Windows
            screen = Wnck.Screen.get_default()
            screen.force_update()
            own = os.getpid()
            wins = []
            for w in screen.get_windows():
                if w.is_skip_tasklist() or w.get_window_type() != Wnck.WindowType.NORMAL:
                    continue
                pid = w.get_pid()
                if pid == own:
                    continue
                names = proc_names(pid) if pid else set()
                for cls in (w.get_class_group_name(), w.get_class_instance_name()):
                    if cls:
                        names.add(cls.lower())
                wins.append(names)
            for t, cands in self.targets.items():
                n = sum(1 for names in wins if matches(cands, names))
                if n:
                    result[t] = n
            return result
        # Wayland: no se pueden ver las ventanas de otras apps -> solo el brillo (sin número)
        procs = []
        for pid in os.listdir('/proc'):
            if pid.isdigit():
                procs.append(proc_names(pid))
        for t, cands in self.targets.items():
            if any(matches(cands, names) for names in procs):
                result[t] = 1
        return result


# ---------------------------------------------------------------------------
# Ventana principal
# ---------------------------------------------------------------------------
class HotbarApp:
    # Mismos tamaños que la versión de Windows (píxeles lógicos; GTK aplica la escala)
    HOTBAR_W, HOTBAR_H = 520, 260
    MODAL_W, MODAL_H = 900, 620

    def __init__(self, root: Path):
        self.root = root
        self.app_dir = root / 'app'
        self.stats = SystemStats()
        self.running = RunningApps()
        self.modal_open = False
        self.user_hidden = False
        self.shown = True
        self.page_ready = False

        cfg_home = Path(os.environ.get('XDG_CONFIG_HOME') or Path.home() / '.config')
        self.cfg_dir = cfg_home / APP_ID
        self.cfg_dir.mkdir(parents=True, exist_ok=True)
        self.cfg_file = self.cfg_dir / 'config.json'
        self.config = self.load_config()

        self.win = Gtk.Window(type=Gtk.WindowType.TOPLEVEL)
        self.win.set_title('Minecraft Desktop Hotbar')
        self.win.set_decorated(False)
        self.win.set_resizable(False)
        self.win.set_skip_taskbar_hint(True)
        self.win.set_skip_pager_hint(True)
        self.win.set_keep_above(True)
        self.win.set_app_paintable(True)
        self.win.connect('draw', self.on_draw)
        icon = self.app_dir / 'icon.ico'
        if icon.exists():
            try:
                self.win.set_icon_from_file(str(icon))
            except Exception:
                pass

        screen = self.win.get_screen()
        self.composited = screen.is_composited()
        visual = screen.get_rgba_visual()
        if visual and self.composited:
            self.win.set_visual(visual)

        if HAS_LAYER_SHELL:
            GtkLayerShell.init_for_window(self.win)
            GtkLayerShell.set_namespace(self.win, APP_ID)
            GtkLayerShell.set_layer(self.win, GtkLayerShell.Layer.TOP)
            GtkLayerShell.set_anchor(self.win, GtkLayerShell.Edge.BOTTOM, True)
            self.set_layer_keyboard(False)
        else:
            self.win.set_type_hint(Gdk.WindowTypeHint.UTILITY)
            self.win.stick()

        # WebView
        self.web = WebKit2.WebView()
        self.web.set_background_color(Gdk.RGBA(0, 0, 0, 0))
        s = self.web.get_settings()
        s.set_enable_developer_extras(False)
        s.set_enable_write_console_messages_to_stdout(True)
        s.set_allow_file_access_from_file_urls(True)        # sonidos y skin 3D desde archivos locales
        s.set_allow_universal_access_from_file_urls(True)
        s.set_media_playback_requires_user_gesture(False)   # sonidos sin tener que hacer clic antes
        try:
            s.set_enable_webgl(True)
        except Exception:
            pass
        self.web.connect('context-menu', lambda *a: True)   # sin menú de navegador al dar clic derecho
        self.web.connect('decide-policy', self.on_policy)

        ucm = self.web.get_user_content_manager()
        ucm.register_script_message_handler('host')
        ucm.connect('script-message-received::host', self.on_message)
        # La configuración del usuario se entrega antes de que corra la página
        boot = (f'window.__HOST_PLATFORM = "linux";'
                f'window.__HOST_CONFIG = {json.dumps(self.config, ensure_ascii=False)};')
        ucm.add_script(WebKit2.UserScript.new(boot, WebKit2.UserContentInjectedFrames.TOP_FRAME,
                                              WebKit2.UserScriptInjectionTime.START, None, None))

        self.win.add(self.web)
        self.win.connect('destroy', self.quit)
        self.web.connect('load-changed', self.on_load)

        html = self.app_dir / 'hotbar.html'
        if not html.exists():
            log(f'No se encontró {html}')
            sys.exit(1)
        # La página se sirve como hotbar://app/... (igual que https://hotbar.local en Windows):
        # todo queda en el mismo origen, así funcionan las máscaras del HUD, la skin 3D y los sonidos.
        ctx = self.web.get_context()
        ctx.register_uri_scheme('hotbar', self.on_scheme)
        sm = ctx.get_security_manager()
        for fn in ('register_uri_scheme_as_secure', 'register_uri_scheme_as_cors_enabled'):
            try:
                getattr(sm, fn)('hotbar')
            except Exception:
                pass
        self.web.load_uri('hotbar://app/hotbar.html')

        self.place(self.HOTBAR_W, self.HOTBAR_H)
        self.win.show_all()

        GLib.timeout_add(2000, self.tick_stats)
        GLib.timeout_add(3000, self.tick_running)

        # Mostrarse solo en el escritorio (como en Windows) cuando se puede saber qué ventana está activa
        self.only_desktop = bool(Wnck) and self.config.get('onlyOnDesktop', True) is not False
        if self.only_desktop:
            scr = Wnck.Screen.get_default()
            scr.connect('active-window-changed', lambda *a: self.update_visibility())
            GLib.timeout_add(1000, lambda: (self.update_visibility(), True)[1])

        # Señales desde otra copia (atajos de teclado del sistema)
        GLib.unix_signal_add(GLib.PRIORITY_DEFAULT, signal.SIGUSR1, self.on_toggle_signal)
        GLib.unix_signal_add(GLib.PRIORITY_DEFAULT, signal.SIGUSR2, self.on_inventory_signal)
        GLib.unix_signal_add(GLib.PRIORITY_DEFAULT, signal.SIGTERM, self.quit)
        GLib.unix_signal_add(GLib.PRIORITY_DEFAULT, signal.SIGINT, self.quit)
        self.make_tray()

    # ---------- configuración ----------
    def load_config(self):
        if not self.cfg_file.exists():
            src = self.root / 'linux' / 'config.linux.json'
            if not src.exists():
                src = self.app_dir / 'config.default.json'
            try:
                shutil.copyfile(src, self.cfg_file)
            except Exception as ex:
                log(f'No se pudo crear la configuración: {ex}')
        for f in (self.cfg_file, self.cfg_dir / 'config.backup.json'):
            try:
                return json.loads(f.read_text(encoding='utf-8'))
            except Exception:
                continue
        return {'slots': []}

    def save_config(self, cfg):
        try:
            tmp = self.cfg_file.with_suffix('.tmp')
            tmp.write_text(json.dumps(cfg, indent=2, ensure_ascii=False), encoding='utf-8')
            if self.cfg_file.exists():
                shutil.copyfile(self.cfg_file, self.cfg_dir / 'config.backup.json')
            os.replace(tmp, self.cfg_file)
            self.config = cfg
        except Exception as ex:
            log(f'Error guardando config: {ex}')

    # ---------- ventana ----------
    def on_draw(self, widget, cr):
        # Fondo 100 % transparente (si hay compositor)
        cr.set_source_rgba(0, 0, 0, 0)
        cr.set_operator(cairo.OPERATOR_SOURCE)
        cr.paint()
        cr.set_operator(cairo.OPERATOR_OVER)
        return False

    def set_layer_keyboard(self, on):
        if not HAS_LAYER_SHELL:
            return
        try:
            mode = GtkLayerShell.KeyboardMode.ON_DEMAND if on else GtkLayerShell.KeyboardMode.NONE
            GtkLayerShell.set_keyboard_mode(self.win, mode)
        except Exception:
            try:
                GtkLayerShell.set_keyboard_interactivity(self.win, on)
            except Exception:
                pass

    def place(self, w, h):
        self.win.set_size_request(w, h)
        self.win.resize(w, h)
        if HAS_LAYER_SHELL:
            return
        display = self.win.get_display()
        mon = display.get_primary_monitor() or display.get_monitor(0)
        if not mon:
            return
        area = mon.get_workarea()  # descuenta los paneles del escritorio
        self.win.move(area.x + (area.width - w) // 2, area.y + area.height - h)

    def set_regions(self, rects):
        """Solo la hotbar (y el inventario) recibe clics; el resto deja pasar el mouse."""
        try:
            reg = cairo.Region([cairo.RectangleInt(int(r['x']), int(r['y']), int(-(-r['w'] // 1)), int(-(-r['h'] // 1)))
                                for r in rects])
            self.win.input_shape_combine_region(reg)
            if not self.composited:
                self.win.shape_combine_region(reg)  # sin compositor: evita el cuadro negro
        except Exception as ex:
            log(f'Región: {ex}')

    def set_shown(self, show):
        if show == self.shown:
            return
        self.shown = show
        if show:
            self.win.show()
            self.win.set_keep_above(True)
            if not HAS_LAYER_SHELL:
                self.win.stick()
                self.place(*(self.MODAL_W, self.MODAL_H) if self.modal_open else (self.HOTBAR_W, self.HOTBAR_H))
        else:
            self.win.hide()

    def is_desktop_active(self):
        scr = Wnck.Screen.get_default()
        active = scr.get_active_window()
        if active is None:
            return True
        if active.get_window_type() == Wnck.WindowType.DESKTOP:
            return True
        return active.get_pid() == os.getpid()

    def update_visibility(self):
        if self.user_hidden:
            self.set_shown(False)
        elif self.modal_open or not self.only_desktop:
            self.set_shown(True)
        else:
            self.set_shown(self.is_desktop_active())

    def focus(self):
        self.set_layer_keyboard(True)
        self.win.present_with_time(Gtk.get_current_event_time() or int(time.time()))
        self.web.grab_focus()

    # ---------- señales ----------
    # ---------- ícono junto al reloj (como en Windows) ----------
    def make_tray(self):
        menu = Gtk.Menu()
        def item(label, fn):
            mi = Gtk.MenuItem(label=label)
            mi.connect('activate', lambda *_: fn())
            menu.append(mi)
        item('Mostrar / ocultar hotbar', self.on_toggle_signal)
        item('Abrir inventario (personalizar)', self.on_inventory_signal)
        item('Editar config.json', lambda: Gio.AppInfo.launch_default_for_uri(self.cfg_file.as_uri(), None))
        item('Reiniciar hotbar', self.restart)
        menu.append(Gtk.SeparatorMenuItem())
        item('Salir', self.quit)
        menu.show_all()
        self.tray_menu = menu
        icon = self.app_dir / 'assets' / 'items' / 'crafting_table.png'
        self.tray = None
        try:
            if AppIndicator:
                ind = AppIndicator.Indicator.new(APP_ID, str(icon), AppIndicator.IndicatorCategory.APPLICATION_STATUS)
                ind.set_status(AppIndicator.IndicatorStatus.ACTIVE)
                ind.set_title('Minecraft Desktop Hotbar')
                ind.set_menu(menu)
                self.tray = ind
            else:
                si = Gtk.StatusIcon.new_from_file(str(icon))
                si.set_tooltip_text('Minecraft Desktop Hotbar')
                si.connect('activate', lambda *_: self.on_toggle_signal())
                si.connect('popup-menu', lambda s, b, t: menu.popup(None, None, Gtk.StatusIcon.position_menu, s, b, t))
                self.tray = si
        except Exception as ex:
            log(f'Sin ícono en la bandeja: {ex}')

    def restart(self):
        try:
            if PID_FILE.exists():
                PID_FILE.unlink()
        except Exception:
            pass
        os.execv(sys.executable, [sys.executable, str(Path(__file__).resolve())])

    def hide_by_user(self):
        if self.modal_open:
            self.send({'action': 'toggleInventory'})
            return
        self.user_hidden = True
        self.set_shown(False)

    def on_toggle_signal(self):
        self.user_hidden = not self.user_hidden if self.shown or self.user_hidden else False
        if not self.user_hidden:
            self.set_shown(True)
            self.focus()
        else:
            self.set_shown(False)
        return True

    def on_inventory_signal(self):
        self.user_hidden = False
        self.set_shown(True)
        self.send({'action': 'toggleInventory'})
        return True

    def quit(self, *a):
        try:
            if PID_FILE.exists() and PID_FILE.read_text().strip() == str(os.getpid()):
                PID_FILE.unlink()
        except Exception:
            pass
        Gtk.main_quit()
        return False

    # ---------- comunicación con la página ----------
    def on_load(self, web, event):
        if event == WebKit2.LoadEvent.FINISHED:
            self.page_ready = True
            self.tick_stats()

    def on_scheme(self, request):
        try:
            rel = unquote(request.get_path() or '/').lstrip('/') or 'hotbar.html'
            base = self.app_dir.resolve()
            f = (base / rel).resolve()
            if base not in f.parents or not f.is_file():
                raise FileNotFoundError(rel)
            data = f.read_bytes()
            mime = mimetypes.guess_type(f.name)[0] or 'application/octet-stream'
            stream = Gio.MemoryInputStream.new_from_bytes(GLib.Bytes.new(data))
            request.finish(stream, len(data), mime)
        except Exception as ex:
            request.finish_error(GLib.Error.new_literal(Gio.io_error_quark(), str(ex), int(Gio.IOErrorEnum.NOT_FOUND)))

    def on_policy(self, web, decision, kind):
        # Nunca navegar fuera de hotbar.html (por ejemplo, al soltar un archivo fuera de una casilla)
        if kind == WebKit2.PolicyDecisionType.NAVIGATION_ACTION:
            uri = decision.get_navigation_action().get_request().get_uri()
            if not uri.endswith('hotbar.html'):
                decision.ignore()
                return True
        if kind == WebKit2.PolicyDecisionType.NEW_WINDOW_ACTION:
            decision.ignore()
            return True
        return False

    def send(self, payload):
        if not self.page_ready:
            return
        js = f'window.__dispatchHostMessage && window.__dispatchHostMessage({json.dumps(payload, ensure_ascii=False)});'
        try:
            self.web.evaluate_javascript(js, -1, None, None, None, None, None)
        except (AttributeError, TypeError):
            self.web.run_javascript(js, None, None, None)

    def on_message(self, ucm, result):
        try:
            try:
                raw = result.get_js_value().to_string()
            except AttributeError:
                raw = result.to_string()  # WebKitGTK 6 / versiones nuevas
            data = json.loads(raw)
            if isinstance(data, str):
                data = json.loads(data)
            action = data.get('action')

            if action == 'launch':
                self.launch(data.get('target', ''))
            elif action == 'saveConfig' and isinstance(data.get('config'), dict):
                self.save_config(data['config'])
            elif action == 'modalState':
                self.modal_open = bool(data.get('open'))
                if self.modal_open:
                    self.place(self.MODAL_W, self.MODAL_H)
                    self.focus()
                else:
                    self.place(self.HOTBAR_W, self.HOTBAR_H)
                    self.set_layer_keyboard(False)
                    if self.only_desktop:
                        GLib.timeout_add(300, lambda: (self.update_visibility(), False)[1])
            elif action == 'regions':
                self.set_regions(data.get('rects') or [])
            elif action == 'watchTargets':
                self.running.set_targets(data.get('targets') or [])
                self.tick_running()
            elif action == 'browseFile':
                self.pick_file()
            elif action == 'dropFiles':
                self.dropped(data)
            elif action == 'hideHotbar':
                self.hide_by_user()  # Esc: se vuelve a mostrar con el ícono del reloj o con minecraft-hotbar
        except Exception as ex:
            log(f'Mensaje de la página: {ex}')

    def tick_stats(self):
        self.send(self.stats.payload())
        return True

    def tick_running(self):
        try:
            self.send({'action': 'running', 'counts': self.running.count()})
        except Exception as ex:
            log(f'Apps abiertas: {ex}')
        return True

    # ---------- abrir apps ----------
    def launch(self, target):
        t = os.path.expandvars((target or '').strip().strip('"'))
        if not t:
            return
        if t == 'inventory':
            self.send({'action': 'toggleInventory'})
            return
        try:
            d = special_dir(t)
            if d:
                Gio.AppInfo.launch_default_for_uri(Path(d).as_uri(), None)
                return
            if t in ('terminal', 'powershell.exe', 'cmd.exe'):
                term = find_terminal()
                if term:
                    subprocess.Popen([term], cwd=str(Path.home()), start_new_session=True)
                return
            if is_uri(t):
                Gio.AppInfo.launch_default_for_uri(t, None)
                return
            path = os.path.expanduser(t)
            if path.startswith('file://'):
                path = unquote(urlparse(path).path)
            if os.path.exists(path):
                if path.endswith('.desktop'):
                    Gio.DesktopAppInfo.new_from_filename(path).launch([], None)
                elif os.path.isdir(path):
                    Gio.AppInfo.launch_default_for_uri(Path(path).as_uri(), None)
                elif os.access(path, os.X_OK):
                    subprocess.Popen([path], cwd=os.path.dirname(path), start_new_session=True)
                else:
                    Gio.AppInfo.launch_default_for_uri(Path(path).as_uri(), None)
                return
            # Comando ("code", "code ~/proyecto", "gnome-calculator"...) o app instalada (.desktop)
            parts = shlex.split(t)
            if parts and shutil.which(parts[0]):
                subprocess.Popen(parts, cwd=str(Path.home()), start_new_session=True)
                return
            info = Gio.DesktopAppInfo.new(parts[0] + '.desktop') if parts else None
            if info:
                info.launch([], None)
                return
            log(f'No se encontró cómo abrir: {t}')
        except Exception as ex:
            log(f'No se pudo abrir "{t}": {ex}')

    def pick_file(self):
        dlg = Gtk.FileChooserNative.new('Seleccionar programa, archivo o carpeta', self.win,
                                        Gtk.FileChooserAction.OPEN, 'Abrir', 'Cancelar')
        apps = '/usr/share/applications'
        if os.path.isdir(apps):
            dlg.set_current_folder(apps)
        if dlg.run() == Gtk.ResponseType.ACCEPT:
            path = dlg.get_filename()
            if path:
                self.send({'action': 'fileSelected', 'path': path})
        dlg.destroy()

    def dropped(self, data):
        for uri in data.get('uris') or []:
            if not uri.startswith('file://'):
                continue
            path = unquote(urlparse(uri).path)
            is_dir = os.path.isdir(path)
            name = os.path.basename(path.rstrip('/')) if is_dir else Path(path).stem
            if path.endswith('.desktop'):
                try:
                    info = Gio.DesktopAppInfo.new_from_filename(path)
                    name = info.get_display_name() or name
                except Exception:
                    pass
            self.send({'action': 'fileDropped', 'group': data.get('group'), 'idx': int(data.get('idx', 0)),
                       'path': path, 'name': name, 'isFolder': is_dir})
            return


def main():
    RUNTIME_DIR.mkdir(parents=True, exist_ok=True)
    PID_FILE.write_text(str(os.getpid()))
    here = Path(__file__).resolve().parent
    root = here.parent if here.name == 'linux' else here
    HotbarApp(root)
    Gtk.main()


if __name__ == '__main__':
    main()
