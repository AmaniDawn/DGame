@echo off
cd /d %~dp0
call path_define.bat

echo UNITYEDITOR_PATH=%UNITYEDITOR_PATH%
echo WORKSPACE=%WORKSPACE%
echo.

set VERSION=1.2
echo Full command:
echo "%UNITYEDITOR_PATH%\Unity.exe" "%WORKSPACE%" -logFile "%~dp0Log\build.log" -executeMethod DGame.ReleaseTools.BuildAndroidABWithVersion -version %VERSION% -quit -batchmode -CustomArgs:Language=en_US;"%WORKSPACE%"
echo.

pause

"%UNITYEDITOR_PATH%\Unity.exe" "%WORKSPACE%" -logFile "%~dp0Log\build.log" -executeMethod DGame.ReleaseTools.BuildAndroidABWithVersion -version %VERSION% -quit -batchmode -CustomArgs:Language=en_US;"%WORKSPACE%"

pause
