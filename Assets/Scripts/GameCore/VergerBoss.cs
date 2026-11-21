// Boss 6: the Verger (Yan, 2026-11-20: "a gothic beast hunter using an axe" -> a scythe; "he comes every run at the end" of the cathedral job, MISSION_CHOIR.md; "he starts as a sprite swinging his scythe ... at 50% he transforms into a beast").
// The fight is Boss.cs (Boss.Verger): the Visor's duel on a war scythe, then at half health the change (Def.Change: hittable at a tenth of the damage for 4 s) and the claws. This file is his entrance and his end.
// NOT built yet: the cathedral job that sends him in at the end of a run (the bell, the Husk waves); the beast's own sprite (until it exists he fights on in the human body); the co-op sync of the change (host-side only).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class VergerBoss
    {
        /// <summary>Puts him on the map and starts the fight (the Testing Ground calls this too; the cathedral job will, at the end of every run).</summary>
        public static Boss.Fight Spawn(int x, int y, bool intro)
        {
            var f = Boss.Spawn(Boss.Verger, x, y); var h = f.Unit; Rt.SetOffset(h, 0f, 0f);
            f.NextMoveAt = Time.time + (intro ? 3.0f : 1.8f);
            if (intro)
            {
                GameLog.Event("The bell goes quiet. From the altar end, a tall man in a long coat walks down the nave with a scythe over his shoulder, and does not hurry.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.2f);
            }
            Banter.Say(h, "This is a church. You do not ring that bell in a church.", 1, 0.3f);
            return f;
        }

        // ---- the beast's drawing: Pixel Lab clips of Yan's generated sprite (ArtSource/mutants_anim/verger_beast/README_BEAST.md, export_verger_beast_clips.py -> Sprites/Mutants/anim/verger_beast_<clip>.bytes; 256x232 cells, the feet 6 px above the bottom, 32 px a tile) ----
        public const float BeastScale = 0.5f;                 // drawn at half the strip's size: about 3 tiles tall, next to the Gaunt's 3 (it grew a lot, 1 tile -> ~3)
        public class Clip
        {
            public string Name; public int Frames; public float[] Ms; public int Hit; public int[] Hits; public bool Loop;      // Hit = the frame the blow lands on (-1: none)
            public float PreHitSeconds { get { float t = 0f; for (int i = 0; i < Hit && i < Ms.Length; i++) t += Ms[i]; return t / 1000f; } }
            /// <summary>The frame at `t` seconds into the clip: -2 = not started, -1 = over (a loop never ends).</summary>
            public int IndexAt(float t)
            {
                if (t < 0f) return -2;
                float total = 0f; foreach (var m in Ms) total += m / 1000f;
                if (Loop) t %= total; else if (t >= total) return -1;
                float acc = 0f; for (int i = 0; i < Ms.Length; i++) { acc += Ms[i] / 1000f; if (t < acc) return i; }
                return Ms.Length - 1;
            }
        }
        // ms per frame (Pixel Lab cannot set timing: ours). The frame counts must match export_verger_beast_clips.py.
        public static readonly Dictionary<string, Clip> Clips = new Dictionary<string, Clip>
        {
            ["walk"]   = new Clip { Name = "walk", Frames = 8, Ms = new float[] { 110, 110, 110, 110, 110, 110, 110, 110 }, Hit = -1, Loop = true },
            ["slam"]   = new Clip { Name = "slam", Frames = 13, Ms = new float[] { 100, 200, 260, 300, 90, 120, 160, 160, 160, 160, 180, 200, 240 }, Hit = 4 },                // rear up (1-3), the slam (4), crouched (5-9), heave up (10-12)
            ["rake"]   = new Clip { Name = "rake", Frames = 13, Ms = new float[] { 100, 100, 120, 140, 120, 70, 120, 110, 120, 100, 120, 160, 200 }, Hit = 6 },       // rears the arm back (2-4), a backhand smear (5, brief), the forward slash (6), recovers
            ["charge"] = new Clip { Name = "charge", Frames = 9, Ms = new float[] { 100, 120, 140, 180, 200, 90, 110, 140, 200 }, Hit = 5 },
            ["sweep"]  = new Clip { Name = "sweep", Frames = 13, Ms = new float[] { 100, 120, 140, 160, 140, 110, 120, 120, 140, 140, 200, 220, 260 }, Hit = 5 },      // draws both arms back (2-4), the low sweep (5-6), arms reaching forward (7-9), rises
            // combos (ArtSource/make_beast_combos.py: the rake, sweep and slam clips stitched where their silhouettes meet; Hits = the frames the blows land on)
            ["rakesweep"] = new Clip { Name = "rakesweep", Frames = 18, Ms = new float[] { 100, 100, 120, 140, 120, 70, 120, 110, 120, 140, 110, 120, 120, 140, 140, 200, 220, 260 }, Hit = 6, Hits = new[] { 6, 10 } },
            ["rakeslam"] = new Clip { Name = "rakeslam", Frames = 22, Ms = new float[] { 100, 100, 120, 140, 120, 70, 120, 110, 120, 100, 200, 260, 300, 90, 120, 160, 160, 160, 160, 180, 200, 240 }, Hit = 6, Hits = new[] { 6, 13 } },
            ["rampage"] = new Clip { Name = "rampage", Frames = 28, Ms = new float[] { 100, 100, 120, 140, 120, 70, 120, 110, 120, 140, 110, 120, 120, 140, 140, 200, 200, 260, 300, 90, 120, 160, 160, 160, 160, 180, 200, 240 }, Hit = 6, Hits = new[] { 6, 10, 19 } },
            ["roar"]   = new Clip { Name = "roar", Frames = 13, Ms = new float[] { 100, 140, 180, 220, 400, 400, 400, 400, 400, 400, 300, 260, 360 }, Hit = 4 },                // the change: rears up (1-3), roars (4-9), settles (10-12): ~4 s
        };
        const float ClipPivotY = 22f / 232f;                   // the feet row is 6 px above the cell's bottom, and the pivot sits 16 px above the feet
        static readonly Dictionary<HumanEnemy, (float x, float y)> _last = new Dictionary<HumanEnemy, (float, float)>();
        public static bool IsBeast(ICombatant u) => u is HumanEnemy && Boss.Of(u) is Boss.Fight f && f.Def == Boss.Verger && f.Beast && !f.Finished;
        public static bool FacesLeft(ICombatant u) { var p = Player.Current; return p != null && p.X < u.X; }
        public static Sprite[] ClipFrames(string name) => Clips.TryGetValue(name, out var c) ? UnitSprites.HDFx("verger_beast_" + name, c.Frames, new Vector2(0.5f, ClipPivotY)) : null;

        /// <summary>Plays `clip` so that its hit frame lands at `hitAt` (a long wind-up shows the stand until the clip begins).</summary>
        public static void StartClip(Boss.Fight f, string clip, float hitAt, float scale = 1f)
        {
            if (!Clips.TryGetValue(clip, out var c)) return;
            f.Clip = clip; f.ClipScale = scale; f.ClipT0 = hitAt - c.PreHitSeconds * scale;       // the fight's pace (1.1 for the beast) stretches the waits, so the clip plays that much slower too
        }

        /// <summary>The beast's picture this frame: the clip that is playing (the change's roar, a move's blow), else the walk cycle while he moves, else the stand (the walk's first frame). Falls back to the old hand-built poses if the clips are missing, and to the man if nothing is.</summary>
        public static Sprite BeastFrame(HumanEnemy h)
        {
            var f = Boss.Of(h); float now = Time.time;
            var (x, y) = Ballistics.Pos(h); bool moving = _last.TryGetValue(h, out var l) && (Mathf.Abs(l.x - x) + Mathf.Abs(l.y - y)) > 0.01f; _last[h] = (x, y);
            if (f.Clip != null && Clips.TryGetValue(f.Clip, out var c))
            {
                int i = c.IndexAt((now - f.ClipT0) / f.ClipScale);
                if (i >= 0) { var cs = ClipFrames(c.Name); if (cs != null && i < cs.Length) return cs[i]; }
                else if (i == -1) f.Clip = null;
            }
            var walk = ClipFrames("walk");
            if (walk != null) return moving ? walk[(int)(now / 0.11f) % walk.Length] : walk[0];
            var legacy = UnitSprites.HDFx("verger_beast", 4, new Vector2(0.5f, 16f / 190f));          // the first, hand-built poses
            return legacy?[0];
        }

        /// <summary>2026-11-21 (Yan: "the double swipe barely looks like an attack"): the beast throws his weight into each blow: for a third of a second after it lands the drawing is pushed up to 0.45 tile toward you (eased out), then settles back. Pure drawing: his tile does not change.</summary>
        public static Vector3 LungeOffset(ICombatant u)
        {
            if (!IsBeast(u)) return Vector3.zero;
            var f = Boss.Of(u); var p = Player.Current; if (f == null || p == null) return Vector3.zero;
            float t = Time.time - f.StruckAt; if (t < 0f || t > 0.33f) return Vector3.zero;
            float k = 1f - t / 0.33f, push = 0.45f * k * k; float dx = p.X - u.X, dy = p.Y - u.Y, d = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dy * dy));
            return new Vector3(dx / d * push, -dy / d * push, 0f);
        }

        /// <summary>His execution (Boss.FinishNow): the scythe and the coat are on the body.</summary>
        public static void Defeated(Player p, HumanEnemy h)
        {
            Story.Journal("The Verger: the cathedral's last hunter kept the bell and the dead apart. He changed at the end, and he is down. His scythe and his coat are yours.");
            GameLog.Event("The beast shrinks into a man in a torn coat, and lies still. The scythe is in his pack.");
        }
    }
}
