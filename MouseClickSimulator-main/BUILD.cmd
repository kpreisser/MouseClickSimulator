@echo off

REM This build script allows you to build the TTR Mouse Click Simulator.
REM For information about prerequisites, see the Wiki page at
REM https://github.com/TTExtensions/MouseClickSimulator/wiki/Running-the-Simulator


SetLocal ENABLEDELAYEDEXPANSION
REM Change the working directory to the script's directory.
REM E.g. if the user right-clicks on the script and selects "Run as Administrator",
REM the working directory would be the windows\system32 dir.
cd /d "%~dp0"

echo.Building the TTR Mouse Click Simulator...
echo.

REM Note that we need to specify both "Configuration" and "Platform" parameters, because
REM otherwise MSBuild will fill missing parameters from environment variables (and some
REM systems may have set a "Platform" variable).
"dotnet.exe" publish "TTMouseclickSimulator\TTMouseclickSimulator.csproj" -f net8.0-windows -c Release -p:Platform=AnyCPU --no-self-contained
if not errorlevel 1 (
	echo.
	echo.Build successful^^!
)
pause
exit /b !ERRORLEVEL!
