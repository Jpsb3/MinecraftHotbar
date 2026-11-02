@echo off
chcp 65001 > nul
echo ========================================================
echo   Empaquetando Minecraft Desktop Hotbar Portable ZIP
echo ========================================================
echo.

set "ROOT=%~dp0"
set "APP_DIR=%ROOT%app"
set "ZIP_OUT=%ROOT%MinecraftHotbar_v1.0.0_Portable.zip"

python -c "import zipfile, os; src = r'%APP_DIR%'; out = r'%ZIP_OUT%'; z = zipfile.ZipFile(out, 'w', zipfile.ZIP_DEFLATED); [z.write(os.path.join(r, f), os.path.relpath(os.path.join(r, f), src)) for r, d, files in os.walk(src) for f in files if not f.endswith('.log') and not f.endswith('.pdb')]; z.close(); print('[OK] ZIP generado: ' + str(round(os.path.getsize(out)/1024, 1)) + ' KB')"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================================
    echo   [OK] Paquete portatil listo en:
    echo   %ZIP_OUT%
    echo ========================================================
) else (
    echo [ERROR] No se pudo crear el archivo ZIP.
)

pause
