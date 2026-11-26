// 2026-11-26 (Yan: "add an easter egg to the lake: if a player gets to the very border and waits 5 seconds, the eel's shadow can be seen moving through the lake"):
// the Fishing Village's lake (south of the jetty) hides the Depths' eel. Stand still on the shore or the end of the pier for 5 seconds and a long dark shape glides
// across the water, 6+ tiles from you, and is gone. Cosmetic and local to the player who waited (no co-op sync: it is only a look); it can happen again after two minutes.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public class LakeEel : MonoBehaviour
    {
        public const int X0 = 146, X1 = 180, Y0 = 89, Y1 = 112;     // the lake south of the village (tile box); only its water counts
        public const float WaitSeconds = 5f, Cooldown = 120f, Speed = 2.4f, Spacing = 0.46f, Fade = 1.6f;
        const int Segments = 26; const float TW = 1f;      // one world unit a tile (TacticalGridView.TileWorldSize)

        public static bool IsWater(int x, int y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1 && TerrainMap.GetTile(x, y).Ground == GroundType.Water && TerrainMap.GetTile(x, y).Type != TileType.Solid;

        /// <summary>Standing on the shore (or a plank) with the lake's water right beside you.</summary>
        public static bool AtEdge(int px, int py)
        {
            if (px < X0 - 1 || px > X1 + 1 || py < Y0 - 1 || py > Y1 + 1 || IsWater(px, py)) return false;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if ((dx != 0 || dy != 0) && IsWater(px + dx, py + dy)) return true;
            return false;
        }

        /// <summary>A straight run across the lake that stays over water, at least 14 tiles long and passes the watcher at 5 tiles or more. False if the lake offers none.</summary>
        public static bool Plan(int px, int py, System.Random rng, out float ax, out float ay, out float bx, out float by)
        {
            var water = new List<Vector2Int>();
            for (int y = Y0; y <= Y1; y++) for (int x = X0; x <= X1; x++) if (IsWater(x, y)) water.Add(new Vector2Int(x, y));
            ax = ay = bx = by = 0f; if (water.Count < 60) return false;
            float best = -1f;
            for (int k = 0; k < 400; k++)
            {
                var p = water[rng.Next(water.Count)]; var q = water[rng.Next(water.Count)];
                float ddx = q.x - p.x, ddy = q.y - p.y, len = Mathf.Sqrt(ddx * ddx + ddy * ddy); if (len < 14f) continue;
                int n = (int)(len * 2f), wet = 0; float near = 99f;
                for (int i = 0; i <= n; i++)
                {
                    float t = (float)i / n; float x = p.x + ddx * t, y = p.y + ddy * t;
                    if (IsWater(Mathf.FloorToInt(x), Mathf.FloorToInt(y))) wet++;
                    near = Mathf.Min(near, Mathf.Sqrt((x - px) * (x - px) + (y - py) * (y - py)));
                }
                if (wet < n * 0.93f || near < 5f) continue;
                float score = len - Mathf.Abs(near - 8f);           // a long run that passes about 8 tiles off
                if (score > best) { best = score; ax = p.x + 0.5f; ay = p.y + 0.5f; bx = q.x + 0.5f; by = q.y + 0.5f; }
            }
            return best >= 0f;
        }

        // ---------------------------------------------------------------- the watcher
        static float _still, _coolUntil = -1f; static (float x, float y) _last = (-99f, -99f);
        static bool _active; static float _t0, _len, _ax, _ay, _dx, _dy;
        public static bool Active => _active;
        public static void Reset() { _still = 0f; _coolUntil = -1f; _active = false; _last = (-99f, -99f); }

        /// <summary>Called every frame for the local player: counts the seconds spent standing still at the water's edge.</summary>
        public static void Watch(Player p, float dt, float now)
        {
            if (p == null || !p.IsAlive() || MacroState.Current.CurrentZoneId != "marshes") { _still = 0f; return; }
            var pos = Ballistics.Pos(p);
            if ((pos.x - _last.x) * (pos.x - _last.x) + (pos.y - _last.y) * (pos.y - _last.y) > 0.0025f) _still = 0f; else _still += dt;
            _last = pos;
            if (_active || _still < WaitSeconds || now < _coolUntil || !AtEdge(p.X, p.Y)) return;
            if (!Plan(p.X, p.Y, new System.Random(p.X * 7919 + p.Y * 104729 + (int)now), out var ax, out var ay, out var bx, out var by)) { _coolUntil = now + 20f; return; }
            _active = true; _t0 = now; _ax = ax; _ay = ay; _len = Mathf.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay)); _dx = (bx - ax) / _len; _dy = (by - ay) / _len; _coolUntil = now + Cooldown; _still = 0f;
        }

        // ---------------------------------------------------------------- the view
        static Sprite _blob;
        static Sprite Blob()
        {
            if (_blob != null) return _blob;
            const int W = 32, H = 16; var tex = new Texture2D(W, H) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                float u = (x - W / 2f + 0.5f) / (W / 2f), v = (y - H / 2f + 0.5f) / (H / 2f), r = Mathf.Sqrt(u * u + v * v);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1.25f - r * 1.25f)));
            }
            tex.Apply(); return _blob = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        }

        readonly SpriteRenderer[] _seg = new SpriteRenderer[Segments];

        void Update()
        {
            if (!Rt.On || Rt.Paused) return;
            Watch(Player.Current, Time.deltaTime, Time.time);
            if (_active) Draw(Time.time - _t0); else Hide();
        }

        void Hide() { for (int i = 0; i < Segments; i++) if (_seg[i] != null) _seg[i].enabled = false; }

        (float x, float y) PathAt(float s, float t)
        {
            float w = 0.7f * Mathf.Sin(s * 0.8f - t * 2.6f);                  // a slow sideways swim along the run
            return (_ax + _dx * s - _dy * w, _ay + _dy * s + _dx * w);
        }

        void Draw(float t)
        {
            float travel = _len + Segments * Spacing, head = t * Speed;
            if (head > travel + 1f) { _active = false; Hide(); return; }
            float fade = Mathf.Clamp01(Mathf.Min(t, (travel / Speed) - t) / Fade);
            for (int i = 0; i < Segments; i++)
            {
                if (_seg[i] == null)
                {
                    var go = new GameObject("LakeEelSeg"); go.transform.SetParent(transform, false);
                    _seg[i] = go.AddComponent<SpriteRenderer>(); _seg[i].sprite = Blob(); _seg[i].sortingOrder = 2;
                }
                float s = head - i * Spacing; var sr = _seg[i];
                if (s < 0f || s > _len) { sr.enabled = false; continue; }
                var pos = PathAt(s, t); var ahead = PathAt(s + 0.2f, t); float dvx = ahead.x - pos.x, dvy = ahead.y - pos.y;
                if (!IsWater(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y))) { sr.enabled = false; continue; }
                float u = (float)i / (Segments - 1);
                float girth = u < 0.1f ? Mathf.Lerp(0.55f, 1f, u / 0.1f) : Mathf.Lerp(1f, 0.15f, (u - 0.1f) / 0.9f);   // a broad head, a long tapering body
                sr.enabled = true; sr.transform.position = new Vector3(pos.x * TW, -pos.y * TW, 0f);      // tile space (y down) -> world
                sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-dvy, dvx) * Mathf.Rad2Deg);
                sr.transform.localScale = new Vector3(0.9f * TW, 0.93f * girth * TW, 1f);
                sr.color = new Color(0.02f, 0.06f, 0.07f, 0.5f * fade * (0.55f + 0.45f * girth));
            }
        }
    }
}
