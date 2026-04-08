@echo off
cd /d %~dp0
call path_define.bat

echo ========================================
echo Building Android AssetBundle (Auto Version)
echo ========================================

"%UNITYEDITOR_PATH%/Unity.exe" "%WORKSPACE%" -logFile "%BUILD_LOGFILE%" -executeMethod DGame.ReleaseTools.BuildAndroidAB -quit -batchmode -CustomArgs:Language=en_US;"%WORKSPACE%"

pause