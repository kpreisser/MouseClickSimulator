@echo off

REM This build script allows you to build the TTR Mouse Click Simulator.
REM For information about prerequisites, see the Wiki page at
REM https://github.com/TTExtensions/MouseClickSimulator/wiki/Running-the-Simulator


SetLocal ENABLEDELAYEDEXPANSION
REM Change the working directory to the script's directory.
REM E.g. if the user right-clicks on the script and selects "Run as Administrator",
REM the working directory would be the windows\system32 dir.
cd "%~dp0"

echo.Building the TTR Mouse Click Simulator...
echo.
REM MSBuild is always installed in the 32-Bit program files folder
if "!ProgramFiles(x86)!"=="" (
	set "ProgramFiles32Bit=!ProgramFiles!"
) else (
	set "ProgramFiles32Bit=!ProgramFiles(x86)!"
)
set "BuildExe=!ProgramFiles32Bit!\MSBuild\14.0\Bin\MSBuild.exe"

if not exist "!BuildExe!" (
	echo.ERROR: MSBuild not found at "!BuildExe!"^^!
	pause
	exit /b 1
)

"!BuildExe!" /v:minimal /nologo /p:Configuration=Release "TTMouseclickSimulator\TTMouseclickSimulator.csproj"
if not errorlevel 1 (
	echo.
	echo.Build successful^^!
)
pause
exit /b !ERRORLEVEL!