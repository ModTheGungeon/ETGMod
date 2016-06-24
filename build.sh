#!/bin/bash
cd Assembly-CSharp.ExampleAPI.mm
if [ -z ${BUILD_NUMBER+x} ]; then
  echo 'build_main: Leaving version default.'
else
  echo 'build_main: Replacing version string in FEZMod.'
  perl -0777 -pi -e 's/public string Version { get { return ".*"; } }/public string Version { get { return "dev-'${BUILD_NUMBER}'"; } }/gm' ./src/ExampleMod.cs
fi
xbuild && \
cd ..
