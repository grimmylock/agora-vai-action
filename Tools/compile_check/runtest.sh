#!/bin/bash
# Compiles the scripts + test/Program.cs against stubs.cs and runs the data/loadout checks.
SRC="$1"; REF=/usr/lib/dotnet/packs/Microsoft.NETCore.App.Ref/8.0.31/ref/net8.0
REFS=$(ls $REF/*.dll | sed 's/^/-r:/' | tr '\n' ' ')
FILES=$(find "$SRC" -name "*.cs" -not -path "*/Editor/*" -not -path "*/Net/*")   # Net/: Unity-only (Netcode, Services)
rm -f /tmp/test.dll; dotnet /usr/lib/dotnet/sdk/8.0.131/Roslyn/bincore/csc.dll -nologo -noconfig -nostdlib -t:exe -langversion:9.0 -out:/tmp/test.dll \
  -nowarn:0168,0169,0219,0414,0649,0162,8981,0108,0114,0067 $REFS "$(dirname "$0")/stubs.cs" "$(dirname "$0")/test/Program.cs" $FILES 2>&1 | grep error
cat > /tmp/test.runtimeconfig.json <<J
{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}
J
dotnet /tmp/test.dll
