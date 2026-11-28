@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -File -Recurse | Unblock-File; & '.\Install-Visa2026TemplateEdit.ps1'"
echo.
pause
