@echo off
setlocal EnableExtensions
rem ==========================================================================
rem  find-game.bat -- locates the Pony Island folder on this machine.
rem  Shared by install.bat, uninstall.bat, patch-on.bat and patch-off.bat.
rem
rem  Usage from another script:
rem      call "%~dp0scripts\find-game.bat" "%~1"
rem      if errorlevel 1 exit /b 1
rem      rem  GAME now holds the game folder
rem
rem  Exits with errorlevel 0 and GAME set to the game folder, or with
rem  errorlevel 1 and GAME empty. Nothing is created, moved or deleted here.
rem
rem  Search order -- the first location that really contains PonyIsland.exe
rem  wins, so no machine-specific path is ever assumed:
rem      1. the path given as the first argument (drag and drop included)
rem      2. game-path.txt next to this pack (saved by install.bat)
rem      3. the PONYPATH environment variable
rem      4. Steam: registry keys, default install folders, libraryfolders.vdf
rem      5. a prompt, so any custom or non-default location still works
rem ==========================================================================

set "GAME="
set "PACK=%~dp0.."

rem ---- 1. argument --------------------------------------------------------
if not "%~1"=="" call :consider "%~1"
if defined GAME goto :found

rem ---- 2. location remembered by a previous install -----------------------
call :from_file
if defined GAME goto :found

rem ---- 3. explicit override ----------------------------------------------
call :from_env
if defined GAME goto :found

rem ---- 4. automatic detection --------------------------------------------
call :from_steam
if defined GAME goto :found

rem ---- 5. last resort: ask ------------------------------------------------
call :ask
if defined GAME goto :found

echo.
echo ERROR: Pony Island was not found.
echo Pass the folder as an argument, for example:
echo     install.bat "X:\SteamLibrary\steamapps\common\Pony Island"
echo or drag the game folder onto the script, or set PONYPATH.
endlocal & set "GAME="
exit /b 1

:found
echo Game folder: "%GAME%"
endlocal & set "GAME=%GAME%"
exit /b 0

rem --------------------------------------------------------------------------
rem  Accepts a game folder, the PonyIsland.exe file, a Steam root or a Steam
rem  library root, and keeps it only if PonyIsland.exe is actually there.
rem  The result is only ever written to GAME after that check, so a candidate
rem  that does not contain the game can never leak out as a "found" path.
rem --------------------------------------------------------------------------
:consider
set "HIT="
set "C=%*"
set C=%C:"=%
set C=%C:/=\%
set C=%C:\\=\%
if not defined C exit /b 0
if "%C:~-1%"=="\" set "C=%C:~0,-1%"
if not defined C exit /b 0
if exist "%C%\PonyIsland.exe" set "HIT=%C%"
if not defined HIT if exist "%C%\steamapps\common\Pony Island\PonyIsland.exe" set "HIT=%C%\steamapps\common\Pony Island"
if not defined HIT if exist "%C%\Pony Island\PonyIsland.exe" set "HIT=%C%\Pony Island"
if not defined HIT if /i "%C:~-14%"=="ponyisland.exe" for %%X in ("%C%") do set "HIT=%%~dpX"
if not defined HIT exit /b 0
if "%HIT:~-1%"=="\" set "HIT=%HIT:~0,-1%"
set "GAME=%HIT%"
exit /b 0

rem --------------------------------------------------------------------------
rem  Path saved by install.bat after a successful install, so the other three
rem  scripts do not have to detect or ask again.
rem --------------------------------------------------------------------------
:from_file
if not exist "%PACK%\game-path.txt" exit /b 0
set "MEM="
set /p MEM=<"%PACK%\game-path.txt"
if not defined MEM exit /b 0
call :consider "%MEM%"
exit /b 0

:from_env
if not defined PONYPATH exit /b 0
call :consider "%PONYPATH%"
exit /b 0

rem --------------------------------------------------------------------------
rem  Automatic detection.
rem --------------------------------------------------------------------------
:from_steam
rem a) Steam's own registry entries (normal installs write one of these)
call :regget "HKCU\Software\Valve\Steam" SteamPath
if defined GAME exit /b 0
call :regget "HKCU\Software\Valve\Steam" InstallPath
if defined GAME exit /b 0
call :regget "HKLM\SOFTWARE\Valve\Steam" InstallPath
if defined GAME exit /b 0
call :regget "HKLM\SOFTWARE\WOW6432Node\Valve\Steam" InstallPath
if defined GAME exit /b 0

rem b) the usual install locations, every drive
for %%D in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    for %%L in ("%%D:\steam" "%%D:\SteamLibrary" "%%D:\Games\Steam" "%%D:\Program Files (x86)\Steam" "%%D:\Program Files\Steam") do (
        if not defined GAME if exist "%%~L\steamapps\common\Pony Island\PonyIsland.exe" set "GAME=%%~L\steamapps\common\Pony Island"
    )
)
if defined GAME exit /b 0

rem c) extra libraries Steam keeps in libraryfolders.vdf
for %%D in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    for %%L in ("%%D:\steam" "%%D:\SteamLibrary" "%%D:\Program Files (x86)\Steam") do (
        if not defined GAME (
            if exist "%%~L\steamapps\libraryfolders.vdf" call :vdf "%%~L\steamapps\libraryfolders.vdf"
            if not defined GAME if exist "%%~L\config\libraryfolders.vdf" call :vdf "%%~L\config\libraryfolders.vdf"
        )
    )
)
exit /b 0

:regget
rem %~1 = registry key, %~2 = value name; silent when the key is absent
set "RV="
for /f "usebackq tokens=2,*" %%A in (`reg query "%~1" /v %~2 2^>nul`) do set "RV=%%B"
if not defined RV exit /b 0
call :consider "%RV%"
exit /b 0

:vdf
rem %~1 = libraryfolders.vdf; every "path" entry is one Steam library
for /f "usebackq tokens=2" %%A in (`findstr /i /c:"\"path\"" "%~1" 2^>nul`) do (
    if not defined GAME call :consider "%%~A\steamapps\common\Pony Island"
)
exit /b 0

rem --------------------------------------------------------------------------
rem  Interactive fallback: a custom path always works.
rem --------------------------------------------------------------------------
:ask
echo.
echo Pony Island was not found automatically.
echo In Steam: Library, right-click Pony Island, Manage, Browse local files,
echo then copy the address of that folder.
echo Type the full path of the folder that contains PonyIsland.exe.
echo Example: X:\SteamLibrary\steamapps\common\Pony Island
set "ANS="
set /p ANS=  Game folder: 
if not defined ANS exit /b 0
call :consider "%ANS%"
exit /b 0
