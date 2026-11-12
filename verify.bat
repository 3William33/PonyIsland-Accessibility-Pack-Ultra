@echo off
setlocal EnableExtensions
rem ==========================================================================
rem  verify.bat -- checks every file listed in SHA256SUMS.txt against this pack.
rem
rem  Nothing is written: not into the pack, not into the game folder. The
rem  manifest paths use forward slashes and are turned into local paths here,
rem  so the list stays identical on every machine and drive layout.
rem
rem  Two local paths are deliberately NOT in the manifest, because they are
rem  your own machine's state rather than release content:
rem      game-path.txt   last install target, written by install.bat
rem      src\build\      compiler output, written by src\build.bat
rem  Repository metadata (.gitignore, .gitattributes, .git) is not listed
rem  either: it is never part of what gets installed into the game folder.
rem
rem  Exit code 0 = every listed file matches, 1 = a mismatch or a missing file,
rem  2 = SHA256SUMS.txt itself is gone, which means the pack is incomplete.
rem ==========================================================================

pushd "%~dp0"

if not exist "SHA256SUMS.txt" (
    echo ERROR: SHA256SUMS.txt is not next to this script.
    echo The pack is incomplete or was extracted partially - re-extract it.
    echo.
    pause
    exit /b 2
)

set /a TOTAL=0
set /a BAD=0
set /a MISS=0

echo Checking every file in SHA256SUMS.txt ...
echo.

rem One manifest line is "<64 hex chars> *<path>". Only "*" separates the two,
rem so a path may contain spaces; eol=# skips the header lines of the file.
for /f "usebackq eol=# tokens=1,* delims=*" %%A in ("SHA256SUMS.txt") do call :check "%%A" "%%B"

echo.
echo Checked %TOTAL% files from the manifest.

if not "%BAD%"=="0" goto :report_bad
if not "%MISS%"=="0" goto :report_bad

echo OK: all files match, the shipped DLLs and the plugin are untouched.
if exist "game-path.txt" echo note: game-path.txt is your local install path, not release content.
if exist "src\build\" echo note: src\build\ is local build output, not release content.
echo.
pause
popd
exit /b 0

:report_bad
echo.
echo ERROR: %BAD% file^(s^) differ and %MISS% file^(s^) are missing.
echo Do not install from this pack. Re-extract it from the original archive and
echo run verify.bat again; if it still differs, the archive itself is damaged.
echo.
pause
popd
exit /b 1

rem ==========================================================================
rem  :check "<hash> " "<path>"
rem ==========================================================================
:check
set /a TOTAL+=1
set "F=%~2"
set "HASH=%~1"

rem The manifest puts one space before the asterisk, so token 1 ends with a
rem space. Trim it, otherwise every comparison would fail.
if not defined HASH exit /b 0
if "%HASH:~-1%"==" " set "HASH=%HASH:~0,-1%"
if "%HASH:~-1%"==" " set "HASH=%HASH:~0,-1%"

if not defined F exit /b 0
if "%F:~0,1%"==" " set "F=%F:~1%"
if not defined F exit /b 0
set "F=%F:/=\%"

if not exist "%F%" (
    echo   MISSING   %F%
    set /a MISS+=1
    exit /b 0
)

call :hash "%F%"

rem certutil did not hand back a 64-character digest, so the file could not be
rem read -- treat that as missing rather than comparing against garbage.
if not defined H (
    echo   MISSING   %F%  ^(unreadable^)
    set /a MISS+=1
    exit /b 0
)

if /i "%H%"=="%HASH%" (
    echo   OK        %F%
    exit /b 0
)

echo   BAD       %F%
echo             expected %HASH%
echo             actual   %H%
set /a BAD+=1
exit /b 0

rem ==========================================================================
rem  :hash "<file>" -- sets H to the SHA-256 digest, empty on failure.
rem  certutil -hashfile prints: header line, digest line, status line. The
rem  status line starts with "CertUtil", which findstr's character classes can
rem  still match, so the digest is taken positionally instead of by pattern.
rem ==========================================================================
:hash
set "H="
for /f "skip=1 delims=" %%V in ('certutil -hashfile "%~1" SHA256 2^>nul') do if not defined H set "H=%%V"
if not defined H exit /b 0
rem a SHA-256 digest is 64 characters; anything shorter was not a digest
if "%H:~63,1%"=="" set "H="
exit /b 0
