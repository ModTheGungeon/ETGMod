@echo off


set target=Debug

if %1.==release. goto _if_setrelease
goto _ifj_setrelease
:_if_setrelease
set target=Release
:_ifj_setrelease

set build_base=build
set build_etgmod=ETGMOD
set build=%build_base%/%build_etgmod%
set build_zip=%build_base%/%build_etgmod%.zip

rmdir /q /s %build_base%
mkdir %build_base%

:: msbuild.exe

echo a
for /f "tokens=*" %%a in (build-files) do (
  echo line=%%a
)
)

for /F "tokens=*" %%L in (build-files) do (
  echo %%L
  if /i "%result:~0,1%"=="#" goto _continue_eachline
  echo copying %%L
  xcopy /E %%L %build%/%%L
  :_continue_eachline
)
