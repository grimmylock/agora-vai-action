# Art audit (Batch 93, 2026-10-14) - every world prop, real renders

Made with the game renderer (`Tools/render/render.sh`), no mockups:
- `audit_props.png`: all 50 props, each on its own patch of ground, at 09:00 and 17:30 in clear weather (real sun shadows).
  Under each one: how many map tiles use it and on which maps. (`PROP_SHEET=<looks> RENDER_WEATHER=Clear RENDER_HOUR=h Tools/render/render.sh prop_sheet`)
- `audit_concept_vs_game.jpg`: concept A (`ArtSource/concept/concept.py`) over the Tier 1 Old Farmstead at 16:00, clear, same zoom.
- `audit_tall_props_shadows.png`: the tall props up close, morning and evening shadows.
- Look counts per map: `LOOK_AUDIT=1 Tools/render/render.sh` -> `Tools/render/_render/look_audit.tsv`.

## Claude's flags (Yan decides: keep / redraw / remove)

**Reads as nothing or as the wrong thing**
- `brush` (388 tiles): draws nothing when alone.
- `fountain` (16): reads as an eye.
- `tires` (211): a black box, not a stack of tyres.
- `jersey` (170): a grey box; a road barrier is a long low wedge.
- `block` (221), `pillar` (32): plain grey shapes with no identity.
- `hatch` (13): a dark box.
- `pipe` (220): a bar floating above the ground. `pipepost` (46): an unexplained T.
- `trash` (89), `scrap` (30), `debris` (1408): unreadable smudges.
- `culvert` (4): a ring lying on flat ground (it needs an embankment to come out of).

**Placed where it makes no sense**
- `armchair`: 35 out in the fields (perimeter, scrapyard).
- `sandbags` (863): stacks in open meadows with nothing to defend (the cover-wrap pass grows them, `CoverWrap.cs`).
- `sunktruck`: meant for water, sits on dry ground.

**Wrong scale** (a person is 16 px, about 1.8 m)
- `container` (969 tiles): 48 px long = about 5 m; a real one is 12 m (about 107 px).
- Birches, cars and the bus are about half the size they are in the concept.

**Missing compared with the concept**
- Power lines between the poles (poles stand alone).
- Tall 3/4 building fronts: the concept shows two storeys of wall, windows and a door; the game shows mostly flat roofs with a thin strip of wall.
- Shadows: in clear weather they're there but faint; in any other weather only small contact blobs. The game starts overcast (`Atmosphere.Current` defaults to Overcast), so most of the time there are no sun shadows at all.
- Renders don't include the in-game colour grade (`ColorGrade.Apply`); the game looks a bit different.

**Unused art**: `barrel`, `engine`.

**Reads fine**: watchtower, pine, birch (but small), dead tree, red tree, rust barrel, crate, logs, lockers, desk, dumpster, bus, wreck, flatcar, tank, rock, stones, reeds, bush.
