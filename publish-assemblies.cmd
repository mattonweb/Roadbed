@echo off
setlocal

rem ===========================================================================
rem  Thin wrapper: the publish itself is tools\publish-assemblies.ps1, the one
rem  path into C:\Workspace\Assemblies\Roadbed. It refuses unless the checkout
rem  is a clean main equal to origin/main, builds the solution in Release with
rem  RoadbedAssembliesDir pointed at a staging folder, mirrors the Roadbed.*
rem  output into the drop folder and writes publish-manifest.json there.
rem
rem  Double-click to run. Optional argument:
rem    publish-assemblies.cmd [nopause]
rem  "nopause" skips the closing pause (Visual Studio External Tools uses it).
rem  Any other arguments are passed to the script (for example -Destination).
rem ===========================================================================

set "PAUSE_AT_END=1"
set "EXTRA="

:args
if "%~1"=="" goto run
if /i "%~1"=="nopause" (set "PAUSE_AT_END=") else (set "EXTRA=%EXTRA% %1")
shift
goto args

:run
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\publish-assemblies.ps1"%EXTRA%
set "RESULT=%ERRORLEVEL%"

if defined PAUSE_AT_END pause
exit /b %RESULT%
