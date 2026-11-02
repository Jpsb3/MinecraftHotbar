# 🗡️ Minecraft Desktop Hotbar & Inventory

[![Descargas](https://img.shields.io/github/downloads/Jpsb3/MinecraftHotbar/total?label=Descargas&color=brightgreen)](https://github.com/Jpsb3/MinecraftHotbar/releases)
[![Versión](https://img.shields.io/github/v/release/Jpsb3/MinecraftHotbar?label=Versi%C3%B3n)](https://github.com/Jpsb3/MinecraftHotbar/releases/latest)
[![Windows 10 / 11](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?logo=windows&logoColor=white)](#-descarga-e-instalaci%C3%B3n)
[![Linux](https://img.shields.io/badge/Linux-X11%20%7C%20Wayland-FCC624?logo=linux&logoColor=black)](#-linux-support-x11--wayland)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Python 3](https://img.shields.io/badge/Python-3.8+-3776AB?logo=python&logoColor=white)](https://www.python.org/)
[![License: GPL-3.0](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

La **hotbar de 9 casillas** y el **inventario de supervivencia** de Minecraft Java Edition, convertidos en un lanzador de aplicaciones para el escritorio de Windows. Con HUD de corazones, comida y experiencia que muestran el estado real de tu PC.

> **English:** A Minecraft-style hotbar & survival inventory app launcher for the Windows desktop. Hearts show free RAM, food shows battery (or disk), the level shows CPU usage, open apps get the enchantment glint, and you unlock achievements as you use it. Free, open source, unofficial fan project.

> [!NOTE]
> Proyecto de fans, **gratuito y no oficial**. No está asociado con Mojang ni Microsoft.

---

## ✨ Qué hace

### 🎮 Hotbar
* **9 casillas** con tus apps, juegos, carpetas o páginas web.
* **Rueda del mouse** o **teclas 1-9** para moverte entre casillas, con el sonido del juego.
* **Clic** (o `Enter`) para abrir la app de la casilla.
* **Tooltip morado** con el nombre y la descripción de cada casilla.
* **Solo aparece en el escritorio:** cuando trabajas en otra ventana o juegas en pantalla completa, se esconde sola.

### ✨ Apps abiertas
* **Brillo de encantamiento:** las casillas de las apps que tienes abiertas brillan como un ítem encantado.
* **Número de stack:** si una app tiene varias ventanas abiertas, la casilla muestra cuántas, como una pila de ítems.

### ❤️ HUD con el estado de tu PC
| En el juego | En tu PC |
| :--- | :--- |
| ❤️ Corazones | RAM libre (si la RAM sube de golpe, los corazones parpadean como al recibir daño) |
| 🍗 Comida | Batería en laptops, o espacio libre del disco en PC de escritorio |
| 🟢 Número de nivel | Uso de CPU en % |
| Barra de experiencia | Cuánto del día ha pasado |

Pasa el mouse sobre cualquiera para ver el dato exacto.

### 🎒 Inventario (tecla `E`)
* Inventario de supervivencia con **27 casillas extra**, armadura, escudo y mesa de crafteo 2x2.
* **Personaje 3D** que sigue tu mouse. Puedes poner tu propia skin.
* **Mueve ítems** entre casillas como en el juego (clic para tomar, clic para soltar).
* **Clic derecho** en una casilla para editar su nombre, ruta e ícono.
* **Doble clic** en cualquier casilla del inventario para abrir su app.
* **Libro de recetas** para elegir el ícono de cada casilla.

### 📥 Arrastrar y soltar
Arrastra un acceso directo, un `.exe` o una carpeta desde el escritorio hasta una casilla para asignarla al instante.

### 🏆 Logros
Avisos estilo Minecraft con sonido, por ejemplo: *¡Primer clic!*, *Rutina diaria*, *¡Diamantes!* (100 apps abiertas), *Multitarea* (5 apps a la vez), *Noctámbulo*... y alguno escondido.

---

## 📥 Descarga e instalación

1. Ve a **[Releases](https://github.com/Jpsb3/MinecraftHotbar/releases/latest)** y descarga **`MinecraftHotbar_v1.0.0_Portable.zip`** (en *Assets*).
   > ⚠️ No descargues "Source code": es solo el código, sin la app lista para usar.
2. Descomprime el zip donde quieras (por ejemplo, en `Documentos`).
3. Abre **`MinecraftHotbar.exe`**.

**No necesitas instalar nada:** la versión portátil ya incluye .NET. Solo requiere Windows 10 u 11 de 64 bits con WebView2, que viene incluido en Windows actualizado.

> [!NOTE]
> Como es una app de un desarrollador independiente (sin firma digital de pago), Windows SmartScreen puede mostrar *"Windows protegió tu PC"*. Haz clic en **Más información → Ejecutar de todas formas**. El código completo está en este repositorio.

---

## 🐧 Linux Support (X11 & Wayland)

A native Linux version is available in the `linux/` directory, built with **Python 3, GTK 3, and WebKit2GTK** (No Electron, ultra-lightweight ~45MB RAM).

### Quick Install & Run (Linux):
```bash
git clone https://github.com/Jpsb3/MinecraftHotbar.git
cd MinecraftHotbar/linux
chmod +x install.sh run.sh
./install.sh
```

Supported on:
* **Ubuntu / Debian / Linux Mint:** `sudo apt install python3-gi gir1.2-webkit2-4.1 gir1.2-gtk-3.0`
* **Arch Linux / Manjaro / SteamOS:** `sudo pacman -S python-gobject webkit2gtk-4.1 gtk3`
* **Fedora:** `sudo dnf install python3-gobject webkit2gtk4.1 gtk3`

See [linux/README.md](linux/README.md) for full Linux documentation and custom configs.

---

| Acción | Cómo |
| :--- | :--- |
| Cambiar de casilla | Rueda del mouse sobre la barra, o teclas `1`-`9` en el escritorio |
| Abrir la app | Clic en la casilla, o `Enter` |
| Abrir / cerrar el inventario | `E` en el escritorio, o desde el ícono junto al reloj |
| Editar una casilla | Clic derecho en el inventario |
| Asignar una app rápido | Arrastra su acceso directo a una casilla |
| Ocultar / mostrar la hotbar | `Esc` en el escritorio |
| Menú (mostrar, inventario, salir) | Clic derecho en el ícono junto al reloj |

Las teclas `E`, `1`-`9` y `Esc` solo funcionan cuando estás en el escritorio y no estás escribiendo, así que nunca interfieren con otras apps.

---

## 🎨 Usar tu propia skin
1. Descarga tu skin de Minecraft en PNG (64x64).
2. Renómbrala `skin.png`.
3. Reemplaza el archivo `assets/skin.png`.
4. Abre el inventario con `E`.

---

## ⚙️ Configuración

Todo se guarda solo en `config.json` al editar desde el inventario. `config.default.json` tiene la configuración inicial. Opciones extra:

* `"showHud": false` esconde los corazones, la comida y la barra de XP.
* `"showSlotNumbers": true` muestra los números 1-9 en las casillas.

---

## 🛠️ Compilar desde el código

Requiere el [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) en Windows.

```bash
git clone https://github.com/Jpsb3/MinecraftHotbar.git
cd MinecraftHotbar
compilar.bat            # compila en app/ y la abre (para probar)
crear_release_zip.bat   # crea el zip portátil autocontenido
```

**Cómo funciona por dentro:** una ventana de C# (.NET 8, WinForms) siempre encima, sin bordes y recortada a la forma exacta de lo que se ve, con **WebView2** dibujando la interfaz en HTML/CSS/JS. C# lee la RAM, CPU, batería y disco, detecta qué apps están abiertas y escucha el teclado y la rueda del mouse en el escritorio.

---

## 📄 Licencia

* El **código** de este proyecto es **GPL-3.0** (ver [LICENSE](LICENSE)): puedes usarlo, modificarlo y compartirlo, pero si distribuyes una versión modificada debe seguir siendo abierta y con crédito.
* Las **texturas, sonidos, la fuente y el estilo visual de Minecraft** son propiedad de **Mojang Studios / Microsoft** y **no** están cubiertos por la GPL-3.0. Se usan solo en este proyecto de fans gratuito y sin fines comerciales.
* Librerías de terceros (skinview3d, three.js) en [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Minecraft es una marca registrada de Mojang Synergies AB. Proyecto no oficial, no aprobado ni asociado con Mojang ni Microsoft.

Hecho por **[Jpsb3](https://github.com/Jpsb3)**.
