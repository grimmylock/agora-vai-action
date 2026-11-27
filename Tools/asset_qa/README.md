# Asset QA (from gamedev-skills/awesome-gamedev-agent-skills, skill `create-game-assets`, Apache-2.0)
`asset_report.py` and `build_preview_sheet.py` are copied unchanged from that repo (commit c6ca6c4, 2026-11-27) under its Apache-2.0 licence. `make_manifest.py` is ours.
- `python3 Tools/asset_qa/make_manifest.py` : scans `ArtSource/world/settle_pl/raw_*.png` (the Pixel Lab props), runs the report checks (alpha present and a transparent corner, colour count, near-white fringe), writes `ArtSource/ASSET_MANIFEST.json` and prints the problems.
- `python3 Tools/asset_qa/build_preview_sheet.py ArtSource/world/settle_pl/raw_*.png --out /tmp/sheet.png --columns 8 --cell-size 192` : contact sheet over a checkerboard.
