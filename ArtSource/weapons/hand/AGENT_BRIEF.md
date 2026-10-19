# Weapon hand-pixel brief (for helper agents)

Game: "agora vai", a 2D pixel-art Zone-survival game (Unity). Characters are 16x16 px. Weapons are drawn
by HAND as text-row pixel grids (no auto-tracing straight to the final art). The pilot guns that set the
quality bar are in `b_v2.py` (VK74, BayouPump, PSh18Pchela, RK12Kuvalda, in-hand B sprites) plus
`vk74.py`, `bayou.py`, `psh18_full.py` (menu A sprites), and `guns/WaldenP9.py` (the template you copy).

## What you deliver per weapon
One file `guns/<Key>.py` (Key exactly as listed, case-sensitive) defining:
- `CAT` : category string ("melee", "sidearm", "smg", "shotgun", "assault", "sniper", "heavy").
- `A`   : list of equal-length strings, the MENU sprite, max 64 wide x 32 tall (it gets centred in a
          64x32 canvas and outlined automatically). Make it as big as the canvas allows while keeping the
          gun's real proportions (long guns ~56-62 wide; pistols/knives limited by the 32 height -> ~40-46 wide).
- `B`   : list of equal-length strings, the IN-HAND sprite at the target width given in your list
          (half-size pixels next to the 16 px characters). Draw it by hand at that size; do NOT shrink A.
- `GRIP`    = (x, y) in B pixels (top-left = 0,0, use .5 for centres): centre of the handle the fist closes on
              (pistol grip; for stocks without a pistol grip, the wrist just behind the trigger guard;
              for knives the middle of the handle).
- `SUPPORT` = (x, y) under the handguard / forend / pump, on its bottom edge row, or `None` for pistols,
              revolvers, machine pistols without a forend, and all melee.
- `MUZZLE`  = (x, y): just past the muzzle (x = B width), y = bore row (+0.5). Melee: the blade tip.
You may build A with code (see `common.grid/fill/put`, as WaldenP9 does) and finish with
`A = ["".join(r) for r in G]`. `.` = transparent. No outline chars: the outline is added for you.

## Palette (only these chars)
- gunmetal `0 1 2 3 4 5` (#d5dadd ... #1f2225): 0/1 = specular highlights, 2 = lit top edges, 3/4 = body, 5 = darkest/shadow/holes
- black polymer `p q r` (#5a5f65 #3a3e42 #26292c)
- wood `a b c d` (light -> dark)   - olive/OD `o O e`   - brass `Y y`   - optics `L l k` (dark glass, lens, glint)
- blade `s S t` (bright steel), leather `m n`, red `R` (laser/red-dot/markings only)

## Style rules (the bar is the pilot guns)
- Light from the top: top edges one tone lighter, bottom edges darker. 3-5 tones per material, flat, no dithering
  (except a sparse stipple/checkering pattern on grips).
- Exact silhouette from the photo: stock shape, magazine curve, trigger-guard loop (keep its hole open),
  sights, muzzle device, rails, bolt handle, cylinder, lever loop... whatever makes the gun recognisable.
- Separate parts with value changes (receiver vs handguard vs barrel vs magazine), not outlines.
- Keep holes (trigger guard, thumbhole stocks, lever loops, skeleton stocks) transparent.
- B sprites are small: favour 2-3 strong readable features over noise. Barrels at least 1 px, muzzle devices 2 px tall.
- No text, logos or brand marks.
- Material colours: blued/black steel = 3/4/5 with 2 top edges; parkerized/grey = 2/3; polymer furniture = p/q/r;
  wood = a/b/c/d; OD-green polymer = o/O/e; tan/FDE furniture = a/b with 2 highlights (sparingly).

## Workflow per weapon
1. `python3 kit.py guide <Key> <width>` prints an auto-classified ASCII guide of the reference photo at that
   width (use it only as an UNDERLAY for proportions and part positions) and writes a gridded trace PNG
   (`/tmp/claude-0/kit/trace_<Key>_<w>.png`). Look at the trace PNG with the Read tool. Do this for the A width
   and the B width.
2. Write `guns/<Key>.py`.
3. `python3 kit.py sheet <Key> [more keys]` renders a review sheet (photo | A | B | held at 0/25/180 deg) to
   `/tmp/claude-0/kit/sheet.png` - pass a unique name instead by calling `kit.sheet([...], out=path)` from
   python if you run in parallel with others: `python3 -c "import kit; kit.sheet(['K1','K2'], out='/tmp/claude-0/kit/sheet_<you>.png')"`.
   READ it, judge it honestly against the pilot guns (VK74/PSh18/WaldenP9 in the same sheet is a good check),
   fix what reads poorly (blobby silhouettes, missing trigger-guard hole, unreadable parts, wrong grip point
   making the fist sit on the receiver) and re-render. Two or three passes per gun is normal.
4. Check the held previews: the fist must sit on the handle and the gun must not look held "like a stick";
   the support glove should be under the forend.

Work from `/home/claude/art/weapons/hand` (run commands there). Do not edit kit.py, common.py, b_v2.py or
anyone else's gun files. If a reference photo is unusable (wrong angle), draw from your own knowledge of the
real gun's side profile and say so in your final report.
Final report: one line per weapon (key, B size, anything you were unsure about).
