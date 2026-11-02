@echo off
rem Local Docker-only launcher. An explicit target is required; help never invokes Docker.
setlocal
if "%~1"=="" goto usage
if /I "%~1"=="help" goto usage
if /I "%~1"=="--help" goto usage
if /I "%~1"=="-h" goto usage
if not "%~2"=="" goto invalid

rem Canonicalize supported targets before passing arguments to Compose.
set "TARGET="
for %%T in (linux windows web android macos ios all) do if /I "%~1"=="%%T" set "TARGET=%%T"
if not defined TARGET goto invalid

rem A valid target must still be refused inside GitHub Actions.
if /I "%GITHUB_ACTIONS%"=="true" (
    echo ERROR: Game exports are disabled in GitHub Actions. Use local Docker.
    exit /b 1
)

rem Resolve the repository from this launcher's location, including cross-drive callers.
pushd "%~dp0"
if errorlevel 1 exit /b 1
docker compose --profile export build export
if errorlevel 1 goto failed
docker compose --profile export run --rm export "%TARGET%"
if errorlevel 1 goto failed
popd
echo Export complete. Outputs: .\build\
exit /b 0

:failed
rem Preserve Docker's failure status while restoring the caller's directory.
set "EXPORT_EXIT=%ERRORLEVEL%"
popd
exit /b %EXPORT_EXIT%

:invalid
rem Reject unknown platforms and extra arguments before any build can start.
echo ERROR: Specify one supported export target.
call :usage
exit /b 2

:usage
rem A bare invocation is intentionally inert, matching the container's default command.
echo Usage: export.bat [linux^|windows^|web^|android^|macos^|ios^|all]
echo No target or --help prints this message without building or exporting.
echo Exports are local Docker operations; GitHub Actions exports are disabled.
exit /b 0
