# 🗡️ Minecraft Desktop Hotbar & Inventory for Windows

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Windows 10 / 11](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows&logoColor=white)](https://microsoft.com/windows)
[![License: GPL-3.0](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![RAM Usage](https://img.shields.io/badge/RAM-~60MB-brightgreen)]()
[![Status](https://img.shields.io/badge/Status-Stable%20v1.0.0-success)]()

Una barra de acceso rápido flotante (**Dock**) y lanzador de aplicaciones interactivo para Windows, inspirado fielmente en la **Hotbar de 9 casillas y el Inventario de Supervivencia de Minecraft Java Edition**.

Permite navegar entre tus programas y juegos favoritos usando la **rueda del ratón** o los **números del 1 al 9**, con efectos de sonido originales de Minecraft (`click.ogg`, `orb.ogg`, `levelup.ogg`), tooltip con estilo oficial de lore, HUD Hardcore con monitoreo de hardware en tiempo real (RAM, CPU, Batería/Disco) y una ventana modal de **Inventario (tecla `E`)** con un **personaje 3D que sigue el cursor del ratón en tiempo real**.

---

## ✨ Características Principales

* 🧱 **Estética Oficial Pixel-Perfect:** Texturas de interfaz originales de Minecraft Java (`hotbar.png`, `inventory_gui.png`, `hotbar_selection.png`) renderizadas con nitidez absoluta (`image-rendering: pixelated;`) y la tipografía auténtica *Minecraft*.
* 👤 **Modelo de Jugador 3D con Seguimiento de Cursor:**
  * Renderizado tridimensional acelerado por hardware con WebGL.
  * La **cabeza y el torso del personaje siguen dinámicamente el cursor del ratón** a lo largo de toda la interfaz del inventario, exactamente como en el juego.
  * Animación suave de respiración y reposo en los brazos.
  * Fácilmente personalizable colocando cualquier skin estándar (`skin.png` de 64x64) en la carpeta `assets/`.
* 🖥️ **Visibilidad Inteligente en el Escritorio (Auto-Hide):**
  * La Hotbar se muestra de forma nativa cuando estás en el **Escritorio de Windows** o en la barra de tareas.
  * Cuando abres o trabajas en cualquier aplicación en ventana (**Chrome, Discord, VS Code, Word, etc.**), la barra **se oculta automáticamente** para no estorbar en tu flujo de trabajo.
  * Al minimizar a tu escritorio (<kbd>Win</kbd> + <kbd>D</kbd>) o alternar ventanas, vuelve a aparecer al instante.
* 📊 **HUD Hardcore en Tiempo Real:**
  * 💖 **Corazones Hardcore (RAM):** 10 corazones con indicador numérico exacto de memoria en uso (`RAM: 42% (6.8 / 16.0 GB)`). ¡Si la memoria sube de golpe, los corazones parpadean con animación de daño!
  * 🍗 **Muslos de Comida (Batería / Disco):** Representa el porcentaje de batería en portátiles (con icono de rayo al cargar) o espacio libre en disco en PC de escritorio.
  * 🟢 **Barra de Experiencia y Nivel (CPU):** El número de nivel verde muestra el porcentaje de uso actual de la CPU, y la barra de XP se llena progresivamente a lo largo del día.
* 🎒 **El Inventario de Supervivencia Clásico (Tecla `E`):**
  * Se abre en el **centro de tu pantalla** para máxima comodidad.
  * Cuadrícula de 27 casillas + 9 casillas inferiores sincronizadas con tu Hotbar.
  * Casillas de armadura para accesos rápidos y ranura para mano secundaria (escudo).
  * Menú popover integrado para personalizar el nombre, la ruta (.exe, carpeta o URL) y el icono de cualquier casilla.
  * Catálogo de recetas con decenas de ítems clásicos transparentes de alta resolución.
* 🖱️ **Navegación Táctil y con Scroll:** Usa la rueda del ratón (`Mouse Wheel`) directamente sobre la barra para alternar entre casillas con el chasquido oficial de Minecraft (`click.ogg`).
* ⌨️ **Atajos de Teclado (1-9 y Enter):** Presiona `1` a `9` para saltar de inmediato a cualquier ranura, y `Enter` o `Espacio` para activarla.
* 💬 **Tooltip Flotante con Lore:** Marco violeta degradado (`#100010`) auto-ajustable con bordes inteligentes que evitan que el texto se corte en los extremos.
* 🚀 **Rendimiento Ultraligero:**
  * Consumo mínimo de memoria (~60 MB de RAM).
  * **0% de uso de CPU** cuando está en reposo (las animaciones 3D se pausan completamente al cerrar el inventario).
  * Prioridad de proceso optimizada para no afectar los FPS en juegos.
* 🔔 **Bandeja del Sistema (System Tray):** Menú rápido junto al reloj de Windows para ocultar/mostrar la barra, abrir el inventario o salir.

---

## 🎮 Controles y Atajos

| Acción | Control | Descripción |
| :--- | :--- | :--- |
| **Cambiar casilla** | `Rueda del Ratón` o `Teclas 1 - 9` | Selecciona la casilla con sonido de clic |
| **Abrir programa** | `Clic Izquierdo` o `Enter` | Lanza la aplicación configurada con sonido de orbe de XP |
| **Abrir Inventario** | `Tecla E` o `Bandeja del Sistema` | Despliega el inventario centrado para personalizar casillas |
| **Cerrar Inventario** | `Tecla Escape`, `Tecla E` o `Botón [✕]` | Cierra el inventario y guarda tu configuración |
| **Ocultar / Mostrar Hotbar** | `Tecla Escape` (en escritorio) o `Doble clic en bandeja` | Alterna manualmente la visibilidad de la Hotbar |

---

## 📦 Casillas Preconfiguradas (Por Defecto)

| Casilla | Ítem de Minecraft | Destino por Defecto |
| :---: | :--- | :--- |
| **1** | ⚔️ **Espada de Netherite** | **Juegos / Steam** (`steam:`) |
| **2** | 💿 **Disco de Música** | **Spotify** (`spotify:`) |
| **3** | 🔮 **Ender Pearl** | **Visual Studio Code** (`code`) |
| **4** | 🧭 **Brújula de Navegación** | **Navegador Web** (`https://www.google.com`) |
| **5** | 📁 **Cofre de Madera** | **Carpeta Descargas** (`shell:Downloads`) |
| **6** | 💬 **Poción de Rapidez** | **Discord** (`discord:`) |
| **7** | 💎 **Diamante Pulido** | **Terminal PowerShell** (`powershell.exe`) |
| **8** | 🍎 **Manzana Dorada** | **Mis Documentos** (`shell:Personal`) |
| **9** | ⚙️ **Mesa de Crafteo** | **Inventario & Ajustes** (`inventory`) |

---

## 🛠️ Instalación y Uso Rápido

### Opción 1: Versión Portátil (Recomendada)
1. Descarga el archivo `MinecraftHotbar_v1.0.0_Portable.zip` desde la pestaña [Releases](https://github.com/Jpsb3/MinecraftHotbar/releases).
2. Descomprime la carpeta en cualquier ubicación.
3. Haz doble clic en `MinecraftHotbar.exe`.
4. ¡Listo! La barra flotará sobre tu escritorio.

```

---

## 🎨 Cambiar tu Skin Personalizada
Para cambiar la skin del modelo 3D por la tuya propia:
1. Consigue tu skin de Minecraft en formato estándar PNG (64x64 píxeles).
2. Renómbrala a `skin.png`.
3. Pégala dentro de la carpeta `app/assets/` reemplazando la existente.
4. Abre el inventario con <kbd>E</kbd> para ver a tu personaje con su nueva skin.

---

## 📄 Licencia

Este proyecto está bajo la Licencia **GNU General Public License v3.0** (GPL-3.0). Consulta el archivo [LICENSE](LICENSE) para más detalles.
Minecraft es una marca registrada de Mojang Synergies AB. Este proyecto es una creación comunitaria sin fines comerciales.
