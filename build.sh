#!/usr/bin/sh
set -e

p="Debug"
if [ "$1" = "release" ]; then
  p="Release"
fi

BUILD_BASE="build"
BUILD_ETGMOD="ETGMOD"

BUILD="$BUILD_BASE/$BUILD_ETGMOD"
BUILD_ZIP="$BUILD_BASE/$BUILD_ETGMOD.zip"

rm -rf $BUILD_BASE
mkdir -p $BUILD

xbuild

deps=(
  # external
  libs/Ionic.Zip.dll
  libs/YamlDotNet/YamlDotNet/bin/$p/YamlDotNet.dll
  libs/MonoMod/bin/$p/MonoMod.exe
  libs/MonoMod/packages/Mono.Cecil.0.10.0-beta5/lib/net35/Mono.Cecil.dll
  libs/MonoMod/packages/Mono.Cecil.0.10.0-beta5/lib/net35/Mono.Cecil.Mdb.dll
  libs/MonoMod/packages/Mono.Cecil.0.10.0-beta5/lib/net35/Mono.Cecil.Pdb.dll
  libs/Harmony/Harmony/bin/$p/0Harmony.dll
  libs/System.Windows.Forms.dll
  libs/System.Drawing.dll

  # internal [Assembly-CSharp]
  Assembly-CSharp.Core.mm/bin/$p/Assembly-CSharp.Core.mm.dll
  Assembly-CSharp.Base.mm/bin/$p/Assembly-CSharp.Base.mm.dll
  Assembly-CSharp.Console.mm/bin/$p/Assembly-CSharp.Console.mm.dll
  Assembly-CSharp.GUI.mm/bin/$p/Assembly-CSharp.GUI.mm.dll
  Assembly-CSharp.TexMod.mm/bin/$p/Assembly-CSharp.TexMod.mm.dll
  GTKClipboard/bin/$p/GTKClipboard.dll
  GTKClipboard/GTKClipboard.config

  # internal [UnityEngine]
  UnityEngine.Core.mm/bin/$p/UnityEngine.Core.mm.dll
  UnityEngine.Base.mm/bin/$p/UnityEngine.Base.mm.dll

  # licenses
  LICENSE_ETGMOD
  LICENSE_MONO
)

for d in ${deps[@]}; do
  cp "$d" "$BUILD"
done

zip_name="$(mktemp -u -p "." .build-XXXXXXXXX.zip)"
pushd $BUILD
zip -r "$zip_name" *
popd
mv "$BUILD/$zip_name" "$BUILD_ZIP"
