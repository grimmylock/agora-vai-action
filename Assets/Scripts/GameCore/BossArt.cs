// 2026-11-21: the creature bosses' drawing (Yan: "use [the Pixel Lab credits] to create the new bosses after you finish the concepts"; BOSS_CONCEPTS_V3.md).
// A boss whose Def has an Art key is drawn from Pixel Lab clips instead of a body + held weapon: the clip a move plays (timed so its hit frame lands on the strike, like the
// Verger's parked beast), else the walk loop while it moves, else the idle loop; "hurt" when a hit lands and nothing else plays; at the break the "death" clip, held on its
// last frame. Strips: Sprites/Mutants/anim/boss_<key>_<clip>.bytes, written with the table below by ArtSource/export_boss_clips.py (32 px a tile, feet 6 px above each cell's bottom).
// Pure drawing: the fight itself is Boss.cs. Co-op: host-side like every boss (the snapshot does not carry boss clips yet).
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public static class BossArt
    {
        public class Art { public string Key, Facing = "left"; public int W, H; public float Scale = 1f; public Dictionary<string, VergerBoss.Clip> Clips; }

        public static readonly Dictionary<string, Art> Table = new Dictionary<string, Art>
        {
            // clips begin
            ["choir"] = new Art { Key = "choir", W = 256, H = 152, Facing = "left", Clips = new Dictionary<string, VergerBoss.Clip> {
                ["death"] = new VergerBoss.Clip { Name = "boss_choir_death", Frames = 13, Ms = new float[] { 124, 128, 132, 137, 141, 145, 150, 154, 158, 162, 167, 171, 175 }, Hit = -1 },
                ["grasp"] = new VergerBoss.Clip { Name = "boss_choir_grasp", Frames = 13, Ms = new float[] { 110, 128, 146, 164, 182, 200, 80, 128, 137, 145, 154, 162, 171 }, Hit = 6 },
                ["hurt"] = new VergerBoss.Clip { Name = "boss_choir_hurt", Frames = 9, Ms = new float[] { 126, 132, 138, 144, 150, 156, 162, 168, 174 }, Hit = -1 },
                ["idle"] = new VergerBoss.Clip { Name = "boss_choir_idle", Frames = 8, Ms = new float[] { 150, 150, 150, 150, 150, 150, 150, 150 }, Hit = -1, Loop = true },
                ["note"] = new VergerBoss.Clip { Name = "boss_choir_note", Frames = 13, Ms = new float[] { 110, 140, 170, 200, 80, 126, 133, 140, 146, 153, 160, 166, 173 }, Hit = 4 },
                ["scream"] = new VergerBoss.Clip { Name = "boss_choir_scream", Frames = 13, Ms = new float[] { 110, 140, 170, 200, 80, 126, 133, 140, 146, 153, 160, 166, 173 }, Hit = 4 },
                ["walk"] = new VergerBoss.Clip { Name = "boss_choir_walk", Frames = 12, Ms = new float[] { 110, 110, 110, 110, 110, 110, 110, 110, 110, 110, 110, 110 }, Hit = -1, Loop = true },
            } },
            ["ferryman"] = new Art { Key = "ferryman", W = 256, H = 176, Facing = "left", Clips = new Dictionary<string, VergerBoss.Clip> {
                ["bite"] = new VergerBoss.Clip { Name = "boss_ferryman_bite", Frames = 13, Ms = new float[] { 110, 122, 135, 148, 161, 174, 187, 200, 80, 132, 144, 156, 168 }, Hit = 8 },
                ["claw"] = new VergerBoss.Clip { Name = "boss_ferryman_claw", Frames = 13, Ms = new float[] { 110, 125, 140, 155, 170, 185, 200, 80, 130, 140, 150, 160, 170 }, Hit = 7 },
                ["death"] = new VergerBoss.Clip { Name = "boss_ferryman_death", Frames = 13, Ms = new float[] { 124, 128, 132, 137, 141, 145, 150, 154, 158, 162, 167, 171, 175 }, Hit = -1 },
                ["hurt"] = new VergerBoss.Clip { Name = "boss_ferryman_hurt", Frames = 9, Ms = new float[] { 126, 132, 138, 144, 150, 156, 162, 168, 174 }, Hit = -1 },
                ["idle"] = new VergerBoss.Clip { Name = "boss_ferryman_idle", Frames = 8, Ms = new float[] { 150, 150, 150, 150, 150, 150, 150, 150 }, Hit = -1, Loop = true },
                ["lure"] = new VergerBoss.Clip { Name = "boss_ferryman_lure", Frames = 13, Ms = new float[] { 110, 128, 146, 164, 182, 200, 80, 128, 137, 145, 154, 162, 171 }, Hit = 6 },
                ["walk"] = new VergerBoss.Clip { Name = "boss_ferryman_walk", Frames = 12, Ms = new float[] { 110, 110, 110, 110, 110, 110, 110, 110, 110, 110, 110, 110 }, Hit = -1, Loop = true },
            } },
            ["grinder"] = new Art { Key = "grinder", W = 224, H = 136, Facing = "left", Clips = new Dictionary<string, VergerBoss.Clip> {
                ["bite"] = new VergerBoss.Clip { Name = "boss_grinder_bite", Frames = 13, Ms = new float[] { 110, 122, 135, 148, 161, 174, 187, 200, 80, 132, 144, 156, 168 }, Hit = 8 },
                ["death"] = new VergerBoss.Clip { Name = "boss_grinder_death", Frames = 13, Ms = new float[] { 124, 128, 132, 137, 141, 145, 150, 154, 158, 162, 167, 171, 175 }, Hit = -1 },
                ["hurt"] = new VergerBoss.Clip { Name = "boss_grinder_hurt", Frames = 9, Ms = new float[] { 126, 132, 138, 144, 150, 156, 162, 168, 174 }, Hit = -1 },
                ["idle"] = new VergerBoss.Clip { Name = "boss_grinder_idle", Frames = 8, Ms = new float[] { 150, 150, 150, 150, 150, 150, 150, 150 }, Hit = -1, Loop = true },
                ["slam"] = new VergerBoss.Clip { Name = "boss_grinder_slam", Frames = 13, Ms = new float[] { 110, 122, 135, 148, 161, 174, 187, 200, 80, 132, 144, 156, 168 }, Hit = 8 },
                ["spit"] = new VergerBoss.Clip { Name = "boss_grinder_spit", Frames = 13, Ms = new float[] { 110, 132, 155, 177, 200, 80, 127, 135, 142, 150, 157, 165, 172 }, Hit = 5 },
                ["walk"] = new VergerBoss.Clip { Name = "boss_grinder_walk", Frames = 8, Ms = new float[] { 110, 110, 110, 110, 110, 110, 110, 110 }, Hit = -1, Loop = true },
            } },
            ["mother_hollow"] = new Art { Key = "mother_hollow", W = 232, H = 112, Facing = "left", Clips = new Dictionary<string, VergerBoss.Clip> {
                ["bite"] = new VergerBoss.Clip { Name = "boss_mother_hollow_bite", Frames = 13, Ms = new float[] { 110, 132, 155, 177, 200, 80, 127, 135, 142, 150, 157, 165, 172 }, Hit = 5 },
                ["death"] = new VergerBoss.Clip { Name = "boss_mother_hollow_death", Frames = 13, Ms = new float[] { 124, 128, 132, 137, 141, 145, 150, 154, 158, 162, 167, 171, 175 }, Hit = -1 },
                ["howl"] = new VergerBoss.Clip { Name = "boss_mother_hollow_howl", Frames = 13, Ms = new float[] { 110, 155, 200, 80, 126, 132, 138, 144, 150, 156, 162, 168, 174 }, Hit = 3 },
                ["hurt"] = new VergerBoss.Clip { Name = "boss_mother_hollow_hurt", Frames = 9, Ms = new float[] { 126, 132, 138, 144, 150, 156, 162, 168, 174 }, Hit = -1 },
                ["idle"] = new VergerBoss.Clip { Name = "boss_mother_hollow_idle", Frames = 8, Ms = new float[] { 150, 150, 150, 150, 150, 150, 150, 150 }, Hit = -1, Loop = true },
                ["pounce"] = new VergerBoss.Clip { Name = "boss_mother_hollow_pounce", Frames = 13, Ms = new float[] { 110, 122, 135, 148, 161, 174, 187, 200, 80, 132, 144, 156, 168 }, Hit = 8 },
                ["spit"] = new VergerBoss.Clip { Name = "boss_mother_hollow_spit", Frames = 9, Ms = new float[] { 110, 132, 155, 177, 200, 80, 135, 150, 165 }, Hit = 5 },
                ["walk"] = new VergerBoss.Clip { Name = "boss_mother_hollow_walk", Frames = 8, Ms = new float[] { 110, 110, 110, 110, 110, 110, 110, 110 }, Hit = -1, Loop = true },
            } },
            ["orderly"] = new Art { Key = "orderly", W = 224, H = 176, Facing = "left", Clips = new Dictionary<string, VergerBoss.Clip> {
                ["chop"] = new VergerBoss.Clip { Name = "boss_orderly_chop", Frames = 13, Ms = new float[] { 110, 128, 146, 164, 182, 200, 80, 128, 137, 145, 154, 162, 171 }, Hit = 6 },
                ["death"] = new VergerBoss.Clip { Name = "boss_orderly_death", Frames = 13, Ms = new float[] { 124, 128, 132, 137, 141, 145, 150, 154, 158, 162, 167, 171, 175 }, Hit = -1 },
                ["drag"] = new VergerBoss.Clip { Name = "boss_orderly_drag", Frames = 13, Ms = new float[] { 110, 155, 200, 80, 126, 132, 138, 144, 150, 156, 162, 168, 174 }, Hit = 3 },
                ["drip"] = new VergerBoss.Clip { Name = "boss_orderly_drip", Frames = 13, Ms = new float[] { 110, 128, 146, 164, 182, 200, 80, 128, 137, 145, 154, 162, 171 }, Hit = 6 },
                ["grab"] = new VergerBoss.Clip { Name = "boss_orderly_grab", Frames = 13, Ms = new float[] { 110, 122, 135, 148, 161, 174, 187, 200, 80, 132, 144, 156, 168 }, Hit = 8 },
                ["hurt"] = new VergerBoss.Clip { Name = "boss_orderly_hurt", Frames = 9, Ms = new float[] { 126, 132, 138, 144, 150, 156, 162, 168, 174 }, Hit = -1 },
                ["idle"] = new VergerBoss.Clip { Name = "boss_orderly_idle", Frames = 8, Ms = new float[] { 150, 150, 150, 150, 150, 150, 150, 150 }, Hit = -1, Loop = true },
                ["overhead"] = new VergerBoss.Clip { Name = "boss_orderly_overhead", Frames = 13, Ms = new float[] { 110, 118, 126, 134, 142, 150, 159, 167, 175, 183, 191, 200, 80 }, Hit = 12 },
                ["saw"] = new VergerBoss.Clip { Name = "boss_orderly_saw", Frames = 13, Ms = new float[] { 110, 122, 135, 148, 161, 174, 187, 200, 80, 132, 144, 156, 168 }, Hit = 8 },
                ["walk"] = new VergerBoss.Clip { Name = "boss_orderly_walk", Frames = 8, Ms = new float[] { 110, 110, 110, 110, 110, 110, 110, 110 }, Hit = -1, Loop = true },
                ["run"] = new VergerBoss.Clip { Name = "boss_orderly_walk", Frames = 8, Ms = new float[] { 62, 62, 62, 62, 62, 62, 62, 62 }, Hit = -1, Loop = true },
            } },
            ["specimen_eye"] = new Art { Key = "specimen_eye", W = 60, H = 68, Facing = "front", Clips = new Dictionary<string, VergerBoss.Clip> {
                ["charge"] = new VergerBoss.Clip { Name = "boss_specimen_eye_charge", Frames = 9, Ms = new float[] { 110, 122, 135, 148, 161, 174, 187, 200, 80 }, Hit = 8 },
                ["death"] = new VergerBoss.Clip { Name = "boss_specimen_eye_death", Frames = 9, Ms = new float[] { 126, 132, 138, 144, 150, 156, 162, 168, 174 }, Hit = -1 },
                ["idle"] = new VergerBoss.Clip { Name = "boss_specimen_eye_idle", Frames = 8, Ms = new float[] { 150, 150, 150, 150, 150, 150, 150, 150 }, Hit = -1, Loop = true },
            } },
            ["specimen_v2"] = new Art { Key = "specimen_v2", W = 152, H = 152, Facing = "front", Clips = new Dictionary<string, VergerBoss.Clip> {
                ["death"] = new VergerBoss.Clip { Name = "boss_specimen_v2_death", Frames = 13, Ms = new float[] { 124, 128, 132, 137, 141, 145, 150, 154, 158, 162, 167, 171, 175 }, Hit = -1 },
                ["hurt"] = new VergerBoss.Clip { Name = "boss_specimen_v2_hurt", Frames = 9, Ms = new float[] { 126, 132, 138, 144, 150, 156, 162, 168, 174 }, Hit = -1 },
                ["idle"] = new VergerBoss.Clip { Name = "boss_specimen_v2_idle", Frames = 8, Ms = new float[] { 150, 150, 150, 150, 150, 150, 150, 150 }, Hit = -1, Loop = true },
                ["laser"] = new VergerBoss.Clip { Name = "boss_specimen_v2_laser", Frames = 13, Ms = new float[] { 110, 140, 170, 200, 80, 126, 133, 140, 146, 153, 160, 166, 173 }, Hit = 4 },
                ["mind"] = new VergerBoss.Clip { Name = "boss_specimen_v2_mind", Frames = 9, Ms = new float[] { 110, 140, 170, 200, 80, 132, 144, 156, 168 }, Hit = 4 },
                ["orbs"] = new VergerBoss.Clip { Name = "boss_specimen_v2_orbs", Frames = 13, Ms = new float[] { 110, 125, 140, 155, 170, 185, 200, 80, 130, 140, 150, 160, 170 }, Hit = 7 },
                ["walk"] = new VergerBoss.Clip { Name = "boss_specimen_v2_walk", Frames = 8, Ms = new float[] { 110, 110, 110, 110, 110, 110, 110, 110 }, Hit = -1, Loop = true },
            } },            // clips end
        };

        /// <summary>How big each boss is drawn (x its strip's 32 px a tile), set apart from the generated table above.</summary>
        static readonly Dictionary<string, float> Scales = new Dictionary<string, float> { ["mother_hollow"] = 0.75f, ["singer"] = 1f, ["ferryman"] = 0.75f, ["specimen"] = 0.7f, ["excavator"] = 1f, ["grinder"] = 0.85f, ["choir"] = 0.75f, ["orderly"] = 0.85f, ["specimen_v2"] = 0.75f, ["singer_wall"] = 2f, ["specimen_eye"] = 1f };
        public static Art Of(string key) => key != null && Table.TryGetValue(key, out var a) ? a : null;
        public static float ScaleOf(string key) => key != null && Scales.TryGetValue(key, out var s) ? s : 1f;

        static Art ArtOf(ICombatant u) => Boss.Of(u) is Boss.Fight f ? Of(f.Def.Art) : null;
        /// <summary>Is this unit drawn from boss clips (a creature boss, or the Verger's parked beast)?</summary>
        public static bool Drawn(ICombatant u) => VergerBoss.IsBeast(u) || (u is HumanEnemy && ArtOf(u) != null && Frames(ArtOf(u), "idle") != null);
        public static float Scale(ICombatant u) => VergerBoss.IsBeast(u) ? VergerBoss.BeastScale : Boss.Of(u) is Boss.Fight f && f.Def.Art != null ? ScaleOf(f.Def.Art) : 1f;
        /// <summary>Mirror it? The strips face where the stand faced; side-on bosses turn toward the nearest player, the ones facing the camera never flip.</summary>
        public static bool Flip(ICombatant u)
        {
            if (VergerBoss.IsBeast(u)) return VergerBoss.FacesLeft(u);
            var a = ArtOf(u); Player p = null; int bd = int.MaxValue; foreach (var q in Player.All) { if (!q.IsAlive()) continue; int d = CombatantUtil.Distance(u, q); if (d < bd) { bd = d; p = q; } } if (a == null || p == null || a.Facing == "front") return false;
            bool targetLeft = p.X < u.X; return a.Facing == "left" ? !targetLeft : targetLeft;
        }
        public static Vector3 Lunge(ICombatant u) => VergerBoss.LungeOffset(u);
        /// <summary>2026-11-21: the hit profile key of a creature boss (GameData.HitProfiles "boss:&lt;art&gt;", made by ArtSource/make_hit_profiles.py from its idle clip at its drawn size), null for anyone else.</summary>
        public static string HitKey(HumanEnemy h) => Boss.Of(h) is Boss.Fight f && f.Def.Art != null && GameData.HitProfiles.All.ContainsKey("boss:" + f.Def.Art) ? "boss:" + f.Def.Art : null;

        public static Sprite[] Frames(Art a, string clip) => a != null && a.Clips != null && a.Clips.TryGetValue(clip, out var c) ? UnitSprites.HDFx(c.Name, c.Frames, new Vector2(0.5f, a.Key == "specimen_eye" ? -(SpecimenGrown.EyeLift * 32f - 6f) / a.H : 6f / a.H)) : null;   // (the grown Specimen's eye floats EyeLift tiles over its tile)

        /// <summary>Starts a move's clip so its hit frame lands at `hitAt` (Boss.Begin). False when this boss has no such clip (the Verger's beast then gets its own).</summary>
        public static bool Start(Boss.Fight f, string clip, float hitAt, float scale)
        {
            var a = Of(f.Def.Art); if (a == null || clip == null || !a.Clips.TryGetValue(clip, out var c)) return false;
            f.Clip = clip; f.ClipScale = scale; f.ClipT0 = hitAt - c.PreHitSeconds * scale; return true;
        }
        /// <summary>A clip that is not a blow (hurt, death, a call): from now, at its own pace.</summary>
        public static void Play(Boss.Fight f, string clip) { var a = Of(f.Def.Art); if (a != null && a.Clips.ContainsKey(clip)) { f.Clip = clip; f.ClipScale = 1f; f.ClipT0 = Time.time; } }
        /// <summary>A hit landed on it: the hurt clip, unless a move's clip is playing (a blow is not interrupted by the drawing).</summary>
        public static void Hurt(Boss.Fight f) { if (f.Def.Art != null && f.Doing == null && (f.Clip == null || f.Clip == "idle" || f.Clip == "walk" || f.Clip == "drive")) Play(f, "hurt"); }

        static readonly Dictionary<HumanEnemy, (float x, float y)> _last = new Dictionary<HumanEnemy, (float, float)>();
        /// <summary>Its picture this frame.</summary>
        public static Sprite Frame(HumanEnemy h)
        {
            if (VergerBoss.IsBeast(h)) return VergerBoss.BeastFrame(h);
            var f = Boss.Of(h); var a = f != null ? Of(f.Def.Art) : null; if (a == null) return null; float now = Time.time;
            var (x, y) = Ballistics.Pos(h); bool moving = _last.TryGetValue(h, out var l) && (Mathf.Abs(l.x - x) + Mathf.Abs(l.y - y)) > 0.01f; _last[h] = (x, y);
            if ((f.Broken || f.Finished) && a.Clips.TryGetValue("death", out var dc))
            {
                if (f.Clip != "death") Play(f, "death");
                int di = dc.IndexAt(now - f.ClipT0); var ds = Frames(a, "death"); if (ds != null) return ds[di < 0 ? ds.Length - 1 : Mathf.Min(di, ds.Length - 1)];
            }
            if (f.Clip != null && a.Clips.TryGetValue(f.Clip, out var c) && !c.Loop)
            {
                int i = c.IndexAt((now - f.ClipT0) / f.ClipScale);
                if (i >= 0) { var cs = Frames(a, f.Clip); if (cs != null && i < cs.Length) return cs[i]; }
                else if (i == -2) { var idle0 = Frames(a, "idle"); if (idle0 != null) return idle0[0]; }     // a long wind-up: the stand until the clip begins
                else f.Clip = null;
            }
            string loop = moving ? (a.Clips.ContainsKey("walk") ? "walk" : a.Clips.ContainsKey("drive") ? "drive" : "idle") : "idle";
            if (moving && f.Stalking && h.IsAggroed && a.Clips.ContainsKey("run")) loop = "run";                          // 2026-11-22: a hunting stalker runs (the walk strip played faster)
            var ls = Frames(a, loop); if (ls == null) return null;
            var lc = a.Clips[loop]; int li = lc.IndexAt(now + (h.GetHashCode() & 7) * 0.13f); return ls[Mathf.Clamp(li, 0, ls.Length - 1)];
        }
    }
}
