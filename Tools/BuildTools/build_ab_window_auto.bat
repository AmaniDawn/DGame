@echo off
cd /d %~dp0
call path_define.bat

echo ========================================
echo Building Windows AssetBundle (Auto Version)
echo ========================================

"%UNITYEDITOR_PATH%/Unity.exe" "%WORKSPACE%" -logFile "%BUILD_LOGFILE%" -executeMethod DGame.ReleaseTools.BuildWindowsAB -quit -batchmode -CustomArgs:Language=en_US;"%WORKSPACE%"

pause