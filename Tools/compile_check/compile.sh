#!/bin/bash
# Type-checks the Unity scripts outside Unity with Roslyn (dotnet SDK 8) against stubs.cs.
# usage: compile.sh <Scripts dir>  -> prints C# compile errors (Unity APIs stubbed in stubs.cs)
SRC="$1"
REF=/usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.31/ref/net8.0
REFS=$(ls $REF/*.dll | sed 's/^/-r:/' | tr '\n' ' ')
FILES=$(find "$SRC" -name "*.cs" -not -path "*/Editor/*")
dotnet /usr/lib/dotnet/sdk/8.0.131/Roslyn/bincore/csc.dll -nologo -noconfig -nostdlib -t:library -langversion:9.0 -out:/tmp/game.dll \
  -nowarn:0168,0169,0219,0414,0649,0162,8981,0108,0114,0067 $REFS "$(dirname "$0")/stubs.cs" $FILES 2>&1 \
  | grep -E "error" | sed "s#$SRC/##" | sort -u
