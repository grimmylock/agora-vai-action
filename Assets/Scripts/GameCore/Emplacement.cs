// The Barrier's two concrete watchtowers and their machine guns (Yan, 2026-11-18: "2 raised concrete watchtowers with the machineguns" ... "heavy machineguns on tripods (not lootable, only usable)").
// Each tower (Barrier.TowerCols) is a small concrete building in the yard right behind the deck (TierMaps: windows on the north wall, a door on the south, three tiles inside), so the ordinary building roof rules apply
// (RoofView): from outside you see its slab roof, step in and the roof comes off and you see the inside. The gun stands on the middle inside tile (the nook).
//  - E inside the tower (or in its doorway, next to the gun): you take the gun. Your legs are locked (no walking, sprinting or diving), your own gun is put away and a belt-fed machine gun (a Corvan LMG, 200-round belt) is in your hands;
//    mouse aims, left click fires like any gun, out through the windows. A tower gun is high above the wall, so the wall never stops your rounds (Ballistics.Trace).
//  - The belt swaps by itself when it runs dry (no reload) from the tower's box: 900 rounds, refilled 12 a second while nobody is on it. The gun is part of the tower: not an item, never lootable.
//  - E again, going down, dying, or leaving the map: you step off and your own gun comes back. A save puts everyone off first (Save -> ReleaseAll): nobody is ever saved holding it.
//  - Co-op: one gunner per tower, any player can man either.
// ponytail: no tripod sprite for the gun in your hands (you hold a normal LMG), no mount animation, the tower has no hit points (the wall in front is what gets shot), NPCs don't use the guns.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Emplacement
    {
        public const int Belt = 200; public const float Stock = 900f, Refill = 12f, Lift = 0f;
        public const string Key = "CorvanLMG";
        public const int TowerHp = 6000; public const int CollapseDamage = 750;
        public class Gun { public int Tx, NookY, StepsY; public float Box = Stock; public Player By; public int Hp = TowerHp; public bool Down; public int Top => NookY - 1; }
        class Held { public Gun G; public Weapon Saved; public int LastMag; public string Cat, WKey; }
        static readonly Dictionary<Player, Held> Mounted = new Dictionary<Player, Held>();
        static List<Gun> _guns;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Mounted.Clear(); _guns = null; }

        public static List<Gun> Guns => _guns ?? (_guns = Barrier.TowerCols().Select(tx => new Gun { Tx = tx, NookY = Barrier.TowerTop(tx) + 1, StepsY = Barrier.TowerTop(tx) + 2 }).ToList());

        /// <summary>The standing tower whose walls (5x3 tiles, windows and door included) cover this tile, or null.</summary>
        public static Gun TowerAt(int x, int y) { if (!Barrier.On) return null; foreach (var g in Guns) if (!g.Down && x >= g.Tx - 2 && x <= g.Tx + 2 && y >= g.Top && y <= g.Top + 2) return g; return null; }

        /// <summary>A hit on any tile of a tower (TerrainMap.DamageTile): it all goes to the tower's hit points, true if it took it. At 0 the tower crumbles (Collapse).</summary>
        public static bool Hurt(int x, int y, int amount)
        {
            var g = TowerAt(x, y); if (g == null || amount <= 0) return false;
            g.Hp = System.Math.Max(0, g.Hp - amount);
            int dmg = Mathf.RoundToInt(Barrier.WallHp * 0.95f * (1f - g.Hp / (float)TowerHp));                                 // the cracks on its walls follow its hit points
            for (int ty = g.Top; ty <= g.Top + 2; ty++) for (int tx = g.Tx - 2; tx <= g.Tx + 2; tx++) { var t = TerrainMap.GetTile(tx, ty); if (t.Type == TileType.Solid) { t.Damage = dmg; TerrainMap.DirtyTiles.Add((tx, ty)); } }
            if (g.Hp <= 0) Collapse(g);
            return true;
        }

        /// <summary>The tower crumbles (Yan, 2026-11-18: "if a player is inside he takes 750 damage and is thrown out"): the gunner lets go, everyone inside takes 750 and is thrown out onto the yard,
        /// the walls and roof are gone and a heap of rubble (the "tower_ruin" sprite) is left.</summary>
        public static void Collapse(Gun g)
        {
            if (g.Down) return; g.Down = true; g.By = null;
            var inside = Player.All.Where(p => p.X >= g.Tx - 2 && p.X <= g.Tx + 2 && p.Y >= g.Top && p.Y <= g.Top + 2).ToList();
            foreach (var p in inside.Where(IsMounted)) { var h = Mounted[p]; Mounted.Remove(p); p.EquippedWeapon = h.Saved; p.EquippedWeaponCategory = h.Cat; p.EquippedWeaponKey = h.WKey; }
            for (int ty = g.Top; ty <= g.Top + 2; ty++) for (int tx = g.Tx - 2; tx <= g.Tx + 2; tx++) TerrainMap.SetTileType(tx, ty, TileType.Difficult, tx == g.Tx && ty == g.Top + 2 ? "tower_ruin" : "part");
            RoofView.Drop(g.Tx, g.Top + 1);
            Sfx.Play("door_break", TacticalGridView.GridToWorld(g.Tx, g.Top + 1));
            GameLog.Event("A Barrier watchtower collapses in a cloud of dust!");
            foreach (var p in inside)
            {
                var (x, y) = TerrainMap.FindNearestEnterable(g.Tx + (p.X >= g.Tx ? 1 : -1) * 2, g.Top + 4, 3); p.X = x; p.Y = y; Rt.SetOffset(p, 0f, 0f);
                p.TakeDamage(CollapseDamage); Rt.Knockdown(p, 2f);
                GameLog.Write($"The collapsing tower throws you out for {CollapseDamage} damage!");
            }
        }

        public static bool IsMounted(Player p) => p != null && Mounted.ContainsKey(p);
        public static Gun GunOf(Player p) => Mounted.TryGetValue(p, out var h) ? h.G : null;
        /// <summary>How far up (tiles) a gunner is drawn (0 for anyone else).</summary>
        public static float LiftOf(ICombatant u) => u is Player p && IsMounted(p) ? Lift : 0f;

        static Gun Near(Player p) => Barrier.On ? Guns.FirstOrDefault(g => !g.Down && g.By == null && System.Math.Abs(p.X - g.Tx) <= 1 && (p.Y == g.NookY || p.Y == g.StepsY)) : null;

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
            GameLog.Write("You take the machine gun. E to leave it.");
            return true;
        }

        public static void Dismount(Player p)
        {
            if (!Mounted.TryGetValue(p, out var h)) return;
            Mounted.Remove(p); h.G.By = null;
            p.EquippedWeapon = h.Saved; p.EquippedWeaponCategory = h.Cat; p.EquippedWeaponKey = h.WKey;
            var (x, y) = TerrainMap.FindNearestEnterable(h.G.Tx + 1, h.G.NookY, 2); p.X = x; p.Y = y; Rt.SetOffset(p, 0f, 0f);
            GameLog.Write("You let go of the machine gun.");
        }

        /// <summary>Everyone off (a save, a new map).</summary>
        public static void ReleaseAll() { foreach (var p in Mounted.Keys.ToList()) Dismount(p); }

        /// <summary>Every frame (Rt.WorldTick): the box fills, the belt swaps, and gunners who died, went down or left the map come off.</summary>
        public static void Tick(float dt)
        {
            if (_guns != null) foreach (var g in _guns) if (!g.Down && g.By == null) g.Box = Mathf.Min(Stock, g.Box + Refill * dt);
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
