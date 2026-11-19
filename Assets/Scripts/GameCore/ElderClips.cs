// The Gaunts' Pixel Lab animations (2026-11-19, ArtSource/mutants_anim/gaunt/pixellab/README.md): the Elder's (table in ElderClips.Data.cs, ArtSource/export_elder_clips.py) and the small Gaunt's (ElderClips.Small.cs,
// ArtSource/export_small_gaunt_clips.py). Each clip is a strip Sprites/Mutants/anim/<elder_gaunt|gaunt>_<clip>.bytes of Cells cells (W x H), played in `Step` order with `Ms` per step (the roar repeats and shakes cells, the swipe plays
// its arc forwards and then backwards, the combos list the cell of every step). The combos are also registered as attack animations so MutantAttack can play them: Contacts are the ms at which a hit lands,
// the last combo4 contact is the slam's ring.
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static partial class ElderClips
    {
        public class Clip { public int Cells; public int[] Step, Ms, Contacts; public int TotalMs; }
        public class ComboAnim : AttackAnim { public int[] Contacts; public bool SlamLast; public Clip Clip; }

        static Dictionary<string, Clip> Table(string who) => who == Gaunt.ElderName ? All : who == Gaunt.Name ? Small : null;
        public static Clip Get(string who, string clip) => clip != null && Table(who) is Dictionary<string, Clip> t && t.TryGetValue(clip, out var c) ? c : null;
        public static Clip Get(string clip) => Get(Gaunt.ElderName, clip);
        /// <summary>The cell to show `tMs` after the clip began (the last step holds), or -1.</summary>
        public static int CellAt(Clip c, float tMs)
        {
            if (c == null) return -1; float acc = 0f;
            for (int i = 0; i < c.Ms.Length; i++) { acc += c.Ms[i]; if (tMs < acc) return c.Step[i]; }
            return c.Step[c.Step.Length - 1];
        }
        /// <summary>The picture of `clip` of this Gaunt at `tMs`, or null when the strip is not in the project.</summary>
        public static Sprite Sprite(string who, string clip, float tMs)
        {
            var c = Get(who, clip); if (c == null) return null;
            var frames = UnitSprites.GauntAnim(who, clip, c.Cells); if (frames == null) return null;
            return frames[Mathf.Clamp(CellAt(c, tMs), 0, frames.Length - 1)];
        }
        public static Sprite Sprite(string clip, float tMs) => Sprite(Gaunt.ElderName, clip, tMs);
        public static bool Has(string who, string clip) => Get(who, clip) != null && UnitSprites.GauntAnim(who, clip, Get(who, clip).Cells) != null;
        public static bool Has(string clip) => Has(Gaunt.ElderName, clip);
        public static float Seconds(string who, string clip) => (Get(who, clip)?.TotalMs ?? 0) / 1000f;
        public static float Seconds(string clip) => Seconds(Gaunt.ElderName, clip);

        static bool _registered;
        /// <summary>Puts the combos into MutantAttackData (hit times and clip) the first time the attack table is read: the Elder's combo2/3/4, the small Gaunt's stab and swipe.</summary>
        public static void Register()
        {
            if (_registered) return; _registered = true;
            Reg(Gaunt.ElderName, All, k => k.StartsWith("combo"), W, H, PivotY);
            Reg(Gaunt.Name, Small, k => k == "stab" || k == "swipe", SmallW, SmallH, SmallPivotY);
        }
        static void Reg(string who, Dictionary<string, Clip> src, System.Func<string, bool> pick, int w, int h, float pivot)
        {
            if (!MutantAttackData.Table.TryGetValue(who, out var t)) MutantAttackData.Table[who] = t = new Dictionary<string, AttackAnim>();
            foreach (var kv in src)
            {
                if (!pick(kv.Key)) continue; var c = kv.Value;
                int ci = 0, acc = 0; while (ci < c.Ms.Length && acc < c.Contacts[0]) acc += c.Ms[ci++];
                t[kv.Key] = new ComboAnim { Frames = c.Ms.Length, W = w, H = h, Contact = ci, ContactMs = c.Contacts[0], TotalMs = c.TotalMs, PivotY = pivot, Ms = c.Ms, Travel = new float[c.Ms.Length], Contacts = c.Contacts, SlamLast = who == Gaunt.ElderName && kv.Key == "combo4", Clip = c };
            }
        }
        /// <summary>The attack picture a combo is showing `tMs` after it began, or null.</summary>
        public static Sprite AttackSprite(string who, string kind, float tMs) => Sprite(who, kind, tMs);
    }
}
