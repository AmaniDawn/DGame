cd /d %~dp0

:: 公共配置
set WORKSPACE=../../
set LUBAN_DLL=../Tools/LubanTools/Luban/Luban.dll
set CONF_ROOT=..

:: 客户端配置
set CLIENT_BIN_DATA_OUTPATH=%WORKSPACE%/GameUnity/Assets/BundleAssets/Configs/Binary/
set CLIENT_JSON_DATA_OUTPATH=%WORKSPACE%/GameUnity/Configs/Json/
set CLIENT_CODE_OUTPATH=%WORKSPACE%/GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/

:: 服务器配置
set SERVER_BIN_DATA_OUTPATH=%WORKSPACE%/GameServer/Configs/Binary/
set SERVER_JSON_DATA_OUTPATH=%WORKSPACE%/GameServer/Configs/Json/
set SERVER_CODE_OUTPATH=%WORKSPACE%/GameServer/Server/Entity/Generate/Config/
