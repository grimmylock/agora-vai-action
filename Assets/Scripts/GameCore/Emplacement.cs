// The Barrier's two concrete watchtowers and their machine guns (Yan, 2026-11-18: "2 raised concrete watchtowers with the machineguns" ... "heavy machineguns on tripods (not lootable, only usable)").
// Each tower (Barrier.TowerCols; the map puts a 3x2 block of "part" tiles with the tower prop on the deck, and ONE open tile in it, the gun nook, right behind the barrier) has a gun:
//  - E on the steps under the tower (its south face): you climb in. You stand in the nook (it can be reached no other way), your legs are locked (no walking, sprinting or diving), your own gun is put away
//    and a belt-fed machine gun (a Corvan LMG, 200-round belt) is in your hands; mouse aims, left click fires like any gun. You fire over the barrier from the nook (the shooter-next-to-cover rule).
//  - The belt swaps by itself when it runs dry (no reload) from the tower's box: 900 rounds, refilled 12 a second while nobody is on it. The gun is part of the tower: not an item, never lootable.
//  - E again, going down, dying, or leaving the map: you step out onto the steps and your own gun comes back. A save puts everyone off first (Save -> ReleaseAll): nobody is ever saved holding it.
//  - Co-op: one gunner per tower, any player can man either.
// The picture: the tower prop already carries a tripod gun; while you man it you are drawn up on the platform, over the tower (TacticalGridView.DrawUnit, LiftOf).
// ponytail: no tripod sprite for the gun in your hands (you hold a normal LMG), no mount animation, the tower has no hit points (the wall in front is what gets shot), NPCs don't use the guns.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Emplacement
    {
        public const int Belt = 200; public const float Stock = 900f, Refill = 12f, Lift = 2.5f;
        public const string Key = "CorvanLMG";
        public class Gun { public int Tx, NookY, StepsY; public float Box = Stock; public Player By; }
        class Held { public Gun G; public Weapon Saved; public int LastMag; public string Cat, WKey; }
        static readonly Dictionary<Player, Held> Mounted = new Dictionary<Player, Held>();
        static List<Gun> _guns;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Mounted.Clear(); _guns = null; }

        public static List<Gun> Guns => _guns ?? (_guns = Barrier.TowerCols().Select(tx => new Gun { Tx = tx, NookY = Barrier.BottomY(tx) + 1, StepsY = Barrier.BottomY(tx) + 3 }).ToList());

        public static bool IsMounted(Player p) => p != null && Mounted.ContainsKey(p);
        public static Gun GunOf(Player p) => Mounted.TryGetValue(p, out var h) ? h.G : null;
        /// <summary>How far up (tiles) a gunner is drawn (0 for anyone else).</summary>
        public static float LiftOf(ICombatant u) => u is Player p && IsMounted(p) ? Lift : 0f;

        static Gun Near(Player p) => Barrier.On ? Guns.FirstOrDefault(g => g.By == null && System.Math.Abs(p.X - g.Tx) <= 1 && System.Math.Abs(p.Y - g.StepsY) <= 1) : null;

        /// <summary>The E prompt: leave the gun, or climb to one.</summary>
        public static (int x, int y, string label)? Prompt(Player p)
        {
            if (IsMounted(p)) return (p.X, p.Y, "Leave the machine gun");
            var g = Near(p); return g == null ? ((int, int, string)?)null : (g.Tx, g.StepsY, "Man the machine gun");
        }

        public static bool TryInteract(Player p)
        {
            if (IsMounted(p)) { Dismount(p); return true; }
            var g = Near(p);
            if (g == null || !p.IsAlive() || Body.Down(p) || Story.InCutscene) return false;
            var db = WeaponDatabase.Get("heavy", Key); if (db == null) return false;
            var w = new Weapon(db.Name, db.BaseDamage, Belt, "Standard", new Dictionary<string, int> { ["Standard"] = 0, ["HollowPoint"] = 0, ["ArmorPiercing"] = 0 }) { Owner = p, Db = db, RangeTiles = RangeUtil.ParseRangeToTiles(db.Range) };
            Mounted[p] = new Held { G = g, Saved = p.EquippedWeapon, LastMag = Belt, Cat = p.EquippedWeaponCategory, WKey = p.EquippedWeaponKey };
            g.By = p; g.Box -= Belt; p.EquippedWeapon = w;
            p.X = g.Tx; p.Y = g.NookY; Rt.SetOffset(p, 0f, 0f);
            GameLog.Write("You climb up and take the machine gun. E to leave it.");
            return true;
        }

        public static void Dismount(Player p)
        {
            if (!Mounted.TryGetValue(p, out var h)) return;
            Mounted.Remove(p); h.G.By = null;
            p.EquippedWeapon = h.Saved; p.EquippedWeaponCategory = h.Cat; p.EquippedWeaponKey = h.WKey;
            var (x, y) = TerrainMap.FindNearestEnterable(h.G.Tx, h.G.StepsY, 3); p.X = x; p.Y = y; Rt.SetOffset(p, 0f, 0f);
            GameLog.Write("You climb back down.");
        }

        /// <summary>Everyone off (a save, a new map).</summary>
        public static void ReleaseAll() { foreach (var p in Mounted.Keys.ToList()) Dismount(p); }

        /// <summary>Every frame (Rt.WorldTick): the box fills, the belt swaps, and gunners who died, went down or left the map come off.</summary>
        public static void Tick(float dt)
        {
            if (_guns != null) foreach (var g in _guns) if (g.By == null) g.Box = Mathf.Min(Stock, g.Box + Refill * dt);
            foreach (var p in Mounted.Keys.ToList())
            {
                var h = Mounted[p];
                if (!p.IsAlive() || Body.Down(p) || !Barrier.On) { Dismount(p); continue; }
                var w = p.EquippedWeapon; if (w == null || w.Db == null || w.Db.Name != WeaponDatabase.Get("heavy", Key)?.Name) { Dismount(p); continue; }
                if (w.CurrentMagazine < h.LastMag) h.LastMag = w.CurrentMagazine;
                if (w.CurrentMagazine <= 0 && h.G.Box >= 1f) { int take = Mathf.Min(Belt, Mathf.FloorToInt(h.G.Box)); w.CurrentMagazine = take; h.G.Box -= take; h.LastMag = take; }
                if (w.CurrentMagazine <= 0 && h.G.Box < 1f && Time.time % 5f < dt) GameLog.Write("The ammo box is empty. It fills slowly.");
                p.X = h.G.Tx; p.Y = h.G.NookY;                                                       // you don't move
            }
        }
    }
}
