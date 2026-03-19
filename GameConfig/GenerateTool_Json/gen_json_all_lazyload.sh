#!/bin/bash

cd "$(dirname "$0")"
echo "Current directory: $(pwd)"

bash ./gen_json_client_lazyload.sh
bash ./gen_json_server_lazyload.sh
