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
        readonly List<SpriteRenderer> _eelBody = new List<SpriteRenderer>(), _eelShadow = new List<SpriteRenderer>(), _eelZones = new List<SpriteRenderer>();
        SpriteRenderer _eelHead;
        const bool EelTelegraphs = false;                    // Yan, 2026-11-06: "remove the red telegraphs for now, they look pretty bad" - the zones are still computed (EelBoss.Fight.Zones), just not drawn
        static readonly List<EelBoss.Zone> NoZones = new List<EelBoss.Zone>();
        static readonly Sprite[][] EelSlices = new Sprite[4][];
        static readonly Sprite[] EelHeads = new Sprite[4];
        static Sprite _eelRing, _eelRingFill;
        const float EelPpu = 32f; const int EelBodyW = 1120, EelBodyH = 80, EelSliceW = 12, EelHeadW = 117, EelHeadH = 92;

        static Sprite[] EelBodySlices(int stage)
        {
            if (EelSlices[stage] != null) return EelSlices[stage];
            var tex = Resources.Load<Texture2D>("Sprites/Fx/boss_eel_body_s" + stage); if (tex == null) return EelSlices[stage] = new Sprite[EelSump.N];
            tex.filterMode = FilterMode.Point; var l = new Sprite[EelSump.N];
            for (int i = 0; i < EelSump.N; i++) l[i] = Sprite.Create(tex, new Rect(Mathf.Clamp(i * 8 - 2, 0, EelBodyW - EelSliceW), 0, EelSliceW, EelBodyH), new Vector2(0.5f, 0.5f), EelPpu);
            return EelSlices[stage] = l;
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
                HideFrom(_eelBody, 0); HideFrom(_eelShadow, 0); HideFrom(_eelZones, 0); if (_eelHead != null && _eelHead.gameObject.activeSelf) _eelHead.gameObject.SetActive(false); return;
            }
            var s = f.Sump; int stage = Mathf.Clamp(f.Phase, 0, 3);
            var evs = EelSump.Drain();                                                                       // impacts: the game's own strips, a shake, a moment of hit-stop
            if (evs != null) foreach (var e in evs)
                if (e.Kind == "impact")
                {
                    CombatFx.PxAt(e.Water ? "splash" : "blunt_hit", Mathf.FloorToInt(e.X), Mathf.FloorToInt(e.Y));
                    Shake(Mathf.Min(0.35f, 0.14f * e.Power)); Boss.HitStop(0.05f * Mathf.Min(2f, e.Power));
                }
            int baseOrder = WorldArt.SortTop - 3000;
            var sl = EelBodySlices(stage); int n = 0;
            for (int i = 0; i < EelSump.N; i++)
            {
                if (s.Z[i] <= EelSump.Surface) continue;
                int i0 = Mathf.Max(0, i - 1), i1 = Mathf.Min(EelSump.N - 1, i + 1);
                var a = W(s.X[i0], s.Y[i0], s.Z[i0]); var b = W(s.X[i1], s.Y[i1], s.Z[i1]);
                float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg; bool left = b.x < a.x;       // turned along the body; going left it is flipped, not upside down: the back stays on top
                var sr = Pooled(_eelBody, n++, "EelBody", 2); if (sr == null) continue;
                sr.sprite = sl[i]; sr.flipX = left; sr.color = Color.white; sr.sortingOrder = baseOrder + (EelSump.N - i);
                sr.transform.position = W(s.X[i], s.Y[i], s.Z[i]); sr.transform.rotation = Quaternion.Euler(0f, 0f, left ? ang + 180f : ang);
                float step = Mathf.Sqrt((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y)) / Mathf.Max(1, i1 - i0);                           // on screen the samples drift apart where the body climbs: stretch the slice to still touch the next one (4 px overlap = 1.5 x the step)
                sr.transform.localScale = new Vector3(Mathf.Clamp(step * 1.5f / (EelSliceW / EelPpu), 1f, 6f), 1f, 1f);
            }
            HideFrom(_eelBody, n);
            if (_eelHead == null) _eelHead = NewSprite("EelHead", null, Color.white, _root, 2);
            bool headOut = s.Z[0] > EelSump.Surface; _eelHead.gameObject.SetActive(headOut);
            if (headOut)
            {
                _eelHead.sprite = EelHead(stage); _eelHead.flipX = s.X[Mathf.Min(8, EelSump.N - 1)] < s.X[0];      // it looks away from its neck
                _eelHead.sortingOrder = baseOrder + 400; _eelHead.transform.position = W(s.X[0], s.Y[0], s.Z[0]);
                var nk = W(s.X[Mathf.Min(8, EelSump.N - 1)], s.Y[Mathf.Min(8, EelSump.N - 1)], s.Z[Mathf.Min(8, EelSump.N - 1)]) - _eelHead.transform.position;      // where the neck goes: tilt the head about its anchor so it meets it (a straight profile left a gap when the neck climbs)
                float tilt = Mathf.Atan2(nk.y, Mathf.Abs(nk.x)) * Mathf.Rad2Deg; tilt = Mathf.Clamp(_eelHead.flipX ? -tilt : tilt, -55f, 55f);
                _eelHead.transform.rotation = Quaternion.Euler(0f, 0f, tilt);
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
