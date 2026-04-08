#!/usr/bin/env bash

cd "$(dirname "$0")"

source ./path_define.sh

VERSION=1.5

echo "========================================"
echo "Building Windows AssetBundle (Manual Version: ${VERSION})"
echo "========================================"
echo "Log File: ${BUILD_LOGFILE}"

"${UNITYEDITOR_PATH}/Unity" \
  -projectPath "${WORKSPACE}" \
  -batchmode \
  -quit \
  -logFile "${BUILD_LOGFILE}" \
  -executeMethod DGame.ReleaseTools.BuildWindowsABWithVersion \
  "-version=${VERSION}" \
  "-CustomArgs:Language=en_US;${WORKSPACE}"

status=$?

if [[ ${status} -ne 0 ]]; then
  echo "Build failed. Check log: ${BUILD_LOGFILE}"
else
  echo "Build finished. Check log: ${BUILD_LOGFILE}"
fi

if [[ -f "${BUILD_LOGFILE}" ]]; then
  cat "${BUILD_LOGFILE}"
fi

echo "Press any key to continue..."
read -n 1 -s -r

exit ${status}
