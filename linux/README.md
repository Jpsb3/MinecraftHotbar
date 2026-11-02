# 🐧 Minecraft Desktop Hotbar for Linux

An authentic, ultra-lightweight **Minecraft Java Edition** HUD dock and application launcher for Linux desktops (X11 & Wayland).

Built natively with **Python 3, GTK 3, and WebKit2GTK** (No Electron!).

---

## 🌟 Highlights

* 🪶 **No Electron:** Native GTK3 + WebKit2GTK container consuming only **~45 MB RAM** and **0% CPU** at idle.
* 🖥️ **X11 & Wayland Support:** True RGBA transparency, floating dock mode (`_NET_WM_WINDOW_TYPE_DOCK`) and native Wayland Layer Shell support (`gtk-layer-shell`).
* 📦 **Interactive 3D Player & Custom Skins:** Real-time 3D Minecraft character in the inventory inspecting your cursor.
* 🔊 **Authentic Sounds:** Original sound effects (`click.ogg`, `orb.ogg`, `levelup.ogg`, etc.).
* 📊 **Real-time Linux System Monitoring:**
  * **Hearts (HP):** Real RAM consumption parsed directly from `/proc/meminfo`.
  * **Food Bar:** Battery level or Root filesystem disk usage.
  * **Level Number:** Real-time CPU utilization parsed from `/proc/stat`.
* ⚡ **App Launching:** Native support for `.desktop` apps, Steam games (`steam:`), Spotify, VS Code, terminals, and custom shell scripts.

---

## 🚀 Quick Start & Installation

### Option 1: Automatic Installer
Clone the repository and run the setup script:
```bash
git clone https://github.com/Jpsb3/MinecraftDesktopHotbar.git
cd MinecraftDesktopHotbar/linux
chmod +x install.sh run.sh
./install.sh
```

---

### Option 2: Manual Installation by Distribution

#### **Ubuntu / Debian / Linux Mint / Pop!_OS**
```bash
sudo apt update
sudo apt install -y python3 python3-gi python3-gi-cairo gir1.2-webkit2-4.1 gir1.2-gtk-3.0
```

#### **Arch Linux / Manjaro / EndeavourOS / SteamOS**
```bash
sudo pacman -S --needed python python-gobject webkit2gtk-4.1 gtk3
# Optional for Wayland compositors (Hyprland / Sway):
sudo pacman -S gtk-layer-shell
```

#### **Fedora / RHEL**
```bash
sudo dnf install -y python3 python3-gobject webkit2gtk4.1 gtk3
```

#### **openSUSE**
```bash
sudo zypper install -y python3-gobject typelib-1_0-WebKit2-4_1 gtk3
```

---

## 🎮 Running the Hotbar

From the project root directory:
```bash
python3 linux/minecraft-hotbar.py
```
Or simply:
```bash
./linux/run.sh
```

---

## ⌨️ Controls & Keybindings

* **Mouse Wheel:** Scroll to switch active hotbar slots with authentic click sound.
* **Keys 1 to 9:** Jump directly to any hotbar slot.
* **Key E:** Open the **Survival Inventory & Settings modal** to customize your slots, items, and shortcuts.
* **Key ESC:** Close the inventory modal or minimize the hotbar.

---

## ⚙️ Configuration

Custom shortcuts and settings are saved at:
```bash
~/.config/minecraft-hotbar/config.json
```
You can edit it directly or use the in-game GUI by pressing <kbd>E</kbd> or clicking slot 9.
