@echo off
taskkill /F /IM MSBuild.exe 2>nul
if %errorlevel% equ 0 (
    echo MSBuild processes killed.
) else (
    echo No MSBuild processes were running.
)
