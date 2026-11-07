#!/bin/bash

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

source ./path_define.sh

cp -R "${CONF_ROOT}/CustomTemplate/Server/Bin/ConfigSystem.cs" \
   "${WORKSPACE}/GameServer/Server/Entity/Generate/ConfigSystem.cs"

dotnet "${LUBAN_DLL}" \
    -t server \
    -c cs-bin \
    -d bin \
    -d json \
    --conf "${CONF_ROOT}/luban.conf" \
    --customTemplateDir "${CONF_ROOT}/CustomTemplate/Server/CustomTemplate_Server" \
    -x tableImporter.name=default \
    -x code.lineEnding=crlf \
    -x outputCodeDir="${SERVER_CODE_OUTPATH}" \
    -x bin.outputDataDir="${SERVER_BIN_DATA_OUTPATH}" \
    -x json.outputDataDir="${SERVER_JSON_DATA_OUTPATH}" \
    -x outputSaver.bin.cleanUpOutputDir=1 \
    -x outputSaver.json.cleanUpOutputDir=1 \
    -x outputSaver.cs-bin.cleanUpOutputDir=1
