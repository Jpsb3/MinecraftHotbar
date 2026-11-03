#!/usr/bin/env bash
# Abre la Minecraft Desktop Hotbar (si ya está abierta, la muestra u oculta)
DIR="$(cd "$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")" && pwd)"
exec python3 "$DIR/minecraft-hotbar.py" "$@"
