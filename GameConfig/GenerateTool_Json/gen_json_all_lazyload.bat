@echo off
cd /d %~dp0
echo %CD%

set AUTO_CONTINUE=1
call gen_json_client_lazyload.bat
call gen_json_server_lazyload.bat
set AUTO_CONTINUE=
pause
