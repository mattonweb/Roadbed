@echo off
setlocal

rem ===========================================================================
rem  Publish the Roadbed library DLLs (.dll, .pdb, .xml) to the vendoring
rem  folder consumers copy from. Builds the whole solution once in Release;
rem  src\Directory.Build.targets copies each library's own output.
rem
rem  Double-click to run. Optional arguments:
rem    publish-assemblies.cmd [destination] [nopause]
rem  "nopause" skips the closing pause (Visual Studio External Tools uses it).
rem ===========================================================================

set "DEST=C:\Workspace\Assemblies\Roadbed"
set "PAUSE_AT_END=1"

:args
if "%~1"=="" goto run
if /i "%~1"=="nopause" (set "PAUSE_AT_END=") else (set "DEST=%~1")
shift
goto args

:run
rem A trailing backslash would escape the closing quote of the -p: argument.
if "%DEST:~-1%"=="\" set "DEST=%DEST:~0,-1%"
cd /d "%~dp0"

where git >nul 2>&1
if not errorlevel 1 (
    for /f %%s in ('git status --porcelain --untracked-files=no 2^>nul ^| find /c /v ""') do (
        if not "%%s"=="0" (
            echo WARNING: the working tree has %%s uncommitted change^(s^). Consumers should only
            echo          vendor DLLs built from committed code.
            echo.
        )
    )
)

echo Publishing Roadbed assemblies to %DEST%
echo.

dotnet build "src\Roadbed .NET Solution.slnx" -c Release -nologo "-p:RoadbedAssembliesDir=%DEST%"
if errorlevel 1 (
    echo.
    echo PUBLISH FAILED: the build did not succeed. Nothing new was copied for the projects that failed.
    set "RESULT=1"
    goto done
)

set "COUNT=0"
for %%f in ("%DEST%\Roadbed.*.dll") do set /a COUNT+=1
echo.
echo PUBLISHED: %COUNT% Roadbed DLLs are in %DEST%
set "RESULT=0"

:done
if defined PAUSE_AT_END pause
exit /b %RESULT%
