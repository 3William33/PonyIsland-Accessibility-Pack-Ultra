@echo off
setlocal EnableExtensions
rem ==========================================================================
rem  patch-off.bat -- temporarily disables the mod loader by renaming
rem  winhttp.dll to winhttp.dll.disabled. The game then starts without
rem  BepInEx at all, which is the closest thing to a vanilla launch that
rem  keeps your BepInEx setup in place. Nothing is deleted; patch-on.bat
rem  reverses this. Original game files and the save game are untouched.
rem
rem  Usage: patch-off.bat ["X:\SteamLibrary\steamapps\common\Pony Island"]
rem  ...or drag the game folder onto this script.
rem ==========================================================================

call "%~dp0scripts\find-game.bat" "%~1"
if errorlevel 1 (
    echo.
    echo Nothing was changed.
    pause
    exit /b 1
)

rem --- the game must be closed, or winhttp.dll is locked --------------------
tasklist /fi "imagename eq PonyIsland.exe" 2>nul | findstr /i /c:"PonyIsland.exe" >nul
if not errorlevel 1 (
    echo.
    echo ERROR: Pony Island is still running.
    echo Close the game, then run this script again.
    echo Nothing was changed.
    pause
    exit /b 1
)

set "RC=0"
if exist "%GAME%\winhttp.dll" (
    ren "%GAME%\winhttp.dll" "winhttp.dll.disabled"
    if errorlevel 1 (
        set "RC=1"
        echo ERROR: the file could not be renamed. Close the game and try again.
    ) else (
        echo Mod loader disabled in "%GAME%".
        echo Start the game normally and it runs without the patch.
    )
) else if exist "%GAME%\winhttp.dll.disabled" (
    echo Already disabled - the loader is switched off in "%GAME%".
) else (
    set "RC=1"
    echo ERROR: winhttp.dll was not found in "%GAME%".
    echo The patch is not installed there; nothing to disable.
)
pause
exit /b %RC%
