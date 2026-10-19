#!/bin/bash
# Renders zones/maps to PNG without Unity (Linux/WSL/macOS with the .NET 8 SDK, python3 + numpy + pillow).
#   Tools/render/render.sh perimeter scrapyard ...      -> Tools/render/_render/<zone>.png (+ labels_<zone>.txt)
#   DUMPAT=x,y,r Tools/render/render.sh <zone>          -> also prints tile types/heights around (x,y)
# The atlas is packed from ArtSource/world the first time. Overviews: python3 Tools/render/overview.py
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"; ROOT="$(cd "$HERE/../.." && pwd)"
OUT="$HERE/_render"; mkdir -p "$OUT"
[ -f "$OUT/world_atlas.raw" ] || (cd "$ROOT/ArtSource/world" && python3 pack.py "$OUT" >/dev/null)
SDK=$(ls -d /usr/lib/dotnet/sdk/8.* 2>/dev/null | head -1); [ -n "$SDK" ] || SDK=$(ls -d "$(dirname "$(readlink -f "$(which dotnet)")")"/sdk/8.* | head -1)
REF=$(ls -d "$(dirname "$SDK")"/../packs/Microsoft.NETCore.App.Ref/8.*/ref/net8.0 | head -1)
REFS=$(ls $REF/*.dll | sed 's/^/-r:/' | tr '\n' ' ')
FILES=$(find "$ROOT/Assets/Scripts" -name "*.cs" -not -path "*/Editor/*")
dotnet "$SDK/Roslyn/bincore/csc.dll" -nologo -noconfig -nostdlib -t:exe -langversion:9.0 -optimize+ -out:"$OUT/render.dll" \
  -nowarn:0168,0169,0219,0414,0649,0162,8981,0108,0114,0067 $REFS "$ROOT/Tools/compile_check/stubs.cs" "$HERE/Render.cs" $FILES 2>&1 | grep error && exit 1 || true
echo '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}' > "$OUT/render.runtimeconfig.json"
(cd "$OUT" && ART_DIR="$OUT" dotnet render.dll "$@")
for z in "$@"; do python3 "$ROOT/Tools/compile_check/raw.py" from "$OUT/render_$z.raw" "$OUT/$z.png"; rm -f "$OUT/render_$z.raw"; done
