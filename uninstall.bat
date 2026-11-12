@echo off
setlocal EnableExtensions
rem ==========================================================================
rem  uninstall.bat -- removes every file added by install.bat.
rem  Original game files and the save game are never touched, so no Steam
rem  "verify integrity" or reinstall is needed afterwards.
rem
rem  Usage:
rem      uninstall.bat                              find the game automatically
rem      uninstall.bat "X:\SteamLibrary\steamapps\common\Pony Island"
rem      ...or drag the game folder onto uninstall.bat
rem
rem  Safety: the script stops unless the folder holds the marker file
rem  .pony_accessibility_patch that install.bat wrote, so a folder that was
rem  not installed by this pack is left alone.
rem ==========================================================================

call "%~dp0scripts\find-game.bat" "%~1"
if errorlevel 1 (
    echo.
    echo Nothing was removed.
    pause
    exit /b 1
)

rem --- the game must be closed, or its files are locked ---------------------
tasklist /fi "imagename eq PonyIsland.exe" 2>nul | findstr /i /c:"PonyIsland.exe" >nul
if not errorlevel 1 (
    echo.
    echo ERROR: Pony Island is still running.
    echo Close the game, then run this script again.
    echo Nothing was removed.
    pause
    exit /b 1
)

if not exist "%GAME%\.pony_accessibility_patch" (
    echo.
    echo ERROR: "%GAME%" has no marker file from this patch
    echo          ".pony_accessibility_patch", so nothing here was installed
    echo by this pack. Refusing to delete anything.
    echo.
    echo If you installed it in a different folder, run:
    echo     uninstall.bat "X:\path\to\Pony Island"
    pause
    exit /b 1
)

echo.
echo Files to remove from "%GAME%":
echo     winhttp.dll, winhttp.dll.disabled, doorstop_config.ini
echo     BepInEx\  (loader, plugin and its configuration, including your log files)
echo Save games are stored in the registry and are not part of this list.
choice /c YN /m "Remove them now"
if errorlevel 2 (
    echo Cancelled. Nothing was removed.
    pause
    exit /b 1
)

echo.
echo Removing patch files from "%GAME%" ...
if exist "%GAME%\winhttp.dll" del /q "%GAME%\winhttp.dll"
if exist "%GAME%\winhttp.dll.disabled" del /q "%GAME%\winhttp.dll.disabled"
if exist "%GAME%\doorstop_config.ini" del /q "%GAME%\doorstop_config.ini"
if exist "%GAME%\BepInEx" rmdir /s /q "%GAME%\BepInEx"
del /q "%GAME%\.pony_accessibility_patch"

if exist "%GAME%\winhttp.dll" (
    echo.
    echo WARNING: winhttp.dll could not be deleted, most likely because the
    echo game is still running. Close it and run uninstall.bat again.
    pause
    exit /b 1
)

echo Done. The game folder is back to an unmodified installation.
pause
exit /b 0
