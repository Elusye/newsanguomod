@echo off
setlocal
cd /d "%~dp0"

rem ============================================================
rem  Local LAN / same-machine multiplayer test (ENet 127.0.0.1:33771, no Steam).
rem  Usage:
rem    local-multiplayer-test.bat          start HOST then JOIN
rem    local-multiplayer-test.bat host     start host only
rem    local-multiplayer-test.bat join     start join only
rem  NOTE: --force-steam off skips Steam init, so workshop mods are NOT
rem  loaded automatically. Copy workshop item 3747602295 (RitsuLib) into
rem  mods\RitsuLib\ first (DLL + all .json manifest files).
rem ============================================================

set "GAME_EXE=E:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.exe"

if exist "%GAME_EXE%" goto :exe_ok
echo Game not found: "%GAME_EXE%"
echo Edit GAME_EXE at the top of this script and try again.
goto :end

:exe_ok
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
