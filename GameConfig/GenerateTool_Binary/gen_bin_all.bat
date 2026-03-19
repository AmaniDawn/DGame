@echo off
cd /d %~dp0
echo %CD%

set AUTO_CONTINUE=1
call gen_bin_client.bat
call gen_bin_server.bat
set AUTO_CONTINUE=
pause
