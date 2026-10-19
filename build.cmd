@echo off
:: Builds OMEN 4-Zone RGB Lighting Utility
setlocal
cd /d "%~dp0"
if exist "%USERPROFILE%\.dotnet\dotnet.exe" set PATH=%USERPROFILE%\.dotnet;%PATH%
echo Building OMEN 4-Zone RGB Keyboard Lighting utility...
dotnet build -c Release
if errorlevel 1 exit /b 1
echo Built bin\Release\OmenRgb.exe successfully.
