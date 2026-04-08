cd /d %~dp0

call path_define.bat

echo Log File: %BUILD_LOGFILE%

"%UNITYEDITOR_PATH%\Unity.exe" -projectPath "%WORKSPACE%" -batchmode -quit -logFile "%BUILD_LOGFILE%" -executeMethod DGame.ReleaseTools.AutoBuildAndroid -CustomArgs:Language=en_US;"%WORKSPACE%"

if errorlevel 1 (
    echo Build failed. Check log: %BUILD_LOGFILE%
) else (
    echo Build finished. Check log: %BUILD_LOGFILE%
)

@REM for /f "delims=[" %%i in (%BUILD_LOGFILE%) do echo %%i

pause
