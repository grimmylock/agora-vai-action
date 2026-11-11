#!/bin/bash
# Compiles the scripts + the harness (test/Program.cs) WITHOUT running the checks: prints compile errors, or "COMPILES".
D="$(cd "$(dirname "$0")" && pwd)"; SRC="$D/../../Assets/Scripts"; REF=$(ls -d /usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.*/ref/net8.0 | tail -1)
REFS=$(ls $REF/*.dll | sed 's/^/-r:/' | tr '\n' ' '); FILES=$(find "$SRC" -name "*.cs" -not -path "*/Editor/*" -not -path "*/Net/*")
rm -f /tmp/test.dll; dotnet $(ls -d /usr/lib/dotnet/sdk/8.0.* | tail -1)/Roslyn/bincore/csc.dll -nologo -noconfig -nostdlib -t:exe -langversion:9.0 -out:/tmp/test.dll \
  -nowarn:0168,0169,0219,0414,0649,0162,8981,0108,0114,0067 $REFS "$D/stubs.cs" "$D/test/Program.cs" $FILES 2>&1 | grep error
[ -f /tmp/test.dll ] && echo COMPILES
