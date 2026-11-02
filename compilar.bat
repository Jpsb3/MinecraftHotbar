@echo off
echo ========================================================
echo   Compilando Minecraft Desktop Hotbar (.NET 8 Win-x64)
echo ========================================================
echo.

rem Cerrar la hotbar si esta abierta, ya sea como MinecraftHotbar.exe o
rem como "dotnet MinecraftHotbar.dll" (asi la abren algunos editores).
rem Si sigue abierta, Windows no deja reemplazar los archivos de la carpeta app.
powershell -NoProfile -Command "Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'MinecraftHotbar.exe' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -like '*MinecraftHotbar*') } | ForEach-Object { Write-Host ('Cerrando copia abierta: ' + $_.Name + ' (' + $_.ProcessId + ')'); Stop-Process -Id $_.ProcessId -Force }"
timeout /t 2 /nobreak >nul

cd /d "%~dp0src"
dotnet publish -c Release -r win-x64 --self-contained false -o "%~dp0app"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================================
    echo   [OK] Compilacion exitosa. Abriendo la hotbar...
    echo ========================================================
    start "" "%~dp0app\MinecraftHotbar.exe"
) else (
    echo.
    echo [ERROR] Hubo un problema durante la compilacion.
)

pause
