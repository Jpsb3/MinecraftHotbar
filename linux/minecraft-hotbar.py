#!/usr/bin/env python3
"""
Minecraft Desktop Hotbar - Linux Edition
A pixel-perfect, interactive Minecraft HUD dock for Linux (X11 & Wayland).
Built with Python 3, GTK 3, and WebKit2GTK.

Author: Juan Pablo Bustos (@Jpsb3)
License: MIT
"""

import os
import sys
import json
import time
import glob
import shutil
import signal
import subprocess
from pathlib import Path

# Required PyGObject bindings
try:
    import gi
    gi.require_version('Gtk', '3.0')
    gi.require_version('Gdk', '3.0')
    try:
        gi.require_version('WebKit2', '4.1')
    except (ValueError, AttributeError):
        gi.require_version('WebKit2', '4.0')
    from gi.repository import Gtk, Gdk, WebKit2, GLib
except ImportError as e:
    print(f"[Error] Missing dependencies: {e}", file=sys.stderr)
    print("Please install PyGObject and WebKit2GTK for your distribution.", file=sys.stderr)
    print("  Ubuntu/Debian: sudo apt install python3-gi python3-gi-cairo gir1.2-webkit2-4.1", file=sys.stderr)
    print("  Arch/Manjaro:  sudo pacman -S python-gobject webkit2gtk-4.1", file=sys.stderr)
    print("  Fedora:        sudo dnf install python3-gobject webkit2gtk4.1", file=sys.stderr)
    sys.exit(1)

# Optional Wayland Layer Shell support (Hyprland, Sway, Wayfire, KDE 6)
HAS_LAYER_SHELL = False
try:
    gi.require_version('GtkLayerShell', '0.1')
    from gi.repository import GtkLayerShell
    if GtkLayerShell.is_supported():
        HAS_LAYER_SHELL = True
except Exception:
    HAS_LAYER_SHELL = False


class SystemStats:
    """Reads real-time Linux hardware metrics from /proc and /sys."""

    def __init__(self):
        self._last_idle = 0
        self._last_total = 0
        self._has_cpu = False
        self._last_disk_time = 0
        self._disk_free_pct = 100
        self._disk_free_gb = 0
        self._disk_total_gb = 0

    def read_cpu(self) -> int:
        try:
            with open('/proc/stat', 'r') as f:
                fields = [int(x) for x in f.readline().split()[1:]]
            idle = fields[3] + fields[4]
            total = sum(fields)

            if not self._has_cpu:
                self._last_idle = idle
                self._last_total = total
                self._has_cpu = True
                return 0

            d_idle = idle - self._last_idle
            d_total = total - self._last_total
            self._last_idle = idle
            self._last_total = total

            if d_total > 0:
                usage = 100.0 * (d_total - d_idle) / d_total
                return int(max(0, min(100, round(usage))))
        except Exception:
            pass
        return 0

    def read_ram(self):
        mem_total = 0
        mem_avail = 0
        try:
            with open('/proc/meminfo', 'r') as f:
                for line in f:
                    if line.startswith('MemTotal:'):
                        mem_total = int(line.split()[1])
                    elif line.startswith('MemAvailable:'):
                        mem_avail = int(line.split()[1])
                    if mem_total and mem_avail:
                        break

            if mem_total > 0:
                total_gb = mem_total / (1024.0 * 1024.0)
                used_gb = (mem_total - mem_avail) / (1024.0 * 1024.0)
                pct = int(round(100.0 * (mem_total - mem_avail) / mem_total))
                return pct, round(used_gb, 1), round(total_gb, 1)
        except Exception:
            pass
        return 0, 0.0, 0.0

    def read_battery(self):
        # Scan /sys/class/power_supply/ for BAT*
        bat_dirs = glob.glob('/sys/class/power_supply/BAT*')
        if not bat_dirs:
            return None, False

        try:
            bat_dir = bat_dirs[0]
            capacity_file = os.path.join(bat_dir, 'capacity')
            status_file = os.path.join(bat_dir, 'status')

            capacity = None
            if os.path.exists(capacity_file):
                with open(capacity_file, 'r') as f:
                    capacity = int(f.read().strip())

            charging = False
            if os.path.exists(status_file):
                with open(status_file, 'r') as f:
                    status = f.read().strip().lower()
                    charging = (status == 'charging')

            return capacity, charging
        except Exception:
            return None, False

    def read_disk(self):
        now = time.time()
        if now - self._last_disk_time > 30:
            try:
                usage = shutil.disk_usage('/')
                self._disk_total_gb = usage.total / (1024.0 ** 3)
                self._disk_free_gb = usage.free / (1024.0 ** 3)
                self._disk_free_pct = 100.0 * usage.free / usage.total
            except Exception:
                pass
            self._last_disk_time = now

        return (
            int(round(self._disk_free_pct)),
            round(self._disk_free_gb),
            round(self._disk_total_gb)
        )

    def get_stats_payload(self) -> dict:
        ram_pct, ram_used_gb, ram_total_gb = self.read_ram()
        cpu_pct = self.read_cpu()
        battery_pct, is_charging = self.read_battery()
        disk_pct, disk_free_gb, disk_total_gb = self.read_disk()

        return {
            "action": "stats",
            "ram": ram_pct,
            "ramUsedGB": ram_used_gb,
            "ramTotalGB": ram_total_gb,
            "cpu": cpu_pct,
            "battery": battery_pct,
            "charging": is_charging,
            "diskFree": disk_pct,
            "diskFreeGB": disk_free_gb,
            "diskTotalGB": disk_total_gb
        }


