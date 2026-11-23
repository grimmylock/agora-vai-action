// New (Batch 21): floating hands + held weapon (Rayman / Worms style - see the agora-vai pixel-art
// skill). Characters are armless; each human gets two small glove sprites (glove + suit-coloured
// cuff) and the weapon sprite, all on top of the body, turned toward where the unit is aiming.
//
// Rules (locked with Yan): the grip hand orbits the chest at a fixed reach per weapon type (pistol 4,
// SMG 3.5, long guns 3 character pixels), snaps to whole character pixels, and never drops below the
// waist. Pistols cup the second hand just in front/below the first; long guns put it on the handguard.
// The gun turns around its grip; aiming left flips it so it never ends up upside down.
using System.Collections.Generic;
using System.Linq;
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
        public float CylDX, CylDY;          // RT 6l: a revolver's cylinder swung out on its crane (character px from its seat)
        public float Lean;                  // RT 7e: the unit's tilt from a melee swing or throw (degrees, + = counter-clockwise)
        public bool Thrown;                 // RT 7e: it has left the hand (the whip forward, after release)
        public float HandDeg;              // Melee 2: the weapon fist's turn (degrees, + = counter-clockwise)
        public string Body;                 // Melee 3: the body's pose frame (UnitSprites.Pose key), null = standing
        public float Length;                // Melee 2: the weapon's drawn length (1, or less mid-sweep: 0 = unset, read as 1)
        public float BreakDeg;              // RT 6i: how far a break-action's barrels hang open (degrees)
        public float ClipIn, PartOpen;      // 2026-11-16 (Sperling): how far the stripper clip has gone into the receiver (0..1, behind the gun), and how far the bolt stands open for the reload (0..1)
        public bool ShellInHand;            // 2026-11-16: a tube-fed gun's reload: the support hand carries a round from the pouch to the loading port
        public bool Left, HasSupport;
    }

    public static class HeldPoseMath
    {
        public const float ChestX = 8f, ChestY = 9.5f;
        public const float WaistTopRow = 10f;   // a 2-row hand starting here ends on the belt row (11)
        public const float SupportTopRow = 3f;  // the support hand never rides higher than this row (just under the chin) when the barrel points up

        public static float Reach(string category) =>
            category == "sidearm" || category == "melee" ? 4f : category == "smg" ? 3.5f : 3f;

        /// <param name="angleDeg">aim angle, counter-clockwise from "right" (Unity/screen convention)</param>
        /// <param name="gripX">weapon-sprite pixel (32x16, half-size pixels) where the main hand closes</param>
        /// <param name="supportX">NaN = no support point (pistols cup the grip hand, melee is one-handed)</param>
        public const float MinSide = 2.5f;   // px the gripping hand stays out from the body's centre line, even aiming straight up or down
        public static HeldPose Compute(string category, float angleDeg, float gripX, float gripY, float supportX, float supportY)
        {
            float a = angleDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            bool left = c < -0.0001f;
            float reach = Reach(category);
            var p = new HeldPose { AimLeft = left };
            // the hand is 3x2 with its cuff on the body side; aiming left mirrors it (x -> 15 - x)
            float right = Mathf.Round(ChestX + Mathf.Max(reach * Mathf.Abs(c), MinSide));   // 2026-11-18 (Yan: aiming straight up or down the gun covers the body): never closer to the chest line than MinSide, so the hand and the gun stay beside the body
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
            // 2026-11-21 (Yan: "hand placement on assault rifles breaks when aiming down and up"): the hand's row used to be clamped on its own (never below the belt row), which slid it OFF the barrel's line whenever
            // the barrel pointed down at a slant, and nothing stopped it from sitting far above the head when the barrel pointed up (a rifle's foregrip is 10+ px from the grip). Now the hand stays ON the barrel's line and
            // slides back toward the grip until it is inside the body's band (no lower than the belt, no higher than SupportTopRow).
            float along = ax, baseY = p.GripY + ay * py;
            if (ax > 0f)
            {
                if (dy > 0.001f) along = Mathf.Min(along, (WaistTopRow + 1f - baseY) / dy);
                else if (dy < -0.001f) along = Mathf.Min(along, (baseY - SupportTopRow) / -dy);
                along = Mathf.Max(along, 0f);
            }
            p.SupportX = Mathf.Round(p.GripX + along * dx + ay * px);
            p.SupportY = Mathf.Round(p.GripY + along * dy + ay * py);
            return p;
        }

        /// <summary>RT 7z-c (Gunslinger): the left-hand pistol's frame - the same pose as a right-hand one (its own recoil already in
        /// f), staggered so the two read as two (Yan: "both pistols are held in the exact same place so it looks like one, make the
        /// character hold one slightly back and above/below the other"): OffSide px "under the gun" and OffBack px back along it.
        /// The left glove holds it; nobody cups.</summary>
        public const float OffSide = 3f, OffBack = 3f;
        public static HeldFrame Offhand(HeldFrame f, float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            float bx = s, by = c;                                            // under the gun, screen pixels (y down)
            if (f.Left) { bx = -bx; by = -by; }
            float ox = Mathf.Round(OffSide * bx - OffBack * c), oy = Mathf.Round(OffSide * by + OffBack * s);
            float y = Mathf.Min(f.GripY + oy, WaistTopRow + 2.5f);           // never under the belt
            f.GunX += ox; f.GripX += ox; f.GunY += y - f.GripY; f.GripY = y;
            f.HasSupport = false;
            return f;
        }

        // ---- Batch 22: combat animation curves (all plain floats, see the Batch 22 checks) ----

        /// <summary>Recoil left after `since` seconds: 1 at the shot, easing to 0 at `recover`.</summary>
        public static float Recoil(float since, float recover, float hold = 0f)
        {
            if (since < 0f || recover <= 0f) return 0f;
            if (since < hold) return 1f;                                   // a hard hitter: the gun stays up for `hold` seconds before it comes down
            since -= hold;
            if (since >= recover) return 0f;
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
            style == "jam" ? 25f : style == "cylinder" ? 70f : style == "break" ? -50f : style == "tube" ? -20f : style == "belt" ? -15f : style == "mag" ? -35f : style == "clip" ? -30f : 0f;

        /// <summary>Reload progress r (0..1) -> how far the gun is tipped (0..1) and where the support
        /// hand is: 0 = on the gun, 1 = at the belt pouch. Mag: out to the pouch and back once.
        /// Tube / cylinder: several trips (one per couple of shells).</summary>
        public static (float tip, float pouch) Reload(float r, string style, int trips)
        {
            if (style == "jam") { var j = Jam(r); return (j.tip, 0f); }
            if (r < 0f || r >= 1f || style == "none") return (0f, 0f);
            float tip = Mathf.Min(1f, Mathf.Min(r / 0.15f, (1f - r) / 0.15f));
            if (style == "belt" || style == "break" || style == "clip") trips = 1;
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

        // ---- RT 7c (Yan: "upside down ... held way too far from the body, too big, the swings have no weight ... make the character
        // hold it with his actual hand and in a position that makes sense"): a melee weapon is held upright at your side - handle up,
        // the edge toward where you aim, the head leaning forward a little - with your fist low on the handle beside the body. Only
        // the side follows the aim. A swing: wind-up (the head goes back over the shoulder, the hand rises, you lean back), the chop
        // (fast, forward and down - the hit lands here), follow-through, back to rest. ----
        public const float WindUp = 0.22f, ChopTime = 0.08f, Follow = 0.15f, Recover = 0.17f, RestTilt = 25f;
        public static float SwingLength => WindUp + ChopTime + Follow + Recover;

        // ---- Melee 2 (Yan, 2026-10-29: "melee should feel like the leon vs krauser fight from re4 ... knifes can have stabs and
        // slashes from up or down, hatchets can have horizontal and vertical sweeps, 2 handed melees can have some different swings";
        // "they need a STANCE"): a fighting stance per kind of weapon instead of hanging it at your side, and a string of different
        // swings per kind - each click in a combo is the next one. A pose is (off: head forward of RestTilt in degrees, + =
        // forward/down; hx, hy: the fist's shift in character px, + = forward / down; lean: the body's tilt, + = back). A move is
        // (wound up, end of the strike); the strike turns the raw way between them (a horizontal sweep turns the opposite way to
        // a chop, under the hands instead of over the head). ----
        public enum Style { Knife, Chop, Heavy, Visor, Verger }
        static readonly HashSet<string> Knives = new HashSet<string> { "HuntingKnife", "SpetsnazSwitchblade", "SerratedTrenchKnife", "ObeliskRitualBlade" };
        public static Style StyleOf(string key) =>
            key == VisorPoses ? Style.Visor
            : key == VergerPoses ? Style.Verger
            : key != null && Knives.Contains(key) ? Style.Knife
            : key != null && WeaponDatabase.Categories["melee"].TryGetValue(key, out var d) && d.WeightKg >= Melee.TwoHandedKg ? Style.Heavy : Style.Chop;

        static readonly (float off, float hx, float hy, float lean)[] Stances =
        {
            // lean 0 at rest (Yan: "the top of the characters heads get cut off": a 16 px body turned a few degrees loses pixel rows)
            (45f, 1f, -1f, 0f),        // knife: point out at chest height (Krauser)
            (-5f, 0f, -2f, 0f),        // hatchet, machete, hammer: raised, head up and forward
            (-35f, -1f, -1f, 0f),      // two-handed: up across the body, ready to come round
            (165f, 1f, -1f, 0f),       // the Visor: the knife reversed, blade down along the forearm, low and forward
            (-55f, -1f, -1f, 0f),      // 2026-11-21 the Verger: the scythe carried high across the body, the blade hanging behind his shoulder
        };
        static readonly ((float off, float hx, float hy, float lean) a, (float off, float hx, float hy, float lean) b)[][] Moves =
        {
            new[] {
                ((45f, -3f, 0f, 4f), (55f, 5f, 0f, -8f)),            // stab: pulled back, thrust straight out
                ((-70f, 0f, -4f, 5f), (130f, 3f, 2f, -6f)),          // slash down from over the shoulder
                ((140f, 1f, 2f, -2f), (-50f, 3f, -4f, 5f)),          // slash up from low
            },
            new[] {
                ((-75f, -1f, -2f, 6f), (85f, 3f, 1f, -8f)),          // vertical chop
                ((-135f, -3f, -2f, 5f), (-275f, 4f, -2f, -8f)),      // horizontal sweep: from behind, round in front at chest height
            },
            new[] {
                ((-150f, -2f, -7f, 10f), (95f, 3f, 2f, -10f)),       // overhead slam
                ((-130f, -4f, -1f, 6f), (-280f, 5f, -1f, -10f)),     // wide sweep
                ((160f, -2f, 3f, -2f), (-60f, 4f, -5f, 8f)),         // rising swing from the floor
            },
            // Visor 2 (Yan: "build the visor's own moveset, he can have some different animation combos"): a reverse-grip knife
            // fighter. Boss.Move.Poses strings these into his combos.
            new[] {
                // 2026-11-23 (Yan: "clearer poses, more expressive movements, it doesn't matter if it looks a bit out of reach for his hands, expression and gameplay come before realism"): every wind-up is exaggerated and DIFFERENT, so the move reads
                // from its silhouette before the glint: backhand = knife cocked far behind a twisted body; rising = crouched, blade by the floor; ice-pick = fist stretched high over the head; thrust = knife drawn all the way back to the hip; spin = coiled the wrong way; execution = arched
                // back with both arms high. The strikes throw the body further (bigger lean) and reach further than a real arm could.
                ((-160f, -6f, -3f, 11f), (-255f, 8f, -1f, -14f)),    // 0 backhand: the blade cocked far behind a twisted body, then round in front, down along the arm
                ((-195f, -3f, 6f, -8f), (-365f, 5f, -8f, 10f)),      // 1 rising: crouched low, the blade by the floor, then up and out through the face
                ((-218f, 1f, -14f, 15f), (-235f, 5f, 5f, -18f)),     // 2 ice-pick: the fist stretched high over the head, blade down, driven into you
                ((72f, -8f, 1f, 11f), (60f, 11f, 0f, -17f)),         // 3 thrust: the knife drawn all the way back to the hip, then flung out with all his weight
                ((-100f, -6f, -1f, -12f), (-500f, 7f, -1f, -14f)),   // 4 spin: coiled the wrong way, then a full turn, the blade out at the end of the arm
                ((-212f, -1f, -15f, 17f), (-240f, 6f, 5f, -19f)),    // 5 execution: arched back with the knife as high as he can reach, then all the way down
            },
            // 2026-11-21 (Yan: "make the human version the entire boss, elaborate more on the human scythe-moveset"): the Verger's war scythe,
            // two hands, the longest reach in the game. Boss.Move.Poses strings these; Melee.Shapes has the matching hit shape per pose.
            new[] {
                ((-160f, -2f, -8f, 11f), (100f, 3f, 2f, -11f)),      // 0 reap: the blade lifted over his head and brought down into the floor in front
                ((-130f, -4f, -1f, 6f), (-285f, 5f, -1f, -10f)),     // 1 sweep: from behind him, round in front at waist height
                ((165f, -2f, 3f, -2f), (-65f, 4f, -5f, 8f)),         // 2 rising: the blade from the floor, up through you
                ((40f, 6f, 0f, -10f), (-60f, -3f, -1f, 7f)),         // 3 hook: thrown out at arm's length, the beard hauled back toward him
                ((-130f, -3f, -1f, 8f), (-490f, 5f, -1f, -10f)),     // 4 danse: a full turn with the scythe at the end of his arms, all round him
                ((-110f, -4f, 4f, 3f), (-275f, 5f, 5f, -6f)),        // 5 ankle reap: bent low, the blade skims the floor at your feet
                ((-285f, 5f, -1f, -6f), (-130f, -4f, -1f, 8f)),      // 6 backhand: the sweep come back the other way
                ((-200f, -1f, -10f, 13f), (-240f, 5f, 3f, -14f)),    // 7 impale: the point raised straight up, then driven down through you
            },
        };
        public const string VisorPoses = "@Visor";             // Melee.Charge.PoseKey: his poses instead of his knife's
        public const string VergerPoses = "@Verger";           // 2026-11-21: the Verger's scythe poses
        public static int MoveCount(Style st) => Moves[(int)st].Length;
        public static (float off, float hx, float hy, float lean) Stance(Style st) => Stances[(int)st];

        /// <summary>The sweeps: the weapon turns flat round in front of you, so mid-strike it points at the camera.</summary>
        public const float ArmArc = 3f, HandTurn = 0.6f;
        // Melee 4 (Yan: "his combos still kind of blend into one another"): the strike is 0.08 s, too quick to read one swing from
        // the next, so it leaves a smear - ghosts of the blade where it was SmearStep, 2x, 3x... s ago - through the strike and
        // the start of the follow-through. Read from the pose math at those times, so it's the same at any frame rate.
        public const float SmearStep = 0.025f; public const int SmearCount = 4;
        public static bool Striking(float sinceSwing, float swingHold) => swingHold < 0f && sinceSwing >= WindUp && sinceSwing < WindUp + ChopTime + Follow * 0.6f;   // px the fist travels on its arc per radian-ish of swing; how much of the turn the wrist takes
        public static bool Sweep(Style st, int move) => st == Style.Verger ? Wrap(st, move) is 1 or 4 or 5 or 6 : st != Style.Knife && Wrap(st, move) == 1;
        static int Wrap(Style st, int move) => ((move % MoveCount(st)) + MoveCount(st)) % MoveCount(st);

        /// <summary>A swing at `t` s of move `move` (any number: it wraps) for this style; outside a swing, the stance. In a combo
        /// (`from` = how far into the last swing this one began, -1 = not in one) the wind-up starts where the last swing was, so
        /// the swings flow into each other instead of going back to the stance between them.</summary>
        public static (float off, float hx, float hy, float lean) Chop(float t, Style st = Style.Chop, int move = 0, float from = -1f)
        {
            var s = Stances[(int)st]; var m = Moves[(int)st][Wrap(st, move)];
            if (t < 0f || t >= SwingLength) return s;
            var start = from >= 0f ? Chop(from, st, move - 1) : s;
            float Sm(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
            (float, float, float, float) L((float off, float hx, float hy, float lean) p, (float off, float hx, float hy, float lean) q, float k, bool near)
            {
                float qo = near ? p.off + Mathf.DeltaAngle(p.off, q.off) : q.off;          // to and from the stance: the short way round,
                if (near && Through(p.off, qo, 155f)) qo += qo > p.off ? -360f : 360f;     // unless that drops it through your feet: then over the top
                return (Mathf.Lerp(p.off, qo, k), Mathf.Lerp(p.hx, q.hx, k), Mathf.Lerp(p.hy, q.hy, k), Mathf.Lerp(p.lean, q.lean, k));
            }
            if (t < WindUp) return L(start, m.a, Sm(t / WindUp), true);
            t -= WindUp;
            if (t < ChopTime) return L(m.a, m.b, t / ChopTime, false);                    // the strike (the hit lands at its end)
            t -= ChopTime;
            if (t < Follow) return m.b;
            return L(m.b, s, Sm((t - Follow) / Recover), true);
        }

        static bool Through(float from, float to, float off)                             // does turning from -> to pass `off` (mod 360)?
        {
            float lo = Mathf.Min(from, to), hi = Mathf.Max(from, to), k = Mathf.Ceil((lo - off) / 360f);
            return off + 360f * k <= hi;
        }

        /// <summary>RT 7e: lifting it over your head to throw (held `t` s): the head goes back and down behind you, the fist up by
        /// your head; charged, it trembles. Then the whip forward over `w` s after release.</summary>
        public const float LiftTime = 0.25f, WhipTime = 0.12f;
        public static (float off, float hx, float hy, float lean) Lift(float t)
        {
            float Sm(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
            float k = Sm(t / LiftTime), shake = t > LiftTime ? 2.5f * Mathf.Sin(t * 33f) : 0f;
            return (-150f * k + shake, -2f * k, -10f * k, 8f * k);
        }
        public static (float off, float hx, float hy, float lean) Whip(float t)
        {
            float c = Mathf.Clamp01(t / WhipTime);
            return (-150f + 220f * c, -2f + 5f * c, -10f + 10f * c, 8f - 18f * c);
        }

        // Melee 2d: the parry guard (the weapon up and across in front of you, for the parry window) and a broken guard (Yan: "the
        // perfect parries need a visual indication, the enemy hand could go UP simulating their guard was broken": the weapon arm
        // knocked up and back, the body rocked back, for the stagger, then it comes back down)
        public const float GuardTime = 0.35f, BrokenTime = 1f, BrokenRise = 0.12f;
        static readonly (float off, float hx, float hy, float lean) Guard = (-95f, 1f, -4f, 2f), Broken = (-60f, -2f, -7f, 12f);
        static float Hold(float t, float len, float rise, float fall) => t < 0f || t >= len ? 0f : Mathf.Min(1f, Mathf.Min(t / rise, (len - t) / fall));

        static HeldFrame MeleeFrame(float angle, float sinceSwing, float swingHold, float throwHold, float sinceThrow, string key, int move, float from,
            float sinceGuard = 99f, float sinceBroken = 99f, float open = 0f)
        {
            bool left = Mathf.Cos(angle * Mathf.Deg2Rad) < -0.0001f;
            float side = left ? -1f : 1f;
            var st = StyleOf(key); var stance = Stance(st);
            var (off, hx, hy, lean) = throwHold >= 0f ? Blend(stance, Lift(throwHold), Mathf.Clamp01(throwHold / LiftTime))
                : sinceThrow < WhipTime ? Whip(sinceThrow)
                : swingHold >= 0f ? Chop(Mathf.Min(swingHold, WindUp), st, move, from)
                : Chop(sinceSwing, st, move, from);
            if (throwHold < 0f && sinceThrow >= WhipTime && swingHold > WindUp) off += 3f * Mathf.Sin(swingHold * 30f);   // a charged swing trembles
            // Yan: "make guard breaks create a slowdown effect like the dive, so that the player sees the enemy getting their hands up
            // slower": the rise plays inside the break's slow motion (Boss.ParryMoment, 0.25x for a real second); "his hand goes up
            // almost at the end of the slowdown": eased out - it's knocked up at once and slows into the top - over BrokenRise (about
            // half a real second in the slow motion), then held there
            float gk = Hold(sinceGuard, GuardTime, 0.04f, 0.12f), bk = Hold(sinceBroken, BrokenTime, BrokenRise, 0.3f);
            if (sinceBroken < BrokenRise) bk = 1f - (1f - bk) * (1f - bk) * (1f - bk);
            if (gk > 0f) (off, hx, hy, lean) = Blend((off, hx, hy, lean), Guard, gk);
            if (open > 0f) (off, hx, hy, lean) = Blend((off, hx, hy, lean), (stance.off + 25f, stance.hx - 1f, stance.hy + 2.5f + 0.6f * Mathf.Sin(Time.time * 7f), 0f), open);   // Visor 5: the weapon sinks, the shoulders heave
            if (bk > 0f) (off, hx, hy, lean) = Blend((off, hx, hy, lean), (Broken.off + 6f * Mathf.Sin(sinceBroken * 25f) * (1f - sinceBroken), Broken.hx, Broken.hy, Broken.lean), bk);   // it shudders
            // Melee 2 (Yan: "the hand and angle stay the same ... they need to rotate as well with the swing"): in a swing the fist
            // rides an arc round the shoulder with the weapon (the arm swinging, not just the wrist) and turns with it; at the stance
            // the arc adds nothing
            float turn = 0f;
            if (throwHold < 0f && sinceThrow >= WhipTime)
            {
                float a = (65f - off) * Mathf.Deg2Rad, a0 = (65f - stance.off) * Mathf.Deg2Rad;   // the weapon's angle facing right, now and at the stance
                hx += ArmArc * (Mathf.Cos(a) - Mathf.Cos(a0)); hy -= ArmArc * (Mathf.Sin(a) - Mathf.Sin(a0));
                turn = Mathf.DeltaAngle(stance.off, off);
            }
            float right = ChestX + 3f;                                                   // the fist beside the body
            float gx = (left ? 13f - right : right) + side * Mathf.Round(hx);
            float gy = Mathf.Min(Mathf.Round(ChestY + 0.5f + hy), WaistTopRow + 2f);   // down to the hip at most (a lifted fist goes up past the head)
            float cx = left ? 1f : 2f, deg = 90f - side * (RestTilt + off);
            // the other hand: on the handle above the fist with a two-handed weapon; otherwise up in a guard in front of the chest,
            // pulled back as the weapon hand goes out (none while throwing - it's up there with the weapon)
            float sx, sy; bool sup = throwHold < 0f && sinceThrow >= WhipTime;
            if (st == Style.Heavy || st == Style.Verger) { float r = deg * Mathf.Deg2Rad; sx = gx + cx + 4f * Mathf.Cos(r); sy = gy + 1.5f - 4f * Mathf.Sin(r); }
            else { float fx = ChestX + 3f - 0.3f * hx; sx = (left ? 13f - fx : fx) + cx; sy = ChestY - 1.5f - (st == Style.Knife ? 0f : 1f) + 0.5f * hy; }   // out in front of the chest
            return new HeldFrame
            {
                Body = BodyPose(sinceSwing, swingHold, throwHold, sinceThrow, gk, sinceBroken, left),
                Left = left, HasSupport = sup, MagShown = false, SupX = Mathf.Round(sx), SupY = Mathf.Round(sy - 0.5f) + 0.5f,
                GunDeg = deg,                                                            // edge forward (a flipped sprite mirrors it)
                HandDeg = -side * turn * HandTurn,
                GunX = gx + cx, GunY = gy + 1.5f, GripX = gx + cx, GripY = gy + 1.5f,
                Length = swingHold < 0f && throwHold < 0f && sinceSwing >= WindUp && sinceSwing < WindUp + ChopTime && Sweep(st, move)
                    ? 0.45f + 0.55f * Mathf.Abs(Mathf.Cos(deg * Mathf.Deg2Rad)) : 1f,   // a sweep foreshortens as it comes at the camera
                Lean = side * lean, Thrown = throwHold < 0f && sinceThrow >= 0f && sinceThrow < WhipTime && sinceThrow >= 0.5f * WhipTime,
            };
        }
        // ---- Use (Yan: "healing doesn't have an animation right now"): while you use an item (Rt.BeginUse .. UseDone) your weapon
        // dips and the free glove works with the item (Sprites/Items/use_<kind>.png, ArtSource/make_use_items.py): a bandage or
        // splint is wound round the arm, a medkit pressed to the chest, a stim jabbed into the thigh, a pill/ration/drink raised to
        // the mouth. t = 0..1 through the use. Returns the free hand's centre (character px, as SupX/SupY), the item's turn, and how
        // far the weapon is lowered (0..1). ----
        /// <summary>Where the glove holds each item, in its 16x16 sprite's pixels (y down): the medkit by its handle, the roll by
        /// one end, the syringe by its plunger, the can at its base - so the item shows past the glove.</summary>
        public static (float x, float y) UseGrip(string kind) =>
            kind == "medkit" ? (8f, 4.5f) : kind == "bandage" ? (4f, 8f) : kind == "syringe" ? (2f, 8f) : (8f, 11f);
        public static string UseKindOf(string id) =>
            id == null ? null : id.Contains("Medkit") ? "medkit" : id == "bandage" || id == "splint" ? "bandage" : id.Contains("Stim") ? "syringe" : "ration";
        public static (float x, float y, float deg, float lower) UsePose(string kind, float t, bool left)
        {
            float k = Mathf.Clamp01(Mathf.Min(t / 0.12f, (1f - t) / 0.12f));                  // in and out
            float hx = ChestX + 2f, hy = ChestY + 1.5f, deg = 0f;                                 // where the hand starts: at the hip
            switch (kind)
            {
                case "bandage": { float a = t * Mathf.PI * 2f * 3f; hx = ChestX + 1.5f + 1.6f * Mathf.Cos(a); hy = ChestY + 0.8f * Mathf.Sin(a); deg = 15f * Mathf.Sin(a); break; }
                case "medkit": { hx = ChestX + 1.5f; hy = ChestY - 0.5f + 0.6f * Mathf.Abs(Mathf.Sin(t * 22f)); deg = -10f; break; }
                case "syringe": { float jab = Mathf.Clamp01((t - 0.35f) / 0.06f); hx = ChestX + 2f - jab; hy = Mathf.Lerp(ChestY - 2.5f, ChestY + 2.5f, jab); deg = -70f; break; }
                default: { hx = ChestX + 1f; hy = ChestY - 4f + 0.4f * Mathf.Sin(t * 18f); deg = 35f; break; }   // up to the mask
            }
            float x = Mathf.Lerp(ChestX + 3f, hx, k), y = Mathf.Lerp(ChestY + 1.5f, hy, k);
            return (left ? 16f - x : x, y, left ? -deg : deg, k);
        }

        /// <summary>Use: the frame while using an item - the weapon dips, the free glove takes the item. Returns the item's turn.</summary>
        public static float ApplyUse(ref HeldFrame f, string kind, float t)
        {
            var (ux, uy, udeg, lower) = UsePose(kind, t, f.Left);
            f.GunDeg = Mathf.LerpAngle(f.GunDeg, f.Left ? 240f : -60f, lower); f.GunY += lower;
            f.HasSupport = true; f.SupX = Mathf.Round(ux); f.SupY = Mathf.Round(uy - 0.5f) + 0.5f;
            return udeg;
        }

        /// <summary>Empty hands: where the two gloves float (centres, character px) at a walk's `phase` (radians) and `swing` (0 still ..
        /// 1 walking): hanging fingers-down (HeldWeaponView turns them) a pixel clear of the torso at the hips, both swaying a pixel
        /// to one side then the other with the steps, snapped to pixels.</summary>
        public static (float rx, float ry, float lx, float ly) FreeHands(float phase, float swing, float run = 0f)
        {
            float a = Mathf.Sin(phase) * swing;
            float dx = Mathf.Round(1.2f * a);                       // Yan: "pointing down and swinging a little to the sides" - no bob
            dx = Mathf.Round((1.2f + 2.2f * run) * a);                  // 2026-11-23: at a run the swing is wider, and a PENDULUM (Yan's drawing): lowest in the middle, lifted at both ends of the swing - an arc, never a loop
            float lift = Mathf.Round(Mathf.Sin(phase) * Mathf.Sin(phase) * 2.2f * run);
            return (14f + dx, 11.5f - lift, 2f + dx, 11.5f - lift);
        }

        /// <summary>Melee 3: the body's frame through a swing - gathered (half crouch) on the wind-up, a charge, a throw's lift and
        /// the parry guard; the lunge's half and full frames on the strike and follow-through, half again as it recovers; rocked
        /// back (stagger) when the guard is broken. The facing ones lean toward where you aim.</summary>
        static string BodyPose(float sinceSwing, float swingHold, float throwHold, float sinceThrow, float guard, float sinceBroken, bool left)
        {
            string side = left ? "_l" : "_r";
            if (sinceBroken >= 0f && sinceBroken < BrokenTime) return (sinceBroken < BrokenRise * 0.5f || sinceBroken > BrokenTime - 0.25f ? "stagger_h" : "stagger") + side;
            if (throwHold >= 0f || swingHold >= 0f || guard > 0.5f) return "crouch_h";
            if (sinceThrow >= 0f && sinceThrow < WhipTime + Follow) return "lunge" + side;
            float t = sinceSwing;
            if (t < 0f || t >= SwingLength) return null;
            if (t < WindUp) return t < WindUp * 0.35f ? null : "crouch_h";
            if (t < WindUp + ChopTime) return "lunge_h" + side;
            if (t < WindUp + ChopTime + Follow) return "lunge" + side;
            return t < SwingLength - Recover * 0.5f ? "lunge_h" + side : null;
        }

        static (float, float, float, float) Blend((float off, float hx, float hy, float lean) p, (float off, float hx, float hy, float lean) q, float k) =>
            (Mathf.Lerp(p.off, q.off, k), Mathf.Lerp(p.hx, q.hx, k), Mathf.Lerp(p.hy, q.hy, k), Mathf.Lerp(p.lean, q.lean, k));

        /// <summary>RT 6f: everything HeldWeaponView.Show draws for a held gun (moved here from it so the harness can render it):
        /// aim, then recoil (k: 0..1 left of the last shot's kick), swing, and the reload or jam-clearing pose at progress r.
        /// g = WeaponArt grip/support/muzzle points.</summary>
        /// <summary>How far (character px) a revolver's cylinder swings out from the frame: Yan circled the limit on a render, 2026-11-16 (it was 2.5, a visible gap; now it sits just beside the window).</summary>
        public const float CylinderOut = 2.0f;

        public static HeldFrame Frame(string cat, float[] g, float angle, float k, float climb, float kick, float sinceSwing,
            float r, string style, int trips, float[] mag = null, float swingHold = -1f, float throwHold = -1f, float sinceThrow = 99f,
            string key = null, int move = 0, float from = -1f, float sinceGuard = 99f, float sinceBroken = 99f, float open = 0f)
        {
            if (cat == "melee") return MeleeFrame(angle, sinceSwing, swingHold, throwHold, sinceThrow, key, move, from, sinceGuard, sinceBroken, open);   // RT 7c, 7e, Melee 2
            float bs = WeaponArt.Boost(cat);                                  // 2026-11-18: a bigger long gun: every point on it is bs times farther from the grip
            if (bs != 1f)
            {
                float g0 = g[0], g1 = g[1];
                g = new[] { g0, g1, g[2] + (g[2] - g0) * (bs - 1f), g[3] + (g[3] - g1) * (bs - 1f), g[4] + (g[4] - g0) * (bs - 1f), g[5] + (g[5] - g1) * (bs - 1f) };   // (NaN stays NaN)
                if (mag != null) mag = mag.Select((v, i) => i % 2 == 0 ? g0 + (v - g0) * bs : g1 + (v - g1) * bs).ToArray();
            }
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
            float partOpen = 0f, clipIn = 0f, clipBack = 0f;                             // the Sperling's reload: the bolt flies open, the clip is pushed home and stripped, the bolt closes
            if (style == "clip" && r >= 0f && r < 1f)
            {
                float Sm(float q) { q = Mathf.Clamp01(q); return q * q * (3f - 2f * q); }
                partOpen = r < 0.08f ? 0f : r < 0.18f ? Sm((r - 0.08f) / 0.10f) : r < 0.93f ? 1f : r < 0.99f ? 1f - Sm((r - 0.93f) / 0.06f) : 0f;
                clipIn = Sm((r - 0.88f) / 0.08f); clipBack = Sm((r - 0.955f) / 0.045f);
            }
            if (style == "clip" && mag != null && pose.HasSupport)                 // a stripper clip: the hand's home is above the clip's seat on the receiver (it holds the clip by its top and pushes it down)
            {
                float ma0 = (angle + extra) * Mathf.Deg2Rad, ca0 = Mathf.Cos(ma0), sa0 = Mathf.Sin(ma0), fl0 = left ? -1f : 1f;
                float vx0 = (mag[0] - g[0]) * 0.5f, vy0 = (mag[1] - 3.5f + 6.0f * clipIn - g[1]) * 0.5f * fl0;      // the hand holds the clip by its top and pushes it down 6 px
                float sxc = Mathf.Round(pose.GripX + vx0 * ca0 + vy0 * sa0), syc = Mathf.Round(pose.GripY + (-vx0 * sa0 + vy0 * ca0));
                pose.SupportX = Mathf.Lerp(sxc, pose.SupportX, clipBack); pose.SupportY = Mathf.Lerp(syc, pose.SupportY, clipBack);                 // then it goes back to the gun
            }
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
                var dn = Rot(0f, key != null && WeaponArt.TopMags.Contains(key) ? -fl : fl);   // the gun's underside (a pan magazine sits on top: the other way)
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
                if (r >= 0.85f && !float.IsNaN(g[2]) && (style == "mag" || style == "belt"))   // a long gun: the support hand reaches back to the charging handle, drags it and lets it go (2026-11-16, Yan: "chambering a round at the end of a reload")
                {
                    float rx0 = Mathf.Lerp(homeX, gunX, 0.6f), ry0 = Mathf.Lerp(homeY, gunY, 0.6f); var back = Rot(-3f, 0f);
                    if (r < 0.90f) hand = L(holdX, holdY, rx0, ry0, (r - 0.85f) / 0.05f);
                    else if (r < 0.94f) hand = L(rx0, ry0, rx0 + back.x, ry0 + back.y, (r - 0.90f) / 0.04f);
                    else if (r < 0.97f) hand = L(rx0 + back.x, ry0 + back.y, rx0, ry0, (r - 0.94f) / 0.03f);
                    else hand = L(rx0, ry0, homeX, homeY, (r - 0.97f) / 0.03f);
                }
                supX = Mathf.Round(hand.hx); supY = Mathf.Round(hand.hy); handMoved = true;
                dropped = r < 0.5f;
                magShown = r >= 0.5f;
                if (r >= 0.5f && r < 0.85f) { magDX = hand.hx - holdX; magDY = hand.hy - holdY; }   // riding in the hand
            }
            // RT 6l: a revolver's cylinder swings out under the frame for the whole reload and snaps back in at the end
            float cylDX = 0f, cylDY = 0f;
            if (style == "cylinder" && r >= 0f && r < 1f)
            {
                float o = Mathf.Clamp01(Mathf.Min((r - 0.03f) / 0.09f, (0.96f - r) / 0.04f)) * CylinderOut, ca2 = (angle + extra) * Mathf.Deg2Rad;
                float vy = (left ? -1f : 1f) * o;                                           // along the gun's underside
                cylDX = vy * Mathf.Sin(ca2); cylDY = vy * Mathf.Cos(ca2);
            }
            bool shellHand = false;                                                         // a tube reload: the hand carries one round at a time from the pouch back to the gun (Reload: u > 0.55 is the way back)
            if ((style == "tube" || style == "clip") && r >= 0f && r < 1f && pose.HasSupport)
            {
                float inner2 = Mathf.Clamp01((r - 0.1f) / 0.8f);
                if (inner2 > 0f && inner2 < 1f) { float u2 = inner2 * Mathf.Max(1, style == "clip" ? 1 : trips) % 1f; shellHand = u2 >= 0.57f && u2 < 0.98f; }
            }
            if (style == "clip") shellHand = pose.HasSupport && r >= 0.556f && r < 0.965f;       // the clip is in view from the pouch until it has gone in
            // RT 6i: a break-action drops its barrels open, holds them while the shells go in, snaps them shut
            float breakDeg = style == "break" && r >= 0f && r < 1f ? 35f * Mathf.Clamp01(Mathf.Min((r - 0.05f) / 0.12f, (0.92f - r) / 0.07f)) : 0f;
            return new HeldFrame
            {
                CylDX = cylDX, CylDY = cylDY, MagDX = magDX, MagDY = magDY, MagShown = magShown, MagDropped = dropped, BreakDeg = breakDeg, ShellInHand = shellHand, ClipIn = clipIn, PartOpen = partOpen,
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
        /// <summary>2026-11-18 (Yan: "any weapon that's not a sidearm is barely visible if you zoom out a little, increase their size by 15 to 20%"): long guns (everything but sidearms and melee) are drawn
        /// this much bigger. The grip stays where it is (the sprites turn about it); the support hand, the magazine well and everything laid out along the gun scale with it (HeldPoseMath.Frame, HeldWeaponView).</summary>
        public const float LongGunBoost = 1.17f;
        public static float Boost(string cat) => cat == null || cat == "sidearm" || cat == "melee" ? 1f : LongGunBoost;
        /// <summary>2026-11-13 (Yan: use the big menu picture in the hand, "making everything bigger to compensate"): a gun listed here is held with its MENU picture (weapon_&lt;Key&gt;_hd) drawn k times denser, so it shows at the
        /// same on-screen size as before. Its grip/support/muzzle entries below stay in "virtual" pixels (texture pixels / k, the old 32-a-unit pixels), so all the pose maths and the muzzle offset need no change; only the sprite
        /// (pivot and pixels-per-unit) is made at k. No cut pieces (magazine, cylinder) for it yet: its reload is the hands alone.</summary>
        public static readonly Dictionary<string, float> Dense = new Dictionary<string, float>
        {
            // 2026-11-23 (Yan: "replace the current mini-sprites with the inventory ones ... reducing the size of the png and placing it in the characters hands"): the melee weapons are drawn from their inventory pictures too (weapon_<Key>_hd.png), k = the picture's content width / the old in-hand picture's (the same size on screen as before); grip and cutting point re-mapped by position on the weapon (ArtSource/weapons/melee_to_hd.py). The Tactical Hatchet stays Yan's own drawing.
            ["TacticalHatchet"] = 1.91f, ["HuntingKnife"] = 3.41f, ["SpetsnazSwitchblade"] = 3.47f, ["SteppeMachete"] = 2.00f, ["SerratedTrenchKnife"] = 2.86f, ["ObeliskRitualBlade"] = 2.54f, ["Hatchet"] = 2.77f, ["WoodenBaseballBat"] = 2.03f, ["Hammer"] = 2.59f, ["FireAxe"] = 2.07f, ["Sledgehammer"] = 1.94f, ["BarbedWireBat"] = 2.03f,   // k = the double-size picture's content width / the old in-hand picture's width (ArtSource/weapons/make_gun_x2.py prints the line; rules: ArtSource/weapons/WEAPON_SPRITE_RULES.md)
            ["RinaldiR6"] = 7.09f, ["HaldenLeverShotgun"] = 6.32f, ["FrontierLeverRifle"] = 6.00f, ["Nightwalker4570"] = 6.00f,
            ["SperlingS96"] = 5.00f, ["Halden45Service"] = 5.71f, ["HaldenServiceRevolver"] = 5.26f, ["WaldenP9"] = 5.38f,
            ["VK74"] = 5.79f, ["RangerP8"] = 5.95f, ["SKNKarabin"] = 5.67f, ["CorvanWasp9"] = 6.91f,
            ["G7RailPrototype"] = 5.50f,
            ["BlackHarrier"] = 5.78f,
            ["PSh18Pchela"] = 4.80f,
            ["SR21Gadyuka"] = 5.20f,
            ["Smerch3"] = 6.21f,
            ["PL74PPolovetsZulu"] = 5.13f,
            ["PL74PPolovets"] = 6.16f,
            ["Corvan57"] = 4.14f,
            ["BR86Bulldog"] = 6.54f,
            ["Sever98"] = 6.64f,
            ["PPZZubr"] = 7.97f,
            ["TR30"] = 6.05f,
            ["VKS74U"] = 6.85f,
            ["PL62Kobza"] = 5.46f,
            ["Obrez66"] = 6.65f,
            ["RK12Kuvalda"] = 7.17f,
            ["VK74Obelisk"] = 5.74f,
            ["Hound13"] = 6.00f,
            ["SP14Warden"] = 6.00f,
            ["HaldenHR36K"] = 6.49f,
            ["M91Trekhlineyka"] = 5.22f,
            ["Kaban12"] = 6.03f,
            ["Karusel5"] = 6.61f,
            ["VK127Grom"] = 5.41f,
            ["SteppeEagle"] = 6.26f,
            ["Tambour12"] = 4.97f,
            ["VSNShepot"] = 4.75f,
            ["Sayan12"] = 4.71f,
            ["TS9Tishina"] = 6.58f,
            ["CorvanHornet45"] = 6.94f,
            ["VosaVz62"] = 8.92f,
            ["Sever34"] = 5.55f,
            ["HaldenAM50Sledge"] = 4.88f,
            ["SV2Strela"] = 5.09f,
            ["CorvanMarksman"] = 5.27f,
            ["CorvanLMG"] = 5.49f,
            ["PL74"] = 5.31f,
            ["VK103"] = 5.55f,
            ["CorvanBullpup"] = 6.00f,
            ["RL44"] = 5.52f,
            ["NA94Yenisei"] = 5.74f,
            ["VKM"] = 5.76f,
            ["CorvanBattleRifle"] = 5.64f, ["BayouPump"] = 5.90f, ["CorvanGnatPDW"] = 6.79f,
        };
        // 2026-11-23 (Yan: "the scythe looked better in the mini version ... make it bigger in hand" / "make all melees a bit bigger and match their reach and hitbox to it"): every melee weapon is drawn 1.15x in the hand (the scythe 1.4x), and Melee.Shape scales its reach by the same factor; the Verger's scythe is its mini picture again (blade straightened: ArtSource/weapons/scythe_straighten.py), drawn bigger in the hand: the
        // sprite and its cutting point scale about the grip, so the hand stays on the snath.
        public static readonly Dictionary<string, float> HandScale = new Dictionary<string, float> { ["HuntingKnife"] = 1.15f, ["SpetsnazSwitchblade"] = 1.15f, ["SteppeMachete"] = 1.15f, ["SerratedTrenchKnife"] = 1.15f, ["ObeliskRitualBlade"] = 1.15f, ["Hatchet"] = 1.15f, ["WoodenBaseballBat"] = 1.15f, ["Hammer"] = 1.15f, ["FireAxe"] = 1.15f, ["Sledgehammer"] = 1.15f, ["BarbedWireBat"] = 1.15f, ["TacticalHatchet"] = 1.15f, ["VergerScythe"] = 1.4f };
        public static float Scale(string key) => key != null && HandScale.TryGetValue(key, out var k) ? k : 1f;
        public static float Density(string key) => key != null && Dense.TryGetValue(key, out var k) ? k : 1f;
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
            // RT 7v: the melee set pixelled by hand from Yan's references (ArtSource/weapons/hand/melee_px.py), on 32x16 canvases
            ["HuntingKnife"] = new[] { 5.5f, 4.5f, float.NaN, float.NaN, 17f, 4.5f },
            ["SpetsnazSwitchblade"] = new[] { 5f, 4.5f, float.NaN, float.NaN, 16.5f, 4.5f },
            ["SteppeMachete"] = new[] { 5f, 8f, float.NaN, float.NaN, 30.5f, 6.5f },
            ["SerratedTrenchKnife"] = new[] { 5f, 4.5f, float.NaN, float.NaN, 21.5f, 5f },
            ["ObeliskRitualBlade"] = new[] { 4.5f, 6.5f, float.NaN, float.NaN, 23.5f, 6f },
            ["Hatchet"] = new[] { 6f, 4.5f, float.NaN, float.NaN, 21f, 8.5f },
            ["FireAxe"] = new[] { 5f, 7.5f, float.NaN, float.NaN, 29f, 12.5f },
            ["Hammer"] = new[] { 6.5f, 5.5f, float.NaN, float.NaN, 19.5f, 9f },
            ["Sledgehammer"] = new[] { 5f, 8f, float.NaN, float.NaN, 28.5f, 12f },
            ["WoodenBaseballBat"] = new[] { 5f, 7.5f, float.NaN, float.NaN, 29f, 7.5f },
            ["VergerClaws"] = new[] { 5f, 8f, float.NaN, float.NaN, 14f, 8f },   // 2026-11-20: bare claws, no weapon sprite: the grip only keeps the swing poses working
            ["ExcavatorBucket"] = new[] { 5f, 8f, float.NaN, float.NaN, 14f, 8f },   // 2026-11-21: drawn by its own clips
            ["OrderlySaw"] = new[] { 5f, 8f, float.NaN, float.NaN, 14f, 8f },        // 2026-11-21: drawn by his own clips
            ["SpecimenLegs"] = new[] { 5f, 8f, float.NaN, float.NaN, 14f, 8f },  // 2026-11-21: the Specimen, grown: drawn by its own clips
            ["FerrymanHook"] = new[] { 5f, 8f, float.NaN, float.NaN, 14f, 8f },  // 2026-11-21: the Ferryman: drawn by his own clips
            ["SingerHands"] = new[] { 5f, 8f, float.NaN, float.NaN, 14f, 8f },   // 2026-11-21: the Singer: no weapon sprite
            ["MotherJaws"] = new[] { 5f, 8f, float.NaN, float.NaN, 14f, 8f },    // 2026-11-21: Mother Hollow's bite: no weapon sprite, the grip keeps the swing code working
            ["VergerScythe"] = new[] { 7f, 15f, float.NaN, float.NaN, 24f, 6.5f },   // 2026-11-20: the grip on the wrap of the low snath, the cutting point mid-blade (a bigger, straighter blade: 36x20 sprite)
            ["BarbedWireBat"] = new[] { 5f, 7.5f, float.NaN, float.NaN, 29f, 7.5f },
            ["TacticalHatchet"] = new[] { 10f, 7f, float.NaN, float.NaN, 26f, 11.5f },   // RT 7c: fist low on the handle, a little handle showing below it   // RT 7a: Yan's drawing (tactical_hatchet.py); "muzzle" = the blade's top corner
            ["PSh18Pchela"] = new[] { 6.3f, 10.3f, float.NaN, float.NaN, 19.8f, 3.5f },
            ["WaldenP9"] = new[] { 5.7f, 10.5f, float.NaN, float.NaN, 21.7f, 3.4f },
            ["Halden45Service"] = new[] { 5.2f, 10f, float.NaN, float.NaN, 21.7f, 3.2f },
            ["HaldenServiceRevolver"] = new[] { 4.3f, 10.1f, float.NaN, float.NaN, 23.8f, 2.3f },
            ["SR21Gadyuka"] = new[] { 4.8f, 9.6f, float.NaN, float.NaN, 19.8f, 3.7f },
            ["BlackHarrier"] = new[] { 5.2f, 9.3f, float.NaN, float.NaN, 22.7f, 5.2f },
            ["Corvan57"] = new[] { 5.5f, 10.9f, float.NaN, float.NaN, 21f, 4.5f },
            ["RinaldiR6"] = new[] { 4.1f, 7.4f, float.NaN, float.NaN, 22.6f, 3.2f },   // virtual px of the double-size picture (see Dense): the grip in the glove, the low bore at the muzzle
            ["SteppeEagle"] = new[] { 5.1f, 9.3f, float.NaN, float.NaN, 23.6f, 2.7f },
            ["VosaVz62"] = new[] { 10.9f, 6.2f, float.NaN, float.NaN, 24.4f, 2.8f },
            ["PPZZubr"] = new[] { 9.8f, 7f, 19f, 5.2f, 29.5f, 3.5f },
            ["CorvanWasp9"] = new[] { 14.2f, 9.6f, 26.5f, 5.4f, 32.6f, 4.7f },
            ["CorvanHornet45"] = new[] { 13f, 7.4f, 28.5f, 4.4f, 32.6f, 4.0f },
            ["CorvanGnatPDW"] = new[] { 17.6f, 7.2f, 25.3f, 6.2f, 29.6f, 3.6f },
            ["Obrez66"] = new[] { 4.8f, 4f, 17f, 2.6f, 25.6f, 2.1f },
            ["HaldenLeverShotgun"] = new[] { 11.4f, 5.5f, 25.6f, 2.6f, 38.6f, 1.3f },   // virtual px of the double-size picture
            ["Hound13"] = new[] { 12.7f, 4f, 28.5f, 2.7f, 40.7f, 2.0f },
            ["RangerP8"] = new[] { 12f, 5.2f, 29.5f, 3.6f, 41.7f, 1.5f },
            ["BayouPump"] = new[] { 12.2f, 4.1f, 29.8f, 2.9f, 42.7f, 2.0f },
            ["Sever34"] = new[] { 13.8f, 4.4f, 25f, 2.4f, 42.7f, 1.8f },
            ["Tambour12"] = new[] { 16.6f, 6f, 33.2f, 5f, 38.8f, 2.5f },
            ["SP14Warden"] = new[] { 12f, 4.7f, 28f, 4f, 40.7f, 3.2f },
            ["Sayan12"] = new[] { 15.2f, 7.2f, 27f, 3f, 41.8f, 2.4f },
            ["Kaban12"] = new[] { 11.9f, 5.8f, 27f, 3.3f, 40.6f, 2.9f },
            ["VKS74U"] = new[] { 12.4f, 7f, 26.5f, 3.7f, 33.6f, 3.7f },
            ["Smerch3"] = new[] { 13f, 5.9f, 24.5f, 2.9f, 28.6f, 3.5f },
            ["VK74"] = new[] { 14.2f, 9f, 28.5f, 4.4f, 42.7f, 3.9f },
            ["VKM"] = new[] { 14f, 7.2f, 28f, 3.7f, 42.7f, 3.4f },
            ["Karusel5"] = new[] { 11.4f, 4.6f, 23.5f, 2.2f, 36.6f, 1.2f },
            ["TS9Tishina"] = new[] { 13f, 6.6f, 27f, 2.2f, 38.6f, 2.0f },
            ["TR30"] = new[] { 11.7f, 7.5f, 26.5f, 4.6f, 39.7f, 4.1f },
            ["HaldenHR36K"] = new[] { 11.9f, 8.4f, 28.6f, 4.1f, 37.6f, 3.7f },
            ["VK103"] = new[] { 13.8f, 7.3f, 29f, 3.6f, 42.7f, 3.5f },
            ["BR86Bulldog"] = new[] { 15.2f, 9.6f, 26.5f, 5.9f, 36.6f, 6.1f },
            ["VK74Obelisk"] = new[] { 13.4f, 7.4f, 28.5f, 3.3f, 41.7f, 5f },
            ["NA94Yenisei"] = new[] { 13.2f, 8f, 28f, 4.3f, 42.7f, 3.9f },
            ["CorvanBullpup"] = new[] { 12f, 8.2f, 23f, 7.2f, 33.7f, 3.5f },
            ["CorvanBattleRifle"] = new[] { 13.6f, 7.2f, 28f, 4.2f, 44.7f, 4.1f },
            ["SKNKarabin"] = new[] { 13f, 5.6f, 28f, 4.1f, 43.7f, 2.2f },
            ["FrontierLeverRifle"] = new[] { 11.2f, 4.8f, 25.5f, 2.6f, 40.7f, 1.5f },   // virtual px of the double-size picture
            ["VSNShepot"] = new[] { 14.6f, 5.4f, 28f, 2.7f, 40.8f, 2.1f },
            ["M91Trekhlineyka"] = new[] { 13.3f, 5.7f, 30f, 3.2f, 46.7f, 2.9f },
            ["Nightwalker4570"] = new[] { 11.2f, 5f, 27f, 3.2f, 40.7f, 2.3f },   // virtual px of the double-size picture
            ["SperlingS96"] = new[] { 4.8f, 10.4f, float.NaN, float.NaN, 23.8f, 2.5f },
            ["CorvanMarksman"] = new[] { 12.5f, 7.4f, 28.8f, 7.4f, 44.8f, 2.2f },
            ["SV2Strela"] = new[] { 11.3f, 5.8f, 27.5f, 3.2f, 46.8f, 3.1f },
            ["Sever98"] = new[] { 13f, 5.6f, 23f, 3.3f, 43.6f, 4.8f },
            ["VK127Grom"] = new[] { 15.8f, 5.9f, 25.9f, 7f, 44.7f, 2.5f },
            ["HaldenAM50Sledge"] = new[] { 12.3f, 7.5f, 29.8f, 8.2f, 48.8f, 3.1f },
            ["G7RailPrototype"] = new[] { 12.8f, 7f, 30f, 4.8f, 41.7f, 5.2f },
            ["CorvanLMG"] = new[] { 14f, 10.8f, 28f, 7.7f, 43.7f, 5.6f },
            ["RL44"] = new[] { 12.3f, 7f, 25f, 4.6f, 44.7f, 2.2f },
            ["PL62Kobza"] = new[] { 15f, 7.4f, 31f, 5.5f, 45.7f, 3.6f },   // Batch 113
            ["PL74"] = new[] { 13.2f, 8f, 34.3f, 8.2f, 45.8f, 3.5f },
            ["PL74PPolovets"] = new[] { 13f, 7.2f, 29.5f, 4.2f, 44.6f, 4.6f },
            ["PL74PPolovetsZulu"] = new[] { 12.8f, 8.3f, 30f, 5.2f, 44.8f, 5.6f },
            ["RK12Kuvalda"] = new[] { 4.4f, 8.8f, float.NaN, float.NaN, 23.6f, 5f },
        };
        /// <summary>Guns loaded with a stripper clip from the top (Sperling S-96): WeaponMags.Points holds the clip's resting place on the gun (centre x, y in virtual px; y negative = above the picture) and the support hand brings it there.</summary>
        public static readonly HashSet<string> Clips = new HashSet<string> { "SperlingS96" };
        /// <summary>Guns whose magazine sits on TOP (a pan magazine: the PL-74P Zulu): the reload moves it up and away, not down.</summary>
        public static readonly HashSet<string> TopMags = new HashSet<string> { "PL74PPolovetsZulu" };
        public static bool HasGrip(string key) => key != null && Grips.ContainsKey(key);
        /// <summary>The grip/support/muzzle points of a weapon's own sprite (its category's when it has none).</summary>
        public static float[] GripOf(string cat, string key) => key != null && Grips.TryGetValue(key, out var kg) ? kg : Grips.TryGetValue("cat_" + cat, out var cg) ? cg : Grips["cat_assault"];
        private static readonly Dictionary<string, (Sprite, float[])> Cache = new Dictionary<string, (Sprite, float[])>();
        private static Dictionary<WeaponDef, (string cat, string key)> _index;
        // Batch 29: the project enters Play mode without a domain reload, so statics survive between
        // runs while the sprites/textures they point at are destroyed - clear the caches each run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches() { Cache.Clear(); MagCache.Clear(); BreakCache.Clear(); LooseCache.Clear(); CylCache.Clear(); }


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
            if (Density(key) > 1f)                                                       // 2026-11-16: the double-size guns - body without the magazine (none for a pistol: its magazine is hidden in the grip) and the magazine, same canvas and pivot as the picture
            {
                float dense = Density(key);
                Sprite LoadHd(string part)
                {
                    var tx = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_hd" + part);
                    if (tx == null) return null;
                    tx.filterMode = FilterMode.Bilinear;
                    return Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(g[0] * dense / tx.width, 1f - g[1] * dense / tx.height), PixelsPerUnit * dense, 0, SpriteMeshType.FullRect);
                }
                return MagCache[key] = (LoadHd("body"), LoadHd("mag"));
            }
            Sprite Load(string part)
            {
                var tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_" + part);
                if (tex == null) return null;
                tex.filterMode = FilterMode.Point;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(g[0] / tex.width, 1f - g[1] / tex.height), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
            return MagCache[key] = (Load("body"), Load("mag"));
        }
        private static readonly Dictionary<string, (Sprite, Sprite)> MagCache = new Dictionary<string, (Sprite, Sprite)>();

        /// <summary>RT 6l: a revolver's frame (with an empty window) and its cylinder, same size and pivot as the gun; nulls otherwise.</summary>
        public static (Sprite body, Sprite cyl) Cylinder(string cat, string key)
        {
            if (key == null) return (null, null);
            if (CylCache.TryGetValue(key, out var hit)) return hit;
            if (Density(key) > 1f)                                                       // 2026-11-16: the double-size revolvers - the frame with an empty window and the cylinder (ArtSource/weapons/mags_hd.py)
            {
                float dense = Density(key); var gh = For(cat, key).grip;
                Sprite LoadHd(string part)
                {
                    var tx = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_hd" + part);
                    if (tx == null) return null;
                    tx.filterMode = FilterMode.Bilinear;
                    return Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(gh[0] * dense / tx.width, 1f - gh[1] * dense / tx.height), PixelsPerUnit * dense, 0, SpriteMeshType.FullRect);
                }
                var hc = LoadHd("cyl");
                return CylCache[key] = hc == null ? (null, null) : (LoadHd("body"), hc);
            }
            var cyl = Part(cat, key, "cyl");
            return CylCache[key] = cyl == null ? (null, null) : (Part(cat, key, "body"), cyl);
        }
        private static readonly Dictionary<string, (Sprite, Sprite)> CylCache = new Dictionary<string, (Sprite, Sprite)>();
        private static Sprite Part(string cat, string key, string part)
        {
            var g = For(cat, key).grip;
            var tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_" + part);
            if (tex == null) return null;
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(g[0] / tex.width, 1f - g[1] / tex.height), PixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        /// <summary>RT 6j: the magazine as a loose object - its sprite turned round its own middle (WeaponMags.Points).</summary>
        public static Sprite LooseMag(string key)
        {
            if (key == null || !WeaponMags.Points.TryGetValue(key, out var mp)) return null;
            if (LooseCache.TryGetValue(key, out var hit)) return hit;
            float dn = Density(key);                                                      // the double-size guns: their own magazine picture (texture px = virtual px x density)
            var tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + (dn > 1f ? "_hdmag" : "_mag"));
            if (tex == null) return LooseCache[key] = null;
            tex.filterMode = dn > 1f ? FilterMode.Bilinear : FilterMode.Point;
            return LooseCache[key] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(mp[2] * dn / tex.width, 1f - mp[3] * dn / tex.height), PixelsPerUnit * dn, 0, SpriteMeshType.FullRect);
        }
        private static readonly Dictionary<string, Sprite> LooseCache = new Dictionary<string, Sprite>();

        /// <summary>RT 6i: a break-action split at its hinge (ArtSource/weapons/mags.py BREAK): the stock and action, the barrels
        /// pivoting on the hinge, and the hinge in weapon-sprite pixels. Nulls for every other gun.</summary>
        public static (Sprite body, Sprite barrels, float hx, float hy) Break(string cat, string key)
        {
            if (key == null || !Hinges.TryGetValue(key, out var h)) return (null, null, 0f, 0f);
            if (BreakCache.TryGetValue(key, out var hit)) return hit;
            var g = For(cat, key).grip;
            if (Density(key) > 1f)                                                       // 2026-11-16: the double-size break actions - the body and the barrels (with the fore-end), the barrels' pivot on the hinge (texture px = virtual px x density)
            {
                float dense = Density(key);
                Sprite LoadHd(string part, float px, float py)
                {
                    var tx = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_hd" + part);
                    if (tx == null) return null;
                    tx.filterMode = FilterMode.Bilinear;
                    return Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(px / tx.width, 1f - py / tx.height), PixelsPerUnit * dense, 0, SpriteMeshType.FullRect);
                }
                var bars = LoadHd("barrels", h.x * dense, h.y * dense);
                return BreakCache[key] = bars == null ? (null, null, 0f, 0f) : (LoadHd("body", g[0] * dense, g[1] * dense), bars, h.x, h.y);
            }
            Sprite Load(string part, float px, float py)
            {
                var tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_" + part);
                if (tex == null) return null;
                tex.filterMode = FilterMode.Point;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(px / tex.width, 1f - py / tex.height), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            }
            return BreakCache[key] = (Load("body", g[0], g[1]), Load("barrels", h.x, h.y), h.x, h.y);
        }
        public static readonly Dictionary<string, (float x, float y)> Hinges = new Dictionary<string, (float, float)> { ["Obrez66"] = (9.8f, 3.6f), ["Sever34"] = (19.3f, 3.4f) };   // virtual px of the double-size pictures (mags_hd.py BRK)
        private static readonly Dictionary<string, (Sprite, Sprite, float, float)> BreakCache = new Dictionary<string, (Sprite, Sprite, float, float)>();

        /// <summary>2026-11-15 (Yan: "test it your way first, no spritecook"): a moving part cut out of a double-size picture and moved in code (ArtSource/weapons/cut_parts.py, gun_parts.json): the pump fore-end, a slide, a bolt.
        /// style: "pump" (back after the shot, then forward) | "slide" (kicks back at the shot, snaps forward); travel in TEXTURE pixels. The body (the picture with the part's place filled) and the part have the picture's size and pivot.</summary>
        public static readonly Dictionary<string, (string style, float travel)> PartInfo = new Dictionary<string, (string, float)>
        {
            ["RangerP8"] = ("pump", 26f),
            ["Halden45Service"] = ("slide", 12f),
            // levers: the whole lever (lever_arc.py: loop + thick root) swings DEGREES (positive = loop tip down and forward) about its hinge (HammerPivot)
            ["FrontierLeverRifle"] = ("lever", 34f), ["HaldenLeverShotgun"] = ("lever", 34f), ["Nightwalker4570"] = ("lever", 34f),   // 
            ["M91Trekhlineyka"] = ("bolt", 18f), ["Sever98"] = ("bolt", 16f), ["SperlingS96"] = ("slide", 22f),   // (Yan, 2026-11-16: "use this version for now", the weapons_only2 GIF) a bolt handle: lifts, runs back, forward, down (travel = the run back in texture px)
            ["HaldenServiceRevolver"] = ("hammer", 38f), ["RinaldiR6"] = ("hammer", 30f), ["RK12Kuvalda"] = ("hammer", 32f),   // a hammer: travel is DEGREES, it turns on its hinge (HammerPivot)
            ["WaldenP9"] = ("slide", 9f), ["SteppeEagle"] = ("slide", 8f), ["Corvan57"] = ("slide", 8f), ["SR21Gadyuka"] = ("slide", 9f), ["PSh18Pchela"] = ("slide", 8f), ["BlackHarrier"] = ("slide", 10f), ["SperlingS96"] = ("slide", 5f),   // the C96's barrel and bolt run back together
            ["BayouPump"] = ("pump", 26f), ["Hound13"] = ("pump", 25f), ["VK127Grom"] = ("bolt", 16f),
            ["Karusel5"] = ("hammer", 35f),   // 2026-11-16: the revolver carbine's hammer spur (ArtSource/weapons/gun_parts.json)
        };
        /// <summary>Hand work on the lever and bolt guns, riding the part animation (Yan, 2026-11-16: "use this version for now, just make sure the characters hands move as well when cocking the lever and bolts"):
        /// the trigger hand works the action after every shot, on the "lever" or "bolt" timing of PartPhase. (style, forward px, up px) in TEXTURE px of the double-size picture, along the gun's own axis.</summary>
        public static readonly Dictionary<string, (string style, float fwd, float up)> HandWork = new Dictionary<string, (string, float, float)>
        {
            ["FrontierLeverRifle"] = ("lever", 3f, -7f), ["HaldenLeverShotgun"] = ("lever", 3f, -8f), ["Nightwalker4570"] = ("lever", 3f, -7f),   // the hand pulls the loop down and forward, then back up
            ["M91Trekhlineyka"] = ("bolt", -16f, 5f), ["Sever98"] = ("bolt", -14f, 5f), ["VK127Grom"] = ("bolt", -14f, 5f),                                                          // Mosin: the hand lifts to the handle, drags the bolt back and pushes it forward; Sever-98: the hand goes up and back with the handle
        };

        /// <summary>The hinge a "hammer" part turns on, in TEXTURE px of the double-size picture (its part sprite has this point as its pivot).</summary>
        public static readonly Dictionary<string, (float x, float y)> HammerPivot = new Dictionary<string, (float, float)>
        {
            ["Karusel5"] = (88f, 9f), ["HaldenServiceRevolver"] = (41f, 15f), ["RinaldiR6"] = (40f, 17f), ["RK12Kuvalda"] = (33f, 26f),
            ["FrontierLeverRifle"] = (87f, 29f), ["HaldenLeverShotgun"] = (90f, 36f), ["Nightwalker4570"] = (93f, 31f),
        };
        /// <summary>The SECOND moving part (2026-11-16, Yan: "build the second part renderer"): a gun may move one part through PartInfo (slide, pump, lever, bolt) and one more through this table, drawn by its own renderer.
        /// style: "hammer" (turns on its pivot as the shot goes off: cocks back, falls) | "trigger" (turns on its pivot, pulled at the shot) | "charge" (a charging handle: runs back with the shot and is dragged at the end of a magazine reload).
        /// travel: DEGREES for hammer and trigger (the sign is the way it turns), texture px for a handle. (px, py): the pivot in TEXTURE px of the double-size picture. Cut by ArtSource/weapons/cut_parts.py ("second").</summary>
        public static readonly Dictionary<string, (string style, float travel, float px, float py)> Part2Info = new Dictionary<string, (string, float, float, float)>
        {
            ["SperlingS96"] = ("hammer", 30f, 19f, 16f),
            ["WaldenP9"] = ("trigger", -14f, 58f, 35f), ["PSh18Pchela"] = ("trigger", -14f, 56f, 31f), ["SR21Gadyuka"] = ("trigger", -14f, 48f, 30f),   // cut by polygon (cut_parts.py "poly")
            ["CorvanBattleRifle"] = ("charge", 8f, 0f, 0f),
            ["VK74"] = ("charge", 6f, 0f, 0f), ["VKM"] = ("charge", 6f, 0f, 0f), ["VKS74U"] = ("charge", 6f, 0f, 0f), ["VK74Obelisk"] = ("charge", 6f, 0f, 0f), ["VK103"] = ("charge", 6f, 0f, 0f),   // drawn handles (second_parts.py)
        };
        public static float TriggerPhase(float since) => since < 0f ? 0f : since < 0.03f ? since / 0.03f : since < 0.12f ? 1f - (since - 0.03f) / 0.09f : 0f;
        /// <summary>The charging handle's drag at the end of a magazine reload (matches the support hand's reach in HeldPoseMath.Frame: back 0.90-0.94, let go 0.94-0.97).</summary>
        public static float ChargeRack(float r) => r < 0.90f ? 0f : r < 0.94f ? (r - 0.90f) / 0.04f : r < 0.97f ? 1f - (r - 0.94f) / 0.03f : 0f;
        private static readonly Dictionary<string, Sprite> Part2Cache = new Dictionary<string, Sprite>();
        public static Sprite Part2Sprite(string key)
        {
            if (key == null || !Part2Info.TryGetValue(key, out var info)) return null;
            if (Part2Cache.TryGetValue(key, out var hit)) return hit;
            var tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_part2");
            if (tex == null) return Part2Cache[key] = null;
            tex.filterMode = FilterMode.Bilinear;
            var g = For(null, key).grip; float dense = Density(key);
            var pv = info.style == "charge" ? (g[0] * dense, g[1] * dense) : (info.px, info.py);          // a handle slides with the gun's own pivot; a hammer or trigger turns on its own
            return Part2Cache[key] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(pv.Item1 / tex.width, 1f - pv.Item2 / tex.height), PixelsPerUnit * dense, 0, SpriteMeshType.FullRect);
        }
        private static readonly Dictionary<string, (Sprite, Sprite)> PartCache = new Dictionary<string, (Sprite, Sprite)>();
        public static (Sprite body, Sprite part, string style, float travel) Parts(string cat, string key)
        {
            if (key == null) return (null, null, null, 0f);
            if (!PartInfo.TryGetValue(key, out var info)) { if (!Part2Info.ContainsKey(key)) return (null, null, null, 0f); info = (null, 0f); }   // only a second part: the body is the gun without it
            if (!PartCache.TryGetValue(key, out var sp))
            {
                var g = For(cat, key).grip; float dense = Density(key);
                Sprite Load(string kind)
                {
                    var tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + "_" + kind);
                    if (tex == null) return null;
                    tex.filterMode = FilterMode.Bilinear;
                    var hp = kind == "part" && (info.style == "hammer" || info.style == "lever") && HammerPivot.TryGetValue(key, out var hv) ? hv : (g[0] * dense, g[1] * dense);   // a hammer turns on its own hinge: that is the part sprite's pivot
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(hp.Item1 / tex.width, 1f - hp.Item2 / tex.height), PixelsPerUnit * dense, 0, SpriteMeshType.FullRect);
                }
                PartCache[key] = sp = (Load("body"), Load("part"));
            }
            return (sp.Item1, sp.Item2, info.style, info.travel);
        }
        /// <summary>2026-11-15 (Yan: "we don't have the shell ejection animation"): where a gun throws its spent case from, in TEXTURE px of the double-size picture, and how long after the shot it does (a pump: when the fore-end is back).
        /// A gun without an entry throws it from a point a third of the way to the muzzle, at the shot.</summary>
        public static readonly Dictionary<string, (float x, float y, float delay, bool onPart)> Ejects = new Dictionary<string, (float, float, float, bool)>
        {
            ["RangerP8"] = (113f, 10f, 0.22f, false),            // the port is on the receiver; the pump racks, then the shell leaves
            ["Halden45Service"] = (62f, 11f, 0.03f, true),       // the port is cut in the slide: it moves back with it
            // 2026-11-15: every other gun that throws its case, the port read off the double-size picture (rounded to the pixel; a pump or a lever throws when the action is cycled, a bolt gun later)
            ["HaldenLeverShotgun"] = (104f, 10f, 0.28f, false),
            ["FrontierLeverRifle"] = (104f, 11f, 0.28f, false),
            ["Nightwalker4570"] = (116f, 10f, 0.28f, false),
            ["SperlingS96"] = (65f, 11f, 0.03f, true),
            ["WaldenP9"] = (60f, 14f, 0.03f, true),
            ["VK74"] = (130f, 14f, 0.03f, false),
            ["SKNKarabin"] = (112f, 11f, 0.03f, false),
            ["CorvanWasp9"] = (118f, 30f, 0.03f, false),
            ["Hound13"] = (100f, 10f, 0.22f, false),
            ["SP14Warden"] = (105f, 14f, 0.03f, false),   // semi-auto (Yan: "the SPAS-12 is semi-auto"): the case leaves with the shot, no pump
            ["HaldenHR36K"] = (105f, 22f, 0.03f, false),
            ["M91Trekhlineyka"] = (100f, 12f, 0.55f, false),
            ["Kaban12"] = (118f, 14f, 0.03f, false),
            ["VK127Grom"] = (62f, 13f, 0.03f, false),
            ["SteppeEagle"] = (50f, 12f, 0.03f, true),
            ["VSNShepot"] = (90f, 10f, 0.03f, false),
            ["Sayan12"] = (105f, 12f, 0.03f, false),
            ["TS9Tishina"] = (130f, 14f, 0.03f, false),
            ["CorvanHornet45"] = (135f, 22f, 0.03f, false),
            ["VosaVz62"] = (118f, 12f, 0.03f, false),
            ["HaldenAM50Sledge"] = (95f, 14f, 0.03f, false),
            ["SV2Strela"] = (95f, 13f, 0.03f, false),
            ["CorvanMarksman"] = (100f, 11f, 0.03f, false),
            ["CorvanLMG"] = (110f, 30f, 0.03f, false),
            ["PL74"] = (120f, 16f, 0.03f, false),
            ["VK103"] = (120f, 14f, 0.03f, false),
            ["CorvanBullpup"] = (150f, 24f, 0.03f, false),
            ["RL44"] = (98f, 16f, 0.03f, false),
            ["NA94Yenisei"] = (125f, 16f, 0.03f, false),
            ["VKM"] = (120f, 14f, 0.03f, false),
            ["CorvanBattleRifle"] = (108f, 20f, 0.03f, false),
            ["BayouPump"] = (112f, 10f, 0.22f, false),
            ["CorvanGnatPDW"] = (125f, 17f, 0.03f, false),
            ["VK74Obelisk"] = (118f, 12f, 0.03f, false), ["PL62Kobza"] = (130f, 26f, 0.03f, false),
            ["Sever98"] = (112f, 10f, 0.55f, false), ["BR86Bulldog"] = (70f, 24f, 0.03f, false), ["Corvan57"] = (58f, 10f, 0.03f, true), ["PL74PPolovets"] = (110f, 24f, 0.03f, false), ["PL74PPolovetsZulu"] = (98f, 26f, 0.03f, false),
            ["Smerch3"] = (90f, 12f, 0.03f, false), ["SR21Gadyuka"] = (78f, 13f, 0.03f, true), ["PSh18Pchela"] = (75f, 12f, 0.03f, true), ["BlackHarrier"] = (62f, 13f, 0.03f, true),
            ["VKS74U"] = (108f, 14f, 0.03f, false), ["TR30"] = (112f, 15f, 0.03f, false), ["PPZZubr"] = (105f, 16f, 0.03f, false),
        };
        /// <summary>How far a bolt handle is lifted (0..1) `since` seconds after the shot: up before the run back, down after the run forward.</summary>
        public static float BoltLift(float since) => since < 0.30f ? 0f : since < 0.36f ? (since - 0.30f) / 0.06f : since < 0.72f ? 1f : since < 0.78f ? 1f - (since - 0.72f) / 0.06f : 0f;
        /// <summary>How far back (0..1 of the travel) a moving part is `since` seconds after the shot.</summary>
        /// <summary>2026-11-16 (Yan: "slide lock-back on an empty pistol, chambering a round at the end of a reload, the pump racking at the end of a shotgun reload"): the part's own shot cycle replayed as a rack at progress u (0..1) of the reload's last stretch.</summary>
        public static float RackSince(string style, float u) => style == "bolt" ? 0.30f + u * 0.48f : style == "lever" ? 0.12f + u * 0.33f : style == "pump" ? 0.14f + u * 0.42f : 0f;
        /// <summary>How far a slide is held back (0..1): locked open when the gun runs dry, kept open through a reload from empty, then let go with a snap that chambers the round.</summary>
        public static float SlideLock(bool dry, bool wasDry, float r)
        {
            if (r < 1f) return wasDry ? (r < 0.86f ? 1f : r < 0.92f ? 1f - (r - 0.86f) / 0.06f : 0f) : 0f;
            return dry ? 1f : 0f;
        }
        public static float PartPhase(string style, float since)
        {
            if (since < 0f) return 0f;
            if (style == "slide") return since < 0.03f ? since / 0.03f : since < 0.14f ? 1f - (since - 0.03f) / 0.11f : 0f;                                  // kicks back in 30 ms, returns in 110 ms
            if (style == "lever") return since < 0.12f ? 0f : since < 0.26f ? Mathf.SmoothStep(0f, 1f, (since - 0.12f) / 0.14f) : since < 0.31f ? 1f : since < 0.45f ? 1f - Mathf.SmoothStep(0f, 1f, (since - 0.31f) / 0.14f) : 0f;   // the action is worked after the shot: down and forward, a beat, back
            if (style == "bolt") return since < 0.36f ? 0f : since < 0.54f ? Mathf.SmoothStep(0f, 1f, (since - 0.36f) / 0.18f) : since < 0.58f ? 1f : since < 0.72f ? 1f - Mathf.SmoothStep(0f, 1f, (since - 0.58f) / 0.14f) : 0f;       // the bolt runs back and forward (BoltLift raises the handle round it)
            if (style == "hammer") return since < 0.06f ? since / 0.06f : since < 0.14f ? 1f - (since - 0.06f) / 0.08f : 0f;                                  // cocks back in 60 ms, falls in 80
            if (style == "pump") return since < 0.14f ? 0f : since < 0.32f ? Mathf.SmoothStep(0f, 1f, (since - 0.14f) / 0.18f) : since < 0.38f ? 1f : since < 0.56f ? 1f - Mathf.SmoothStep(0f, 1f, (since - 0.38f) / 0.18f) : 0f;   // the recoil settles, back, a beat, forward
            return 0f;
        }

        public static (Sprite sprite, float[] grip) For(string cat, string key)
        {
            string id = key ?? "cat_" + cat;
            if (Cache.TryGetValue(id, out var hit)) return hit;
            float dense = Density(key);
            var tex = key != null ? Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key + (dense > 1f ? "_hd" : "")) : null;
            if (tex == null && dense > 1f) { dense = 1f; tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_" + key); }        // no big picture: the small one at its own scale
            bool own = tex != null;
            if (!own) { dense = 1f; tex = Resources.Load<Texture2D>("Sprites/Weapons/weapon_cat_" + cat); }
            var g = own && Grips.TryGetValue(key, out var kg) ? kg : Grips.TryGetValue("cat_" + cat, out var cg) ? cg : Grips["cat_assault"];
            float hs = own ? Scale(key) : 1f; if (hs != 1f) g = new[] { g[0], g[1], g[2], g[3], g[0] + (g[4] - g[0]) * hs, g[1] + (g[5] - g[1]) * hs };      // bigger in hand: the cutting point moves out with the blade
            Sprite s = null;
            if (tex != null)
            {
                tex.filterMode = dense > 1f ? FilterMode.Bilinear : FilterMode.Point;   // 2026-11-15: the guns used at the SpriteCook still's own size (about 6 texture px per old-pixel) are drawn smaller than their picture: bilinear, so they shrink smoothly instead of being sampled to noise
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(g[0] * dense / tex.width, 1f - g[1] * dense / tex.height), PixelsPerUnit * dense / hs, 0, SpriteMeshType.FullRect);   // grip in virtual px = texture px / dense
            }
            return Cache[id] = (s, g);
        }
    }

    /// <summary>The gun + two hands for one unit marker (children of the body's SpriteRenderer).
    /// Batch 22: also plays recoil, melee swings and reloads (started by CombatFxView).</summary>
    public class HeldWeaponView
    {
        private readonly SpriteRenderer _part2, _gun, _support, _grip, _mag, _barrels, _optic, _under, _magHd, _shell;
        private readonly SpriteRenderer[] _smear = new SpriteRenderer[HeldPoseMath.SmearCount];   // Melee 4: the strike's ghosts
        private float _angle;
        private static readonly Dictionary<string, Sprite> HandCache = new Dictionary<string, Sprite>();
        // Batch 29: the project enters Play mode without a domain reload, so statics survive between
        // runs while the sprites/textures they point at are destroyed - clear the caches each run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches() { HandCache.Clear(); ItemSprites.Clear(); }


        private float _shotAt = -99f, _kick, _climb, _recover, _hold;
        private float _swingAt = -99f;
        private float _reloadAt = -99f, _reloadTime;
        private string _reloadStyle = "none"; private float _lastReloadAt = -1f; private bool _reloadEmpty;   // _reloadEmpty: the gun was dry when this reload began (the slide was locked back)
        private int _reloadTrips = 1;
        private bool _magThrown;
        /// <summary>RT 6j: (sprite, world centre, degrees, flipped, floor y, sorting order) - a magazine let go.</summary>
        public static System.Action<Sprite, Vector3, float, bool, float, int> MagDropped;
        private Vector3 _aimOverride;
        private float _aimUntil = -99f;
        private float[] _g;          // grip/support/muzzle points of the sprite on show
        float _bs = 1f;              // 2026-11-18: this weapon's size boost (WeaponArt.Boost); Ppu = the pixels per unit the loose parts are laid out with
        float Ppu => WeaponArt.PixelsPerUnit / _bs;

        private readonly int _base;   // the body's sorting order: gun and hands sit just above it, or just under it from behind

        public HeldWeaponView(SpriteRenderer body)
        {
            _base = body.sortingOrder;
            _gun = Child(body, "HeldWeapon", 1);
            _support = Child(body, "SupportHand", 2);
            _grip = Child(body, "GripHand", 3);
            _mag = Child(body, "Magazine", 1);
            _shell = Child(body, "ShellInHand", 3);   // a round in the support hand on a tube reload
            _magHd = Child(body, "MagazineHd", 1);   // the double-size guns' magazine (the slide of a pistol uses _mag)
            _barrels = Child(body, "Barrels", 2);
            _part2 = Child(body, "Part2", 1); _optic = Child(body, "Optic", 1); _under = Child(body, "UnderAttachment", 1);   // Optics.cs: a sight on the rail, a laser under the barrel
            _gun2 = Child(body, "OffhandWeapon", 2);
            for (int i = 0; i < _smear.Length; i++) _smear[i] = Child(body, "Smear" + i, 0);
            _item = Child(body, "UseItem", 2);
        }

        // RT 7z-c (Gunslinger): the left-hand pistol - its own sprite, recoil and muzzle. Effects pick a gun with Use(weapon).
        private readonly SpriteRenderer _gun2;
        private Weapon _w2;
        private float[] _g2;
        private float _shotAt2 = -99f, _kick2, _climb2, _recover2, _hold2;
        private bool _sel2;
        /// <summary>Points Gun / Muzzle / Recoil at the gun that fired: the left-hand one when it's `w`, else the main one.</summary>
        public HeldWeaponView Use(Weapon w) { _sel2 = w != null && w == _w2 && _gun2.gameObject.activeSelf; return this; }
        SpriteRenderer Sel => _sel2 ? _gun2 : _gun;

        public bool Visible => _gun.gameObject.activeSelf;
        public Transform Gun => Sel.transform;
        public bool AimLeft => Sel.flipY;
        public float Angle => _angle;
        private string _ejectKey;
        /// <summary>When the spent case leaves the gun, seconds after the trigger pull (0 = at the shot).</summary>
        public float EjectDelay => _ejectKey != null && WeaponArt.Ejects.TryGetValue(_ejectKey, out var e) ? e.delay : 0f;
        /// <summary>World position of the gun's ejection port (the gun as last drawn).</summary>
        public Vector3 EjectWorld()
        {
            var g = _sel2 ? _g2 : _g;
            if (g == null) return Sel.transform.position;
            float side = Sel.flipY ? -1f : 1f, lx, ly;
            if (!_sel2 && _ejectKey != null && WeaponArt.Ejects.TryGetValue(_ejectKey, out var e))
            {
                float dense = WeaponArt.Density(_ejectKey); lx = (e.x / dense - g[0]) / WeaponArt.PixelsPerUnit; ly = side * (g[1] - e.y / dense) / WeaponArt.PixelsPerUnit;
                if (e.onPart && WeaponArt.PartInfo.TryGetValue(_ejectKey, out var pi)) lx -= WeaponArt.PartPhase(pi.style, Time.time - _shotAt) * pi.travel / (WeaponArt.PixelsPerUnit * dense);   // the port rides on the slide: where the slide is NOW
            }
            else { lx = 0.35f * (g[4] - g[0]) / WeaponArt.PixelsPerUnit; ly = side * 0.35f * (g[1] - g[5]) / WeaponArt.PixelsPerUnit; }
            return Sel.transform.TransformPoint(new Vector3(lx, ly, 0f));
        }
        public Sprite GunSprite => _gun.sprite;
        public SpriteRenderer GunRenderer => _gun;      // 2026-11-23: the Verger's afterimage copies the held scythe too

        public void Recoil(WeaponFeel f, float scale)
        {
            if (_sel2) { _shotAt2 = Time.time; _kick2 = f.Kick * scale; _climb2 = f.Climb * scale; _recover2 = Mathf.Max(0.05f, f.Recover); _hold2 = f.Hold; return; }
            _shotAt = Time.time; _kick = f.Kick * scale; _climb = f.Climb * scale; _recover = Mathf.Max(0.05f, f.Recover); _hold = f.Hold;
        }
        public void Twitch()   // jammed: click, nothing
        {
            if (_sel2) { _shotAt2 = Time.time; _kick2 = 0.5f; _climb2 = -4f; _recover2 = 0.1f; _hold2 = 0f; return; }
            _shotAt = Time.time; _kick = 0.5f; _climb = -4f; _recover = 0.1f; _hold = 0f;
        }
        public void Swing() => _swingAt = Time.time;
        /// <summary>RT 7c: the body's lean while swinging a melee weapon (degrees, for the unit's tilt): back on the wind-up, into the chop.</summary>
        public float Lean { get; private set; }
        /// <summary>RT 7e: the player's melee charge (Melee.ChargeOf) - drives the wind-up, the lift and the whip; null for others.</summary>
        public Melee.Charge MeleeCharge;
        public string BodyPose;                 // Melee 3: the body frame the swing wants (TacticalGridView.DrawUnit swaps it in)
        public string UseKind; public float UseT;   // Use: the item in use (HeldPoseMath.UseKindOf) and how far through (TacticalGridView sets it)
        public bool JawHold; public float JawGrip, JawTwitch;   // Boss 3: holding the eel's jaws open - hands up over the head (TacticalGridView sets it from EelSump)
        private readonly SpriteRenderer _item;
        private static readonly Dictionary<string, Sprite> ItemSprites = new Dictionary<string, Sprite>();
        static Sprite ItemSprite(string kind)
        {
            if (ItemSprites.TryGetValue(kind, out var s) && s != null) return s;
            var tex = Resources.Load<Texture2D>("Sprites/Items/use_" + kind);
            var gp = HeldPoseMath.UseGrip(kind);
            return ItemSprites[kind] = tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(gp.x / tex.width, 1f - gp.y / tex.height), WeaponArt.PixelsPerUnit, 0, SpriteMeshType.FullRect);
        }
        /// <summary>RT 6f: clearing a jam (tap the magazine, rack the action) over `seconds`.</summary>
        public void StartClear(float seconds) { if (_sel2) return; _reloadAt = Time.time; _reloadTime = seconds; _reloadStyle = "jam"; _reloadTrips = 1; }
        public void StartReload(WeaponFeel f, int capacity, float seconds = 0f, int rounds = 0)
        {
            if (_sel2) return;                                                          // ponytail: the left gun reloads without its own animation
            _reloadAt = Time.time; _reloadTime = seconds > 0f ? seconds : f.ReloadTime; _reloadStyle = f.Reload;
            _reloadTrips = f.Reload == "tube" || f.Reload == "cylinder" ? (rounds > 0 ? rounds : Mathf.Clamp(capacity / 2, 2, 4)) : 1;
        }
        /// <summary>RT 6h: a round-by-round reload cut short - the gun comes back up.</summary>
        public void StopReload() { if (!_sel2) _reloadTime = 0f; }
        /// <summary>Point the gun at a world position until `until` (the shot's target, even if the
        /// player has already moved the mouse on).</summary>
        public void AimAt(Vector3 world, float until) { _aimOverride = world; _aimUntil = until; }

        /// <summary>World position of the muzzle (or blade tip) of the gun as last drawn.</summary>
        public Vector3 Muzzle() => Sel.transform.TransformPoint(MuzzleLocal());

        /// <summary>The muzzle in the gun's own space (the gun turns around its grip point).</summary>
        public Vector3 MuzzleLocal()
        {
            var g = _sel2 ? _g2 : _g;
            if (g == null) return Vector3.zero;
            float side = Sel.flipY ? -1f : 1f;
            return new Vector3((g[4] - g[0]) / WeaponArt.PixelsPerUnit, side * (g[1] - g[5]) / WeaponArt.PixelsPerUnit, 0f);
        }

        /// <summary>Boss 3 (the jaw-hold, Yan's sketch): no weapon in sight, the two big gloves go to two world points - one on the front of the
        /// upper jaw, one on the front of the lower - fingers toward the head (faceX: +1 = the head is to his right, -1 = to his left).
        /// Called after Show each frame while he holds the eel's jaws.</summary>
        public void GripHands(ArmorDef armor, Vector3 upperWorld, Vector3 lowerWorld, float faceX)
        {
            _gun.gameObject.SetActive(false); _gun2.gameObject.SetActive(false); _mag.gameObject.SetActive(false); _barrels.gameObject.SetActive(false); _item.gameObject.SetActive(false); _part2.gameObject.SetActive(false); _optic.gameObject.SetActive(false); _under.gameObject.SetActive(false); _magHd.gameObject.SetActive(false); _shell.gameObject.SetActive(false);
            foreach (var sm in _smear) sm.gameObject.SetActive(false);
            var big = HandSprite(armor, false);
            for (int h = 0; h < 2; h++)
            {
                var sr = h == 0 ? _grip : _support; var w = h == 0 ? upperWorld : lowerWorld;
                sr.gameObject.SetActive(true); sr.sprite = big; sr.flipY = false; sr.flipX = faceX < 0f;                 // the glove's fingers point right at 0 degrees (cuff on the left): mirrored when the head is on his left
                sr.transform.position = new Vector3(w.x, w.y, 0f); sr.transform.rotation = Quaternion.Euler(0f, 0f, (h == 0 ? -10f : 10f) * (faceX < 0f ? -1f : 1f));
                sr.sortingOrder = _base + 4;
            }
            _g = null;
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
        void AttachmentView(SpriteRenderer r, Optics.Def d, string key, float[] g, HeldFrame f, int slot, float now, float slideHeld = 0f)
        {
            var sp = d != null && key != null ? Optics.SpriteOf(d.Id) : null;
            if (sp == null || !GameData.OpticMounts.Mount.TryGetValue(key, out var mt)) { r.gameObject.SetActive(false); return; }
            float hd = WeaponArt.Density(key), ang = f.GunDeg * Mathf.Deg2Rad;
            float hx = (mt[slot] / hd - g[0]) / Ppu, hy = (f.Left ? 1f : -1f) * (mt[slot + 1] / hd - g[1]) / Ppu;
            if (slot == 0 && WeaponArt.PartInfo.TryGetValue(key, out var pi) && pi.style == "slide") hx -= Mathf.Max(WeaponArt.PartPhase(pi.style, now - _shotAt), slideHeld) * pi.travel / (Ppu * hd);   // the sight sits on the slide
            r.gameObject.SetActive(true); r.sprite = sp; r.flipY = f.Left; r.sortingOrder = _gun.sortingOrder;
            r.transform.localPosition = _gun.transform.localPosition + new Vector3(hx * Mathf.Cos(ang) - hy * Mathf.Sin(ang), hx * Mathf.Sin(ang) + hy * Mathf.Cos(ang), slot == 0 ? -0.02f : -0.015f);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, f.GunDeg);
        }

        // 2026-11-23 (Yan: "make the hands swing a little with the walk and a lot when running, as a test"): Show is wrapped. The wrapper reads how fast the body really moves (minus UnitLife's own 1 px hop, LifeOff)
        // and, with a weapon in the hands, bobs the whole held assembly (gun and both hands) a pixel with each step at a walk and 3 px with a lateral rock at a run; empty hands get a wider swing
        // (HeldPoseMath.FreeHands, RunK). Only for units that UnitLife.Applies to (LifeOn): the rest are as they were.
        // 2026-11-23 (Yan: "I'd like for the weapon to stay in the resting position unless aiming or shooting ... held naturally close to the body in front of it"): Carry (set by the view each frame): the long gun is held across
        // the front of the body, close to it, level, pointing to the side he faces (the game's own resting stance at aim 0 or 180, drawn in front of the body, shifted 3 px toward the torso) instead of following the mouse
        // round to straight up or down. Aiming (right mouse), firing, reloading and melee leave it. _restLeft = which side (from the aim's sideways part, kept when the aim is nearly vertical).
        public bool Carry, CarrySnap; bool _restLeft; float _carryK;
        public Vector3 LifeOff; public bool LifeOn; float _runK, _walkK, _phase2; Vector3? _lastPos2;
        public void Show(Weapon weapon, ArmorDef armor, Vector3 unitPos, Vector3? aimAt, bool visible, bool back = false, Weapon offhand = null)
        {
            var clean = unitPos - LifeOff; float spd = 0f;
            if (_lastPos2.HasValue) { float mx = clean.x - _lastPos2.Value.x, my = clean.y - _lastPos2.Value.y, mv = Mathf.Min(Mathf.Sqrt(mx * mx + my * my), 0.5f); float dt = Mathf.Max(Time.deltaTime, 0.0001f); spd = mv / dt; _phase2 += mv * Mathf.PI * 2f / CycleTiles(spd); }
            _lastPos2 = clean; float wk = LifeOn ? Mathf.Clamp01(spd / 1.5f) : 0f, rk = LifeOn ? Mathf.Clamp01((spd - 3.4f) / 1.0f) : 0f;
            float k = Mathf.Max(Time.deltaTime, 0.0001f) * 6f; _walkK = Mathf.MoveTowards(_walkK, wk, k); _runK = Mathf.MoveTowards(_runK, rk, k);
            ShowCore(weapon, armor, unitPos, aimAt, visible, back, offhand);
            bool carrying = _carryK > 0.01f;
            if ((!LifeOn && !carrying) || !visible || weapon?.Db == null || JawHold || (_walkK < 0.05f && !carrying)) return;
            // Yan, twice, with drawings: the rifle must swing like a PENDULUM across the body (a smile-shaped arc: sideways, lowest in the middle, lifted at both ends), not bob up and down. So: the whole held assembly
            // (gun, both hands, parts) slides along that arc on SCREEN axes (1 px each way and 1 up at the ends at a walk, 3 px and 2 up at a run) and the gun leans with the swing (5 degrees at a run)
            // 2026-11-23: the numbers come from the weapon's category (WeaponCarry): its own carry, walk and run swing; aiming or shooting while moving gets no swing, only a 1 px bob on each step
            var cd = WeaponCarry.Of(WeaponArt.Identify(weapon.Db).Item1); float s1 = Mathf.Sin(_phase2), amp = Mathf.Lerp(cd.WalkAmp, cd.RunAmp, _runK), up = Mathf.Lerp(cd.WalkLift, cd.RunLift, _runK);
            float sw = carrying ? 1f : 0f, bobAim = carrying ? 0f : Mathf.Round((Mathf.Sin(_phase2 * 2f) > 0.2f ? 1f : 0f) * _walkK);
            float dxp = Mathf.Round(s1 * amp * _walkK) * sw, liftp = Mathf.Round(s1 * s1 * up * _walkK) * sw - bobAim, th = -s1 * cd.RunTilt * _runK * sw;
            float carryDx = (_restLeft ? cd.ShiftPx : -cd.ShiftPx) * _carryK, carryDrop = cd.DropPx * _carryK;                                  // close to the body: 3 px in toward the torso
            Vector3 Swing(Vector3 p) => new Vector3(p.x + (dxp + carryDx) / 16f, p.y + (liftp - carryDrop) / 16f, p.z);
            bool freeArm = cd.FreeArm && carrying && _carryK > 0.5f;                                   // the pistol's other arm: hangs at the side and swings with the steps (not carried with the gun)
            if (freeArm)
            {
                var (rx, ry, lx, ly) = HeldPoseMath.FreeHands(_phase2, _walkK, _runK); bool fl = _restLeft; float fx = fl ? rx : lx, fy = fl ? ry : ly;       // the arm on the side away from the gun
                _support.gameObject.SetActive(true); _support.sprite = HandSprite(armor, false); _support.flipX = !fl; _support.transform.localPosition = Local(fx, fy) + new Vector3(0f, 0f, -0.015f); _support.transform.localRotation = Quaternion.Euler(0f, 0f, fl ? -90f : 90f);
            }
            foreach (var r in new[] { _gun, _gun2, _grip, _support, _mag, _barrels, _part2, _optic, _under, _magHd, _shell }) if (r != null && r.gameObject.activeSelf && !(freeArm && r == _support)) { r.transform.localPosition = Swing(r.transform.localPosition); r.transform.localRotation = Quaternion.Euler(0f, 0f, r.transform.localEulerAngles.z + th); }
        }
        void ShowCore(Weapon weapon, ArmorDef armor, Vector3 unitPos, Vector3? aimAt, bool visible, bool back = false, Weapon offhand = null)
        {
            bool on = visible && weapon?.Db != null; BodyPose = null;
            foreach (var sm in _smear) sm.gameObject.SetActive(false);
            _item.gameObject.SetActive(false);
            _w2 = on ? offhand : null;
            _gun2.gameObject.SetActive(_w2?.Db != null);
            _gun.gameObject.SetActive(on); _grip.gameObject.SetActive(visible); _support.gameObject.SetActive(visible); _mag.gameObject.SetActive(false); _barrels.gameObject.SetActive(false); _part2.gameObject.SetActive(false); _optic.gameObject.SetActive(false); _under.gameObject.SetActive(false); _magHd.gameObject.SetActive(false); _shell.gameObject.SetActive(false);
            if (!visible) return;
            // 2026-11-23 (Yan: "fix this" - the gun jumps from the carry to the aim; "in the run up preview the weapon is behind the npc"): the carry blends in and out (_carryK: raised in 0.12 s when you aim, lowered over 0.25 s; a trigger pull
            // snaps it up at once so the first shot leaves the muzzle where it is aimed), and from the moment it is mostly carried it is drawn IN FRONT of the body even when the body shows its back (the `back` layering used to hide it)
            { float dtc = Mathf.Max(Time.deltaTime, 0.0001f); float tgt = Carry && on && !CombatRules.IsMelee(weapon) ? 1f : 0f;
              if (tgt < 0.5f && CarrySnap) _carryK = 0f; else _carryK = Mathf.MoveTowards(_carryK, tgt, dtc / (tgt > _carryK ? 0.25f : 0.12f));
              if (_carryK > 0.5f) back = false; }
            // RT 6g: the magazine just under the gun (a pistol's sits inside the grip, hidden by it until it slides out)
            _mag.sortingOrder = _base + (back ? -4 : 1); _gun.sortingOrder = _base + (back ? -3 : 2); _barrels.sortingOrder = _gun.sortingOrder; _support.sortingOrder = _base + (back ? -2 : 3); _grip.sortingOrder = _base + (back ? -1 : 4);
            _grip.flipY = _support.flipY = back;
            if (JawHold)
            {
                // Boss 3, the jaw-hold (Yan: "can't see myself"; the pose was never built): both gloves up over the head, fingers up, holding the eel's jaws apart. They strain (a fast shake), come
                // together as the grip fills (the mouth shuts) and shove up and out on every press (JawTwitch 1 .. 0). No weapon in the hands.
                _gun.gameObject.SetActive(false); _gun2.gameObject.SetActive(false);
                var big = HandSprite(armor, false);
                _grip.sprite = big; _support.sprite = big; _grip.flipX = false; _support.flipX = true;
                float spread = 5.5f - 3.5f * Mathf.Clamp01(JawGrip), shake = Mathf.Sin(Time.unscaledTime * 38f) * 0.5f, lift = JawTwitch * 1.5f;
                float jx = Mathf.Round(spread + JawTwitch * 1.2f), jy = Mathf.Round(-1.5f - lift + shake - 0.5f) + 0.5f;
                _grip.transform.localPosition = Local(8f + jx, jy); _grip.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);           // fingers up
                _support.transform.localPosition = Local(8f - jx, jy - (Mathf.Sin(Time.unscaledTime * 31f) > 0f ? 0.5f : 0f)); _support.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                _g = null;
                return;
            }
            if (!on)
            {
                // nothing equipped: the big gloves float at the torso's sides (Batch 28, Yan)
                var big = HandSprite(armor, false);
                _grip.sprite = big; _support.sprite = big;
                _grip.flipX = false; _support.flipX = true;                           // cuffs toward the body
                // Yan: "when nothing's equipped the hands ... look like chicken wings ... should stay still detached from the body but make
                // movements as if they're swinging a little with each step": a pixel clear of the body at the hips, swinging opposite
                // ways as you walk (the walk is read from how far the body moved), easing back to still when you stop
                float mx = _lastPos.HasValue ? unitPos.x - _lastPos.Value.x : 0f, my = _lastPos.HasValue ? unitPos.y - _lastPos.Value.y : 0f;
                float moved = Mathf.Min(Mathf.Sqrt(mx * mx + my * my), 0.5f); _lastPos = unitPos;   // (a jump - a new map - isn't a step)
                float dt = Mathf.Max(Time.deltaTime, 0.0001f), walking = Mathf.Clamp01(moved / dt / 1.5f);
                _stepPhase += moved * Mathf.PI * 2f / CycleTiles(moved / dt);
                _swing = Mathf.MoveTowards(_swing, walking, dt * 4f);
                var (rx, ry, lx, ly) = HeldPoseMath.FreeHands(_stepPhase, _swing, _runK);
                _grip.transform.localPosition = Local(rx, ry); _grip.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);   // hanging, fingers down
                _support.transform.localPosition = Local(lx, ly); _support.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
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
            if (_carryK > 0.001f)
            {
                float cx = Mathf.Cos(_angle * Mathf.Deg2Rad); if (Carry && Mathf.Abs(cx) > 0.25f) _restLeft = cx < 0f;
                if (WeaponCarry.Of(WeaponArt.Identify(weapon.Db).Item1).FreeArm) _restLeft = true;     // the right hand (the screen's left): a one-handed carry always sits there
                float ease = _carryK * _carryK * (3f - 2f * _carryK);
                _angle += Mathf.DeltaAngle(_angle, WeaponCarry.RestAngle(WeaponArt.Identify(weapon.Db).Item1, _restLeft)) * ease;                       // between the aim and the resting stance
            }
            var (cat, key) = WeaponArt.Identify(weapon.Db);
            _ejectKey = key;
            var (sprite, g) = WeaponArt.For(cat, key);
            _g = g;
            _bs = WeaponArt.Boost(cat);                                                  // 2026-11-18: long guns are drawn 17% bigger (the loose parts too: Ppu lays them out)
            var bsv = new Vector3(_bs, _bs, 1f);
            _mag.transform.localScale = _magHd.transform.localScale = _barrels.transform.localScale = _part2.transform.localScale = _optic.transform.localScale = _under.transform.localScale = bsv;
            _gun2.transform.localScale = Vector3.one;

            // animation on top of the aim: recoil (back + muzzle climb), melee swing, reload tilt, clearing a jam
            var mc = cat == "melee" ? MeleeCharge : null;
            // RT 7z-c: the hands and guns are children of the body - when it's turned (a dive, lying down) the aim is taken in its
            // turned frame, so the guns still point at the target instead of turning with the body
            float bodyTurn = _gun.transform.parent != null ? _gun.transform.parent.localEulerAngles.z : 0f;
            float local = Mathf.DeltaAngle(0f, _angle - bodyTurn);
            float since = mc != null ? mc.Since(now) : now - _swingAt, hold = mc != null && mc.SwingFrom >= 0f ? now - mc.SwingFrom : -1f;
            HeldFrame At(float sw) => HeldPoseMath.Frame(cat, g, local, HeldPoseMath.Recoil(now - _shotAt, _recover, _hold), _climb, _kick, sw,
                _reloadTime > 0f ? (now - _reloadAt) / _reloadTime : 1f, _reloadStyle, _reloadTrips,
                key != null && (WeaponArt.Mag(cat, key).mag != null || WeaponArt.Clips.Contains(key)) && WeaponMags.Points.TryGetValue(key, out var pts) ? pts : null,
                hold, mc != null && mc.ThrowFrom >= 0f ? now - mc.ThrowFrom : -1f, mc != null ? now - mc.ThrewAt : 99f,
                mc?.PoseKey ?? key, mc != null ? mc.Move : (int)(_swingAt * 7f), mc != null ? mc.From : -1f,
                mc != null ? Melee.T - mc.GuardAt : 99f, mc != null ? Melee.T - mc.BrokenAt : 99f, mc != null ? mc.OpenK(Melee.T) : 0f);
            var f = At(since);
            if (UseKind != null)                                                       // Use: the weapon dips, the free hand works the item
            {
                float udeg = HeldPoseMath.ApplyUse(ref f, UseKind, UseT);
                var isp = ItemSprite(UseKind);
                if (isp != null)
                {
                    _item.gameObject.SetActive(true); _item.sprite = isp; _item.flipX = f.Left; _item.sortingOrder = _base + (back ? -2 : 3);
                    _item.transform.localPosition = Local(f.SupX, f.SupY); _item.transform.localRotation = Quaternion.Euler(0f, 0f, udeg);
                }
            }
            bool smear = cat == "melee" && HeldPoseMath.Striking(since, hold);
            for (int i = 0; i < _smear.Length; i++)
            {
                _smear[i].gameObject.SetActive(smear);
                if (!smear) continue;
                var gf = At(since - (i + 1) * HeldPoseMath.SmearStep);
                _smear[i].sprite = sprite; _smear[i].flipY = gf.Left; _smear[i].sortingOrder = _base + (back ? -5 : 0);
                _smear[i].transform.localPosition = Local(gf.GunX, gf.GunY);
                _smear[i].transform.localRotation = Quaternion.Euler(0f, 0f, gf.GunDeg);
                _smear[i].transform.localScale = new Vector3((gf.Length > 0f ? gf.Length : 1f) * _bs, _bs, 1f);
                var col = mc != null && mc.Red ? new Color(1f, 0.25f, 0.18f) : new Color(1f, 0.97f, 0.88f);
                col.a = 0.55f * (1f - i / (float)_smear.Length); _smear[i].color = col;   // ponytail: tinted, not a white silhouette (needs a shader)
            }
            Lean = f.Lean; BodyPose = cat == "melee" ? f.Body : null;

            var (body, mag) = WeaponArt.Mag(cat, key);
            _gun.sprite = mag != null && body != null ? body : sprite;
            _gun.flipY = f.Left; _gun.color = VergerPhase2.Tint(weapon);                              // 2026-11-23: the Verger's scythe glows red in phase 2
            _gun.transform.localPosition = Local(f.GunX, f.GunY);
            _gun.transform.localRotation = Quaternion.Euler(0f, 0f, f.GunDeg);
            _gun.transform.localScale = new Vector3((f.Length > 0f ? f.Length : 1f) * _bs, _bs, 1f);
            var brk = WeaponArt.Break(cat, key);
            if (brk.body != null && brk.barrels != null)
            {
                // the barrels turn on the hinge: grip -> hinge in the gun's own space (a flipped gun mirrors it), then the aim
                _gun.sprite = brk.body;
                float a0 = f.GunDeg * Mathf.Deg2Rad, hx = (brk.hx - g[0]) / Ppu, hy = (f.Left ? 1f : -1f) * (brk.hy - g[1]) / Ppu;
                _barrels.gameObject.SetActive(true);
                _barrels.sprite = brk.barrels; _barrels.flipY = f.Left;
                _barrels.transform.localPosition = _gun.transform.localPosition + new Vector3(hx * Mathf.Cos(a0) - hy * Mathf.Sin(a0), hx * Mathf.Sin(a0) + hy * Mathf.Cos(a0), 0f);
                _barrels.transform.localRotation = Quaternion.Euler(0f, 0f, f.GunDeg + (f.Left ? 1f : -1f) * f.BreakDeg);
            }
            if (mag != null && f.MagShown)
            {
                bool hdMag = WeaponArt.Density(key) > 1f; var mr = hdMag ? _magHd : _mag;   // a double-size gun's magazine has its own renderer (its slide uses _mag)
                mr.gameObject.SetActive(true);
                mr.sprite = mag; mr.flipY = f.Left;
                mr.transform.localPosition = Local(f.GunX + f.MagDX, f.GunY + f.MagDY) + new Vector3(0f, 0f, hdMag ? (body == null ? 0.02f : -0.005f) : 0f);   // seated, or riding in the hand; a pistol's hidden one sits behind the gun
                mr.transform.localRotation = _gun.transform.localRotation;
                if (hdMag) mr.sortingOrder = body == null ? _gun.sortingOrder - 1 : _gun.sortingOrder;
            }
            var cy = WeaponArt.Cylinder(cat, key);
            if (cy.cyl != null && cy.body != null)                                    // RT 6l: frame + cylinder, the cylinder on the magazine's layer
            {
                bool hdCyl = WeaponArt.Density(key) > 1f; var cr = hdCyl ? _magHd : _mag;   // a double-size revolver's hammer part uses _mag, so its cylinder has its own renderer
                _gun.sprite = cy.body;                                                    // (a hammer part's body, set after this, is cut from the same cylinder-less picture)
                cr.gameObject.SetActive(true);
                cr.sprite = cy.cyl; cr.flipY = f.Left;
                bool seated = f.CylDX * f.CylDX + f.CylDY * f.CylDY < 0.05f;                 // in the frame it shows in its window (on top); swung out it goes behind the frame (Yan: to the shooter's left, the far side from the camera) and peeks out beside the window
                cr.transform.localPosition = Local(f.GunX + f.CylDX, f.GunY + f.CylDY) + new Vector3(0f, 0f, hdCyl ? (seated ? -0.004f : 0.02f) : 0f);
                cr.transform.localRotation = _gun.transform.localRotation;
                if (hdCyl) cr.sortingOrder = seated ? _gun.sortingOrder : _gun.sortingOrder - 1;
            }
            Vector3 pumpOff = Vector3.zero; bool pumping = false;                       // a pump: the off-hand goes with the fore-end
            float rr = _reloadTime > 0f ? (now - _reloadAt) / _reloadTime : 1f;
            if (_reloadAt != _lastReloadAt) { _lastReloadAt = _reloadAt; _reloadEmpty = weapon.CurrentMagazine <= 0; }
            float psince = now - _shotAt;
            var pt = WeaponArt.Parts(cat, key);                                       // a moving part (pump fore-end, slide...): the body plus the part, the part slid along the barrel
            float lockBack = pt.style == "slide" && !CombatRules.IsMelee(weapon) ? WeaponArt.SlideLock(weapon.CurrentMagazine <= 0, _reloadEmpty, rr) : 0f;
            if (pt.body != null && pt.part == null) _gun.sprite = pt.body;                    // a gun whose only moving part is the second one
            if (pt.body != null && pt.part != null)
            {
                _gun.sprite = pt.body;
                if ((_reloadStyle == "tube" || _reloadStyle == "clip") && rr >= 0.86f && rr < 1f && (pt.style == "pump" || pt.style == "lever" || pt.style == "bolt"))
                    psince = WeaponArt.RackSince(pt.style, (rr - 0.86f) / 0.14f);                 // the action is worked once at the end of a reload: the pump racks, the lever cycles, the bolt runs
                if ((pt.style == "hammer" || pt.style == "lever") && WeaponArt.HammerPivot.TryGetValue(key, out var hinge))
                {
                    float hd = WeaponArt.Density(key), hang = f.GunDeg * Mathf.Deg2Rad, hx = (hinge.x / hd - g[0]) / Ppu, hy = (f.Left ? 1f : -1f) * (hinge.y / hd - g[1]) / Ppu;
                    _mag.gameObject.SetActive(true); _mag.sprite = pt.part; _mag.flipY = f.Left; _mag.sortingOrder = _gun.sortingOrder;
                    _mag.transform.localPosition = _gun.transform.localPosition + new Vector3(hx * Mathf.Cos(hang) - hy * Mathf.Sin(hang), hx * Mathf.Sin(hang) + hy * Mathf.Cos(hang), -0.01f);
                    _mag.transform.localRotation = Quaternion.Euler(0f, 0f, f.GunDeg + (f.Left ? -1f : 1f) * WeaponArt.PartPhase(pt.style, psince) * pt.travel);
                }
                else {
                float slideBack = (pt.style == "lever" ? -0.35f : 1f) * Mathf.Max(WeaponArt.PartPhase(pt.style, psince), pt.style == "slide" ? Mathf.Max(f.PartOpen, lockBack) : 0f) * pt.travel / (Ppu * WeaponArt.Density(key)), ang = f.GunDeg * Mathf.Deg2Rad;
                _mag.gameObject.SetActive(true);
                _mag.sprite = pt.part; _mag.flipY = f.Left; _mag.sortingOrder = _gun.sortingOrder;
                float lift = pt.style == "lever" ? -WeaponArt.PartPhase(pt.style, psince) * pt.travel / (Ppu * WeaponArt.Density(key)) * (f.Left ? -1f : 1f) : pt.style == "bolt" ? WeaponArt.BoltLift(psince) * 0.05f * pt.travel / (Ppu * WeaponArt.Density(key)) * (f.Left ? -1f : 1f) : 0f;   // the handle is raised before it runs back
                _mag.transform.localPosition = _gun.transform.localPosition + new Vector3(-slideBack * Mathf.Cos(ang) - lift * Mathf.Sin(ang), -slideBack * Mathf.Sin(ang) + lift * Mathf.Cos(ang), -0.01f);
                if (pt.style == "pump") { pumping = true; pumpOff = new Vector3(-slideBack * Mathf.Cos(ang), -slideBack * Mathf.Sin(ang), 0f); }   // toward the stock = against the aim; in front of the body
                _mag.transform.localRotation = _gun.transform.localRotation;
                }
            }
            var p2s = pt.body != null ? WeaponArt.Part2Sprite(key) : null;                    // the second moving part: a hammer, a trigger, a charging handle
            if (p2s != null)
            {
                var p2 = WeaponArt.Part2Info[key]; float hd2 = WeaponArt.Density(key), ang2 = f.GunDeg * Mathf.Deg2Rad, sg = f.Left ? -1f : 1f;
                _part2.gameObject.SetActive(true); _part2.sprite = p2s; _part2.flipY = f.Left; _part2.sortingOrder = _gun.sortingOrder;
                if (p2.style == "charge")
                {
                    float ph = Mathf.Max(WeaponArt.PartPhase("slide", now - _shotAt), _reloadStyle == "mag" || _reloadStyle == "belt" ? WeaponArt.ChargeRack(rr) : 0f), pback = ph * p2.travel / (Ppu * hd2);
                    _part2.transform.localPosition = _gun.transform.localPosition + new Vector3(-pback * Mathf.Cos(ang2), -pback * Mathf.Sin(ang2), -0.012f);
                    _part2.transform.localRotation = _gun.transform.localRotation;
                }
                else
                {
                    float hx2 = (p2.px / hd2 - g[0]) / Ppu, hy2 = (f.Left ? 1f : -1f) * (p2.py / hd2 - g[1]) / Ppu;
                    float ph = p2.style == "trigger" ? WeaponArt.TriggerPhase(now - _shotAt) : WeaponArt.PartPhase("hammer", now - _shotAt);
                    _part2.transform.localPosition = _gun.transform.localPosition + new Vector3(hx2 * Mathf.Cos(ang2) - hy2 * Mathf.Sin(ang2), hx2 * Mathf.Sin(ang2) + hy2 * Mathf.Cos(ang2), -0.012f);
                    _part2.transform.localRotation = Quaternion.Euler(0f, 0f, f.GunDeg + sg * ph * p2.travel);
                }
            }
            // Optics (Optics.cs): a sight on the rail, a laser under the barrel; drawn with the gun, so they ride its kick (a sight on a slide pistol rides the slide)
            AttachmentView(_optic, Optics.OpticOf(weapon), key, g, f, 0, now, Mathf.Max(f.PartOpen, lockBack));
            AttachmentView(_under, Optics.UnderOf(weapon), key, g, f, 2, now);
            // RT 6j: the old magazine leaves as a loose object (CombatFxView lets it fall and lie on the floor)
            if (f.MagDropped && !_magThrown && mag != null && MagDropped != null && WeaponMags.Points.TryGetValue(key, out var mp))
            {
                var c = _gun.transform.TransformPoint(new Vector3((mp[2] - g[0]) / WeaponArt.PixelsPerUnit, (f.Left ? 1f : -1f) * (mp[3] - g[1]) / WeaponArt.PixelsPerUnit, 0f));
                var feet = _gun.transform.parent != null ? _gun.transform.parent.position.y - 0.38f : c.y - 0.5f;
                MagDropped(WeaponArt.LooseMag(key), c, f.GunDeg, f.Left, feet, _base + 5);
            }
            _magThrown = f.MagDropped;

            var hand = HandSprite(armor, cat != "melee");                            // RT 7c: a fist round a melee handle is the big glove, so it reads
            _grip.sprite = hand; _support.sprite = hand;
            _grip.flipX = _support.flipX = f.Left;                                    // cuff stays on the body side
            _grip.transform.localPosition = Local(f.GripX, f.GripY);
            _grip.transform.localRotation = Quaternion.Euler(0f, 0f, f.HandDeg);
            if (WeaponArt.HandWork.TryGetValue(key, out var hw))                                // lever/bolt guns: the trigger hand works the action after each shot
            {
                float hph = WeaponArt.PartPhase(hw.style, psince), hup = hw.style == "bolt" ? WeaponArt.BoltLift(psince) : hph;
                float hu = 1f / (Ppu * WeaponArt.Density(key)), hf = hw.fwd * hph * hu, hv = hw.up * hup * hu * (f.Left ? -1f : 1f), hang = f.GunDeg * Mathf.Deg2Rad;
                _grip.transform.localPosition += new Vector3(hf * Mathf.Cos(hang) - hv * Mathf.Sin(hang), hf * Mathf.Sin(hang) + hv * Mathf.Cos(hang), 0f);
            }
            _support.gameObject.SetActive(f.HasSupport);
            _support.transform.localRotation = Quaternion.identity;               // (the empty hands hang turned)
            if (f.HasSupport) _support.transform.localPosition = Local(f.SupX, f.SupY);
            if (f.HasSupport && pumping) _support.transform.localPosition += pumpOff;
            bool shellOn = f.ShellInHand && f.HasSupport && weapon?.Db != null;                   // a tube reload: the round the hand brings to the port
            _shell.gameObject.SetActive(shellOn);
            if (shellOn)
            {
                var fl = WeaponFeel.For(weapon.Db); _shell.sprite = fl.Reload == "clip" ? CombatFxView.ClipSprite() : CombatFxView.AmmoSprite(fl.CaseKind, true); _shell.flipY = f.Left; _shell.sortingOrder = _support.sortingOrder + 1;
                _shell.transform.localPosition = _support.transform.localPosition + new Vector3(0f, 0f, -0.01f); _shell.transform.localRotation = Quaternion.Euler(0f, 0f, f.GunDeg);
                if (fl.Reload == "clip")                                                   // the clip hangs 3.5 px under the hand, in the gun's frame (its seat is below the hand's home)
                {
                    float a1 = f.GunDeg * Mathf.Deg2Rad, dn = (f.Left ? 1f : -1f) * 3.5f / WeaponArt.PixelsPerUnit;
                    _shell.transform.localPosition += new Vector3(-dn * Mathf.Sin(a1), dn * Mathf.Cos(a1), 0f);
                    if (f.ClipIn > 0.01f) { _shell.sortingOrder = _gun.sortingOrder - 1; _shell.transform.localPosition += new Vector3(0f, 0f, 0.03f); }   // pushed into the receiver: behind the gun
                }
            }
            if (_w2?.Db != null && cat != "melee")
            {
                // RT 7z-c: the left-hand pistol in the other glove, beside the first, with its own kick
                var (cat2, key2) = WeaponArt.Identify(_w2.Db);
                var (sprite2, g2) = WeaponArt.For(cat2, key2);
                _g2 = g2;
                var f2 = HeldPoseMath.Offhand(HeldPoseMath.Frame(cat2, g2, local, HeldPoseMath.Recoil(now - _shotAt2, _recover2, _hold2), _climb2, _kick2, 99f, 1f, "", 1), local);
                _gun2.sprite = sprite2; _gun2.flipY = f2.Left;
                _gun2.sortingOrder = _base + (back ? -3 : 1);                           // behind the right-hand gun
                _gun2.transform.localPosition = Local(f2.GunX, f2.GunY);
                _gun2.transform.localRotation = Quaternion.Euler(0f, 0f, f2.GunDeg);
                _support.gameObject.SetActive(true);
                _support.transform.localPosition = Local(f2.GripX, f2.GripY);
            }
        }

        // character pixel (16x16 sprite, y down) -> marker-local units (sprite spans -0.5..0.5)
        private Vector3 Local(float px, float py) => new Vector3((px - 8f) / 16f, (8f - py - BodyDrop) / 16f, 0f);
        public float BodyDrop;                  // Poses: px the body sits lower (crouched, kneeling, seated) - the hands go down with it

        // nothing equipped: big glove centres beside the torso (torso is columns 5-10, rows 8-11)
        public const float RestRightX = 13.5f, RestLeftX = 2.5f, RestY = 10f;
        // 2026-11-23: the arm swing's pace follows the feet (UnitMotionView.AnimFactor): a walk takes twice the tiles per full swing (half as fast), a run swings at the old walking rate (4.4 tiles/s / 2.17 Hz = 2.03 tiles)
        static float CycleTiles(float tilesPerSecond) => Mathf.Lerp(StrideTiles * 2f, 2.03f, Mathf.Clamp01(tilesPerSecond - 3f));
        const float StrideTiles = 1.2f;                     // a full arm swing (two steps) per this many tiles walked
        private Vector3? _lastPos; private float _stepPhase, _swing;

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
            bool unique = UnitSprites.UniqueGloves.TryGetValue(name, out var ug);   // Boss 1: one-of-a-kind suits
            Color cuff = Hex(unique ? ug.cuff : i >= 0 ? SpriteAtlasIndex.ArmorCuff[i] : "#5a4a36");
            Color glove = Hex(unique ? ug.glove : i >= 0 ? SpriteAtlasIndex.ArmorGlove[i] : "#3d4148");
            Color k = Hex("#141617"), lit = Scale(glove, 1.45f), dark = Scale(glove, 0.65f), none = new Color(0, 0, 0, 0);
            string[] rows = small ? GloveSmall : GloveBig;
            int w = rows[0].Length, h = rows.Length;
            const int D = 2;                                                       // 2026-11-09: the body is doubled (32 px cells), so the glove is drawn at 2x too: same size and place, same look
            var tex = new Texture2D(w * D, h * D) { filterMode = FilterMode.Point };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char ch = rows[y][x];
                    var c = ch == 'K' ? k : ch == 'c' ? cuff : ch == 'l' ? lit : ch == 'g' ? glove : ch == 'd' ? dark : none;
                    for (int dy = 0; dy < D; dy++) for (int dx = 0; dx < D; dx++) tex.SetPixel(x * D + dx, (h - 1 - y) * D + dy, c);
                }
            tex.Apply();
            return HandCache[id] = Sprite.Create(tex, new Rect(0, 0, w * D, h * D), new Vector2(0.5f, 0.5f), 16f * D, 0, SpriteMeshType.FullRect);
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
