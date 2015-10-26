@echo off
REM I didn't use the if (...) else (...) form because when the environment variable
REM is expanded, the ")" in the directory name seems to be interpreted by the IF command.
if not "%ProgramFiles(x86)%"=="" set ProgramFiles32Bit=%ProgramFiles(x86)%
if "%ProgramFiles(x86)%"=="" set ProgramFiles32Bit=%ProgramFiles%
set BuildExe=%ProgramFiles32Bit%\MSBuild\14.0\Bin\MSBuild.exe

if not exist "%BuildExe%" (
	echo ERROR: MSBuild not found at "%BuildExe%"!
	pause
	exit /b
)

"%BuildExe%" /p:Configuration=Release "TTMouseclickSimulator.sln"
pause