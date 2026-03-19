#!/bin/bash

cd "$(dirname "$0")"
echo "Current directory: $(pwd)"

bash ./gen_bin_client.sh
bash ./gen_bin_server.sh
