@echo off
setlocal EnableExtensions
rem ==========================================================================
rem  install.bat -- installs Pony Island Accessibility Mode
rem  (BepInEx 5.4.23.3 x86 + the plugin) by ADDING files to the game folder.
rem  No original game file is edited or replaced.
rem
rem  Usage:
rem      install.bat                              find the game automatically
rem      install.bat "X:\SteamLibrary\steamapps\common\Pony Island"
rem      ...or drag the game folder (or PonyIsland.exe) onto install.bat
rem  scripts\find-game.bat documents the whole search order, including the
rem  PONYPATH override for non-default or custom installations.
rem
rem  Refuses to run when a mod loader is already present, when the game is
rem  still running, and when the pack is incomplete.
rem ==========================================================================

set "PAYLOAD=%~dp0payload"

call "%~dp0scripts\find-game.bat" "%~1"
if errorlevel 1 (
    echo.
    echo Nothing was changed.
    pause
    exit /b 1
)

rem --- the game must be closed, or its files are locked ---------------------
tasklist /fi "imagename eq PonyIsland.exe" 2>nul | findstr /i /c:"PonyIsland.exe" >nul
if not errorlevel 1 (
    echo.
    echo ERROR: Pony Island is still running.
    echo Close the game - including any "running" entry in Steam -
    echo and then run this script again.
    echo Nothing was changed.
    pause
    exit /b 1
)

rem --- the pack itself has to be complete ----------------------------------
if not exist "%PAYLOAD%\winhttp.dll" (
    echo.
    echo ERROR: this pack is incomplete, "%PAYLOAD%\winhttp.dll" is missing.
    echo Extract the whole archive again and keep the folder structure.
    echo Nothing was changed.
    pause
    exit /b 1
)
if not exist "%PAYLOAD%\BepInEx\plugins\PonyIslandAccessibility\PonyIslandAccessibility.dll" (
    echo.
    echo ERROR: this pack is incomplete, the plugin DLL is missing.
    echo Extract the whole archive again and keep the folder structure.
    echo Nothing was changed.
    pause
    exit /b 1
)

rem --- never overwrite something that is already there ---------------------
if exist "%GAME%\BepInEx" (
    echo.
    echo ERROR: "%GAME%\BepInEx" already exists.
    echo A BepInEx loader or another mod is already installed here, so this
    echo script stops instead of overwriting it. Nothing was changed.
    echo If it was installed by this pack, run uninstall.bat first.
    pause
    exit /b 1
)
if exist "%GAME%\winhttp.dll" (
    echo.
    echo ERROR: "%GAME%\winhttp.dll" already exists.
    echo Refusing to overwrite it. Nothing was changed.
    echo If it was installed by this pack, run uninstall.bat first.
    pause
    exit /b 1
)
if exist "%GAME%\winhttp.dll.disabled" (
    echo.
    echo NOTE: a disabled winhttp.dll is present, so patch-off.bat was used here.
    echo Either run patch-on.bat to re-enable it, or uninstall.bat to remove it,
    echo then run install.bat again. Nothing was changed.
    pause
    exit /b 1
)

echo.
echo Installing into "%GAME%" ...
xcopy "%PAYLOAD%\*" "%GAME%\" /E /I /Y >nul
if errorlevel 1 (
    echo ERROR: the copy failed. Check that the folder is writable and that
    echo your antivirus is not blocking winhttp.dll, then run install.bat again.
    pause
    exit /b 1
)

>"%GAME%\.pony_accessibility_patch" echo Installed by PonyIsland Accessibility Mode
rem Remember the folder so patch-on/off and uninstall find it without asking.
>"%~dp0game-path.txt" echo %GAME% 2>nul

echo.
echo Done. Everything is in place.
echo Start the game from Steam and press F8 for Accessibility Mode, F9 for the
echo final-boss assist. Both switches are OFF until you press them.
echo patch-off.bat disables the loader, uninstall.bat removes every file added now.
pause
exit /b 0
