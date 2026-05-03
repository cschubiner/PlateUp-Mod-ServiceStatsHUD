@echo off
setlocal
set TARGET=C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\Mods\ServiceStatsHUD
if not exist "%TARGET%" mkdir "%TARGET%"
xcopy "%~dp0workshop\*" "%TARGET%\" /E /I /Y >nul
echo Synced workshop folder to %TARGET%
