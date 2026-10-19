@echo off
:: Builds OmenGlow - Standalone 4-Zone RGB Keyboard Lighting Utility
setlocal
cd /d "%~dp0"
if exist "%USERPROFILE%\.dotnet\dotnet.exe" set PATH=%USERPROFILE%\.dotnet;%PATH%
echo Building OmenGlow (4-Zone RGB Lighting)...
dotnet build -c Release
if errorlevel 1 exit /b 1
echo.
echo Built bin\Release\OmenGlow.exe successfully.
