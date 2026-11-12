@echo off
setlocal EnableExtensions
rem ==========================================================================
rem  build.bat -- recompiles AccessibilityPlugin.cs into
rem  PonyIslandAccessibility.dll without editing any path first.
rem
rem  What it locates automatically:
rem    compiler   .NET Framework 4 csc.exe  (override with PONYCSC)
rem    game       the Pony Island folder, through scripts\find-game.bat,
rem               because the plugin is compiled against the game's own
rem               Unity and mscorlib assemblies
rem    game API   <game folder>\PonyIsland_Data\Managed
rem    loader     BepInEx.dll and 0Harmony.dll from payload\BepInEx\core
rem
rem  Usage:
rem      build.bat                                   detect everything
rem      build.bat "X:\SteamLibrary\steamapps\common\Pony Island"
rem      build.bat /copy                             also replace the DLL in payload\
rem  Only /copy writes into the pack; without it the result stays in
rem  src\build\ and payload\ keeps the tested DLL.
rem
rem  Nothing here touches the game folder: building only reads from it.
rem
rem  build.rsp is deliberately comment-free and path-free. The C# compiler
rem  rejects "//" lines inside a response file, so the explanation of each
rem  option lives here instead: the game assemblies are named in build.rsp
rem  and their folder is handed over with -lib below, and the two loader
rem  DLLs come from payload\BepInEx\core through the relative -lib already
rem  written in build.rsp.
rem ==========================================================================

set "SRC=%~dp0"
set "PACK=%~dp0.."
set "COPY=0"
set "GARG="

for %%A in (%*) do (
    if /i "%%~A"=="/copy" set "COPY=1"
    if not "%%~A"=="" if not defined GARG if /i not "%%~A"=="/copy" set "GARG=%%~A"
)

rem ---- 1. the C# compiler -------------------------------------------------
set "CSC="
if defined PONYCSC if exist "%PONYCSC%" set "CSC=%PONYCSC%"
for %%P in (
    "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) do if not defined CSC if exist "%%~P" set "CSC=%%~P"
if not defined CSC (
    echo ERROR: csc.exe was not found.
    echo Install the .NET Framework 4 developer pack, or point the compiler out
    echo yourself before running this script:
    echo     set PONYCSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
    exit /b 1
)
echo Compiler:  %CSC%

rem ---- 2. the game, so its Managed assemblies can be referenced -----------
call "%PACK%\scripts\find-game.bat" "%GARG%"
if errorlevel 1 exit /b 1
set "MANAGED=%GAME%\PonyIsland_Data\Managed"
if not exist "%MANAGED%" (
    echo.
    echo ERROR: "%MANAGED%" does not exist.
    echo That does not look like a Unity install of Pony Island.
    exit /b 1
)
echo References: %MANAGED%

rem ---- 3. every assembly the build needs, before invoking the compiler ----
set "MISSING=0"
for %%F in (mscorlib.dll System.dll System.Core.dll UnityEngine.dll UnityEngine.CoreModule.dll UnityEngine.IMGUIModule.dll UnityEngine.InputModule.dll UnityEngine.Physics2DModule.dll UnityEngine.TextRenderingModule.dll) do (
    if not exist "%MANAGED%\%%F" (
        echo     missing game assembly: %%F
        set "MISSING=1"
    )
)
for %%F in (BepInEx.dll 0Harmony.dll) do (
    if not exist "%PACK%\payload\BepInEx\core\%%F" (
        echo     missing loader assembly in payload: %%F
        set "MISSING=1"
    )
)
if not "%MISSING%"=="0" (
    echo.
    echo ERROR: the assemblies above are missing, so the build was not started.
    echo Check the game folder, or re-extract the pack to restore payload\.
    exit /b 1
)

rem ---- 4. compile ---------------------------------------------------------
pushd "%SRC%"
if not exist build mkdir build
echo.
echo Compiling AccessibilityPlugin.cs ...
"%CSC%" -noconfig -lib:"%MANAGED%" @build.rsp
set "RC=%errorlevel%"
popd
if not "%RC%"=="0" (
    echo.
    echo ERROR: the compiler exited with code %RC%.
    exit /b %RC%
)

rem ---- 5. report ----------------------------------------------------------
set "NEW=%SRC%build\PonyIslandAccessibility.dll"
set "OLD=%PACK%\payload\BepInEx\plugins\PonyIslandAccessibility\PonyIslandAccessibility.dll"
echo.
echo Built: %NEW%
echo.
echo SHA-256 of the new DLL:
call :hash "%NEW%"
echo   %H%
echo SHA-256 currently in payload:
call :hash "%OLD%"
echo   %H%
echo.

if "%COPY%"=="1" (
    copy /y "%NEW%" "%OLD%" >nul
    if errorlevel 1 (
        echo ERROR: the new DLL could not be copied into payload\.
        exit /b 1
    )
    echo The new DLL was installed into payload\ as the shipped file.
    echo payload\ no longer matches SHA256SUMS.txt: verify.bat will report it as
    echo BAD. Re-test in game first, then regenerate SHA256SUMS.txt before you
    echo distribute the pack again.
    exit /b 0
)

echo payload\ still holds the tested DLL; the build output is only in src\build\.
echo To replace the shipped file with it: build.bat /copy
exit /b 0

rem ==========================================================================
rem  :hash "<file>" -- sets H to the SHA-256 digest of that file.
rem  certutil -hashfile prints a header line, the digest, then a status line
rem  that starts with "CertUtil" -- which findstr character classes can match,
rem  so the digest is taken positionally as the second line instead.
rem ==========================================================================
:hash
set "H="
for /f "skip=1 delims=" %%V in ('certutil -hashfile "%~1" SHA256 2^>nul') do if not defined H set "H=%%V"
if not defined H set "H=(unreadable)"
exit /b 0
