#!/usr/bin/env bash
# ========================================================
#  Minecraft Desktop Hotbar - instalador para Linux
#  Instala dependencias, crea el comando "minecraft-hotbar",
#  el acceso en el menú de apps y (opcional) el inicio automático.
# ========================================================
set -e

DIR="$(cd "$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")" && pwd)"
ROOT="$(dirname "$DIR")"

echo "== Minecraft Desktop Hotbar - instalación =="

if command -v apt-get >/dev/null; then
    sudo apt-get update
    sudo apt-get install -y python3 python3-gi python3-gi-cairo gir1.2-gtk-3.0 gir1.2-wnck-3.0 \
        gstreamer1.0-plugins-good gstreamer1.0-plugins-base
    sudo apt-get install -y gir1.2-webkit2-4.1 || sudo apt-get install -y gir1.2-webkit2-4.0
    sudo apt-get install -y gir1.2-gtklayershell-0.1 2>/dev/null || true
    sudo apt-get install -y gir1.2-ayatanaappindicator3-0.1 2>/dev/null || true
elif command -v pacman >/dev/null; then
    sudo pacman -S --needed --noconfirm python python-gobject python-cairo gtk3 webkit2gtk-4.1 libwnck3 \
        gst-plugins-good gst-plugins-base
    sudo pacman -S --needed --noconfirm gtk-layer-shell libayatana-appindicator 2>/dev/null || true
elif command -v dnf >/dev/null; then
    sudo dnf install -y python3 python3-gobject python3-cairo gtk3 webkit2gtk4.1 libwnck3 \
        gstreamer1-plugins-good gstreamer1-plugins-base
    sudo dnf install -y gtk-layer-shell libayatana-appindicator-gtk3 2>/dev/null || true
elif command -v zypper >/dev/null; then
    sudo zypper install -y python3-gobject python3-gobject-cairo typelib-1_0-Gtk-3_0 typelib-1_0-WebKit2-4_1 \
        typelib-1_0-Wnck-3_0 gstreamer-plugins-good
else
    echo "[!] No reconocí tu gestor de paquetes. Instala: Python 3, PyGObject (+cairo), GTK 3, WebKit2GTK 4.1, libwnck3 y GStreamer."
fi

chmod +x "$DIR/minecraft-hotbar.py" "$DIR/run.sh"

# Comando "minecraft-hotbar"
mkdir -p "$HOME/.local/bin"
ln -sf "$DIR/minecraft-hotbar.py" "$HOME/.local/bin/minecraft-hotbar"

# Ícono (PNG del bloque de crafteo) y acceso en el menú de aplicaciones
ICON_DIR="$HOME/.local/share/icons/hicolor/32x32/apps"
mkdir -p "$ICON_DIR" "$HOME/.local/share/applications"
cp "$ROOT/app/assets/items/crafting_table.png" "$ICON_DIR/minecraft-hotbar.png" 2>/dev/null || true
sed "s|^Exec=minecraft-hotbar|Exec=$HOME/.local/bin/minecraft-hotbar|" "$DIR/minecraft-hotbar.desktop" \
    > "$HOME/.local/share/applications/minecraft-hotbar.desktop"

read -r -p "¿Abrir la hotbar al iniciar sesión? [s/N] " r
if [[ "$r" =~ ^[sSyY] ]]; then
    mkdir -p "$HOME/.config/autostart"
    cp "$HOME/.local/share/applications/minecraft-hotbar.desktop" "$HOME/.config/autostart/"
    echo "[OK] Inicio automático activado"
fi

echo ""
echo "[OK] Listo. Ábrela desde el menú de apps o con:  minecraft-hotbar"
echo "     Consejo: crea un atajo de teclado del sistema que ejecute"
echo "     'minecraft-hotbar --inventory' para abrir el inventario desde cualquier parte."
