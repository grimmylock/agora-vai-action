// New (Batch 21): floating hands + held weapon (Rayman / Worms style - see the agora-vai pixel-art
// skill). Characters are armless; each human gets two small glove sprites (glove + suit-coloured
// cuff) and the weapon sprite, all on top of the body, turned toward where the unit is aiming.
//
// Rules (locked with Yan): the grip hand orbits the chest at a fixed reach per weapon type (pistol 4,
// SMG 3.5, long guns 3 character pixels), snaps to whole character pixels, and never drops below the
// waist. Pistols cup the second hand just in front/below the first; long guns put it on the handguard.
// The gun turns around its grip; aiming left flips it so it never ends up upside down.
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameCore
{
    /// <summary>Hand/gun placement in character-sprite pixels (16x16, origin top-left, y down).
    /// Plain floats on purpose, so the rules are testable outside Unity.</summary>
    public struct HeldPose
    {
        public float GripX, GripY;        // top-left pixel of the grip hand's 3x2 interior (whole pixels)
        public float SupportX, SupportY;  // same for the support hand
        public bool HasSupport, AimLeft;
    }

    /// <summary>RT 6f: one drawn frame of a held gun, in character pixels (16x16, y down): where the gun's grip point sits
    /// and its rotation, and the centres of both gloves. HeldWeaponView turns it into transforms; the harness renders it.</summary>
    public struct HeldFrame
    {
        public float GunX, GunY, GunDeg, GripX, GripY, SupX, SupY;
        public float MagDX, MagDY;          // RT 6j: where the magazine is, from its seat in the gun (character px)
        public bool MagShown;               // false while the old one is on the floor and the new one still in the pouch
        public bool MagDropped;             // the old magazine has left the gun (HeldWeaponView throws it once)
        public float BreakDeg;              // RT 6i: how far a break-action's barrels hang open (degrees)
        public bool Left, HasSupport;
    }

    public static class HeldPoseMath
    {
        public const float ChestX = 8f, ChestY = 9.5f;
        public const float WaistTopRow = 10f;   // a 2-row hand starting here ends on the belt row (11)

        public static float Reach(string category) =>
            category == "sidearm" || category == "melee" ? 4f : category == "smg" ? 3.5f : 3f;

        /// <param name="angleDeg">aim angle, counter-clockwise from "right" (Unity/screen convention)</param>
        /// <param name="gripX">weapon-sprite pixel (32x16, half-size pixels) where the main hand closes</param>
        /// <param name="supportX">NaN = no support point (pistols cup the grip hand, melee is one-handed)</param>
        public static HeldPose Compute(string category, float angleDeg, float gripX, float gripY, float supportX, float supportY)
        {
            float a = angleDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            bool left = c < -0.0001f;
            float reach = Reach(category);
            var p = new HeldPose { AimLeft = left };
            // the hand is 3x2 with its cuff on the body side; aiming left mirrors it (x -> 15 - x)
            float right = Mathf.Round(ChestX + reach * Mathf.Abs(c));
            p.GripX = left ? 13f - right : right;
            p.GripY = Mathf.Min(Mathf.Round(ChestY - reach * s), WaistTopRow);
            // gun-local axes in sprite pixels (y down): along the barrel, and "below the gun"
            float dx = c, dy = -s, px = s, py = c;
            if (left) { px = -px; py = -py; }   // the gun is flipped, so its underside stays down
            if (category == "melee") return p;
            float ax, ay;
            if (float.IsNaN(supportX)) { ax = 1f; ay = 1f; }                               // pistol cup
            else { ax = (supportX - gripX) * 0.5f; ay = (supportY - gripY) * 0.5f; }       // half-size px -> character px
            p.HasSupport = true;
            p.SupportX = Mathf.Round(p.GripX + ax * dx + ay * px);
            p.SupportY = Mathf.Min(Mathf.Round(p.GripY + ax * dy + ay * py), WaistTopRow + 1f);
            return p;
        }

        // ---- Batch 22: combat animation curves (all plain floats, see the Batch 22 checks) ----

        /// <summary>Recoil left after `since` seconds: 1 at the shot, easing to 0 at `recover`.</summary>
        public static float Recoil(float since, float recover)
        {
            if (since < 0f || recover <= 0f || since >= recover) return 0f;
            float k = 1f - since / recover;
            return k * k;
        }

        /// <summary>Melee swing: degrees added to the aim, from a raised wind-up (+70) through the cut
        /// (-40) over 0.2 s, and how far forward (character px) the weapon lunges.</summary>
        public static (float deg, float lunge) Swing(float since)
        {
            if (since < 0f || since >= SwingTime) return (0f, 0f);
            float t = since / SwingTime, e = 1f - (1f - t) * (1f - t);
            return (70f - 110f * e, 2f * Mathf.Sin(t * Mathf.PI));
        }
        public const float SwingTime = 0.2f;

        /// <summary>How far the gun tips while reloading (+ = muzzle up when aiming right).</summary>
        /// <summary>Walk cycle by distance walked (tiles): left foot up, stand, right foot up, stand -
        /// one full cycle per tile. -1 = standing frame, 0 = left foot up, 1 = right foot up.</summary>
        public static int WalkFrame(float stride)
        {
            int q = (int)(stride * 4f) % 4;
            return q == 0 ? 0 : q == 2 ? 1 : -1;
        }

        public static float ReloadTilt(string style) =>
            style == "jam" ? 25f : style == "cylinder" ? 70f : style == "break" ? -50f : style == "tube" ? -20f : style == "belt" ? -15f : style == "mag" ? -35f : 0f;

        /// <summary>Reload progress r (0..1) -> how far the gun is tipped (0..1) and where the support
        /// hand is: 0 = on the gun, 1 = at the belt pouch. Mag: out to the pouch and back once.
        /// Tube / cylinder: several trips (one per couple of shells).</summary>
        public static (float tip, float pouch) Reload(float r, string style, int trips)
        {
            if (style == "jam") { var j = Jam(r); return (j.tip, 0f); }
            if (r < 0f || r >= 1f || style == "none") return (0f, 0f);
            float tip = Mathf.Min(1f, Mathf.Min(r / 0.15f, (1f - r) / 0.15f));
            if (style == "belt" || style == "break") trips = 1;
            float inner = Mathf.Clamp01((r - 0.1f) / 0.8f);        // the hand works between 10% and 90%
            if (inner <= 0f || inner >= 1f) return (tip, 0f);
            float u = inner * Mathf.Max(1, trips) % 1f;             // 0..1 within one trip
            float pouch = u < 0.35f ? u / 0.35f : u < 0.55f ? 1f : 1f - (u - 0.55f) / 0.45f;
            return (tip, pouch);
        }

        /// <summary>RT 6j: a loose magazine falling (world units, y up): gravity, one or two small bounces off the floor, then it
        /// lies at `lie` or `lie` + 180 degrees. True once it has come to rest.</summary>
        public static bool Fall(ref float x, ref float y, ref float vx, ref float vy, ref float deg, ref float spin, float floor, float dt, float lie = 90f)
        {
            vy -= 14f * dt; x += vx * dt; y += vy * dt; deg += spin * dt;
            if (y > floor || vy > 0f) return false;
            y = floor;
            if (vy < -1.2f) { vy = -vy * 0.3f; vx *= 0.5f; spin *= 0.4f; return false; }
            vx = vy = spin = 0f;
            float d = (deg % 360f + 360f) % 360f; deg = d < 180f ? lie : lie + 180f;   // a magazine on its side (90), brass flat (0)
            return true;
        }

        /// <summary>RT 6f: clearing a jam, progress r (0..1) - cant the gun to look into it, slap the magazine (tap: the support
        /// hand goes under the gun and bumps it), then rack it (rack: the hand pulls back over the action, holds, lets go; jolt:
        /// the action slams home and nudges the gun forward).</summary>
        public static (float tip, float tap, float rack, float jolt) Jam(float r)
        {
            if (r < 0f || r >= 1f) return (0f, 0f, 0f, 0f);
            float tip = Mathf.Min(1f, Mathf.Min(r / 0.12f, (1f - r) / 0.12f));
            float u = (r - 0.12f) / 0.26f, tap = u <= 0f || u >= 1f ? 0f : u < 0.4f ? u / 0.4f : u < 0.6f ? 1f : 1f - (u - 0.6f) / 0.4f;
            float v = (r - 0.42f) / 0.43f, rack = v <= 0f || v >= 1f ? 0f : v < 0.45f ? v / 0.45f : v < 0.6f ? 1f : v < 0.7f ? 1f - (v - 0.6f) / 0.1f : 0f;
            float jolt = v >= 0.6f && v < 0.8f ? 1f : 0f;
            return (tip, tap, rack, jolt);
        }

        /// <summary>RT 6f: everything HeldWeaponView.Show draws for a held gun (moved here from it so the harness can render it):
        /// aim, then recoil (k: 0..1 left of the last shot's kick), swing, and the reload or jam-clearing pose at progress r.
        /// g = WeaponArt grip/support/muzzle points.</summary>
        public static HeldFrame Frame(string cat, float[] g, float angle, float k, float climb, float kick, float sinceSwing,
            float r, string style, int trips, float[] mag = null)
        {
            bool left = Mathf.Cos(angle * Mathf.Deg2Rad) < -0.0001f;
            float side = left ? -1f : 1f;
            var (swingDeg, lunge) = Swing(sinceSwing);
            var (tip, pouch) = Reload(r, style, trips);
            var jam = style == "jam" ? Jam(r) : default;
            float extra = side * (climb * k + swingDeg + ReloadTilt(style) * tip);
            float push = lunge - kick * k + jam.jolt;                       // + forward, - back (character px)

            var pose = Compute(cat, angle + extra, g[0], g[1], g[2], g[3]);
            if (pose.AimLeft != left) pose = Compute(cat, angle, g[0], g[1], g[2], g[3]);   // never flip sides mid-animation
            float a = angle * Mathf.Deg2Rad;
            float ox = Mathf.Round(push * Mathf.Cos(a)), oy = Mathf.Round(-push * Mathf.Sin(a)) - Mathf.Round(jam.tap);   // the tap bumps it up
            bool free = false;                                              // the support hand is off the gun
            if (pouch > 0f && pose.HasSupport)                              // support hand off to the belt pouch
            {
                float bx = pose.GripX + (left ? 3f : -3f), by = WaistTopRow + 1f;
                pose.SupportX = Mathf.Round(Mathf.Lerp(pose.SupportX, bx, pouch));
                pose.SupportY = Mathf.Round(Mathf.Lerp(pose.SupportY, by, pouch));
                free = true;
            }
            if (style == "jam" && pose.HasSupport && (jam.tap > 0f || jam.rack > 0f))
            {
                bool pistol = float.IsNaN(g[2]);
                // the magazine: under the grip on a pistol, between the hands on a long gun; the action: the slide over a
                // pistol's grip, just behind and over the grip on a long gun
                float mx = pistol ? pose.GripX : Mathf.Round((pose.GripX + pose.SupportX) * 0.5f), my = pistol ? pose.GripY + 2f : Mathf.Round((pose.GripY + pose.SupportY) * 0.5f) + 1f;
                float rx = pistol ? pose.GripX : pose.GripX - side * 2f, ry = pose.GripY - 2f;
                float tx = jam.tap > 0f ? mx : rx, ty = jam.tap > 0f ? my : ry, w = System.Math.Max(jam.tap, jam.rack);
                pose.SupportX = Mathf.Round(Mathf.Lerp(pose.SupportX, tx, w));
                pose.SupportY = Mathf.Round(Mathf.Lerp(pose.SupportY, ty, w));
                free = true;
            }
            // Batch 28: small 4x3 gloves while holding something; the grip point (centre of the handle)
            // sits in the middle of the glove's 2x1 interior, so the fist closes on the handle
            float cx = pose.AimLeft ? 1f : 2f;
            // RT 6j (Yan: "treat magazines as actual physical objects"): a magazine reload - the old one drops free (12%, the view
            // throws it as a loose object), the support hand goes to the pouch, brings the new one up under the well, pushes it
            // home and goes back to the gun. mag = WeaponMags.Points (well x, y, centre x, y in gun pixels).
            float magDX = 0f, magDY = 0f, supX = 0f, supY = 0f;
            bool magShown = true, dropped = false, handMoved = false;
            if ((style == "mag" || style == "belt") && mag != null && pose.HasSupport && r >= 0.12f && r < 1f)   // belt: a drum or box
            {
                float ma = (angle + extra) * Mathf.Deg2Rad, ca = Mathf.Cos(ma), sa = Mathf.Sin(ma), fl = left ? -1f : 1f;
                (float x, float y) Rot(float vx, float vy) => (vx * ca + vy * sa, -vx * sa + vy * ca);   // gun space -> character px (y down)
                float gunX = pose.GripX + ox + cx, gunY = pose.GripY + oy + 1.5f;
                float homeX = pose.SupportX + ox + cx, homeY = pose.SupportY + oy + 1.5f;  // the hand's place on the gun
                var w = Rot((mag[0] - g[0]) * 0.5f, (mag[1] - g[1]) * 0.5f * fl);
                var dn = Rot(0f, fl);                                                       // the gun's underside
                float wellX = gunX + w.x, wellY = gunY + w.y;
                float holdX = wellX + 3f * dn.x, holdY = wellY + 3f * dn.y;                 // the hand on the seated magazine
                float belowX = wellX + 6f * dn.x, belowY = wellY + 6f * dn.y;               // ... with it still under the well
                float bx = pose.GripX + (left ? 3f : -3f) + cx, by = WaistTopRow + 1f + 1.5f;  // the belt pouch
                float Sm(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
                (float, float) L(float x0, float y0, float x1, float y1, float t) => (Mathf.Lerp(x0, x1, Sm(t)), Mathf.Lerp(y0, y1, Sm(t)));
                (float hx, float hy) hand;
                if (r < 0.38f) hand = L(homeX, homeY, bx, by, (r - 0.12f) / 0.26f);
                else if (r < 0.5f) hand = (bx, by);
                else if (r < 0.75f) hand = L(bx, by, belowX, belowY, (r - 0.5f) / 0.25f);
                else if (r < 0.85f) hand = L(belowX, belowY, holdX, holdY, (r - 0.75f) / 0.1f);
                else hand = L(holdX, holdY, homeX, homeY, (r - 0.85f) / 0.15f);
                supX = Mathf.Round(hand.hx); supY = Mathf.Round(hand.hy); handMoved = true;
                dropped = r < 0.5f;
                magShown = r >= 0.5f;
                if (r >= 0.5f && r < 0.85f) { magDX = hand.hx - holdX; magDY = hand.hy - holdY; }   // riding in the hand
            }
            // RT 6i: a break-action drops its barrels open, holds them while the shells go in, snaps them shut
            float breakDeg = style == "break" && r >= 0f && r < 1f ? 35f * Mathf.Clamp01(Mathf.Min((r - 0.05f) / 0.12f, (0.92f - r) / 0.07f)) : 0f;
            return new HeldFrame
            {
                MagDX = magDX, MagDY = magDY, MagShown = magShown, MagDropped = dropped, BreakDeg = breakDeg,
                Left = pose.AimLeft, GunDeg = angle + extra, HasSupport = pose.HasSupport,
                GunX = pose.GripX + ox + cx, GunY = pose.GripY + oy + 1.5f,
                GripX = pose.GripX + ox + cx, GripY = pose.GripY + oy + 1.5f,
                SupX = handMoved ? supX : pose.SupportX + (free ? 0f : ox) + cx, SupY = handMoved ? supY : pose.SupportY + (free ? 0f : oy) + 1.5f,
            };
        }
    }

    /// <summary>Weapon sprites + grip points. A weapon uses Sprites/Weapons/weapon_&lt;Key&gt; if it exists,
    /// otherwise its category's stand-in (weapon_cat_&lt;category&gt;) until its own art is drawn.</summary>
    public static class WeaponArt
    {
        public const int PixelsPerUnit = 32;   // half-size pixels next to the 16 px characters
        // grip, support (NaN = none), muzzle (blade tip for melee) in weapon-sprite pixels;
        // per-weapon entries override the category
        private static readonly Dictionary<string, float[]> Grips = new Dictionary<string, float[]>
        {
            ["cat_melee"]   = new[] { 7f, 7f, float.NaN, float.NaN, 27f, 7.5f },
            ["cat_sidearm"] = new[] { 10.5f, 11.5f, float.NaN, float.NaN, 24f, 6.5f },
            ["cat_smg"]     = new[] { 10.5f, 10.5f, 20f, 9.5f, 28f, 6.5f },
            ["cat_shotgun"] = new[] { 9.5f, 8.5f, 14f, 9f, 32f, 5.5f },
            ["cat_assault"] = new[] { 12.5f, 10f, 22f, 8.5f, 32f, 7.5f },
            ["cat_sniper"]  = new[] { 14.5f, 10f, 19.5f, 7.5f, 32f, 7.5f },
            ["cat_heavy"]   = new[] { 11.5f, 10.5f, 24f, 7.5f, 32f, 7f },
            // Batch 29: every weapon hand-pixelled (ArtSource/weapons/hand, exported by export_hand.py)
            ["HuntingKnife"] = new[] { 4f, 3f, float.NaN, float.NaN, 17f, 3.5f },
            ["SpetsnazSwitchblade"] = new[] { 4f, 3.5f, float.NaN, float.NaN, 17f, 3.5f },
            ["SteppeMachete"] = new[] { 4f, 3.5f, float.NaN, float.NaN, 24f, 2.5f },
            ["SerratedTrenchKnife"] = new[] { 5.5f, 5f, float.NaN, float.NaN, 18f, 5.5f },
            ["ObeliskRitualBlade"] = new[] { 3f, 5.5f, float.NaN, float.NaN, 22f, 5.5f },
            ["PSh18Pchela"] = new[] { 5.5f, 9f, float.NaN, float.NaN, 19f, 2.5f },
            ["WaldenP9"] = new[] { 5f, 10f, float.NaN, float.NaN, 20f, 3.5f },
            ["Halden45Service"] = new[] { 4.5f, 9.5f, float.NaN, float.NaN, 20f, 3.5f },
            ["HaldenServiceRevolver"] = new[] { 3.5f, 8.5f, float.NaN, float.NaN, 22f, 3f },
            ["SR21Gadyuka"] = new[] { 4f, 10.5f, float.NaN, float.NaN, 19f, 3.5f },
            ["BlackHarrier"] = new[] { 4.5f, 9.5f, float.NaN, float.NaN, 22f, 5f },
            ["Corvan57"] = new[] { 4.5f, 10f, float.NaN, float.NaN, 20f, 3.5f },
            ["RinaldiR6"] = new[] { 3.5f, 7f, float.NaN, float.NaN, 21f, 5.5f },
            ["SteppeEagle"] = new[] { 4.5f, 10f, float.NaN, float.NaN, 22f, 3.5f },
            ["VosaVz62"] = new[] { 3.5f, 10f, float.NaN, float.NaN, 23f, 3.5f },
            ["PPZZubr"] = new[] { 3.5f, 8f, 18f, 7f, 29f, 4f },
            ["CorvanWasp9"] = new[] { 6f, 13f, 20.5f, 8f, 31f, 6.5f },
            ["CorvanHornet45"] = new[] { 12f, 8f, 23.5f, 8f, 31f, 4f },
            ["CorvanGnatPDW"] = new[] { 8f, 11.5f, 14f, 9f, 28f, 7.5f },
            ["Obrez66"] = new[] { 5.5f, 4f, 17f, 4f, 25f, 2f },
            ["HaldenLeverShotgun"] = new[] { 11.5f, 5.5f, 26f, 5f, 37f, 2.5f },
            ["Hound13"] = new[] { 12f, 5f, 27.5f, 4f, 39f, 2f },
            ["RangerP8"] = new[] { 12.5f, 4.5f, 28f, 4f, 40f, 2f },
            ["BayouPump"] = new[] { 11.5f, 4.5f, 29f, 4.5f, 41f, 2f },
            ["Sever34"] = new[] { 13f, 5.5f, 24f, 4f, 41f, 2f },
            ["Tambour12"] = new[] { 16f, 6.5f, 30f, 5.5f, 37f, 2.5f },
            ["SP14Warden"] = new[] { 3f, 13f, 23f, 12f, 39f, 7.5f },
            ["Sayan12"] = new[] { 10f, 4f, 25f, 3f, 40f, 2.5f },
            ["Kaban12"] = new[] { 11f, 6.5f, 27f, 4f, 39f, 3.5f },
            ["VKS74U"] = new[] { 11.5f, 7f, 25f, 4f, 33f, 3.5f },
            ["Smerch3"] = new[] { 10.5f, 7.5f, 17f, 5f, 28f, 3.5f },
            ["VK74"] = new[] { 13.5f, 7.5f, 27.5f, 5f, 41f, 3.5f },
            ["VKM"] = new[] { 13.5f, 7.5f, 27.5f, 5f, 41f, 3.5f },
            ["TS9Tishina"] = new[] { 11.5f, 6.5f, 25f, 4f, 37f, 3.5f },
            ["TR30"] = new[] { 11f, 7.5f, 25f, 5f, 39f, 3.5f },
            ["HaldenHR36K"] = new[] { 12f, 9f, 28f, 7f, 36f, 5.5f },
            ["VK103"] = new[] { 13.5f, 7.5f, 27.5f, 5f, 41f, 3.5f },
            ["BR86Bulldog"] = new[] { 15f, 11f, 25f, 8f, 36f, 6.5f },
            ["VK74Obelisk"] = new[] { 13.5f, 9.5f, 27f, 8f, 41f, 5.5f },
            ["NA94Yenisei"] = new[] { 12.5f, 8f, 29f, 6f, 41f, 4f },
            ["CorvanBullpup"] = new[] { 11f, 11.5f, 22f, 11f, 32f, 8f },
            ["CorvanBattleRifle"] = new[] { 13f, 7.5f, 27f, 5.5f, 43f, 4f },
            ["SKNKarabin"] = new[] { 12.5f, 5f, 27f, 6f, 42f, 3.5f },
            ["FrontierLeverRifle"] = new[] { 11.5f, 5f, 24f, 6.5f, 39f, 3f },
            ["VSNShepot"] = new[] { 12f, 8.5f, 23.5f, 8f, 39f, 6f },
            ["M91Trekhlineyka"] = new[] { 12.5f, 7f, 31f, 7.5f, 45f, 5f },
            ["Nightwalker4570"] = new[] { 11f, 5.5f, 24f, 6.5f, 39f, 3f },
            ["CorvanMarksman"] = new[] { 11.5f, 8f, 26f, 7.5f, 43f, 5.5f },
            ["SV2Strela"] = new[] { 10.5f, 7.5f, 27f, 7.5f, 45f, 6f },
            ["Sever98"] = new[] { 11.5f, 7.5f, 26f, 8f, 43f, 4.5f },
            ["VK127Grom"] = new[] { 14f, 11.5f, 26f, 10.5f, 43f, 7f },
            ["HaldenAM50Sledge"] = new[] { 13.5f, 11f, 28f, 9f, 47f, 6f },
            ["G7RailPrototype"] = new[] { 12.5f, 10.5f, 28f, 9f, 41f, 6.5f },
            ["CorvanLMG"] = new[] { 12.5f, 11f, 28f, 9.5f, 42f, 6f },
            ["RL44"] = new[] { 11.5f, 7.5f, 24f, 6f, 43f, 4f },
            ["PL62Kobza"] = new[] { 15f, 7.5f, 31f, 5.5f, 45f, 3.5f },   // Batch 113
            ["PL74"] = new[] { 12.5f, 8.5f, 27f, 8.5f, 44f, 5f },
            ["PL74PPolovets"] = new[] { 12.5f, 8.5f, 28f, 7.5f, 44f, 5f },
            ["PL74PPolovetsZulu"] = new[] { 12.5f, 8.5f, 28f, 7.5f, 44f, 5f },
            ["RK12Kuvalda"] = new[] { 3f, 10f, float.NaN, float.NaN, 23f, 5.5f },
        };
        public static bool HasGrip(string key) => key != null && Grips.ContainsKey(key);
        /// <summary>The grip/support/muzzle points of a weapon's own sprite (its category's when it has none).</summary>
        public static float[] GripOf(string cat, string key) => key != null && Grips.TryGetValue(key, out var kg) ? kg : Grips.TryGetValue("cat_" + cat, out var cg) ? cg : Grips["cat_assault"];
        private static readonly Dictionary<string, (Sprite, float[])> Cache = new Dictionary<string, (Sprite, float[])>();
        private static Dictionary<WeaponDef, (string cat, string key)> _index;
        // Batch 29: the project enters Play mode without a domain reload, so statics survive between
        // runs while the sprites/textures they point at are destroyed - clear the caches each run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches() { Cache.Clear(); MagCache.Clear(); BreakCache.Clear(); LooseCache.Clear(); }


        public static (string cat, string key) Identify(WeaponDef db)
        {
            if (_index == null)
            {
                _index = new Dictionary<WeaponDef, (string, string)>();
                foreach (var cat in WeaponDatabase.Categories)
                    foreach (var w in cat.Value) _index[w.Value] = (cat.Key, w.Key);
            }
            return db != null && _index.TryGetValue(db, out var id) ? id : ("assault", null);
        }

        /// <summary>The weapon's own sprite and grip points, or its category stand-in and the category's points.</summary>
        /// <summary>RT 6g: the gun without its magazine and the magazine alone (ArtSource/weapons/mags.py), same size and pivot as
        /// the gun; body null = the magazine sits inside the grip (the gun is drawn whole, the magazine behind it). Both null: none cut.</summary>
        public static (Sprite body, Sprite mag) Mag(string cat, string key)
        {
            if (key == null) return (null, null);
            if (MagCache.TryGetValue(key, out var hit)) return hit;
            var g = For(cat, key).grip;
            Sprite Load(string part)
            {
                var tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_" + part);
                if (tex == null) return null;
                tex.filterMode = FilterMode.Point;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(g[0] / tex.width, 1f - g[1] / tex.height), PixelsPerUnit);
            }
            return MagCache[key] = (Load("body"), Load("mag"));
        }
        private static readonly Dictionary<string, (Sprite, Sprite)> MagCache = new Dictionary<string, (Sprite, Sprite)>();

        /// <summary>RT 6j: the magazine as a loose object - its sprite turned round its own middle (WeaponMags.Points).</summary>
        public static Sprite LooseMag(string key)
        {
            if (key == null || !WeaponMags.Points.TryGetValue(key, out var mp)) return null;
            if (LooseCache.TryGetValue(key, out var hit)) return hit;
            var tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_mag");
            if (tex == null) return LooseCache[key] = null;
            tex.filterMode = FilterMode.Point;
            return LooseCache[key] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(mp[2] / tex.width, 1f - mp[3] / tex.height), PixelsPerUnit);
        }
        private static readonly Dictionary<string, Sprite> LooseCache = new Dictionary<string, Sprite>();

        /// <summary>RT 6i: a break-action split at its hinge (ArtSource/weapons/mags.py BREAK): the stock and action, the barrels
        /// pivoting on the hinge, and the hinge in weapon-sprite pixels. Nulls for every other gun.</summary>
        public static (Sprite body, Sprite barrels, float hx, float hy) Break(string cat, string key)
        {
            if (key == null || !Hinges.TryGetValue(key, out var h)) return (null, null, 0f, 0f);
            if (BreakCache.TryGetValue(key, out var hit)) return hit;
            var g = For(cat, key).grip;
            Sprite Load(string part, float px, float py)
            {
                var tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_" + part);
                if (tex == null) return null;
                tex.filterMode = FilterMode.Point;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(px / tex.width, 1f - py / tex.height), PixelsPerUnit);
            }
            return BreakCache[key] = (Load("body", g[0], g[1]), Load("barrels", h.x, h.y), h.x, h.y);
        }
        public static readonly Dictionary<string, (float x, float y)> Hinges = new Dictionary<string, (float, float)> { ["Obrez66"] = (9f, 4f), ["Sever34"] = (16f, 4f) };
        private static readonly Dictionary<string, (Sprite, Sprite, float, float)> BreakCache = new Dictionary<string, (Sprite, Sprite, float, float)>();

        public static (Sprite sprite, float[] grip) For(string cat, string key)
        {
            string id = key ?? "cat_" + cat;
            if (Cache.TryGetValue(id, out var hit)) return hit;
            var tex = key != null ? Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key) : null;
            bool own = tex != null;
            if (!own) tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_cat_" + cat);
            var g = own && Grips.TryGetValue(key, out var kg) ? kg : Grips.TryGetValue("cat_" + cat, out var cg) ? cg : Grips["cat_assault"];
            Sprite s = null;
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(g[0] / tex.width, 1f - g[1] / tex.height), PixelsPerUnit);
            }
            return Cache[id] = (s, g);
        }
    }

    /// <summary>The gun + two hands for one unit marker (children of the body's SpriteRenderer).
    /// Batch 22: also plays recoil, melee swings and reloads (started by CombatFxView).</summary>
    public class HeldWeaponView
    {
        private readonly SpriteRenderer _gun, _support, _grip, _mag, _barrels;
        private float _angle;
        private static readonly Dictionary<string, Sprite> HandCache = new Dictionary<string, Sprite>();
        // Batch 29: the project enters Play mode without a domain reload, so statics survive between
        // runs while the sprites/textures they point at are destroyed - clear the caches each run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches() { HandCache.Clear(); }


        private float _shotAt = -99f, _kick, _climb, _recover;
        private float _swingAt = -99f;
        private float _reloadAt = -99f, _reloadTime;
        private string _reloadStyle = "none";
        private int _reloadTrips = 1;
        private bool _magThrown;
        /// <summary>RT 6j: (sprite, world centre, degrees, flipped, floor y, sorting order) - a magazine let go.</summary>
        public static System.Action<Sprite, Vector3, float, bool, float, int> MagDropped;
        private Vector3 _aimOverride;
        private float _aimUntil = -99f;
        private float[] _g;          // grip/support/muzzle points of the sprite on show

        private readonly int _base;   // the body's sorting order: gun and hands sit just above it, or just under it from behind

        public HeldWeaponView(SpriteRenderer body)
        {
            _base = body.sortingOrder;
            _gun = Child(body, "HeldWeapon", 1);
            _support = Child(body, "SupportHand", 2);
            _grip = Child(body, "GripHand", 3);
            _mag = Child(body, "Magazine", 1);
            _barrels = Child(body, "Barrels", 2);
        }

        public bool Visible => _gun.gameObject.activeSelf;
        public Transform Gun => _gun.transform;
        public bool AimLeft => _gun.flipY;
        public float Angle => _angle;
        public Sprite GunSprite => _gun.sprite;

        public void Recoil(WeaponFeel f, float scale)
        {
            _shotAt = Time.time; _kick = f.Kick * scale; _climb = f.Climb * scale; _recover = Mathf.Max(0.05f, f.Recover);
        }
        public void Twitch() { _shotAt = Time.time; _kick = 0.5f; _climb = -4f; _recover = 0.1f; }   // jammed: click, nothing
        public void Swing() => _swingAt = Time.time;
        /// <summary>RT 6f: clearing a jam (tap the magazine, rack the action) over `seconds`.</summary>
        public void StartClear(float seconds) { _reloadAt = Time.time; _reloadTime = seconds; _reloadStyle = "jam"; _reloadTrips = 1; }
        public void StartReload(WeaponFeel f, int capacity, float seconds = 0f, int rounds = 0)
        {
            _reloadAt = Time.time; _reloadTime = seconds > 0f ? seconds : f.ReloadTime; _reloadStyle = f.Reload;
            _reloadTrips = f.Reload == "tube" || f.Reload == "cylinder" ? (rounds > 0 ? rounds : Mathf.Clamp(capacity / 2, 2, 4)) : 1;
        }
        /// <summary>RT 6h: a round-by-round reload cut short - the gun comes back up.</summary>
        public void StopReload() { _reloadTime = 0f; }
        /// <summary>Point the gun at a world position until `until` (the shot's target, even if the
        /// player has already moved the mouse on).</summary>
        public void AimAt(Vector3 world, float until) { _aimOverride = world; _aimUntil = until; }

        /// <summary>World position of the muzzle (or blade tip) of the gun as last drawn.</summary>
        public Vector3 Muzzle() => _gun.transform.TransformPoint(MuzzleLocal());

        /// <summary>The muzzle in the gun's own space (the gun turns around its grip point).</summary>
        public Vector3 MuzzleLocal()
        {
            if (_g == null) return Vector3.zero;
            float side = _gun.flipY ? -1f : 1f;
            return new Vector3((_g[4] - _g[0]) / WeaponArt.PixelsPerUnit, side * (_g[1] - _g[5]) / WeaponArt.PixelsPerUnit, 0f);
        }

        private static SpriteRenderer Child(SpriteRenderer body, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(body.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = body.sortingOrder + order;
            return sr;
        }

        /// <summary>aimAt: world position the unit points its weapon at, or null to keep the last angle. back: the unit shows
        /// its back (UnitSprites.Away) - Batch 128 (Yan: "when they're with their back turned, the weapon is covered by the body
        /// and the hands change orientation"): gun and hands go under the body and the gloves flip, seen from the other side.</summary>
        public void Show(Weapon weapon, ArmorDef armor, Vector3 unitPos, Vector3? aimAt, bool visible, bool back = false)
        {
            bool on = visible && weapon?.Db != null;
            _gun.gameObject.SetActive(on); _grip.gameObject.SetActive(visible); _support.gameObject.SetActive(visible); _mag.gameObject.SetActive(false); _barrels.gameObject.SetActive(false);
            if (!visible) return;
            // RT 6g: the magazine just under the gun (a pistol's sits inside the grip, hidden by it until it slides out)
            _mag.sortingOrder = _base + (back ? -4 : 1); _gun.sortingOrder = _base + (back ? -3 : 2); _barrels.sortingOrder = _gun.sortingOrder; _support.sortingOrder = _base + (back ? -2 : 3); _grip.sortingOrder = _base + (back ? -1 : 4);
            _grip.flipY = _support.flipY = back;
            if (!on)
            {
                // nothing equipped: the big gloves float at the torso's sides (Batch 28, Yan)
                var big = HandSprite(armor, false);
                _grip.sprite = big; _support.sprite = big;
                _grip.flipX = false; _support.flipX = true;                           // cuffs toward the body
                _grip.transform.localPosition = Local(RestRightX, RestY);
                _support.transform.localPosition = Local(RestLeftX, RestY);
                _g = null;
                return;
            }

            float now = Time.time;
            if (now < _aimUntil) aimAt = _aimOverride;
            if (aimAt.HasValue)
            {
                float ddx = aimAt.Value.x - unitPos.x, ddy = aimAt.Value.y - unitPos.y;
                if (ddx * ddx + ddy * ddy > 0.0001f) _angle = Mathf.Atan2(ddy, ddx) * Mathf.Rad2Deg;
            }
            var (cat, key) = WeaponArt.Identify(weapon.Db);
            var (sprite, g) = WeaponArt.For(cat, key);
            _g = g;

            // animation on top of the aim: recoil (back + muzzle climb), melee swing, reload tilt, clearing a jam
            var f = HeldPoseMath.Frame(cat, g, _angle, HeldPoseMath.Recoil(now - _shotAt, _recover), _climb, _kick, now - _swingAt,
                _reloadTime > 0f ? (now - _reloadAt) / _reloadTime : 1f, _reloadStyle, _reloadTrips,
                key != null && WeaponArt.Mag(cat, key).mag != null && WeaponMags.Points.TryGetValue(key, out var pts) ? pts : null);

            var (body, mag) = WeaponArt.Mag(cat, key);
            _gun.sprite = mag != null && body != null ? body : sprite;
            _gun.flipY = f.Left;
            _gun.transform.localPosition = Local(f.GunX, f.GunY);
            _gun.transform.localRotation = Quaternion.Euler(0f, 0f, f.GunDeg);
            var brk = WeaponArt.Break(cat, key);
            if (brk.body != null && brk.barrels != null)
            {
                // the barrels turn on the hinge: grip -> hinge in the gun's own space (a flipped gun mirrors it), then the aim
                _gun.sprite = brk.body;
                float a0 = f.GunDeg * Mathf.Deg2Rad, hx = (brk.hx - g[0]) / WeaponArt.PixelsPerUnit, hy = (f.Left ? 1f : -1f) * (brk.hy - g[1]) / WeaponArt.PixelsPerUnit;
                _barrels.gameObject.SetActive(true);
                _barrels.sprite = brk.barrels; _barrels.flipY = f.Left;
                _barrels.transform.localPosition = _gun.transform.localPosition + new Vector3(hx * Mathf.Cos(a0) - hy * Mathf.Sin(a0), hx * Mathf.Sin(a0) + hy * Mathf.Cos(a0), 0f);
                _barrels.transform.localRotation = Quaternion.Euler(0f, 0f, f.GunDeg + (f.Left ? 1f : -1f) * f.BreakDeg);
            }
            if (mag != null && f.MagShown)
            {
                _mag.gameObject.SetActive(true);
                _mag.sprite = mag; _mag.flipY = f.Left;
                _mag.transform.localPosition = Local(f.GunX + f.MagDX, f.GunY + f.MagDY);   // seated, or riding in the hand
                _mag.transform.localRotation = _gun.transform.localRotation;
            }
            // RT 6j: the old magazine leaves as a loose object (CombatFxView lets it fall and lie on the floor)
            if (f.MagDropped && !_magThrown && mag != null && MagDropped != null && WeaponMags.Points.TryGetValue(key, out var mp))
            {
                var c = _gun.transform.TransformPoint(new Vector3((mp[2] - g[0]) / WeaponArt.PixelsPerUnit, (f.Left ? 1f : -1f) * (mp[3] - g[1]) / WeaponArt.PixelsPerUnit, 0f));
                var feet = _gun.transform.parent != null ? _gun.transform.parent.position.y - 0.38f : c.y - 0.5f;
                MagDropped(WeaponArt.LooseMag(key), c, f.GunDeg, f.Left, feet, _base + 5);
            }
            _magThrown = f.MagDropped;

            var hand = HandSprite(armor, true);
            _grip.sprite = hand; _support.sprite = hand;
            _grip.flipX = _support.flipX = f.Left;                                    // cuff stays on the body side
            _grip.transform.localPosition = Local(f.GripX, f.GripY);
            _support.gameObject.SetActive(f.HasSupport);
            if (f.HasSupport) _support.transform.localPosition = Local(f.SupX, f.SupY);
        }

        // character pixel (16x16 sprite, y down) -> marker-local units (sprite spans -0.5..0.5)
        private static Vector3 Local(float px, float py) => new Vector3((px - 8f) / 16f, (8f - py) / 16f, 0f);

        // nothing equipped: big glove centres beside the torso (torso is columns 5-10, rows 8-11)
        public const float RestRightX = 13.5f, RestLeftX = 2.5f, RestY = 10f;

        /// <summary>Glove sprite: big 5x4 (incl. outline, suit-coloured cuff on the body side, lit top edge,
        /// knuckle shadow) when the hands are empty; small 4x3 (cuff + glove) when holding something, so the
        /// weapon stays readable. One per armor and size, built at runtime from SpriteAtlasIndex colours.</summary>
        public static Sprite Glove(ArmorDef armor, bool small = false) => HandSprite(armor, small);
        private static Sprite HandSprite(ArmorDef armor, bool small)
        {
            string name = armor?.Name ?? "";
            string id = (small ? "s:" : "b:") + name;
            if (HandCache.TryGetValue(id, out var s)) return s;
            int i = System.Array.IndexOf(SpriteAtlasIndex.Armor, name);
            Color cuff = Hex(i >= 0 ? SpriteAtlasIndex.ArmorCuff[i] : "#5a4a36");
            Color glove = Hex(i >= 0 ? SpriteAtlasIndex.ArmorGlove[i] : "#3d4148");
            Color k = Hex("#141617"), lit = Scale(glove, 1.45f), dark = Scale(glove, 0.65f), none = new Color(0, 0, 0, 0);
            string[] rows = small ? GloveSmall : GloveBig;
            int w = rows[0].Length, h = rows.Length;
            var tex = new Texture2D(w, h) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char ch = rows[y][x];
                    tex.SetPixel(x, h - 1 - y, ch == 'K' ? k : ch == 'c' ? cuff : ch == 'l' ? lit : ch == 'g' ? glove : ch == 'd' ? dark : none);
                }
            tex.Apply();
            return HandCache[id] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }
        public static readonly string[] GloveBig = { ".KKK.", "KcllK", "KcgdK", ".KKK." };
        public static readonly string[] GloveSmall = { ".KK.", "KcgK", ".KK." };

        private static Color Hex(string h)
        {
            int v = System.Convert.ToInt32(h.TrimStart('#'), 16);
            return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f);
        }
        private static Color Scale(Color c, float f) => new Color(Mathf.Min(1f, c.r * f), Mathf.Min(1f, c.g * f), Mathf.Min(1f, c.b * f), 1f);
    }
}
