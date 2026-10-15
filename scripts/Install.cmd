@echo off
setlocal
set "installer=%~dp0scripts\install.ps1"
if not exist "%installer%" set "installer=%~dp0install.ps1"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%installer%"
if errorlevel 1 (
  echo Installation failed.
  pause
  exit /b 1
)
echo Quiet PC installed.
pause
