// Boss 3: the eel, drawn from EelSump's spine every frame (pooled sprites, like the brain's orbs). FIRST PASS, not yet seen in Unity.
// The body is Yan's drawing baked as a STRAIGHT strip (ArtSource/bosses/eel_bake_unity.py: boss_eel_body_s0..3.png, 1120 x 80, one wound stage each, column = spine sample x 8): every spine
// sample gets a thin slice of it (12 px wide, 4 px overlap), put at the sample's point, turned along the spine; the head (profile, never turned) goes on the end. A sample that is under the
// water (Z at or below the surface) is not drawn: that is how it comes up out of a pool and goes down. Height is a lift up the screen (W's `up`). Shadows go under the raised parts.
// Telegraphs (EelBoss.Fight.Zones: rings, the sweep's circle, the arch's and the dash's bands) are drawn in red, white once live. Impacts (EelSump.Drain) play the game's splash / blunt-hit
// strips, a screen shake and a short hit-stop. Damage numbers and bars: HudView.Boss.cs (the eel's bar) and the game log.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public partial class CombatFxView
    {
        readonly List<SpriteRenderer> _eelBolt = new List<SpriteRenderer>(); float _stormUntil;
        readonly List<SpriteRenderer> _eelBody = new List<SpriteRenderer>(), _eelShadow = new List<SpriteRenderer>(), _eelZones = new List<SpriteRenderer>(), _eelFoam = new List<SpriteRenderer>();
        SpriteRenderer _eelHead;
        const float EelHalfThick = 1.1f, EelDipH = 0.9f, EelSolidZ = 0.3f; const int EelShadowLen = 40;   // under EelSolidZ the body is a shadow in the water (the flood surge's swim: z 0.05), and only its first 10 tiles show
        static readonly Color EelShade = new Color(0.02f, 0.08f, 0.09f, 0.55f);                      // the body's half thickness in tiles, for thinning the inside of a tight bend
        const bool EelTelegraphs = false;                    // Yan, 2026-11-06: "remove the red telegraphs for now, they look pretty bad" - the zones are still computed (EelBoss.Fight.Zones), just not drawn
        static readonly List<EelBoss.Zone> NoZones = new List<EelBoss.Zone>();
        static readonly Sprite[][] EelSlices = new Sprite[4][];
        static readonly Sprite[] EelHeads = new Sprite[4];
        static Sprite _eelRing, _eelRingFill;
        const float EelPpu = 32f; const int EelBodyW = 1120, EelBodyH = 80, EelSliceW = 6, EelSub = 2, EelHeadW = 117, EelHeadH = 92;

        int Bolt(int n, Vector3 a, Vector3 b, int baseOrder)                                              // one jagged segment of electricity: a wide cold glow and a white core, laid between two points
        {
            float dx = b.x - a.x, dy = b.y - a.y, len = Mathf.Sqrt(dx * dx + dy * dy); if (len < 0.01f) return n;
            var mid = (a + b) * 0.5f; float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            for (int layer = 0; layer < 2; layer++)
            {
                var sr = Pooled(_eelBolt, n++, "EelBolt", 2); if (sr == null) continue;
                sr.sprite = White(); sr.sortingOrder = baseOrder + 340 + layer; sr.color = layer == 0 ? new Color(0.45f, 0.85f, 1f, 0.45f) : new Color(0.95f, 1f, 1f, 0.95f);
                sr.transform.position = mid; sr.transform.rotation = Quaternion.Euler(0f, 0f, ang); sr.transform.localScale = new Vector3(len + 0.05f, layer == 0 ? 0.2f : 0.07f, 1f);
            }
            return n;
        }
        // The electricity belongs to the body: arcs crawl ALONG it, following its outline (re-rolled 18 times a second, so it flickers), and others jump from it to the water right beneath;
        // the body itself flickers cold where they pass (DrawEel). `charge` 0..1: the storm's charge, the scream, the burst.
        void DrawBolts(EelSump.State s, float charge, int baseOrder)
        {
            int n = 0;
            if (charge > 0.01f)
            {
                var rng = new System.Random(Mathf.FloorToInt(Time.time * 18f) * 7919 + 13); int crawl = 2 + (int)(9f * charge);
                for (int a = 0; a < crawl; a++)
                {
                    int i0 = rng.Next(0, EelSump.N - 24), len = 8 + rng.Next(0, 16); Vector3 prev = default; bool have = false;
                    for (int i = i0; i < i0 + len; i += 2)
                    {
                        if (s.Z[i] <= 0.3f) { have = false; continue; }
                        var p = W(s.X[i], s.Y[i], s.Z[i]) + new Vector3(((float)rng.NextDouble() - 0.5f) * 0.7f, ((float)rng.NextDouble() - 0.5f) * 0.7f, 0f);
                        if (have) n = Bolt(n, prev, p, baseOrder); prev = p; have = true;
                    }
                }
                int drops = 1 + (int)(6f * charge);
                for (int d2 = 0; d2 < drops; d2++)
                {
                    int i = rng.Next(0, EelSump.N / 2); if (s.Z[i] <= 0.6f) continue;
                    var top = W(s.X[i], s.Y[i], s.Z[i]); var bot = W(s.X[i] + ((float)rng.NextDouble() - 0.5f) * 1.6f, s.Y[i] + ((float)rng.NextDouble() - 0.5f) * 1.6f, 0f); Vector3 prev = top;
                    for (int k3 = 1; k3 <= 4; k3++) { var p = Vector3.Lerp(top, bot, k3 / 4f) + (k3 < 4 ? new Vector3(((float)rng.NextDouble() - 0.5f) * 0.6f, ((float)rng.NextDouble() - 0.5f) * 0.3f, 0f) : Vector3.zero); n = Bolt(n, prev, p, baseOrder); prev = p; }
                }
            }
            HideFrom(_eelBolt, n);
        }
        // ---- the flood is WATER: the flooded tiles get a teal, shimmering water surface (runs of tiles, a sprite per run) with ripples breaking on it - no outlines, no black holes
        readonly List<SpriteRenderer> _eelWater = new List<SpriteRenderer>(); readonly List<(int y, int x0, int x1)> _eelRuns = new List<(int, int, int)>(); readonly List<(int x, int y)> _eelWet = new List<(int, int)>(); int _eelRunsSig = -1; float _ripT;
        void DrawFlood(EelSump.State s)
        {
            int cnt = s.Flip.Count, n = 0;
            if (cnt != _eelRunsSig)
            {
                _eelRunsSig = cnt; _eelRuns.Clear(); _eelWet.Clear();
                for (int y = 0; y < TerrainMap.Height && cnt > 0; y++)
                {
                    int x0 = -1;
                    for (int x = 0; x <= TerrainMap.Width; x++)
                    {
                        bool wet = x < TerrainMap.Width && s.Flip.Contains((x, y));
                        if (wet) { if (x0 < 0) x0 = x; _eelWet.Add((x, y)); } else if (x0 >= 0) { _eelRuns.Add((y, x0, x - 1)); x0 = -1; }
                    }
                }
            }
            for (int r = 0; r < _eelRuns.Count; r++)
            {
                var run = _eelRuns[r]; var sr = Pooled(_eelWater, n++, "EelWater", 4); if (sr == null) continue;
                float sh = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.9f + run.y * 0.8f + run.x0 * 0.13f);
                sr.sprite = White(); sr.color = new Color(0.03f, 0.10f + 0.04f * sh, 0.20f + 0.05f * sh, 0.22f + 0.06f * sh);                  // the room's own water, a little deeper and bluer: a translucent tint, not a different colour
                sr.transform.position = new Vector3((run.x0 + run.x1 + 1) * 0.5f, -(run.y + 0.5f), 0f); sr.transform.localScale = new Vector3(run.x1 - run.x0 + 1, 1f, 1f);
            }
            HideFrom(_eelWater, n);
            if (_eelWet.Count > 0 && Time.time >= _ripT)                                                                // ripples breaking on the surface
            {
                _ripT = Time.time + 0.09f;
                for (int q = 0; q < 2; q++) { var tile = _eelWet[Random.Range(0, _eelWet.Count)]; CombatFx.PxAt("ripples", tile.x, tile.y); }
            }
        }
        void BodySparks(EelSump.State s, int count)                                                      // small lightning arcs at random points along the part of the body that is out of the water
        {
            for (int k2 = 0; k2 < count; k2++)
            {
                int i = Random.Range(0, EelSump.N); if (s.Z[i] <= 0.3f) { i = Random.Range(0, EelSump.N / 3); if (s.Z[i] <= 0.3f) continue; }
                PxFx("thunderclap", W(s.X[i], s.Y[i], s.Z[i]) + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.4f, 0.4f), 0f), 0f, 0f, 64f);
            }
        }
        public static Vector3? EelStand, EelHandUp, EelHandLo; public static float EelFace = 1f;         // the jaw-hold: where he stands (in front of the jaws), where his two gloves go (the tips of the upper and lower jaw), which way he faces
        // the jaw tips for each jaw frame, in tiles from the anchor for the head as drawn (looking left), x right / y up: ArtSource/bosses/eel_jaw_tips.py
        static readonly float[] JawUpX = { -3.199f, -3.199f, -3.199f, -3.199f, -3.199f, -3.199f, -3.199f }, JawUpY = { 0.188f, 0.188f, 0.188f, 0.188f, 0.188f, 0.188f, 0.188f };
        static readonly float[] JawLoX = { -2.944f, -2.944f, -2.939f, -2.927f, -2.910f, -2.887f, -2.782f }, JawLoY = { -0.120f, -0.234f, -0.348f, -0.462f, -0.575f, -0.687f, -1.013f };
        readonly List<SpriteRenderer> _eelStars = new List<SpriteRenderer>(); static Sprite _eelStar;
        static Sprite EelStarSprite()                                                                     // a 7x7 pixel star: what circles its head while it is stunned
        {
            if (_eelStar != null) return _eelStar;
            string[] rows = { "...Y...", "...Y...", "..YYY..", "YYYWYYY", "..YYY..", "...Y...", "...Y..." }; var tex = new Texture2D(7, 7) { filterMode = FilterMode.Point };
            for (int y = 0; y < 7; y++) for (int x = 0; x < 7; x++) { char ch = rows[y][x]; tex.SetPixel(x, 6 - y, ch == 'W' ? new Color(1f, 1f, 0.9f, 1f) : ch == 'Y' ? new Color(1f, 0.85f, 0.25f, 1f) : new Color(0f, 0f, 0f, 0f)); }
            tex.Apply(); return _eelStar = Sprite.Create(tex, new Rect(0, 0, 7, 7), new Vector2(0.5f, 0.5f), 32f);
        }                                                                  // where the jaws are, for the player held in them (TacticalGridView)

        static Sprite[] EelBodySlices(int stage)                       // EelSub thin slices per spine sample (each sample is 8 columns of the strip): thinner slices make a bend a smoother polygon
        {
            if (EelSlices[stage] != null) return EelSlices[stage];
            int cnt = EelSump.N * EelSub;
            var tex = Resources.Load<Texture2D>("Sprites/Fx/boss_eel_body_s" + stage); if (tex == null) return EelSlices[stage] = new Sprite[cnt];
            tex.filterMode = FilterMode.Point; var l = new Sprite[cnt];
            for (int q = 0; q < cnt; q++) l[q] = Sprite.Create(tex, new Rect(Mathf.Clamp(q * (8 / EelSub) - 1, 0, EelBodyW - EelSliceW), 0, EelSliceW, EelBodyH), new Vector2(0.5f, 0.5f), EelPpu);
            return EelSlices[stage] = l;
        }
        static readonly Sprite[][] EelJaws = new Sprite[4][];
        const int JawFrames = 7, JawW = 117, JawH = 101; const float JawAnchorX = 116.38f, JawAnchorY = 52.02f;     // ArtSource/bosses/eel_bake_jaw.py: openness 0 .2 .4 .6 .8 1 (as drawn) 1.4 (forced wide), cut from his drawing
        static Sprite EelJaw(int stage, float open)
        {
            if (EelJaws[stage] == null)
            {
                var tex = Resources.Load<Texture2D>("Sprites/Fx/boss_eel_jaw_s" + stage); var l = new Sprite[JawFrames];
                if (tex != null) { tex.filterMode = FilterMode.Point; for (int i = 0; i < JawFrames; i++) l[i] = Sprite.Create(tex, new Rect(i * JawW, 0, JawW, JawH), new Vector2(JawAnchorX / JawW, 1f - JawAnchorY / JawH), EelPpu); }
                EelJaws[stage] = l;
            }
            int k = open > 1.2f ? JawFrames - 1 : Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(open) * 5f), 0, 5); return EelJaws[stage][k];
        }
        static Sprite EelHead(int stage)
        {
            if (EelHeads[stage] != null) return EelHeads[stage];
            var tex = Resources.Load<Texture2D>("Sprites/Fx/boss_eel_head_s" + stage); if (tex == null) return null;
            tex.filterMode = FilterMode.Point; return EelHeads[stage] = Sprite.Create(tex, new Rect(0, 0, EelHeadW, EelHeadH), new Vector2(116f / EelHeadW, 1f - 52f / EelHeadH), EelPpu);   // the anchor: where the body grows from
        }
        static Sprite EelCircle(bool fill)
        {
            if (fill ? _eelRingFill != null : _eelRing != null) return fill ? _eelRingFill : _eelRing;
            const int N = 128; var tex = new Texture2D(N, N) { filterMode = FilterMode.Point };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float r = Mathf.Sqrt((x - N / 2f + 0.5f) * (x - N / 2f + 0.5f) + (y - N / 2f + 0.5f) * (y - N / 2f + 0.5f)); tex.SetPixel(x, y, new Color(1f, 1f, 1f, r > N / 2f - 3f && r <= N / 2f ? 1f : fill && r < N / 2f - 3f ? 0.22f : 0f)); }
            tex.Apply(); var sp = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N / 2f);   // two world units across: scale = the radius in tiles
            if (fill) _eelRingFill = sp; else _eelRing = sp; return sp;
        }

        void DrawEel()
        {
            var f = EelBoss.Current;
            if (f == null || f.Gone || f.Sump == null)
            {
                HideFrom(_eelBody, 0); HideFrom(_eelWater, 0); _eelRunsSig = -1; HideFrom(_eelBolt, 0); EelStand = EelHandUp = EelHandLo = null; HideFrom(_eelStars, 0); HideFrom(_eelFoam, 0); HideFrom(_eelShadow, 0); HideFrom(_eelZones, 0); if (_eelHead != null && _eelHead.gameObject.activeSelf) _eelHead.gameObject.SetActive(false); return;
            }
            var s = f.Sump; int stage = Mathf.Clamp(f.Phase, 0, 3);
            var evs = EelSump.Drain();                                                                       // impacts: the game's own strips, a shake, a moment of hit-stop
            if (evs != null) foreach (var e in evs)
                if (e.Kind == "impact")
                {
                    CombatFx.PxAt(e.Water ? "splash" : "blunt_hit", Mathf.FloorToInt(e.X), Mathf.FloorToInt(e.Y));
                    Shake(Mathf.Min(0.35f, 0.14f * e.Power)); Boss.HitStop(0.05f * Mathf.Min(2f, e.Power));
                    Sfx.Play(e.Water ? "eel_splash" : "eel_slam", new Vector3(e.X, -e.Y, 0f), Mathf.Clamp(0.35f + 0.4f * e.Power, 0.3f, 1.2f), 0.05f, e.Power >= 1.5f ? 0.85f : 1.1f);
                }
                else if (e.Kind == "spark")                                                              // the storm's charge: many small crackles across the body, thicker as it fills
                {
                    Shake(0.03f + 0.06f * e.Power);
                }
                else if (e.Kind == "parry") { CombatFx.PxAt("parry", Mathf.FloorToInt(e.X), Mathf.FloorToInt(e.Y)); Shake(0.3f); }                // the bite is caught in bare hands
                else if (e.Kind == "slam") { CombatFx.PxAt("blunt_hit", Mathf.FloorToInt(e.X), Mathf.FloorToInt(e.Y)); CombatFx.PxAt("splash", Mathf.FloorToInt(e.X), Mathf.FloorToInt(e.Y)); Shake(0.9f); Boss.HitStop(0.22f); }   // the jaws shut and the head hits the ground
                else if (e.Kind == "storm")                                                              // the burst: the whole body lights up with small arcs, and the water all round
                {
                    _stormUntil = Time.time + 0.8f; for (int q = 0; q < 8; q++) { float ang = q / 8f * Mathf.PI * 2f, r = 5f + 4f * (q % 2); CombatFx.PxAt("thunderclap", Mathf.FloorToInt(e.X + Mathf.Cos(ang) * r), Mathf.FloorToInt(e.Y + Mathf.Sin(ang) * r * 0.7f)); }
                    Shake(0.5f); Boss.HitStop(0.12f);
                }
                else if (e.Kind == "glint") { for (int g2 = 0; g2 < 3; g2++) PxFx("parry", W(s.X[0] + Random.Range(-0.5f, 0.5f), s.Y[0], s.Z[0] + 0.4f + Random.Range(-0.3f, 0.5f)), 0f, 0f, 20f); Shake(0.2f); Boss.HitStop(0.06f); }                  // the white glint: parry now
            int baseOrder = WorldArt.SortTop - 3000;
            var sl = EelBodySlices(stage); int n = 0, NN = EelSump.N; float chg = Mathf.Max(EelSump.ChargeOf(f), Time.time < _stormUntil ? 1f : 0f); float strainShake = EelSump.GrappleSpot(out _, out _) ? 0.05f + 0.07f * EelSump.TwitchOf(f) : 0f;
            var sp = new Vector3[NN]; var ag = new float[NN]; var rd = new float[NN];                           // each sample's screen point, the way the body runs there (degrees) and the bend's radius in tiles
            for (int i = 0; i < NN; i++) sp[i] = W(s.X[i], s.Y[i], s.Z[i]);
            for (int i = 0; i < NN; i++)
            {
                int i0 = Mathf.Max(0, i - 1), i1 = Mathf.Min(NN - 1, i + 1);
                ag[i] = Mathf.Atan2(sp[i1].y - sp[i0].y, sp[i1].x - sp[i0].x) * Mathf.Rad2Deg;
            }
            for (int i = 0; i < NN; i++)
            {
                int i0 = Mathf.Max(0, i - 1), i1 = Mathf.Min(NN - 1, i + 1);
                float da = Mathf.DeltaAngle(ag[i0], ag[i1]) * Mathf.Deg2Rad, len = Mathf.Sqrt((sp[i1].x - sp[i0].x) * (sp[i1].x - sp[i0].x) + (sp[i1].y - sp[i0].y) * (sp[i1].y - sp[i0].y));
                rd[i] = Mathf.Abs(da) < 0.02f ? 99f : len / Mathf.Abs(da);                                       // a hairpin has a radius smaller than the body is thick
            }
            for (int i = 0; i + 1 < NN; i++)
            {
                if (s.Z[i] <= EelSump.Surface || s.Z[i + 1] <= EelSump.Surface) continue;
                bool left = sp[Mathf.Min(NN - 1, i + 1)].x < sp[Mathf.Max(0, i - 1)].x;                          // going left it is flipped, not upside down: the back stays on top
                float seg = Mathf.Sqrt((sp[i + 1].x - sp[i].x) * (sp[i + 1].x - sp[i].x) + (sp[i + 1].y - sp[i].y) * (sp[i + 1].y - sp[i].y));
                for (int kq = 0; kq < EelSub; kq++)
                {
                    float u = kq / (float)EelSub; var pos = Vector3.Lerp(sp[i], sp[i + 1], u);
                    float ang = ag[i] + Mathf.DeltaAngle(ag[i], ag[i + 1]) * u, r = Mathf.Lerp(rd[i], rd[i + 1], u), rot = left ? ang + 180f : ang;
                    float sy = 1f, shift = 0f;                                                                       // a bend tighter than the body is thick: thin the INSIDE of it to the radius (the painter does the same), keeping the outside where it was
                    if (r < EelHalfThick)
                    {
                        float da = Mathf.DeltaAngle(ag[Mathf.Max(0, i - 1)], ag[Mathf.Min(NN - 1, i + 1)]); float turn = da >= 0f ? 1f : -1f;      // + = turning left of the way it runs
                        float ar = ang * Mathf.Deg2Rad, rr = rot * Mathf.Deg2Rad; var centre = new Vector2(-Mathf.Sin(ar) * turn, Mathf.Cos(ar) * turn); var back = new Vector2(-Mathf.Sin(rr), Mathf.Cos(rr));
                        float inner = Mathf.Max(0.25f, 0.85f * r); sy = (EelHalfThick + inner) / (2f * EelHalfThick); float outwardShift = EelHalfThick * (1f - sy);
                        float toward = (centre.x * back.x + centre.y * back.y) > 0f ? -1f : 1f;                 // which way along the sprite's back/belly axis is outward
                        shift = outwardShift * toward;
                        pos += new Vector3(back.x, back.y, 0f) * shift;
                    }
                    float zs = Mathf.Lerp(s.Z[i], s.Z[i + 1], u); bool shade = zs <= EelSolidZ; if (shade && (i >= EelShadowLen || s.Z[0] > EelSolidZ + 0.2f)) continue;     // the shadow shows only while the head is down in the water (the swim)
                    if (!shade) sy *= Mathf.SmoothStep(0.15f, 1f, zs / EelDipH);                                      // where the body goes under the water it narrows to a rounded end instead of ending in a flat cut
                    var sr = Pooled(_eelBody, n++, "EelBody", 2); if (sr == null) continue;
                    sr.sprite = sl[i * EelSub + kq]; sr.flipX = left; sr.color = shade ? EelShade : EelSump.Stunned(f) ? new Color(0.78f, 0.82f, 0.95f, 1f) : chg > 0.01f && ((i * 13 + Mathf.FloorToInt(Time.time * 24f) * 7) % 11) < chg * 5f ? new Color(0.7f, 0.93f, 1f, 1f) : Color.white;       // cold flicker where the electricity passes sr.sortingOrder = baseOrder + (NN - i) * EelSub - kq;
                    if (strainShake > 0f) pos += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * strainShake;                          // the whole body shakes with the strain
                    sr.transform.position = pos; sr.transform.rotation = Quaternion.Euler(0f, 0f, rot);
                    sr.transform.localScale = new Vector3(Mathf.Clamp(seg / EelSub * 1.5f / (EelSliceW / EelPpu), 1f, 6f), sy, 1f);   // on screen the samples drift apart where the body climbs: stretch the slice to still touch the next one (1.5 x the step)
                }
            }
            HideFrom(_eelBody, n); DrawBolts(s, chg, baseOrder); DrawFlood(s);
            int fo = 0;                                                                                       // foam where the body goes under or comes up: the water closes round it
            for (int i = 0; i + 1 < NN; i++)
                if ((s.Z[i] > EelSump.Surface) != (s.Z[i + 1] > EelSump.Surface))
                {
                    int up = s.Z[i] > EelSump.Surface ? i : i + 1; var fr = Pooled(_eelFoam, fo++, "EelFoam", 2); if (fr == null) continue;
                    fr.sprite = EelCircle(false); fr.sortingOrder = baseOrder + 300; fr.color = new Color(0.8f, 0.95f, 1f, 0.4f + 0.2f * Mathf.Sin(Time.time * 6f + i));
                    fr.transform.position = W(s.X[up], s.Y[up]); fr.transform.localScale = new Vector3(1.7f, 0.9f, 1f);
                }
            HideFrom(_eelFoam, fo);
            if (_eelHead == null) _eelHead = NewSprite("EelHead", null, Color.white, _root, 2);
            bool headOut = s.Z[0] > EelSump.Surface; _eelHead.gameObject.SetActive(headOut); if (!headOut) { EelStand = EelHandUp = EelHandLo = null; HideFrom(_eelStars, 0); }
            if (headOut)
            {
                float mouth = EelSump.MouthOpen(f); var jaw = mouth >= 0f ? EelJaw(stage, mouth) : null; _eelHead.sprite = jaw != null ? jaw : EelHead(stage); _eelHead.color = s.Z[0] <= EelSolidZ ? EelShade : Color.white; _eelHead.flipX = s.X[Mathf.Min(8, EelSump.N - 1)] < s.X[0];      // it looks away from its neck
                int gface = EelSump.GrabFace(f); if (gface != 0) _eelHead.flipX = gface > 0;                       // held: it faces HIM, whatever the neck does
                _eelHead.sortingOrder = baseOrder + 400; _eelHead.transform.position = W(s.X[0], s.Y[0], s.Z[0]);
                var nk = W(s.X[Mathf.Min(8, EelSump.N - 1)], s.Y[Mathf.Min(8, EelSump.N - 1)], s.Z[Mathf.Min(8, EelSump.N - 1)]) - _eelHead.transform.position;      // where the neck goes: tilt the head about its anchor so it meets it (a straight profile left a gap when the neck climbs)
                float tilt = Mathf.Atan2(nk.y, Mathf.Abs(nk.x)) * Mathf.Rad2Deg; tilt = Mathf.Clamp(_eelHead.flipX ? -tilt : tilt, -88f, 88f);
                float tw = EelSump.TwitchOf(f); if (tw > 0f) { _eelHead.transform.position += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * 0.14f * tw; tilt += Random.Range(-7f, 7f) * tw; }
                _eelHead.transform.rotation = Quaternion.Euler(0f, 0f, tilt);
                bool stun = EelSump.Stunned(f);                                                                   // stunned: dimmed, with stars circling its head
                if (stun) _eelHead.color = new Color(0.7f, 0.76f, 0.95f, 1f);
                int ns = 0; if (stun) for (int q = 0; q < 4; q++) { var st2 = Pooled(_eelStars, ns++, "EelStar", 2); if (st2 == null) continue; float an = Time.time * 3.2f + q * Mathf.PI * 0.5f; st2.sprite = EelStarSprite(); st2.sortingOrder = baseOrder + 420; st2.color = Color.white; st2.transform.position = _eelHead.transform.position + new Vector3(Mathf.Cos(an) * 0.95f, 1.25f + Mathf.Sin(an) * 0.28f, 0f); st2.transform.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(an * 2f)); }
                HideFrom(_eelStars, ns);
                if (EelSump.GrappleSpot(out var gsx, out var gsy))                                                // the jaw-hold: the two gloves on the jaw tips, him standing in front of the face
                {
                    int jk = mouth >= 0f ? (mouth > 1.2f ? JawFrames - 1 : Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(mouth) * 5f), 0, 5)) : 5;
                    float mir = _eelHead.flipX ? -1f : 1f, rr = tilt * Mathf.Deg2Rad, cs = Mathf.Cos(rr), sn2 = Mathf.Sin(rr); var hp0 = _eelHead.transform.position;
                    Vector3 Tip(float ox, float oy) { float x = mir * ox, y = oy; return hp0 + new Vector3(x * cs - y * sn2, x * sn2 + y * cs, 0f); }
                    EelHandUp = Tip(JawUpX[jk], JawUpY[jk]); EelHandLo = Tip(JawLoX[jk], JawLoY[jk]);
                    float outward = _eelHead.flipX ? 1f : -1f; EelFace = -outward;                              // he stands out in front of its face, looking at it
                    EelStand = new Vector3((EelHandUp.Value.x + EelHandLo.Value.x) * 0.5f, (EelHandUp.Value.y + EelHandLo.Value.y) * 0.5f, 0f);   // the middle of his hands: where the camera closes in (he is not moved)
                }
                else EelStand = EelHandUp = EelHandLo = null;
            }
            int k = 0;                                                                                       // shadows on the ground under what is raised
            for (int i = 0; i < EelSump.N; i += 2)
            {
                if (s.Z[i] < 0.5f) continue;
                var sh = Pooled(_eelShadow, k++, "EelShadow", 2); if (sh == null) continue;
                sh.sprite = White(); sh.color = new Color(0f, 0f, 0f, 0.22f * Mathf.Min(1f, s.Z[i] / 2.5f)); sh.transform.position = W(s.X[i], s.Y[i], 0f); sh.transform.rotation = Quaternion.identity; sh.transform.localScale = new Vector3(0.95f, 0.5f, 1f);
            }
            HideFrom(_eelShadow, k);
            int z = 0; float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 9f);                                // the telegraphs: red, white once they can hurt
            foreach (var zn in EelTelegraphs ? f.Zones : NoZones)
            {
                var col = zn.Live ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 0.2f, 0.14f, 0.55f + 0.3f * pulse);
                if (zn.Kind == "band" && zn.Path != null)
                {
                    for (int q = 0; q + 3 < zn.Path.Length; q += 2)
                    {
                        float x0 = zn.Path[q], y0 = zn.Path[q + 1], x1 = zn.Path[q + 2], y1 = zn.Path[q + 3], len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)) + 0.1f;
                        var b = Pooled(_eelZones, z++, "EelBand", 3); if (b == null) continue;
                        b.sprite = White(); b.color = new Color(1f, 0.2f, 0.14f, 0.26f); b.transform.position = W((x0 + x1) / 2f, (y0 + y1) / 2f); b.transform.rotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(y1 - y0, x1 - x0) * Mathf.Rad2Deg); b.transform.localScale = new Vector3(len, f.Move == "arch" ? 3f : 4f, 1f);
                    }
                    if (f.Move != "arch") continue;
                }
                var c = Pooled(_eelZones, z++, "EelZone", 3); if (c == null) continue;
                c.sprite = EelCircle(zn.Kind != "sweep"); c.color = col; c.transform.position = W(zn.X, zn.Y); c.transform.rotation = Quaternion.identity; c.transform.localScale = new Vector3(zn.R, zn.R, 1f);
            }
            HideFrom(_eelZones, z);
        }
    }
}
