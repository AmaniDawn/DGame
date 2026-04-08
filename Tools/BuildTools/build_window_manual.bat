@echo off
cd /d %~dp0
call path_define.bat

set VERSION=1.0
echo ========================================
echo Building Windows AssetBundle (Manual Version: %VERSION%)
echo ========================================
echo Log File: %BUILD_LOGFILE%

"%UNITYEDITOR_PATH%\Unity.exe" -projectPath "%WORKSPACE%" -batchmode -quit -logFile "%BUILD_LOGFILE%" -executeMethod DGame.ReleaseTools.BuildWindowWithVersion -version=%VERSION% -CustomArgs:Language=en_US;"%WORKSPACE%"

if errorlevel 1 (
    echo Build failed. Check log: %BUILD_LOGFILE%
) else (
    echo Build finished. Check log: %BUILD_LOGFILE%
)

pause
