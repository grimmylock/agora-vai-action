// Real lighting, first prototype (Yan, 2026-11-04: "real lighting in the game with shadows reacting to light sources, similar to how
// octopath traveler looks, instead of flat lighting we have today"). The sun's shadows (CastShadows) already come from the real
// sprite pixels and their heights above the ground; this does the same for point lights:
//   - every standing sprite pixel is a point at (x, foot row, height); from a light at height H the pixel's shadow lands on the
//     ground at  pixel + (pixel - light) * h / (H - h)  - so trees, walls and props throw shadows shaped like themselves, long
//     when the light is low, away from the light
//   - ground pixels are lit by distance and by the angle to the light (a hot spot under a lamp, a soft pool round it), minus the
//     shadow; wall faces by whether the light is on their side; props and trees by a soft front/top term
//   - lights have a colour and a height (a campfire is low and orange, a lamp high and warm, an emergency light red), the
//     campfire flickers, a lamp's head burns white
// Used by the offline render (Tools/render, LIGHTS=x0,y0,x1,y1) to judge the look on a real map. Not wired into the game yet:
// in game the sprites are separate renderers, so this would bake a light texture the way the sun's shadows re-bake.
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static partial class WorldArt
    {
        /// <summary>A light in map pixels (y down): where it stands on the ground, how high its head is, its reach, colour and power.</summary>
        public struct PointLight { public float X, Y, H, R, Power, Flicker, Cr, Cg, Cb; }

        static bool LightKind(string look, out PointLight l)
        {
            l = default;
            switch (look)
            {
                case "lamp": l = new PointLight { H = 56f, R = 10f * Px, Cr = 1f, Cg = 0.78f, Cb = 0.48f, Power = 1.75f }; return true;   // the lamp sprite is 62 px tall, its head ~56 up
                case "campfire": l = new PointLight { H = 10f, R = 8f * Px, Cr = 1f, Cg = 0.46f, Cb = 0.16f, Power = 1.5f, Flicker = 0.16f }; return true;
                case "light": l = new PointLight { H = 26f, R = 6f * Px, Cr = 1f, Cg = 0.93f, Cb = 0.76f, Power = 1.1f }; return true;
                case "redlight": l = new PointLight { H = 22f, R = 5f * Px, Cr = 1f, Cg = 0.10f, Cb = 0.06f, Power = 1.0f }; return true;
            }
            return false;
        }

        /// <summary>The placed lights (lamp posts, campfires, ceiling and emergency lights) in a range of cells.</summary>
        public static List<PointLight> PlacedLights(int tx0, int ty0, int tx1, int ty1)
        {
            var list = new List<PointLight>();
            for (int y = System.Math.Max(0, ty0); y <= System.Math.Min(_h - 1, ty1); y++)
                for (int x = System.Math.Max(0, tx0); x <= System.Math.Min(_w - 1, tx1); x++)
                {
                    if (!LightKind(TerrainMap.GetTile(x, y)?.Look, out var l)) continue;
                    if (StandingAt(x, y, out var st)) { l.X = st.X; l.Y = st.Y; } else { l.X = x * Px + 8; l.Y = y * Px + 13; }
                    list.Add(l);
                }
            var vat = BrainBoss.Current;                                                                    // the brain's vat glows: green light spilling over the lab floor (Yan: "the brain looks good but really out of place")
            if (vat != null && !vat.Gone && vat.Unit != null && vat.Unit.X >= tx0 && vat.Unit.X <= tx1 && vat.Unit.Y >= ty0 && vat.Unit.Y <= ty1)
                list.Add(new PointLight { X = vat.Unit.X * Px + 8, Y = vat.Unit.Y * Px + 13, H = 34f, R = 7f * Px, Cr = 0.42f, Cg = 0.95f, Cb = 0.55f, Power = 1.05f, Flicker = 0.07f });
            return list;
        }


        /// <summary>Up to 1 the light is what it is; past it, lights add up more and more gently (to about 2), so overlapping pools and a
        /// lamp's head glow without clipping the picture to flat orange.</summary>
        static float Shoulder(float v) => v <= 1f ? v : 1f + (v - 1f) / (1f + (v - 1f));

        /// <summary>One light baked against its surroundings (the expensive part, done once and redone only when something near it changes):
        /// a local grid of Mw x Mw map pixels with its top-left at (Ox, Oy), holding how shadowed each ground pixel is and, for every
        /// pixel of a standing sprite, its height above its foot, its foot row and what kind of surface it is.</summary>
        public sealed class LightBake
        {
            public PointLight L; public int Ox, Oy, Mw;
            public float[] Mask;                 // ground pixels: 0 lit .. 1 in the shadow of something
            public short[] Up; public int[] Foot; public byte[] Kind;   // sprite pixels: height, foot row, kind (0 ground, 1 wall face, 2 wall top, 3 prop)
        }

        public static LightBake BakeLight(PointLight lamp)
        {
            int R = Mathf.CeilToInt(lamp.R) + 2, mw = 2 * R + 1, ox = Mathf.RoundToInt(lamp.X) - R, oy = Mathf.RoundToInt(lamp.Y) - R;
            var b = new LightBake { L = lamp, Ox = ox, Oy = oy, Mw = mw, Mask = new float[mw * mw], Up = new short[mw * mw], Foot = new int[mw * mw], Kind = new byte[mw * mw] };
            var casters = new List<Standing>();
            for (int y = System.Math.Max(0, oy / Px - 2); y <= System.Math.Min(_h - 1, (oy + mw) / Px + 5); y++)
                for (int x = System.Math.Max(0, ox / Px - 2); x <= System.Math.Min(_w - 1, (ox + mw) / Px + 2); x++)
                    if (StandingAt(x, y, out var st)) casters.Add(st);
            casters.Sort((a, c) => a.Sort != c.Sort ? a.Sort.CompareTo(c.Sort) : a.X.CompareTo(c.X));
            var m = b.Mask;
            foreach (var st in casters)
            {
                var s = st.Spr; int left = st.Left, top = st.Top; bool wall = s.Name.StartsWith("w_"); int hw = wall ? s.H - Px : int.MaxValue;
                bool near = System.Math.Abs(st.X - lamp.X) <= lamp.R + 40f && System.Math.Abs(st.Y - lamp.Y) <= lamp.R + 40f;
                for (int sy = 0; sy < s.MH; sy++)
                {
                    int gy = top + sy, u = st.Y - gy; if (u < 0) continue;
                    float h = u <= hw ? u : hw, gyp = u <= hw ? st.Y : st.Y - (u - hw);
                    for (int sx = 0; sx < s.MW; sx++)
                    {
                        if (AtM(s, sx, sy).a < 128) continue;
                        int gx = left + sx, lx = gx - ox, ly = gy - oy;
                        if (lx >= 0 && ly >= 0 && lx < mw && ly < mw)            // what this pixel of the picture is
                        {
                            int i = ly * mw + lx;
                            if (u <= hw) { b.Up[i] = (short)u; b.Foot[i] = st.Y; b.Kind[i] = (byte)(wall ? 1 : 3); }
                            else { b.Up[i] = (short)hw; b.Foot[i] = st.Y - (u - hw); b.Kind[i] = 2; }
                        }
                        if (!near || u < 1 || h >= lamp.H - 3f) continue;         // the shadow it throws on the ground (not from above the light)
                        float dx = gx - lamp.X, dy = gyp - lamp.Y;
                        for (int sub = 0; sub < 3; sub++)                          // sub-steps in height: the shadow is a smear, not dots
                        {
                            float hh = System.Math.Max(0f, h - sub * 0.4f), k = System.Math.Min(5f, hh / (lamp.H - hh));
                            int mx = Mathf.RoundToInt(gx + dx * k) - ox, my = Mathf.RoundToInt(gyp + dy * k) - oy;
                            if (mx >= 0 && my >= 0 && mx < mw && my < mw) m[my * mw + mx] = 1f;
                        }
                    }
                }
            }
            Blur3(m, mw, mw); Blur3(m, mw, mw);                                    // soft edges
            return b;
        }

        /// <summary>Adds one baked light into a buffer covering map pixels (gx0, gy0)..(gx0 + rw, gy0 + rh), one sample every `step` pixels
        /// (the buffer is (rw / step) wide). `time` drives the flicker.</summary>
        public static void AddLight(LightBake b, float time, float[] lr, float[] lg, float[] lb, int gx0, int gy0, int rw, int rh, int step = 1)
        {
            var lamp = b.L; int tw = rw / step;
            float tell = EelLamps.Factor(lamp.X / Px, lamp.Y / Px, time);                                       // the eel's tell: the lamps near it flicker, then go dark (EelBoss.UpdateTell)
            float pw = lamp.Power * tell * (1f + lamp.Flicker * (Mathf.Sin(time * 11f) * 0.5f + Mathf.Sin(time * 17.3f + 1f) * 0.5f));
            int x0 = System.Math.Max(gx0, b.Ox), x1 = System.Math.Min(gx0 + rw, b.Ox + b.Mw), y0 = System.Math.Max(gy0, b.Oy), y1 = System.Math.Min(gy0 + rh, b.Oy + b.Mw);
            for (int gy = y0 + (step - (y0 - gy0) % step) % step; gy < y1; gy += step)
                for (int gx = x0 + (step - (x0 - gx0) % step) % step; gx < x1; gx += step)
                {
                    int bi = (gy - b.Oy) * b.Mw + (gx - b.Ox), i = ((gy - gy0) / step) * tw + (gx - gx0) / step;
                    float py = gy, pz = 0f, nx = 0f, ny = 0f, nz = 1f, shadow = 0f; byte kind = b.Kind[bi];
                    switch (kind)
                    {
                        case 0: shadow = Mathf.Clamp01(b.Mask[bi] * 1.6f); break;
                        case 1: py = b.Foot[bi]; pz = b.Up[bi]; nx = 0f; ny = 1f; nz = 0.25f; break;                  // a wall face looks south, a little up
                        case 2: py = b.Foot[bi]; pz = b.Up[bi]; break;                                                  // a top surface
                        default: py = b.Foot[bi]; pz = b.Up[bi]; ny = 0.45f; nz = 0.89f; break;                        // props, trees: lit from the front and top
                    }
                    float vx = lamp.X - gx, vy = lamp.Y - py, vz = lamp.H - pz, d = Mathf.Sqrt(vx * vx + vy * vy + vz * vz);
                    float dg = Mathf.Sqrt(vx * vx + vy * vy); if (dg >= lamp.R) continue;
                    float t = 1f - (dg / lamp.R) * (dg / lamp.R), atten = 0.35f * t + 0.9f * t * t * t;                // a bright core inside a long soft skirt
                    float lam = (nx * vx + ny * vy + nz * vz) / d; lam = kind == 0 ? 0.55f + 0.45f * lam : kind == 3 ? lam * 0.7f + 0.3f : lam; if (lam <= 0f) continue;   // the ground keeps some glow far out: light bounces
                    float e = pw * atten * lam * (1f - 0.88f * shadow);
                    lr[i] += lamp.Cr * e; lg[i] += lamp.Cg * e; lb[i] += lamp.Cb * e;
                }
            // the lamp's own head burns (a small white-hot disc where it hangs)
            float hx = lamp.X, hy = lamp.Y - lamp.H + 2f; int hr = lamp.H > 20f ? 4 : 5;
            for (int gy = Mathf.RoundToInt(hy) - hr; gy <= Mathf.RoundToInt(hy) + hr; gy++)
                for (int gx = Mathf.RoundToInt(hx) - hr; gx <= Mathf.RoundToInt(hx) + hr; gx++)
                {
                    if (gx < gx0 || gx >= gx0 + rw || gy < gy0 || gy >= gy0 + rh || (gx - gx0) % step != 0 || (gy - gy0) % step != 0) continue;
                    float d = Mathf.Sqrt((gx - hx) * (gx - hx) + (gy - hy) * (gy - hy)); if (d > hr) continue;
                    int i = ((gy - gy0) / step) * tw + (gx - gx0) / step; float e = 5.5f * tell * (1f - d / (hr + 1f));
                    lr[i] += lamp.Cr * e; lg[i] += lamp.Cg * e; lb[i] += lamp.Cb * e;
                }
        }

        public static float ShoulderOf(float v) => Shoulder(v);
        public static int Generation => _gen;            // bumps on every map load (Lighting's bakes are per map)

        /// <summary>Lights a region of a composed map buffer (ComposePreview's: rows bottom-up) in place - the offline render's way (a
        /// true multiply). `ambient` is what everything gets with no light on it (night: dark blue); `time` drives the flicker.</summary>
        public static void ApplyLights(Color32[] px, int gx0, int gy0, int gx1, int gy1, float ar, float ag, float ab, List<PointLight> lights, float time)
        {
            int W = _w * Px, Hh = _h * Px, rw = gx1 - gx0, rh = gy1 - gy0;
            var lr = new float[rw * rh]; var lg = new float[rw * rh]; var lb = new float[rw * rh];
            for (int i = 0; i < lr.Length; i++) { lr[i] = ar; lg[i] = ag; lb[i] = ab; }
            foreach (var lamp in lights) AddLight(BakeLight(lamp), time, lr, lg, lb, gx0, gy0, rw, rh, 1);
            for (int gy = gy0; gy < gy1; gy++)                                     // multiply the picture by the light
                for (int gx = gx0; gx < gx1; gx++)
                {
                    int i = (gy - gy0) * rw + (gx - gx0), k = (Hh - 1 - gy) * W + gx; var c = px[k];
                    px[k] = new Color32((byte)Mathf.Min(255f, c.r * Shoulder(lr[i])), (byte)Mathf.Min(255f, c.g * Shoulder(lg[i])), (byte)Mathf.Min(255f, c.b * Shoulder(lb[i])), c.a);
                }
        }
    }
}
