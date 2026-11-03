# 🐧 Minecraft Desktop Hotbar para Linux

La misma hotbar de Windows (mismo `hotbar.html`, texturas, sonidos, HUD, inventario 3D y logros) en una ventana nativa **GTK 3 + WebKit2GTK**. Sin Electron.

> **Beta.** Si algo falla en tu distro, abre un *issue* con el nombre de la distro y del escritorio.
>
> **English:** Same hotbar as the Windows version, running in a native GTK 3 + WebKit2GTK window. Install with `./install.sh`, run `minecraft-hotbar`. Tested setups and limitations below.

## Instalar

```bash
git clone https://github.com/Jpsb3/MinecraftHotbar.git
cd MinecraftHotbar/linux
chmod +x install.sh
./install.sh
```

El instalador detecta tu distro (Ubuntu/Debian/Mint, Arch/Manjaro, Fedora, openSUSE), instala las dependencias y crea:
* el comando `minecraft-hotbar`
* el acceso en el menú de aplicaciones
* (opcional) el inicio automático al entrar a la sesión

En Arch también puedes usar el `PKGBUILD` de esta carpeta (`makepkg -si`).

## Uso

| Acción | Cómo |
| :--- | :--- |
| Abrir la hotbar | `minecraft-hotbar` o desde el menú de apps |
| Mostrar / ocultar | `Esc` oculta · ícono junto al reloj o volver a ejecutar `minecraft-hotbar` la muestra |
| Abrir el inventario | Clic en la hotbar y tecla `E`, la casilla 9, o `minecraft-hotbar --inventory` |
| Cambiar de casilla | Rueda del mouse sobre la barra, o teclas `1`-`9` después de hacer clic en ella |
| Menú (mostrar, inventario, editar config, reiniciar, salir) | Ícono junto al reloj |
| Cerrar | Ícono junto al reloj → Salir, o `minecraft-hotbar --quit` |

**Tecla global:** Linux no deja que una app escuche el teclado de todo el sistema, así que crea un atajo en la configuración de tu escritorio (por ejemplo `Super+E`) que ejecute `minecraft-hotbar --inventory`.

## Diferencias con Windows

| | X11 (Ubuntu con Xorg, Mint, XFCE, KDE X11...) | Wayland con layer-shell (KDE Plasma 6, Hyprland, Sway) | GNOME en Wayland |
| :--- | :--- | :--- | :--- |
| Se oculta cuando usas otra ventana | ✅ | ❌ siempre visible | ✅ (usa XWayland) |
| Brillo de apps abiertas | ✅ | ✅ | ✅ |
| Número de ventanas por app | ✅ | ❌ solo el brillo | ⚠️ solo apps X11 |

* Para que el fondo sea transparente necesitas un escritorio con efectos/compositor (todos los modernos lo tienen). Sin compositor la barra se recorta igual, pero sin bordes suaves.
* Rutas de casillas: comandos (`code`, `gnome-calculator`), archivos `.desktop`, carpetas (`~/Proyectos`), enlaces (`https://`, `steam:`, `spotify:`), `terminal` y `shell:Downloads` / `shell:Personal` (Descargas / Documentos).
* Puedes arrastrar apps y carpetas desde tu gestor de archivos a una casilla.

## Configuración

Se guarda en `~/.config/minecraft-hotbar/config.json` (con copia `config.backup.json`). Opción extra de Linux:

* `"onlyOnDesktop": false` hace que la hotbar quede siempre visible.

## Dependencias

Python 3, PyGObject (+ cairo), GTK 3, WebKit2GTK 4.1 (o 4.0), GStreamer (sonidos). Opcionales: `libwnck3` (X11), `gtk-layer-shell` (Wayland) y `libayatana-appindicator` (ícono junto al reloj; en GNOME requiere la extensión AppIndicator, que Ubuntu ya trae).

## Licencia

GPL-3.0, igual que el resto del proyecto. Las texturas, sonidos y fuente de Minecraft son de Mojang Studios / Microsoft. Proyecto de fans no oficial.
