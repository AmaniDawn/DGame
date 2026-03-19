Cd /d %~dp0
echo %CD%

call path_define.bat

xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\Client\Json\ConfigSystem.cs" "%WORKSPACE%\GameUnity\Assets\Scripts\HotFix\GameProto\ConfigSystem.cs"
xcopy /s /e /i /y "%CONF_ROOT%\CustomTemplate\Client\Json\ExternalTypeUtil.cs" "%WORKSPACE%\GameUnity\Assets\Scripts\HotFix\GameProto\ExternalTypeUtil.cs"

dotnet %LUBAN_DLL% ^
    -t client ^
    -c cs-simple-json ^
    -d json2 ^
    --conf %CONF_ROOT%\luban.conf ^
    --customTemplateDir %CONF_ROOT%\CustomTemplate\Client\CustomTemplate_Client_LazyLoad ^
    -x code.lineEnding=crlf ^
    -x outputCodeDir=%CLIENT_CODE_OUTPATH% ^
    -x outputDataDir=%DATA_OUTPATH% ^
    -x outputSaver.bin.cleanUpOutputDir=1 ^
    -x outputSaver.json.cleanUpOutputDir=1 ^
    -x outputSaver.cs-bin.cleanUpOutputDir=1
if not defined AUTO_CONTINUE pause
