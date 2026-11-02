#!/usr/bin/env bash
# ========================================================
# Minecraft Desktop Hotbar - Linux Installer
# Automatically installs dependencies and sets up launcher
# ========================================================

set -e

echo "========================================================"
echo "  Minecraft Desktop Hotbar - Linux Setup"
echo "========================================================"
echo ""

# Detect package manager
if command -v apt-get &> /dev/null; then
    echo "[*] Detected Debian / Ubuntu / Linux Mint system"
    echo "[*] Installing dependencies with apt..."
    sudo apt-get update -y
    sudo apt-get install -y python3 python3-gi python3-gi-cairo gir1.2-webkit2-4.1 gir1.2-gtk-3.0

elif command -v pacman &> /dev/null; then
    echo "[*] Detected Arch Linux / Manjaro / SteamOS"
    echo "[*] Installing dependencies with pacman..."
    sudo pacman -S --needed --noconfirm python python-gobject webkit2gtk-4.1 gtk3

elif command -v dnf &> /dev/null; then
    echo "[*] Detected Fedora / RHEL system"
    echo "[*] Installing dependencies with dnf..."
    sudo dnf install -y python3 python3-gobject webkit2gtk4.1 gtk3

elif command -v zypper &> /dev/null; then
    echo "[*] Detected openSUSE system"
    echo "[*] Installing dependencies with zypper..."
    sudo zypper install -y python3-gobject typelib-1_0-WebKit2-4_1 gtk3

else
    echo "[!] Could not detect package manager. Please ensure python3, PyGObject, and WebKit2GTK are installed."
fi

# Ensure executable permissions
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
chmod +x "$DIR/minecraft-hotbar.py"
chmod +x "$DIR/run.sh" 2>/dev/null || true

# Optional: Install .desktop launcher into ~/.local/share/applications/
DESKTOP_ENTRY="$HOME/.local/share/applications/minecraft-hotbar.desktop"
if [ -f "$DIR/minecraft-hotbar.desktop" ]; then
    mkdir -p "$HOME/.local/share/applications"
    sed "s|Exec=.*|Exec=$DIR/minecraft-hotbar.py|g" "$DIR/minecraft-hotbar.desktop" > "$DESKTOP_ENTRY"
    sed -i "s|Icon=.*|Icon=$DIR/../app/icon.ico|g" "$DESKTOP_ENTRY"
    chmod +x "$DESKTOP_ENTRY"
    echo "[OK] Installed desktop launcher: $DESKTOP_ENTRY"
fi

echo ""
echo "========================================================"
echo "  [OK] Installation complete!"
echo "  Run './run.sh' or 'python3 linux/minecraft-hotbar.py'"
echo "========================================================"
