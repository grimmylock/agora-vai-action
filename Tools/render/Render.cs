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
      if (Environment.GetEnvironmentVariable("STASHES") == "1")   // Batch 124: where the stashes are
        foreach (var o in TerrainMap.InteractiveObjects) Console.WriteLine($"stash {o.X},{o.Y} {TerrainMap.GetTile(o.X, o.Y).Look} {o.Name}");
      Vision.Reset();
      var px = WorldArt.Ready ? WorldArt.ComposePreview(id) : TerrainArt.Compose(id);
      if (Environment.GetEnvironmentVariable("TIME_BAKE") == "1") {   // Batch 94: what a sun re-bake costs
        var tw = System.Diagnostics.Stopwatch.StartNew(); for (int i = 0; i < 3; i++) WorldArt.ComposeGround();
        Console.WriteLine($"{id}: ComposeGround {tw.ElapsedMilliseconds / 3} ms per bake"); }
      int w = TerrainMap.Width * 16, h = TerrainMap.Height * 16;
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
      if (Environment.GetEnvironmentVariable("LIT") == "1") {   // Batch 92: darkness and placed lights as the game draws them (no player light)
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
