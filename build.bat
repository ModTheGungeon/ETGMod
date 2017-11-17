@echo off
setlocal ENABLEDELAYEDEXPANSION

:: Requirements
where xbuild >nul 2>nul
if not %errorlevel%==0 (
  where msbuild >nul 2>nul
  if not %errorlevel%==0 (
    echo ERROR: Neither xbuild nor msbuild were found. Make sure you have Mono or .NET Framework installed and the binaries are available in the PATH.
    goto _exit
  )
)

set sevenz=7z
if exist C:\Program Files\7-Zip\7z.exe (
  set sevenz="C:\Program Files\7-Zip\7z.exe"
) else (
  where 7z >nul 2>nul
  if not %errorlevel%==0 (
    echo ERROR: 7zip was not found. Make sure that you have it installed and in the PATH.
    goto _exit
  )
)

:: Prepare the target
set target=Debug
if %1.==release. goto _if_setrelease
goto _ifj_setrelease
:_if_setrelease
set target=Release
:_ifj_setrelease

:: Prepare the build directory
set build_base=build
set build_etgmod=ETGMOD
set "build=%build_base%\%build_etgmod%"
set "build_zip=%build_base%\%build_etgmod%.zip"

if exist "%build_base%" rmdir /q /s "%build_base%"
mkdir "%build_base%" 2>nul
mkdir "%build%" 2>nul

:: Build
where xbuild >nul 2>nul
if %errorlevel%==0 (
  ::call xbuild
  rem
) else (
  call msbuild
)

for /f "tokens=*" %%L in (build-files) do (
  set "line=%%L"
  set "line=!line:/=\!"
  if not "!line:~0,1!"=="#" (
    set "file=!line:{TARGET}=%target%!"
    for %%f in (!file!) do set target=%%~nxf
      rem

    echo Copying '!file!' to '%build%/!target!'

    for %%i in (!file!) do (
      if exist %%~si\nul (
        robocopy "!file!" "%build%/!target!" /s /e
      ) else (
        copy "!file!" "%build%/!target!"
      )
    )
  )
)

:: Zipping it all up
pushd "%build%"
%sevenz% a ETGMOD.zip *
popd
move "%build%\ETGMOD.zip" "%build_zip%"

:: The End
:_exit
