#!/usr/bin/env bash
# Launch Minecraft Desktop Hotbar for Linux
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
python3 "$DIR/minecraft-hotbar.py" "$@"
