#!/usr/bin/env bash

cd "$(dirname "$0")"

source ./path_define.sh

echo "========================================"
echo "Building Android AssetBundle (Auto Version)"
echo "========================================"
echo "Log File: ${BUILD_LOGFILE}"

"${UNITYEDITOR_PATH}/Unity" \
  -projectPath "${WORKSPACE}" \
  -batchmode \
  -quit \
  -logFile "${BUILD_LOGFILE}" \
  -executeMethod DGame.ReleaseTools.BuildAndroidAB \
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
