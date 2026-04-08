@echo off
cd /d %~dp0
call path_define.bat

echo ========================================
echo Building Windows AssetBundle (Auto Version)
echo ========================================
echo Log File: %BUILD_LOGFILE%

"%UNITYEDITOR_PATH%\Unity.exe" -projectPath "%WORKSPACE%" -batchmode -quit -logFile "%BUILD_LOGFILE%" -executeMethod DGame.ReleaseTools.BuildWindowsAB -CustomArgs:Language=en_US;"%WORKSPACE%"

if errorlevel 1 (
    echo Build failed. Check log: %BUILD_LOGFILE%
) else (
    echo Build finished. Check log: %BUILD_LOGFILE%
)

pause
