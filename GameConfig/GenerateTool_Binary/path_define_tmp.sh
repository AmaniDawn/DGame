#!/bin/bash

cd "$(dirname "$0")"

# 公共配置
export WORKSPACE="$(realpath ../../)"
export LUBAN_DLL="../Tools/LubanTools/Luban/Luban.dll"
export CONF_ROOT="$(pwd)/.."

# 客户端配置
export DATA_OUTPATH="${WORKSPACE}/GameUnity/Assets/BundleAssets/Configs/Binary/"
export CLIENT_CODE_OUTPATH="${WORKSPACE}/GameUnity/Assets/Scripts/HotFix/GameProto/LubanConfig/"

# 服务器配置
export BINARY_DATA_OUTPATH="${WORKSPACE}/GameServer/Configs/Binary/"
export JSON_DATA_OUTPATH="${WORKSPACE}/GameServer/Configs/Json/"
export SERVER_CODE_OUTPATH="${WORKSPACE}/GameServer/Server/Entity/Generate/Config/"

echo "环境变量已设置："
echo "WORKSPACE=${WORKSPACE}"
echo "LUBAN_DLL=${LUBAN_DLL}"
echo "CONF_ROOT=${CONF_ROOT}"
echo "DATA_OUTPATH=${DATA_OUTPATH}"
echo "CLIENT_CODE_OUTPATH=${CLIENT_CODE_OUTPATH}"
echo "BINARY_DATA_OUTPATH=${BINARY_DATA_OUTPATH}"
echo "JSON_DATA_OUTPATH=${JSON_DATA_OUTPATH}"
echo "SERVER_CODE_OUTPATH=${SERVER_CODE_OUTPATH}"