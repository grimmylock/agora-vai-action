// Batch 92 (Yan: "make it dark, gritty ... add a light system, make some places dark requiring a flashlight"): placed lights
// (street lamps, campfires, and the ceiling lights floor plans mark with "light") bake a per-cell light map when a map loads.
// Vision.Light counts it, so lit rooms show from the dark and dark rooms need the flashlight. Stealth: standing in a lamp's
// pool you're seen as by day; with the flashlight off in the dark, enemies see you at 60%.
namespace GameCore
{
    public static class LightMap
    {
        static float[] _lit, _red;
        static object _tiles;
        static int _w;

        static float Radius(string look) => look == "lamp" ? 6f : look == "campfire" ? 5f : look == "light" ? 5f : look == "redlight" ? 4f : 0f;

        // ponytail: baked once per map; a door opened later doesn't let light through. Rebake on door changes if it shows.
        static void Ensure()
        {
            if (ReferenceEquals(_tiles, TerrainMap.Tiles) && _lit != null) return;
            _tiles = TerrainMap.Tiles; _w = TerrainMap.Width; int h = TerrainMap.Height;
            _lit = new float[_w * h]; _red = new float[_w * h];
            if (TerrainMap.Tiles == null) return;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < _w; x++)
                {
                    string look = TerrainMap.Tiles[y, x]?.Look;
                    float r = Radius(look);
                    if (r <= 0f) continue;
                    int ri = (int)r;
                    for (int yy = y - ri; yy <= y + ri; yy++)
                        for (int xx = x - ri; xx <= x + ri; xx++)
                        {
                            if (xx < 0 || yy < 0 || xx >= _w || yy >= h) continue;
                            float d = (float)System.Math.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y));
                            float v = System.Math.Min(1f, (r - d) / 2.5f);
                            if (v <= _lit[yy * _w + xx] || !Clear(x, y, xx, yy)) continue;
                            _lit[yy * _w + xx] = v; _red[yy * _w + xx] = look == "redlight" ? 1f : 0f;
                        }
                }
        }

        /// <summary>Bresenham from the light to the cell; walls and closed doors stop it (the far wall face itself is lit).</summary>
        static bool Clear(int x0, int y0, int x1, int y1)
        {
            int dx = System.Math.Abs(x1 - x0), dy = -System.Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                if (x0 == x1 && y0 == y1) return true;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
                if ((x0 != x1 || y0 != y1) && TerrainMap.BlocksSight(x0, y0)) return false;
            }
        }

        /// <summary>0..1 light from placed lights at a cell.</summary>
        public static float At(int x, int y)
        {
            Ensure();
            return x < 0 || y < 0 || x >= _w || _lit == null || y * _w + x >= _lit.Length ? 0f : _lit[y * _w + x];
        }

        /// <summary>The colour a lamp gives its pool: warm work light, or the red of an emergency lamp.</summary>
        public static UnityEngine.Color32 Tint(int x, int y)
        {
            Ensure();
            bool red = x >= 0 && y >= 0 && x < _w && _red != null && y * _w + x < _red.Length && _red[y * _w + x] > 0.5f;
            return red ? new UnityEngine.Color32(200, 30, 20, 255) : new UnityEngine.Color32(255, 214, 150, 255);
        }

        public static void Reset() { _lit = null; _tiles = null; }

        /// <summary>Multiplier on how far an enemy sees you: as by day in a lamp's pool, 0.6 with your flashlight off in the
        /// dark, 1 otherwise (the dark itself already cuts sight through Atmosphere.SightFactor).</summary>
        public static float Exposure(Player p)
        {
            var l = Atmosphere.Look();
            if (p == null || l.Night < 0.4f) return 1f;
            if (At(p.X, p.Y) >= 0.5f) return 1f / Atmosphere.SightFactor();
            return p.FlashlightOn ? 1f : 0.6f;
        }
    }
}
