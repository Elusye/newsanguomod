@echo off
setlocal

set "CONFIGURATION=%~1"
if "%CONFIGURATION%"=="" set "CONFIGURATION=Debug"

pushd "%~dp0"

dotnet build ".\newsanguo.csproj" -c "%CONFIGURATION%"
set "BUILD_EXIT=%ERRORLEVEL%"
if not "%BUILD_EXIT%"=="0" (
    popd
    exit /b %BUILD_EXIT%
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\export-pck.ps1"
set "EXPORT_EXIT=%ERRORLEVEL%"

popd
exit /b %EXPORT_EXIT%
