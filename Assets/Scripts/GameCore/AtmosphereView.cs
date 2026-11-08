// Batch 31: draws Atmosphere.Look() over the world, every frame, around the camera:
//   wash      - one full-view quad tinting everything (overcast grey, golden dusk, night blue)
//   fog       - two soft noise layers drifting across, under the light layer
//   rain      - two streak layers falling at different speeds + splashes on the ground; storms add
//               lightning flashes (and thunder a moment later)
//   light     - a small texture over the view, two texels per tile, bilinear so edges are soft:
//               darkness outside your light (night, underground) and outside your line of sight
// All textures are generated here; nothing to import. Order: world < wash < fog < rain < light < fx.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class AtmosphereView
    {
        const int TexelsPerTile = 2;
        readonly Transform _root;
        SpriteRenderer _wash, _light, _flash;
        readonly SpriteRenderer[] _fog = new SpriteRenderer[2], _rain = new SpriteRenderer[2];
        readonly List<SpriteRenderer> _splash = new List<SpriteRenderer>();
        Texture2D _lightTex;
        Color32[] _px;
        int _tw, _th;
        float _nextBolt = -1f, _boltAt = -10f, _thunderAt = -1f;
        static Sprite _white, _fogSprite, _rainSprite, _dropSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCaches() { _white = _fogSprite = _rainSprite = _dropSprite = null; }

        public AtmosphereView(Transform root)
        {
            _root = root;
            _wash = Make("Wash", White(), WorldArt.SortTop - 30);
            _flash = Make("Lightning", White(), WorldArt.SortTop - 5);
            for (int i = 0; i < 2; i++)
            {
                _fog[i] = Make("Fog" + i, FogSprite(), WorldArt.SortTop - 25); _fog[i].drawMode = SpriteDrawMode.Tiled;
                _rain[i] = Make("Rain" + i, RainSprite(), WorldArt.SortTop - 20); _rain[i].drawMode = SpriteDrawMode.Tiled;
            }
            for (int i = 0; i < 40; i++) { var s = Make("Splash", DropSprite(), WorldArt.SortTop - 21); s.gameObject.SetActive(false); _splash.Add(s); }
            _light = Make("Light", null, WorldArt.SortTop - 10);
        }

        SpriteRenderer Make(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = order;
            return sr;
        }

        bool _backdrop;

        public void Tick(Player p, AtmosphereLook l)
        {
            var cam = Camera.main;
            if (cam == null) return;
            _backdrop = p == null;   // Batch 123: the main menu's view - no player, so nothing is "out of sight"; the night still falls
            float vh = cam.orthographicSize * 2f, vw = vh * cam.aspect;
            var c = cam.transform.position; c.z = 0f;
            float t = Time.time;

            // wash + lightning
            _wash.transform.position = c; _wash.transform.localScale = new Vector3(vw + 2f, vh + 2f, 1f);
            _wash.color = new Color(l.Wash.r, l.Wash.g, l.Wash.b, Mathf.Clamp01(l.WashA * (ColorGrade.Active ? 0.5f : 1f)));
            if (l.Lightning)
            {
                if (_nextBolt < 0f) _nextBolt = t + Random.Range(6f, 16f);
                if (t >= _nextBolt) { _boltAt = t; _nextBolt = t + Random.Range(7f, 20f); _thunderAt = t + Random.Range(0.4f, 1.8f); }
                if (_thunderAt > 0f && t >= _thunderAt) { Sfx.Play("thunder", null, 0.9f, 0.1f); _thunderAt = -1f; }
            }
            else _nextBolt = -1f;
            float bolt = t - _boltAt < 0.35f ? (1f - (t - _boltAt) / 0.35f) * (((int)((t - _boltAt) * 20f) % 2 == 0) ? 0.55f : 0.3f) : 0f;
            _flash.gameObject.SetActive(bolt > 0f);
            if (bolt > 0f) { _flash.transform.position = c; _flash.transform.localScale = _wash.transform.localScale; _flash.color = new Color(0.85f, 0.88f, 1f, bolt); }

            // fog: two layers drifting slowly at different speeds
            for (int i = 0; i < 2; i++)
            {
                var f = _fog[i];
                f.gameObject.SetActive(l.Fog > 0.01f);
                if (l.Fog <= 0.01f) continue;
                float period = 8f;                                  // the fog tile is 8 world units
                float ox = Mathf.Repeat(t * (0.12f + i * 0.07f), period), oy = Mathf.Repeat(t * (0.03f + i * 0.02f), period);
                f.size = new Vector2(vw + period * 2f, vh + period * 2f);
                f.transform.position = new Vector3(Mathf.Floor(c.x / period) * period + ox, Mathf.Floor(c.y / period) * period - oy, 0f);
                f.color = new Color(0.72f, 0.74f, 0.7f, l.Fog * (i == 0 ? 0.42f : 0.28f));
            }

            // rain
            for (int i = 0; i < 2; i++)
            {
                var r = _rain[i];
                r.gameObject.SetActive(l.Rain > 0.01f);
                if (l.Rain <= 0.01f) continue;
                float period = 4f, speed = i == 0 ? 14f : 9f;
                float fall = Mathf.Repeat(t * speed, period), drift = Mathf.Repeat(t * speed * 0.25f, period);
                r.size = new Vector2(vw + period * 2f, vh + period * 2f);
                r.transform.position = new Vector3(Mathf.Floor(c.x / period) * period - drift, Mathf.Floor(c.y / period) * period - fall, 0f);
                r.color = new Color(0.75f, 0.8f, 0.9f, (i == 0 ? 0.34f : 0.2f) * Mathf.Clamp01(l.Rain * 1.4f));
            }
            int live = Mathf.RoundToInt(_splash.Count * l.Rain);
            for (int i = 0; i < _splash.Count; i++)
            {
                var s = _splash[i];
                bool on = i < live;
                if (s.gameObject.activeSelf != on) s.gameObject.SetActive(on);
                if (!on) continue;
                float phase = Mathf.Repeat(t * 3f + i * 0.37f, 1f);
                if (phase < 0.08f)                                   // a new drop lands somewhere in view
                    s.transform.position = new Vector3(c.x + Random.Range(-vw / 2, vw / 2), c.y + Random.Range(-vh / 2, vh / 2), 0f);
                s.color = new Color(0.8f, 0.85f, 0.95f, (1f - phase) * 0.6f);
                s.transform.localScale = Vector3.one * (0.6f + phase);
            }

            DrawLight(c, vw, vh, l);
        }

        /// <summary>Darkness outside your light and outside your line of sight, two texels per tile - or, with Lighting v2 and a lamp in
        /// view, eight, with the lamps' own light and shadows merged in (DrawLightFine).</summary>
        void DrawLight(Vector3 c, float vw, float vh, AtmosphereLook l)
        {
            if (Lighting.Enabled && !Lighting.Failed && (l.Night > 0.02f || _fineHold > 0))
            {
                try { if (DrawLightFine(c, vw, vh, l)) return; }
                catch (System.Exception e) { Lighting.Fail(e); }
            }
            int tw = Mathf.CeilToInt(vw) * TexelsPerTile + 6, th = Mathf.CeilToInt(vh) * TexelsPerTile + 6;
            if (_lightTex == null || tw != _tw || th != _th || _ppu != TexelsPerTile)
            {
                _tw = tw; _th = th; _ppu = TexelsPerTile;
                _lightTex = new Texture2D(tw, th, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                _px = new Color32[tw * th];
                _light.sprite = Sprite.Create(_lightTex, new Rect(0, 0, tw, th), new Vector2(0f, 1f), TexelsPerTile);
            }
            // top-left of the texture snapped to the texel grid, so texels sit still as the camera moves
            float left = Mathf.Floor((c.x - vw / 2f) * TexelsPerTile) / TexelsPerTile - 1.5f;
            float top = Mathf.Ceil((c.y + vh / 2f) * TexelsPerTile) / TexelsPerTile + 1.5f;
            _light.transform.position = new Vector3(left, top, 0f);
            // in daytime fog what you can't see fades into grey mist, not into darkness
            float mist = Mathf.Clamp01(l.Fog * (1f - l.Night * 2f));
            var night = new Color32((byte)(5 + 145 * mist), (byte)(7 + 149 * mist), (byte)(13 + 137 * mist), 255);
            for (int j = 0; j < th; j++)
                for (int i = 0; i < tw; i++)
                {
                    // texel centre in world, then in grid cells (y down). Batch 67: j counts from the top but texture rows count
                    // from the bottom - it was written upside down, so the flashlight and the seen area were mirrored top to bottom
                    // (Yan: "the flashlight seems to be inverted")
                    float wx = left + (i + 0.5f) / TexelsPerTile, wy = top - (j + 0.5f) / TexelsPerTile;
                    float gx = wx - 0.5f, gy = -wy - 0.5f;
                    int cx = Mathf.FloorToInt(wx), cy = Mathf.FloorToInt(-wy);
                    bool sight = _backdrop || Vision.InSight(cx, cy) || RoofView.Covered(cx, cy) || RoofView.Covered(cx, cy + 1);   // a roof is seen from outside
                    float lit = _backdrop ? LightMap.At(Mathf.RoundToInt(gx), Mathf.RoundToInt(gy)) : Mathf.Max(sight ? Vision.Light(gx, gy) : 0f, Vision.GlowAt(gx, gy));   // a glow (the eel) shows even where your line of sight does not reach
                    float unseen = _backdrop ? 0f : 0.6f * (1f - lit);                  // day: unseen is dimmed
                    float dark = l.Night * (1f - lit);                                  // night: unlit is black
                    float a = 1f - (1f - unseen) * (1f - dark);
                    var col = night;
                    // Batch 92: a lamp's pool is tinted (warm work light, red emergency light) where the dark would be
                    float lamp = sight && l.Night > 0.3f ? LightMap.At(Mathf.RoundToInt(gx), Mathf.RoundToInt(gy)) : 0f;
                    if (lamp > 0.02f)
                    {
                        var t = LightMap.Tint(Mathf.RoundToInt(gx), Mathf.RoundToInt(gy));
                        float ta = lamp * (1f - a) * (t.r > t.g * 2 ? 0.30f : 0.14f), oa = 1f - (1f - a) * (1f - ta);   // tint over the dark
                        if (oa > 0.001f) col = new Color32((byte)((col.r * a * (1f - ta) + t.r * ta) / oa), (byte)((col.g * a * (1f - ta) + t.g * ta) / oa), (byte)((col.b * a * (1f - ta) + t.b * ta) / oa), 255);
                        a = oa;
                    }
                    _px[(th - 1 - j) * tw + i] = new Color32(col.r, col.g, col.b, (byte)(Mathf.Clamp01(a) * 255f));
                }
            _lightTex.SetPixels32(_px);
            _lightTex.Apply();
        }

        // ------------------------------------------------------------------ Lighting v2
        const int FineTexels = 8, Coarse = TexelsPerTile, Ratio = FineTexels / Coarse;   // 8 texels a tile for the lamp field, the vision work stays at 2
        int _ppu = TexelsPerTile, _fineHold;
        float[] _cLit, _cSight; Color32[] _fpx;

        /// <summary>The night overlay with Lighting v2: your own light and line of sight as before (at two texels a tile), plus the placed lights' field
        /// from WorldArt's bake (eight a tile): where a lamp reaches the dark lifts, in its shadows it stays, round a flame it warms.
        /// False (the caller draws the old overlay) when no placed light reaches the view and it's day.</summary>
        bool DrawLightFine(Vector3 c, float vw, float vh, AtmosphereLook l)
        {
            int tw = Mathf.CeilToInt(vw) * Coarse + 6, th = Mathf.CeilToInt(vh) * Coarse + 6;
            float left = Mathf.Floor((c.x - vw / 2f) * Coarse) / Coarse - 1.5f, top = Mathf.Ceil((c.y + vh / 2f) * Coarse) / Coarse + 1.5f;
            int gx0 = Mathf.RoundToInt(left * WorldArt.Px), gy0 = Mathf.RoundToInt(-top * WorldArt.Px), rw = tw * (WorldArt.Px / Coarse), rh = th * (WorldArt.Px / Coarse);
            bool lamps = Lighting.Field(gx0, gy0, rw, rh, WorldArt.Px / FineTexels, Time.time, out var lr, out var lg, out var lb);
            if (!lamps && l.Night <= 0.02f) { _fineHold = 0; return false; }      // day and no lamp in view: the cheap overlay
            _fineHold = lamps ? 60 : _fineHold - 1;                              // stay fine a second after the last lamp leaves (no flicker between the two)
            int fw = tw * Ratio, fh = th * Ratio;
            if (_lightTex == null || fw != _tw || fh != _th || _ppu != FineTexels)
            {
                _tw = fw; _th = fh; _ppu = FineTexels;
                _lightTex = new Texture2D(fw, fh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                _fpx = new Color32[fw * fh];
                _light.sprite = Sprite.Create(_lightTex, new Rect(0, 0, fw, fh), new Vector2(0f, 1f), FineTexels);
            }
            if (_cLit == null || _cLit.Length < tw * th) { _cLit = new float[tw * th]; _cSight = new float[tw * th]; }
            _light.transform.position = new Vector3(left, top, 0f);
            float mist = Mathf.Clamp01(l.Fog * (1f - l.Night * 2f));
            var night = new Color32((byte)(5 + 145 * mist), (byte)(7 + 149 * mist), (byte)(13 + 137 * mist), 255);
            // 1. the coarse part, as before: line of sight and your own light (the placed lamps are the field's now)
            for (int j = 0; j < th; j++)
                for (int i = 0; i < tw; i++)
                {
                    float wx = left + (i + 0.5f) / Coarse, wy = top - (j + 0.5f) / Coarse, gx = wx - 0.5f, gy = -wy - 0.5f;
                    int cx = Mathf.FloorToInt(wx), cy = Mathf.FloorToInt(-wy);
                    bool sight = _backdrop || Vision.InSight(cx, cy) || RoofView.Covered(cx, cy) || RoofView.Covered(cx, cy + 1);
                    _cSight[j * tw + i] = sight ? 1f : 0f;
                    _cLit[j * tw + i] = _backdrop ? 0f : Mathf.Max(sight ? Vision.Light(gx, gy, false) : 0f, Vision.GlowAt(gx, gy));   // a glow (the eel) shows even where your line of sight does not reach
                }
            // 2. the fine part: each texel looks its coarse neighbours up (bilinear) and adds the lamp field
            for (int j = 0; j < fh; j++)
            {
                float cj = (j + 0.5f) / Ratio - 0.5f; int j0 = Mathf.Clamp(Mathf.FloorToInt(cj), 0, th - 1), j1 = Mathf.Min(j0 + 1, th - 1); float fy = Mathf.Clamp01(cj - j0);
                for (int i = 0; i < fw; i++)
                {
                    float ci = (i + 0.5f) / Ratio - 0.5f; int i0 = Mathf.Clamp(Mathf.FloorToInt(ci), 0, tw - 1), i1 = Mathf.Min(i0 + 1, tw - 1); float fx = Mathf.Clamp01(ci - i0);
                    int a = j0 * tw + i0, b = j0 * tw + i1, d = j1 * tw + i0, e = j1 * tw + i1;
                    float sight = (_cSight[a] * (1 - fx) + _cSight[b] * fx) * (1 - fy) + (_cSight[d] * (1 - fx) + _cSight[e] * fx) * fy;
                    float litV = (_cLit[a] * (1 - fx) + _cLit[b] * fx) * (1 - fy) + (_cLit[d] * (1 - fx) + _cLit[e] * fx) * fy;
                    int k = j * fw + i;
                    _fpx[(fh - 1 - j) * fw + i] = FineTexel(sight, litV, lamps, lamps ? lr[k] : 0f, lamps ? lg[k] : 0f, lamps ? lb[k] : 0f, _backdrop, l.Night, night);
                }
            }
            _lightTex.SetPixels32(_fpx);
            _lightTex.Apply();
            return true;
        }

        static float LightingShoulder(float v) => WorldArt.ShoulderOf(v);

        /// <summary>One texel of the Lighting v2 night overlay (alpha-blended over the picture: out = picture * (1 - alpha) + colour * alpha). `sight` 0..1 is
        /// whether it's in your line of sight, `litV` your own light there, (lr, lg, lb) the placed lights' summed light (WorldArt.AddLight); `night` the
        /// hour's darkness and `nightCol` the colour of the dark. Shared with the offline render (Tools/render, LIGHTS=.. GAME=1) so what it shows is what
        /// the game draws.</summary>
        public static Color32 FineTexel(float sight, float litV, bool lamps, float rr, float gg, float bb, bool backdrop, float nightAmount, Color32 nightCol)
        {
            float lampE = lamps ? Mathf.Clamp01(0.30f * LightingShoulder(rr) + 0.59f * LightingShoulder(gg) + 0.11f * LightingShoulder(bb)) * (backdrop ? 1f : sight) : 0f;
            float lit = Mathf.Max(litV, lampE);
            float unseen = backdrop ? 0f : 0.6f * (1f - lit);
            float dark = nightAmount * (1f - lit);
            float al = 1f - (1f - unseen) * (1f - dark);
            var col = nightCol;
            if (lampE > 0.02f && nightAmount > 0.3f)                          // the lamp's pool is tinted by its light (warm work light, red emergency light)
            {
                float m = Mathf.Max(rr, Mathf.Max(gg, bb)) + 0.0001f; var tc = new Color32((byte)(255 * rr / m), (byte)(255 * gg / m), (byte)(255 * bb / m), 255);
                float ta = lampE * (1f - al) * (tc.r > tc.g * 2 ? 0.30f : 0.20f), oa = 1f - (1f - al) * (1f - ta);
                if (oa > 0.001f) col = new Color32((byte)((col.r * al * (1f - ta) + tc.r * ta) / oa), (byte)((col.g * al * (1f - ta) + tc.g * ta) / oa), (byte)((col.b * al * (1f - ta) + tc.b * ta) / oa), 255);
                al = oa;
            }
            return new Color32(col.r, col.g, col.b, (byte)(Mathf.Clamp01(al) * 255f));
        }

        // ------------------------------------------------------------------ generated textures
        static Sprite White()
        {
            if (_white != null) return _white;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixels32(new[] { new Color32(255, 255, 255, 255) }); tex.Apply();
            return _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        /// <summary>Soft tileable fog: 128 px = 8 world units, value noise folded to wrap.</summary>
        static Sprite FogSprite()
        {
            if (_fogSprite != null) return _fogSprite;
            const int N = 128; var px = new Color32[N * N]; var rnd = new System.Random(5);
            var g = new float[8, 8]; for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) g[y, x] = (float)rnd.NextDouble();
            var g2 = new float[16, 16]; for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) g2[y, x] = (float)rnd.NextDouble();
            float Sample(float[,] a, int n, float x, float y)
            {
                int ix = (int)x, iy = (int)y; float tx = x - ix, ty = y - iy;
                tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                float v00 = a[iy % n, ix % n], v10 = a[iy % n, (ix + 1) % n], v01 = a[(iy + 1) % n, ix % n], v11 = a[(iy + 1) % n, (ix + 1) % n];
                return Mathf.Lerp(Mathf.Lerp(v00, v10, tx), Mathf.Lerp(v01, v11, tx), ty);
            }
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float v = Sample(g, 8, x * 8f / N, y * 8f / N) * 0.7f + Sample(g2, 16, x * 16f / N, y * 16f / N) * 0.3f;
                    v = Mathf.Clamp01((v - 0.35f) / 0.45f);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(v * v * 255));
                }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            tex.SetPixels32(px); tex.Apply();
            return _fogSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Tileable rain streaks: 64 px = 4 world units, slanted 1 px across per 4 down.</summary>
        static Sprite RainSprite()
        {
            if (_rainSprite != null) return _rainSprite;
            const int N = 64; var px = new Color32[N * N]; var rnd = new System.Random(9);
            for (int k = 0; k < 40; k++)   // Batch 34: was 70, too dense in Unity
            {
                int x0 = rnd.Next(N), y0 = rnd.Next(N), len = rnd.Next(4, 9);
                for (int s = 0; s < len; s++)
                {
                    int x = (x0 + s / 4) % N, y = (y0 + s) % N;
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(120 + 100 * s / len));
                }
            }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            tex.SetPixels32(px); tex.Apply();
            return _rainSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        }

        static Sprite DropSprite()
        {
            if (_dropSprite != null) return _dropSprite;
            string[] rows = { ".x.x.", "x...x", ".x.x." };
            var tex = new Texture2D(5, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[15];
            for (int y = 0; y < 3; y++) for (int x = 0; x < 5; x++) px[(2 - y) * 5 + x] = rows[y][x] == 'x' ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            tex.SetPixels32(px); tex.Apply();
            return _dropSprite = Sprite.Create(tex, new Rect(0, 0, 5, 3), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
