# Plan editor (Yan, 2026-11-28)
Open `Tools/editor/plan_editor.html` in a browser (double-click it; nothing to install). Left-drag paints, right-drag erases, Shift-drag fills a rectangle.
- Pick a tool (wall / window / door / erase), a floor, or a prop from the palette (real sprites, search by name). Floors paint only the floor layer.
- **Check** marks floor tiles no door can reach (red) and props sitting in the row a wall hides (orange), the same checks the compiler makes.
- **Mirror** flips left-right and swaps east/west props. **Resize** changes the grid (keeps the top-left). Zoom, and "glyphs" shows the plan letters instead of sprites.
- **Load** opens any plan in `ArtSource/plans/*.plan` (embedded when the page was built) or a `.plan` file you pick; **Save .plan** writes the file (Chrome/Edge ask where; elsewhere it downloads). Save into `ArtSource/plans/`, commit, and tell Claude: it compiles with `python3 Tools/plans/plan.py` and stamps the plan on the map.
- The preview is flat: walls are blocks, with none of the game's wall height. For the real look, Claude runs the map renderer.
- After new props or plans are added, rebuild the page: `python3 Tools/editor/build_editor.py`.
