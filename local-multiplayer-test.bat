@echo off
setlocal
cd /d "%~dp0"

rem ============================================================
rem  Local LAN / same-machine multiplayer test (ENet 127.0.0.1:33771, no Steam).
rem  Usage:
rem    local-multiplayer-test.bat          start HOST then JOIN
rem    local-multiplayer-test.bat host     start host only
rem    local-multiplayer-test.bat join     start join only
rem  Before launch, sync the complete RitsuLib Workshop package into local mods.
rem  Old local copies are backed up outside the mods directory.
rem ============================================================

set "GAME_EXE=E:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.exe"

if exist "%GAME_EXE%" goto :exe_ok
echo Game not found: "%GAME_EXE%"
echo Edit GAME_EXE at the top of this script and try again.
goto :end

:exe_ok
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\prepare-local-multiplayer.ps1" -GameExe "%GAME_EXE%"
if errorlevel 1 (
    echo RitsuLib preparation failed. No game instance was started.
    pause
    exit /b 1
)

if /i "%~1"=="host" goto :host
if /i "%~1"=="join" goto :join

rem No argument: start host, wait, then start join.
echo === Starting HOST instance ===
start "STS2 Host" "%GAME_EXE%" --fastmp host_standard --force-steam off
timeout /t 5 /nobreak >nul
echo === Starting JOIN instance ===
start "STS2 Join" "%GAME_EXE%" --fastmp join --force-steam off
goto :end

:host
echo === Starting HOST instance ===
start "STS2 Host" "%GAME_EXE%" --fastmp host_standard --force-steam off
goto :end

:join
echo === Starting JOIN instance ===
start "STS2 Join" "%GAME_EXE%" --fastmp join --force-steam off
goto :end

:end
endlocal
exit /b 0
