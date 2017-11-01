#!/usr/bin/sh
set -e

TARGET="Debug"
if [ "$1" = "release" ]; then
  TARGET="Release"
fi

BUILD_BASE="build"
BUILD_ETGMOD="ETGMOD"

BUILD="$BUILD_BASE/$BUILD_ETGMOD"
BUILD_ZIP="$BUILD_BASE/$BUILD_ETGMOD.zip"

rm -rf $BUILD_BASE
mkdir -p $BUILD

xbuild

deps=()

while read line; do
  if [[ "$line" == "#"* ]]; then continue; fi
  deps+=("$(echo "$line" | sed "s/{TARGET}/$TARGET/")")
done < build-files

echo "${deps[@]}"

for d in ${deps[@]}; do
  cp -r "$d" "$BUILD"
done

zip_name="$(mktemp -u -p "." .build-XXXXXXXXX.zip)"
pushd $BUILD
zip -r "$zip_name" *
popd
mv "$BUILD/$zip_name" "$BUILD_ZIP"
zenity --info --title "Ey" --text="Hey faggot, install ETGMod"
