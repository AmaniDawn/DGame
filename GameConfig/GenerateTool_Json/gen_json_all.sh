#!/bin/bash

cd "$(dirname "$0")"
echo "Current directory: $(pwd)"

bash ./gen_json_client.sh
bash ./gen_json_server.sh
