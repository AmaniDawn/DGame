#!/bin/bash

cd "$(dirname "$0")"
echo "Current directory: $(pwd)"

bash ./gen_bin_client_lazyload.sh
bash ./gen_bin_server_lazyload.sh
