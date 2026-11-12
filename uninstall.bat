@echo off
setlocal EnableExtensions
rem ===========================================================================
rem  Removes ONLY this v2 plugin and its own state.
rem  BepInEx core, other plugins, game files, saves and registry progress stay.
rem ===========================================================================
set "GAME=%~1"
if "%GAME%"=="" set "GAME=D:\steam\steamapps\common\Pony Island"
if not exist "%GAME%\.pony_accessibility_v2" (
  echo ERROR: v2 marker not found. Nothing was removed.
  exit /b 1
)
tasklist /fi "imagename eq PonyIsland.exe" 2>nul | findstr /i /c:"PonyIsland.exe" >nul && exit /b 1
echo Removing only Pony Island Global Survival Assist v2 ...
del /q "%GAME%\BepInEx\plugins\PonyIslandGlobalSurvivalV2\PonyIslandGlobalSurvivalV2.dll" 2>nul
rmdir "%GAME%\BepInEx\plugins\PonyIslandGlobalSurvivalV2" 2>nul
del /q "%GAME%\BepInEx\config\local.ponyisland.accessibility.v2.cfg" 2>nul
if exist "%GAME%\.pony_accessibility_v2_rollback\PonyIslandAccessibility.dll" (
  mkdir "%GAME%\BepInEx\plugins\PonyIslandAccessibility" >nul 2>nul
  copy /y "%GAME%\.pony_accessibility_v2_rollback\PonyIslandAccessibility.dll" "%GAME%\BepInEx\plugins\PonyIslandAccessibility\PonyIslandAccessibility.dll" >nul
  rmdir /s /q "%GAME%\.pony_accessibility_v2_rollback" >nul 2>nul
)
del /q "%GAME%\.pony_accessibility_v2" 2>nul
echo Done. v2 plugin and its config were removed.
echo BepInEx loader, other plugins, game files, saves and registry progress were not touched.
exit /b 0
