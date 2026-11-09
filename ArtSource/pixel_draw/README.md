# pixel-draw for item icons (trial, 2026-11-09)

`sprite-gen pixel-draw` (https://github.com/aldegad/sprite-gen, installed at account level, never into this repo) has Claude write a
16x16 icon as a grid of colour keys. `grid.txt` is the truth; the PNG is derived from it. Yan OK'd dropping the old "no AI image
generation" rule (2026-11-09).

**Status: one icon tried (an ammo box). Not shipped, not checked in Unity, not compared with the real item sprites beyond a side by side.**
It runs on the Claude subscription login (no per-call cost) when `claude auth status` shows a login; otherwise it bills `ANTHROPIC_API_KEY`.

## Install (per cloud session; the container forgets it)
```
D=~/.claude/skills/sprite-gen
git clone --depth 1 https://github.com/aldegad/sprite-gen "$D" && rm -rf "$D/.git"
cd "$D" && python3 -m venv .venv && .venv/bin/pip install -q -e .
```
Windows: same steps in PowerShell, the venv's python is `.venv\Scripts\python.exe`.

## The recipe that worked
1. Draw, locked to the palette (about 40 s, 2 candidates, a vision call picks one):
   ```
   sprite-gen pixel-draw --out-dir <out> --kind icon --size 16 --palette ArtSource/pixel_draw/agora_items.hex --subject "<see below>"
   ```
   Say the shape in numbers, the view, which tone goes where (lit top/left, shade bottom/right, flat, no dithering), "1 px outline in
   the near-black outline colour" and "one pixel of empty margin". Free palettes and vague prompts gave chest-like, 11-colour results.
2. Look at `preview-x16.png` and `report.json` (`pick.verdict` notes). The tool's own bar is low: it called weak boxes "ship".
3. Edit `grid.txt` by hand (see `ammo_box_16_hand.grid.txt`), then re-render with no model call. Do NOT pass `--size` with `--grid`:
   ```
   sprite-gen pixel-draw --grid <edited grid.txt> --out-dir <out2> --kind icon --palette ArtSource/pixel_draw/agora_items.hex
   ```
4. Put the result next to the real icons (`Assets/Resources/Sprites/Items/`) at 10x and compare before anything goes in the game.
   Real icons use `ArtSource/make_item_icons.py`; an icon that is kept should be added there or copied as a PNG on Yan's approval.

Subject used for the ammo box: a steel ammo can seen from a high 3/4 angle so both lid top and front face show; a wide low box about
14 px wide and 9 px tall, not a chest and not cube-like; lit olive lid top, base olive front, shade olive on the bottom and right
edge; a steel folding carry handle along the lid middle; one plain steel latch centred on the front edge; a 2-3 pixel pale-yellow
stencil and one tiny rust scuff; 1 px outline; one pixel of empty margin.

## What it did not do well
- Still front views with a lid band, not a true 3/4 angle (that needs a hand-drawn side face).
- Lids still read a little like a chest. The hand-edited grid is the cleanest of the four tries.
- Past 48 px, walk cycles, and painterly work are outside what the tool is good at (its own doc: `docs/pixel-draw.md`). Our 64x32 guns
  are past that edge; the weapon method Yan approved stays `ArtSource/weapons/hand/PIXEL_GUIDE.md`.
- No GPT or Grok login is needed for pixel-draw. `gen` and the video features do need one.
