@echo off
setlocal enabledelayedexpansion

set TARGET=%1
if "%TARGET%"=="" set TARGET=all

echo ==========================================================
echo   AXIOM FORCE — On-Demand Godot Exporter (Docker)
echo ==========================================================
echo   Target: %TARGET%
echo   Usage : export.bat [windows^|linux^|android^|macos^|ios^|visionos^|xbox^|web^|all]
echo ==========================================================

docker compose build exporter
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to build exporter image.
    exit /b %ERRORLEVEL%
)

docker compose --profile export run --rm -e TARGET=%TARGET% exporter %TARGET%
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Export failed.
    exit /b %ERRORLEVEL%
)

echo ==========================================================
echo   Export complete! Outputs located in .\build\
echo ==========================================================
