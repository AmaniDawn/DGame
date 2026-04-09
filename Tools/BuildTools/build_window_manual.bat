@echo off
cd /d %~dp0
call path_define.bat

@REM set VERSION=1.0

set "VERSION="
echo ========================================
echo Please input Android AssetBundle version:
set /p VERSION=Version^> 

if "%VERSION%"=="" (
    echo Version cannot be empty.
    pause
    exit /b 1
)

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
