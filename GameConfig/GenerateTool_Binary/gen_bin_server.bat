Cd /d %~dp0
echo %CD%

call path_define.bat

xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\Server\Bin\ConfigSystem.cs" "%WORKSPACE%\GameServer\Server\Entity\Generate\ConfigSystem.cs"

dotnet %LUBAN_DLL% ^
    -t server^
    -c cs-bin ^
    -d bin^
    -d json^
    --conf %CONF_ROOT%\luban.conf ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir=%SERVER_CODE_OUTPATH% ^
    -x bin.outputDataDir=%BINARY_DATA_OUTPATH% ^
    -x json.outputDataDir=%JSON_DATA_OUTPATH% ^
    -x outputSaver.bin.cleanUpOutputDir=1 ^
    -x outputSaver.json.cleanUpOutputDir=1 ^
    -x outputSaver.cs-bin.cleanUpOutputDir=1
if not defined AUTO_CONTINUE pause
