@echo off
setlocal
rem ============================================================
rem  Crea el ZIP para el Release de GitHub.
rem  Version AUTOCONTENIDA: incluye .NET dentro del .exe, asi
rem  funciona en cualquier PC sin instalar nada.
rem  (compilar.bat es solo para probar en tu PC.)
rem ============================================================
set "VERSION=1.0.0"
set "ROOT=%~dp0"
set "TMP_DIR=%ROOT%release_tmp"
set "PKG=%ROOT%release_tmp\MinecraftHotbar"
set "ZIP_OUT=%ROOT%MinecraftHotbar_v%VERSION%_Portable.zip"

echo ========================================================
echo   Creando MinecraftHotbar_v%VERSION%_Portable.zip
echo ========================================================
echo.

echo [1/4] Cerrando la hotbar si esta abierta...
powershell -NoProfile -Command "Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'MinecraftHotbar.exe' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -like '*MinecraftHotbar*') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }"
timeout /t 2 /nobreak >nul

echo [2/4] Compilando version autocontenida (tarda un poco)...
if exist "%TMP_DIR%" rmdir /s /q "%TMP_DIR%"
cd /d "%ROOT%src"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o "%PKG%"
if errorlevel 1 goto :error

echo [3/4] Copiando interfaz, recursos y licencias...
xcopy "%ROOT%app\assets" "%PKG%\assets" /E /I /Y /Q >nul
copy /Y "%ROOT%app\hotbar.html" "%PKG%\hotbar.html" >nul
copy /Y "%ROOT%app\icon.ico" "%PKG%\icon.ico" >nul
copy /Y "%ROOT%app\config.default.json" "%PKG%\config.default.json" >nul
copy /Y "%ROOT%app\config.default.json" "%PKG%\config.json" >nul
copy /Y "%ROOT%LICENSE" "%PKG%\LICENSE" >nul
copy /Y "%ROOT%THIRD_PARTY_NOTICES.md" "%PKG%\THIRD_PARTY_NOTICES.md" >nul
del /q "%PKG%\*.xml" "%PKG%\*.pdb" 2>nul
if not exist "%PKG%\MinecraftHotbar.exe" goto :error
if not exist "%PKG%\hotbar.html" goto :error

echo [4/4] Comprimiendo...
if exist "%ZIP_OUT%" del /q "%ZIP_OUT%"
powershell -NoProfile -Command "Compress-Archive -Path '%PKG%' -DestinationPath '%ZIP_OUT%' -CompressionLevel Optimal"
if errorlevel 1 goto :error
rmdir /s /q "%TMP_DIR%"

echo.
echo ========================================================
echo   [OK] Listo: %ZIP_OUT%
echo   Subelo en GitHub - Releases - Assets.
echo ========================================================
pause
exit /b 0

:error
echo.
echo [ERROR] Algo fallo. Revisa los mensajes de arriba.
pause
exit /b 1
