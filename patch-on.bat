@echo off
setlocal EnableExtensions
rem ==========================================================================
rem  patch-on.bat -- re-enables the mod loader after patch-off.bat.
rem  It only renames winhttp.dll.disabled back to winhttp.dll, so nothing
rem  inside the game changes. The in-game F8 / F9 switches keep their own
rem  saved state, which is stored in BepInEx\config.
rem
rem  Usage: patch-on.bat ["X:\SteamLibrary\steamapps\common\Pony Island"]
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
if exist "%GAME%\winhttp.dll.disabled" (
    ren "%GAME%\winhttp.dll.disabled" "winhttp.dll"
    if errorlevel 1 (
        set "RC=1"
        echo ERROR: the file could not be renamed. Close the game and try again.
    ) else (
        echo Mod loader re-enabled in "%GAME%".
        echo The patch loads again at the next start; F8 and F9 keep their saved state.
    )
) else if exist "%GAME%\winhttp.dll" (
    echo Already enabled - "%GAME%\winhttp.dll" is in place.
) else (
    set "RC=1"
    echo ERROR: winhttp.dll was not found in "%GAME%".
    echo The patch is not installed there; run install.bat first.
)
pause
exit /b %RC%