class RunningAppsMonitor:
    """Scans running process names in /proc to track active hotbar apps."""

    def __init__(self):
        self._targets = []

    def set_targets(self, targets):
        self._targets = targets or []

    def count(self) -> dict:
        counts = {}
        for t in self._targets:
            counts[t] = 0

        if not self._targets:
            return counts

        try:
            # Quick /proc scan on Linux (extremely fast, zero overhead)
            pids = [pid for pid in os.listdir('/proc') if pid.isdigit()]
            for pid in pids:
                try:
                    comm_path = os.path.join('/proc', pid, 'comm')
                    if os.path.exists(comm_path):
                        with open(comm_path, 'r') as f:
                            comm = f.read().strip().lower()
                            for t in self._targets:
                                t_clean = t.lower()
                                if t_clean in comm or comm in t_clean:
                                    counts[t] = counts.get(t, 0) + 1
                except (IOError, PermissionError):
                    continue
        except Exception:
            pass

        return counts


class MinecraftHotbarApp:
    """Main GTK3 + WebKit2GTK Application Window."""

    HOTBAR_WIDTH = 600
    HOTBAR_HEIGHT = 260
    MODAL_WIDTH = 900
    MODAL_HEIGHT = 620

    def __init__(self, root_dir: Path):
        self.root_dir = root_dir
        self.app_dir = root_dir / 'app'
        self.stats = SystemStats()
        self.running_monitor = RunningAppsMonitor()
        self.is_modal_open = False

        # Config paths (XDG compliant)
        config_home = Path(os.environ.get('XDG_CONFIG_HOME', Path.home() / '.config'))
        self.user_config_dir = config_home / 'minecraft-hotbar'
        self.user_config_dir.mkdir(parents=True, exist_ok=True)
        self.config_file = self.user_config_dir / 'config.json'

        self._ensure_config()

        # Build GTK Window
        self.window = Gtk.Window(type=Gtk.WindowType.TOPLEVEL)
        self.window.set_title("Minecraft Desktop Hotbar")
        self.window.set_decorated(False)
        self.window.set_skip_taskbar_hint(True)
        self.window.set_skip_pager_hint(True)
        self.window.set_keep_above(True)
        self.window.set_app_paintable(True)

        # True RGBA transparency
        screen = self.window.get_screen()
        visual = screen.get_rgba_visual()
        if visual and screen.is_composited():
            self.window.set_visual(visual)

        # Wayland Layer Shell support or X11 Dock
        if HAS_LAYER_SHELL:
            GtkLayerShell.init_for_window(self.window)
            GtkLayerShell.set_layer(self.window, GtkLayerShell.Layer.TOP)
            GtkLayerShell.set_anchor(self.window, GtkLayerShell.Edge.BOTTOM, True)
            GtkLayerShell.set_margin(self.window, GtkLayerShell.Edge.BOTTOM, 0)
        else:
            self.window.set_type_hint(Gdk.WindowTypeHint.DOCK)

        # WebKit2GTK WebView setup
        self.webview = WebKit2.WebView()
        self.webview.set_background_color(Gdk.RGBA(0, 0, 0, 0))

        settings = self.webview.get_settings()
        settings.set_enable_developer_extras(False)
        settings.set_enable_write_console_messages_to_stdout(False)
        settings.set_javascript_can_access_clipboard(True)

        # IPC Script Message Handler ("host")
        ucm = self.webview.get_user_content_manager()
        ucm.register_script_message_handler("host")
        ucm.connect("script-message-received::host", self._on_script_message)

        # Container
        self.window.add(self.webview)

        # Signals
        self.window.connect('destroy', Gtk.main_quit)
        self.window.connect('scroll-event', self._on_scroll_event)
        self.window.connect('key-press-event', self._on_key_press_event)

        # Load hotbar.html
        html_path = self.app_dir / 'hotbar.html'
        if html_path.exists():
            self.webview.load_uri(html_path.as_uri())
        else:
            print(f"[Error] hotbar.html not found at: {html_path}", file=sys.stderr)

        self._reposition(self.HOTBAR_WIDTH, self.HOTBAR_HEIGHT)
        self.window.show_all()

        # Timers
        GLib.timeout_add(2000, self._timer_stats)
        GLib.timeout_add(3000, self._timer_running)

    def _ensure_config(self):
        """Ensures a valid config.json exists in user config dir."""
        if not self.config_file.exists():
            default_linux_cfg = self.root_dir / 'linux' / 'config.linux.json'
            default_app_cfg = self.app_dir / 'config.default.json'
            source = default_linux_cfg if default_linux_cfg.exists() else default_app_cfg

            if source.exists():
                shutil.copyfile(source, self.config_file)
            else:
                # Fallback minimal config
                self.config_file.write_text('{"slots": []}', encoding='utf-8')

    def _reposition(self, width: int, height: int):
        """Centers window horizontally above the bottom of the screen."""
        if HAS_LAYER_SHELL:
            self.window.set_size_request(width, height)
            return

        screen = self.window.get_screen()
        display = screen.get_display()
        monitor = display.get_primary_monitor() or display.get_monitor(0)
        if monitor:
            geom = monitor.get_geometry()
            x = geom.x + (geom.width - width) // 2
            y = geom.y + geom.height - height - 36 # 36px above taskbar
            self.window.resize(width, height)
            self.window.move(x, y)

    def _send_to_js(self, payload: dict):
        """Sends JSON payload to hotbar.html through the universal bridge."""
        json_str = json.dumps(payload)
        script = f"if (window.__dispatchHostMessage) window.__dispatchHostMessage({json_str});"
        self.webview.run_javascript(script, None, None, None)

    def _on_script_message(self, ucm, js_result):
        """Handles messages sent from hotbar.html postMessage()."""
        try:
            raw = js_result.get_js_value().to_string()
            data = json.loads(raw)
            action = data.get('action')

            if action == 'saveConfig':
                cfg = data.get('config')
                if cfg:
                    with open(self.config_file, 'w', encoding='utf-8') as f:
                        json.dump(cfg, f, indent=2, ensure_ascii=False)

            elif action == 'modalState':
                open_state = bool(data.get('open', False))
                self.is_modal_open = open_state
                if open_state:
                    self._reposition(self.MODAL_WIDTH, self.MODAL_HEIGHT)
                else:
                    self._reposition(self.HOTBAR_WIDTH, self.HOTBAR_HEIGHT)

            elif action == 'hideHotbar':
                if self.is_modal_open:
                    self._send_to_js({"action": "toggleInventory"})
                else:
                    self.window.iconify()

            elif action == 'browseFile':
                self._show_file_picker()

            elif action == 'watchTargets':
                targets = data.get('targets', [])
                self.running_monitor.set_targets(targets)

            elif action == 'launch':
                target = data.get('target', '')
                self._launch_target(target)

        except Exception as ex:
            print(f"[IPC Error] {ex}", file=sys.stderr)

    def _launch_target(self, target: str):
        """Launches Linux desktop apps, shell commands, or URLs."""
        if not target:
            return

        target = target.strip()

        # Inventory shortcut
        if target == 'inventory':
            self._send_to_js({"action": "toggleInventory"})
            return

        # Special URI schemes (Steam, Spotify, Discord, web)
        if target.startswith(('http://', 'https://', 'steam:', 'spotify:', 'discord:')):
            subprocess.Popen(['xdg-open', target])
            return

        # Special folder shortcuts
        if target.startswith('shell:Downloads') or target == '~/Downloads':
            downloads = os.path.expanduser('~/Downloads')
            subprocess.Popen(['xdg-open', downloads])
            return

        if target.startswith('shell:Personal') or target == '~':
            home = os.path.expanduser('~')
            subprocess.Popen(['xdg-open', home])
            return

        # Terminal launcher
        if target in ('terminal', 'powershell.exe', 'cmd.exe'):
            terminals = ['x-terminal-emulator', 'gnome-terminal', 'kitty', 'alacritty', 'konsole', 'xterm']
            for term in terminals:
                if shutil.which(term):
                    subprocess.Popen([term])
                    return
            return

        # Expand user path (~/...)
        expanded = os.path.expanduser(target)
        if os.path.exists(expanded):
            if os.path.isdir(expanded):
                subprocess.Popen(['xdg-open', expanded])
            else:
                subprocess.Popen([expanded])
            return

        # Standard system command / binary
        try:
            subprocess.Popen(target, shell=True)
        except Exception as ex:
            print(f"[Launch Error] Could not start '{target}': {ex}", file=sys.stderr)

    def _show_file_picker(self):
        """Native GTK file picker dialog for configuring slot paths."""
        dialog = Gtk.FileChooserDialog(
            title="Select Application or File",
            parent=self.window,
            action=Gtk.FileChooserAction.OPEN
        )
        dialog.add_buttons(
            Gtk.STOCK_CANCEL, Gtk.ResponseType.CANCEL,
            Gtk.STOCK_OPEN, Gtk.ResponseType.OK
        )

        res = dialog.run()
        if res == Gtk.ResponseType.OK:
            filename = dialog.get_filename()
            dialog.destroy()
            if filename:
                name = os.path.splitext(os.path.basename(filename))[0]
                self._send_to_js({
                    "action": "filePicked",
                    "path": filename,
                    "name": name
                })
        else:
            dialog.destroy()

    def _on_scroll_event(self, widget, event):
        """Mouse wheel scroll event selects hotbar slots."""
        if event.direction == Gdk.ScrollDirection.UP:
            self._send_to_js({"action": "scroll", "delta": -1})
            return True
        elif event.direction == Gdk.ScrollDirection.DOWN:
            self._send_to_js({"action": "scroll", "delta": 1})
            return True
        return False

    def _on_key_press_event(self, widget, event):
        """Keyboard event listener."""
        keyval = event.keyval

        # Escape closes modal or minimizes
        if keyval == Gdk.KEY_Escape:
            if self.is_modal_open:
                self._send_to_js({"action": "toggleInventory"})
                return True

        # Number keys 1-9
        if Gdk.KEY_1 <= keyval <= Gdk.KEY_9:
            slot_idx = keyval - Gdk.KEY_1 + 1
            self._send_to_js({"action": "select", "slot": slot_idx})
            return True

        # Key 'E' toggles inventory
        if keyval in (Gdk.KEY_e, Gdk.KEY_E):
            self._send_to_js({"action": "toggleInventory"})
            return True

        return False

    def _timer_stats(self) -> bool:
        """Periodic hardware stats update."""
        payload = self.stats.get_stats_payload()
        self._send_to_js(payload)
        return True

    def _timer_running(self) -> bool:
        """Periodic running apps watcher update."""
        counts = self.running_monitor.count()
        self._send_to_js({"action": "running", "counts": counts})
        return True


def main():
    # Handle Ctrl+C cleanly
    signal.signal(signal.SIGINT, signal.SIG_DFL)

    script_path = Path(__file__).resolve()
    # If located in 'linux/', root is parent directory
    root_dir = script_path.parent.parent if script_path.parent.name == 'linux' else script_path.parent

    app = MinecraftHotbarApp(root_dir)
    Gtk.main()


if __name__ == '__main__':
    main()
