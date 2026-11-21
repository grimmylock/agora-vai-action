using System; using System.Linq; using GameCore; using GameData;
static class RenderMain {
  static int Main(string[] args) {
    string art = Environment.GetEnvironmentVariable("ART_DIR") ?? "_render";
    UnityEngine.Resources.Hook = (path, type) => {
      if (path == "Sprites/world_atlas") return new UnityEngine.TextAsset { bytes = System.IO.File.ReadAllBytes(art + "/world_atlas.raw") };
      if (path == "Sprites/world_atlas_meta") return new UnityEngine.TextAsset { text = System.IO.File.ReadAllText(art + "/world_atlas.txt") };
      return null;
    };
    GameServices.Grid = new TacticalGridSystem();
    WorldArt.NoRoofs = Environment.GetEnvironmentVariable("NOROOF") == "1";
    GameServices.Combat = new CombatResolver();
    if (Environment.GetEnvironmentVariable("RENDER_HOUR") is string rh) MacroState.Current.Hour = float.Parse(rh, System.Globalization.CultureInfo.InvariantCulture);
    else MacroState.Current.Hour = 13f;
    if (Environment.GetEnvironmentVariable("RENDER_WEATHER") is string rw) Atmosphere.Current = Enum.Parse<Weather>(rw);
    if (Environment.GetEnvironmentVariable("LOOK_AUDIT") == "1") {   // Batch 93: every look on every built map -> look_audit.tsv (look, type, map, count)
      var sb = new System.Text.StringBuilder();
      foreach (var kv in MapLayoutDatabase.All) {
        var cnt = new System.Collections.Generic.Dictionary<string, int>(); var m = kv.Value;
        for (int y = 0; y < m.Tiles.Length; y++) for (int x = 0; x < m.Tiles[y].Length; x++) {
          string l = m.Looks?[y]?[x]; if (l == null) continue; string k = l + "\t" + m.Tiles[y][x] + (l == "armchair" || l == "sunktruck" ? "@" + (m.Grounds?[y]?[x] ?? "theme") : "");
          cnt[k] = (cnt.TryGetValue(k, out var n) ? n : 0) + 1; }
        foreach (var c in cnt) sb.AppendLine($"{c.Key}\t{kv.Key}\t{c.Value}");
      }
      System.IO.File.WriteAllText($"{art}/look_audit.tsv", sb.ToString());
    }
    if (Environment.GetEnvironmentVariable("PROP_SHEET") is string props) {   // Batch 93: every prop on its own patch -> a test map "prop_sheet"
      var names = props.Split(','); int cols = 8, cw = 9, ch = 10, rowsN = (names.Length + cols - 1) / cols;
      var c = new TerrainCanvas(cols * cw + 2, rowsN * ch + 2, 1);
      c.Paint(0, 0, cols * cw + 2, rowsN * ch + 2, GroundType.Dirt);
      for (int i = 0; i < names.Length; i++) { int x = 1 + (i % cols) * cw + cw / 2, y = 1 + (i / cols) * ch + ch - 3;
        if (names[i].StartsWith("L:")) {   // an L of cover: 3 north-south ending in the corner, 3 east-west
          string l = names[i].Substring(2); for (int k = 0; k < 3; k++) c.Put(x + 1, y - 1 - k, TileType.HardCover, l); for (int k = -1; k <= 1; k++) c.Put(x + k, y, TileType.HardCover, l); }
        else c.Put(x, y, TileType.Empty, names[i]); }
      var lay = c.ToMapLayout(); lay.FullSize = true;
      MapLayoutDatabase.All["prop_sheet"] = lay;
    }
    foreach (var id in args) {
      var sw = System.Diagnostics.Stopwatch.StartNew();
      MacroState.Current.CurrentZoneId = id;
      TerrainMap.BuildTerrainForZone(id);
      if (Environment.GetEnvironmentVariable("MAP_QA") == "1") MapQa.Run(id, art);   // 2026-11-21: the structural quality check (MapQa below)
      if (Environment.GetEnvironmentVariable("STASHES") == "1")   // Batch 124: where the stashes are
        foreach (var o in TerrainMap.InteractiveObjects) Console.WriteLine($"stash {o.X},{o.Y} {TerrainMap.GetTile(o.X, o.Y).Look} {o.Name}");
      var annexLines = new System.Collections.Generic.List<string>();
      if (Environment.GetEnvironmentVariable("ANNEX") == "1" && id == "loading_annex") {   // 2026-11-19: what the Annex holds (papers, bags, mutants) as markers: O x y kind name / M x y name
        EncounterState.Enemies.Clear(); Tier3Annex.PlaceForRender();
        foreach (var o in TerrainMap.InteractiveObjects) annexLines.Add($"O\t{o.X}\t{o.Y}\t{(o.Lock == "doc" ? "doc" : "bag")}\t{o.Name}\t{TerrainMap.GetTile(o.X, o.Y).Look}");
        foreach (var m in EncounterState.Enemies.OfType<Mutant>()) annexLines.Add($"M\t{m.X}\t{m.Y}\t-\t{m.Name}");
      }
      Vision.Reset();
      int xs = Environment.GetEnvironmentVariable("RENDER_X2") == "1" && WorldArt.Ready ? 2 : 1;   // RENDER_X2=1: 32 px per tile, double-density sprites at full resolution
      var px = WorldArt.Ready ? (xs == 2 ? WorldArt.ComposePreviewX2(id) : WorldArt.ComposePreview(id)) : TerrainArt.Compose(id);
      if (Environment.GetEnvironmentVariable("TIME_BAKE") == "1") {   // Batch 94: what a sun re-bake costs
        var tw = System.Diagnostics.Stopwatch.StartNew(); for (int i = 0; i < 3; i++) WorldArt.ComposeGround();
        Console.WriteLine($"{id}: ComposeGround {tw.ElapsedMilliseconds / 3} ms per bake"); }
      int w = TerrainMap.Width * 16 * xs, h = TerrainMap.Height * 16 * xs;
      if (Environment.GetEnvironmentVariable("GRADE") == "1") {   // Batch 94: the in-game colour of the hour (AtmosphereView wash at half + ColorGrade: saturation, filter, haze); no vignette
        var g = Atmosphere.Look(); float wa = g.WashA * 0.5f;
        for (int i = 0; i < px.Length; i++) {
          var c = px[i]; float r = c.r / 255f, gg = c.g / 255f, b = c.b / 255f;
          r += (g.Wash.r - r) * wa; gg += (g.Wash.g - gg) * wa; b += (g.Wash.b - b) * wa;
          float lum = (r + gg + b) / 3f; r = lum + (r - lum) * g.Saturation; gg = lum + (gg - lum) * g.Saturation; b = lum + (b - lum) * g.Saturation;
          r *= g.Filter.r; gg *= g.Filter.g; b *= g.Filter.b;
          r += g.Haze * (1 - r); gg += g.Haze * (1 - gg); b += g.Haze * (1 - b);
          px[i] = new UnityEngine.Color32((byte)(Math.Clamp(r, 0, 1) * 255), (byte)(Math.Clamp(gg, 0, 1) * 255), (byte)(Math.Clamp(b, 0, 1) * 255), c.a);
        }
      }
      if (Environment.GetEnvironmentVariable("FIND") is string fl)   // light sources and other looks by name: where they are
        foreach (var look in fl.Split(',')) for (int y = 0; y < TerrainMap.Height; y++) for (int x = 0; x < TerrainMap.Width; x++)
          if (TerrainMap.GetTile(x, y).Look == look) Console.WriteLine($"find {look} {x},{y}");
      if (Environment.GetEnvironmentVariable("LIGHTS") is string lq) {   // Lighting prototype (WorldArt.Lights.cs): real point lights with shadows on a region, x0,y0,x1,y1 in tiles
        var q = Array.ConvertAll(lq.Split(','), int.Parse); var l = Atmosphere.Look(); float n = l.Night;
        var all = WorldArt.PlacedLights(q[0] - 12, q[1] - 12, q[2] + 12, q[3] + 12);
        Console.WriteLine($"lights: {all.Count} (night {n:0.00})");
        float amb(float day, float night) => day + (night - day) * n;
        if (Environment.GetEnvironmentVariable("GAME") == "1") {   // what the game's night overlay (AtmosphereView.FineTexel) draws from the same light field: an alpha overlay, not a multiply
          int gx0 = q[0] * 16, gy0 = q[1] * 16, lw = (q[2] - q[0] + 1) * 16, lh = (q[3] - q[1] + 1) * 16, W = TerrainMap.Width * 16, Hh = TerrainMap.Height * 16;
          Lighting.Reset(); Lighting.Enabled = true;
          var nightCol = new UnityEngine.Color32(5, 7, 13, 255); int st = 2, fw = lw / st;
          for (int pass = 0; pass <= all.Count + 1; pass++) {               // a light bakes per call, as in game: call until every one has
            if (!Lighting.Field(gx0, gy0, lw, lh, st, 0f, out var lr, out var lg, out var lb)) break;
            if (pass < all.Count + 1) continue;
            for (int gy = gy0; gy < gy0 + lh; gy++) for (int gx = gx0; gx < gx0 + lw; gx++) {
              int k = ((gy - gy0) / st) * fw + (gx - gx0) / st; int pi = (Hh - 1 - gy) * W + gx; var c = px[pi];
              var o = AtmosphereView.FineTexel(1f, 0f, true, lr[k], lg[k], lb[k], true, n, nightCol); float a = o.a / 255f;
              px[pi] = new UnityEngine.Color32((byte)(c.r * (1 - a) + o.r * a), (byte)(c.g * (1 - a) + o.g * a), (byte)(c.b * (1 - a) + o.b * a), c.a);
            }
          }
        } else
        WorldArt.ApplyLights(px, q[0] * 16, q[1] * 16, (q[2] + 1) * 16, (q[3] + 1) * 16, amb(1f, 0.10f), amb(1f, 0.135f), amb(1f, 0.25f), all, 0f);
      }
      else if (Environment.GetEnvironmentVariable("LIT") == "1") {   // Batch 92: darkness and placed lights as the game draws them (no player light)
        var l = Atmosphere.Look(); float night = l.Night;
        float L(int x, int y) => LightMap.At(x, y);
        for (int py = 0; py < h; py++) for (int pxx = 0; pxx < w; pxx++) {
          float gx = pxx / 16f - 0.5f, gy = (h - 1 - py) / 16f - 0.5f;int x0 = (int)Math.Floor(gx), y0 = (int)Math.Floor(gy); float fx = gx - x0, fy = gy - y0;
          float lit = (L(x0, y0) * (1 - fx) + L(x0 + 1, y0) * fx) * (1 - fy) + (L(x0, y0 + 1) * (1 - fx) + L(x0 + 1, y0 + 1) * fx) * fy;
          float a = night * (1f - lit); int i = py * w + pxx; var c = px[i];
          UnityEngine.Color32 n = new UnityEngine.Color32(5, 7, 13, 255);
          int tx = (int)Math.Round(gx), ty = (int)Math.Round(gy); float lamp = L(tx, ty);
          if (lamp > 0.02f) { var t = LightMap.Tint(tx, ty); float ta = lamp * (1f - a) * (t.r > t.g * 2 ? 0.30f : 0.14f), oa = 1f - (1f - a) * (1f - ta);
            if (oa > 0.001f) n = new UnityEngine.Color32((byte)((n.r * a * (1f - ta) + t.r * ta) / oa), (byte)((n.g * a * (1f - ta) + t.g * ta) / oa), (byte)((n.b * a * (1f - ta) + t.b * ta) / oa), 255); a = oa; }
          px[i] = new UnityEngine.Color32((byte)(c.r + (n.r - c.r) * a), (byte)(c.g + (n.g - c.g) * a), (byte)(c.b + (n.b - c.b) * a), c.a);
        }
      }
      var bytes = new byte[12 + px.Length * 4];
      System.Text.Encoding.ASCII.GetBytes("RAW1").CopyTo(bytes, 0);
      BitConverter.GetBytes(w).CopyTo(bytes, 4); BitConverter.GetBytes(h).CopyTo(bytes, 8);
      for (int i = 0; i < px.Length; i++) { bytes[12 + i * 4] = px[i].r; bytes[13 + i * 4] = px[i].g; bytes[14 + i * 4] = px[i].b; bytes[15 + i * 4] = px[i].a; }
      System.IO.File.WriteAllBytes($"{art}/render_{id}.raw", bytes);
      // labels: landmarks and exits in tile coords
      var sb = new System.Text.StringBuilder();
      if (MapLayoutDatabase.All.TryGetValue(id, out var lay) && lay.Anchors != null) foreach (var kv in lay.Anchors) sb.AppendLine($"A\t{kv.Value.x}\t{kv.Value.y}\t{kv.Key}");
      foreach (var e in TerrainMap.Exits) sb.AppendLine($"E\t{e.X}\t{e.Y}\t{e.TargetZoneId}");
      foreach (var l in annexLines) sb.AppendLine(l);
      System.IO.File.WriteAllText($"{art}/labels_{id}.txt", sb.ToString());
      Console.WriteLine($"{id}: {TerrainMap.Width}x{TerrainMap.Height} in {sw.ElapsedMilliseconds} ms");
      if (Environment.GetEnvironmentVariable("DUMPAT") is string da) {
        var q = da.Split(','); int cx = int.Parse(q[0]), cy = int.Parse(q[1]), r = int.Parse(q[2]);
        for (int y = cy - r; y <= cy + r; y++) { var line = new System.Text.StringBuilder($"{y,4} ");
          for (int x = cx - r; x <= cx + r; x++) { var t = TerrainMap.GetTile(x, y); char ch = t.Type switch { TileType.Solid => '#', TileType.ScatterCover => 'c', TileType.HardCover => '=', TileType.Difficult => '~', TileType.Door => 'D', _ => '.' }; line.Append(ch).Append(t.Height.ToString().PadLeft(2)).Append(t.Ground.ToString()[0]).Append(' '); }
          Console.WriteLine(line); }
      }
    }
    return 0;
  }
}


// 2026-11-21 (Yan: "run a quality check on all dungeons ... i've noticed some badly done structures"): MAP_QA=1 render.sh <maps> prints, per built map, what a player would hit:
//  POCKET  floor no exit can reach (every lock counted open, low cover counted vaultable): size and box; LOST  a landmark or stash no reachable tile touches;
//  DOOR    a door tile not set in a wall between two walkable sides; BLOCKED  a doorway with a prop or cover on its walkable side; PILLAR  a lone 1-tile wall in the open (in a built room);
//  DIAG    two walls touching only at a corner with floor on the other diagonal (the odd dark blocks / a gap that looks walkable); EXIT  an exit on a tile you cannot stand on.
// Writes map_qa_<id>.tsv (kind x y w h note) for Tools/render/map_qa_overlay.py.
static class MapQa {
  static System.Collections.Generic.HashSet<(int, int)> _secret = new System.Collections.Generic.HashSet<(int, int)>();   // walls meant to be broken (Lab Beta's cracked wall): open for the flood
  static bool Pass(int x, int y) { var t = TerrainMap.GetTile(x, y); return t != null && x >= 0 && y >= 0 && x < TerrainMap.Width && y < TerrainMap.Height && (t.Type != TileType.Solid || _secret.Contains((x, y))); }
  static bool Walk(int x, int y) { var t = TerrainMap.GetTile(x, y); return t != null && x >= 0 && y >= 0 && x < TerrainMap.Width && y < TerrainMap.Height && (t.Type == TileType.Empty || t.Type == TileType.Difficult || t.Type == TileType.Door || t.Type == TileType.DoorOpen); }
  static bool Wall(int x, int y) { var t = TerrainMap.GetTile(x, y); return t == null || x < 0 || y < 0 || x >= TerrainMap.Width || y >= TerrainMap.Height || t.Type == TileType.Solid; }
  public static void Run(string id, string art) {
    int W = TerrainMap.Width, H = TerrainMap.Height; var lines = new System.Collections.Generic.List<string>(); var seen = new bool[W, H];
    _secret.Clear(); var cracks = new System.Collections.Generic.List<(int X, int Y)>(); foreach (var l in TerrainMap.Landmarks ?? new System.Collections.Generic.List<LandmarkInstance>()) if (l.Name == Boss.CrackedWall) cracks.Add((l.X, l.Y));
    if (MapLayoutDatabase.All.TryGetValue(id, out var qlay) && qlay.Anchors != null) foreach (var kv in qlay.Anchors) if (kv.Key == Boss.CrackedWall) cracks.Add((kv.Value.x, kv.Value.y));   // the layout's anchors (TerrainMap.Landmarks can be empty here)
    foreach (var l in cracks) { _secret.Add((l.X, l.Y)); foreach (var (ox, oy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) { var tt = TerrainMap.GetTile(l.X + ox, l.Y + oy); if (tt != null && tt.Type == TileType.Solid) _secret.Add((l.X + ox, l.Y + oy)); } }   // the landmark can be nudged off the wall onto the floor beside it
    var q = new System.Collections.Generic.Queue<(int, int)>();
    foreach (var e in TerrainMap.Exits) { if (!Pass(e.X, e.Y)) lines.Add($"EXIT\t{e.X}\t{e.Y}\t1\t1\tto {e.TargetZoneId} on a wall"); else if (!seen[e.X, e.Y]) { seen[e.X, e.Y] = true; q.Enqueue((e.X, e.Y)); } }
    if (q.Count == 0) { var (sx, sy) = TerrainMap.FindNearestEnterable(W / 2, H / 2, 40); seen[sx, sy] = true; q.Enqueue((sx, sy)); lines.Add($"NOTE\t{sx}\t{sy}\t1\t1\tno exits: flood from the middle"); }
    int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
    while (q.Count > 0) { var (x, y) = q.Dequeue(); for (int k = 0; k < 4; k++) { int nx = x + dx[k], ny = y + dy[k]; if (nx < 0 || ny < 0 || nx >= W || ny >= H || seen[nx, ny] || !Pass(nx, ny)) continue; seen[nx, ny] = true; q.Enqueue((nx, ny)); } }
    // pockets: connected unreachable passable areas
    var done = new bool[W, H]; int pockets = 0, small = 0;
    for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) {
      if (seen[x, y] || done[x, y] || !Pass(x, y)) continue;
      var cq = new System.Collections.Generic.Queue<(int, int)>(); cq.Enqueue((x, y)); done[x, y] = true; int n = 0, x0 = x, x1 = x, y0 = y, y1 = y, walk = 0;
      while (cq.Count > 0) { var (cx, cy) = cq.Dequeue(); n++; if (Walk(cx, cy)) walk++; x0 = Math.Min(x0, cx); x1 = Math.Max(x1, cx); y0 = Math.Min(y0, cy); y1 = Math.Max(y1, cy);
        for (int k = 0; k < 4; k++) { int nx = cx + dx[k], ny = cy + dy[k]; if (nx < 0 || ny < 0 || nx >= W || ny >= H || done[nx, ny] || seen[nx, ny] || !Pass(nx, ny)) continue; done[nx, ny] = true; cq.Enqueue((nx, ny)); } }
      bool edge = x0 == 0 || y0 == 0 || x1 == W - 1 || y1 == H - 1;
      if (walk >= 4 && !edge) { pockets++; lines.Add($"POCKET\t{x0}\t{y0}\t{x1 - x0 + 1}\t{y1 - y0 + 1}\t{n} tiles ({walk} floor) no exit reaches"); } else if (walk > 0 && !edge) small++;
    }
    bool Touch(int x, int y) { for (int ox = -1; ox <= 1; ox++) for (int oy = -1; oy <= 1; oy++) { int nx = x + ox, ny = y + oy; if (nx >= 0 && ny >= 0 && nx < W && ny < H && seen[nx, ny]) return true; } return false; }
    if (qlay != null && qlay.Anchors != null) foreach (var kv in qlay.Anchors) { int lx = kv.Value.x, ly = kv.Value.y; if (!kv.Key.StartsWith("exit:") && lx >= 0 && ly >= 0 && lx < W && ly < H && !Touch(lx, ly)) lines.Add($"LOST\t{lx}\t{ly}\t1\t1\tlandmark {kv.Key}"); }
    foreach (var o in TerrainMap.InteractiveObjects) if (o.X >= 0 && o.Y >= 0 && o.X < W && o.Y < H && !Touch(o.X, o.Y)) lines.Add($"LOST\t{o.X}\t{o.Y}\t1\t1\tobject {o.Name}");
    bool built = TerrainMap.Tiles != null;
    for (int y = 1; y < H - 1; y++) for (int x = 1; x < W - 1; x++) {
      var t = TerrainMap.GetTile(x, y);
      if (t.Type == TileType.Door || t.Type == TileType.DoorOpen) {
        bool ns = Walk(x, y - 1) && Walk(x, y + 1) && Wall(x - 1, y) && Wall(x + 1, y), ew = Walk(x - 1, y) && Walk(x + 1, y) && Wall(x, y - 1) && Wall(x, y + 1);
        bool nsP = Pass(x, y - 1) && Pass(x, y + 1) && Wall(x - 1, y) && Wall(x + 1, y), ewP = Pass(x - 1, y) && Pass(x + 1, y) && Wall(x, y - 1) && Wall(x, y + 1);
        bool wide = (Wall(x - 1, y) || TerrainMap.GetTile(x - 1, y).Type == TileType.Door || TerrainMap.GetTile(x - 1, y).Type == TileType.DoorOpen) && (Wall(x + 1, y) || TerrainMap.GetTile(x + 1, y).Type == TileType.Door || TerrainMap.GetTile(x + 1, y).Type == TileType.DoorOpen) && Pass(x, y - 1) && Pass(x, y + 1)
                 || (Wall(x, y - 1) || TerrainMap.GetTile(x, y - 1).Type == TileType.Door || TerrainMap.GetTile(x, y - 1).Type == TileType.DoorOpen) && (Wall(x, y + 1) || TerrainMap.GetTile(x, y + 1).Type == TileType.Door || TerrainMap.GetTile(x, y + 1).Type == TileType.DoorOpen) && Pass(x - 1, y) && Pass(x + 1, y);
        if (!ns && !ew && !nsP && !ewP && !wide) lines.Add($"DOOR\t{x}\t{y}\t1\t1\t{t.Look ?? t.Type.ToString()} not set in a wall between two walkable sides");
        else if (!ns && !ew && (nsP || ewP)) lines.Add($"BLOCKED\t{x}\t{y}\t1\t1\tdoorway {t.Look} has cover or a prop against it");
      }
      if (t.Type == TileType.Solid && Walk(x - 1, y) && Walk(x + 1, y) && Walk(x, y - 1) && Walk(x, y + 1) && Walk(x - 1, y - 1) && Walk(x + 1, y + 1) && Walk(x + 1, y - 1) && Walk(x - 1, y + 1)) {
        var g = TerrainMap.GetTile(x, y).Ground; if (g != GroundType.CaveFloor && g != GroundType.Dirt && g != GroundType.Grass && g != GroundType.DarkGrass && g != GroundType.Mud && g != GroundType.Sand && g != GroundType.Gravel) lines.Add($"PILLAR\t{x}\t{y}\t1\t1\tlone wall {t.Look} in the open");
      }
      if (x < W - 1 && y < H - 1) {
        bool a = Wall(x, y), b = Wall(x + 1, y), c = Wall(x, y + 1), d = Wall(x + 1, y + 1);
        if (a && d && !b && !c && Walk(x + 1, y) && Walk(x, y + 1)) lines.Add($"DIAG\t{x}\t{y}\t2\t2\twalls touch only at a corner");
        if (b && c && !a && !d && Walk(x, y) && Walk(x + 1, y + 1)) lines.Add($"DIAG\t{x}\t{y}\t2\t2\twalls touch only at a corner");
      }
    }
    // jagged walls: a wall tile with walkable floor on two perpendicular sides (a stub end, a staircase step, a bitten corner); in a cave they are rock and natural, so caves are counted apart
    int jag = 0; var jagAt = new System.Collections.Generic.List<(int, int)>();
    for (int y = 1; y < H - 1; y++) for (int x = 1; x < W - 1; x++) {
      if (!Wall(x, y)) continue; bool n = Walk(x, y - 1), so = Walk(x, y + 1), w = Walk(x - 1, y), e = Walk(x + 1, y);
      if ((n || so) && (w || e)) { jag++; jagAt.Add((x, y)); }
    }
    int cave = 0, tiles = 0; for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { var t = TerrainMap.GetTile(x, y); if (t != null && Walk(x, y)) { tiles++; if (t.Ground == GroundType.CaveFloor) cave++; } }
    lines.Add($"JAGSUM\t0\t0\t0\t0\t{jag} jagged wall tiles ({(tiles > 0 ? cave * 100 / tiles : 0)}% cave floor)");
    foreach (var (jx, jy) in jagAt) lines.Add($"JAG\t{jx}\t{jy}\t1\t1\tjagged");
    int reach = 0, pass = 0; for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) { if (Pass(x, y)) { pass++; if (seen[x, y]) reach++; } }
    Console.WriteLine($"QA {id}: {W}x{H}, reachable {reach}/{pass} passable tiles, {pockets} pockets, {small} tiny pockets, " + string.Join(", ", lines.GroupBy(l => l.Split('\t')[0]).Select(g => $"{g.Count()} {g.Key}")));
    foreach (var l in lines.Where(l => !l.StartsWith("DIAG") && !l.StartsWith("JAG\t")).Take(40)) Console.WriteLine("  " + l.Replace('\t', ' '));
    System.IO.File.WriteAllLines($"{art}/map_qa_{id}.tsv", lines);
  }
}
