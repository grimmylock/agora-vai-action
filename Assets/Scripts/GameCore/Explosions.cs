// Blast damage shared by explosive barrels now (and frag grenades later - same rules, file 01):
// everyone within the radius takes physical damage (armor Phys-DR applies, cover doesn't, Anom-DR
// never does); real time: full damage in the middle, half at the edge, everyone caught is knocked down, a dive skips it. The blast also
// wrecks the terrain: light cover, doors and barrels in it are destroyed (barrels chain), hard
// cover loses 3 Integrity.
// ponytail: radius is a ruling (the docs only say "everyone in the blast") - 1 tile = the 3x3
// around the impact; bump BarrelRadius if barrels feel weak.
using System.Collections.Generic;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Explosions
    {
        public const int BarrelRadius = 1;
        public const int BarrelDamage = 400;
        public const int ThrowRadius = 1;   // 3x3 (file 01)
        // 2026-11-17 (Yan: "remove the throw roll and the save rolls ... do all three"): real time has no d20 here. A throw lands where it was aimed, off by up to ScatterPerTile tiles for every tile it flies (a rule of
        // thumb: 10 tiles = under a tile); it FLIES (ThrowSpeed tiles a second) and a frag then burns its fuse, so you can run out of the blast or dive through it (Rt.Untouchable at the instant it goes off).
        // The blast hurts most in the middle and half as much at the edge, and throws everyone it catches down for a moment (a dive skips it entirely). A flashbang has no damage: whoever it catches with a clear line within
        // FlashRadius tiles can't shoot or walk for FlashSeconds (a player is concussed instead: his aim is wild).
        public const float ScatterPerTile = 0.08f, ThrowSpeed = 14f, FragFuse = 1.1f, FlashFuse = 0.7f, FlashSeconds = 3f, KnockSeconds = 0.9f;
        public const int FlashRadius = 4;

        class Pending { public ICombatant Thrower; public ConsumableDef Def; public int X, Y; public float At; }
        static readonly List<Pending> Fuses = new List<Pending>();
        public static void Reset() { Fuses.Clear(); }
        /// <summary>Grenades in the air or burning their fuse (the harness and the HUD can look).</summary>
        public static int InFlight => Fuses.Count;

        /// <summary>A thrown grenade: it lands where it was aimed (a little off at long range), and goes off after its flight and fuse.</summary>
        public static void Throw(ICombatant thrower, ConsumableDef def, int x, int y)
        {
            string who = thrower is Player ? "You" : thrower.Name;
            float dist = Mathf.Sqrt((x - thrower.X) * (x - thrower.X) + (y - thrower.Y) * (y - thrower.Y)), scatter = dist * ScatterPerTile;
            if (scatter > 0.01f)
            {
                float a = Random.value * 6.2832f, r = Mathf.Sqrt(Random.value) * scatter;
                x = Mathf.Clamp(x + Mathf.RoundToInt(Mathf.Cos(a) * r), 0, TerrainMap.Width - 1);
                y = Mathf.Clamp(y + Mathf.RoundToInt(Mathf.Sin(a) * r), 0, TerrainMap.Height - 1);
            }
            GameLog.Write($"{who} throw{(thrower is Player ? "" : "s")} the {def.Name}.");
            float fuse = def.Throwable == "frag" ? FragFuse : def.Throwable == "flash" ? FlashFuse : 0f;
            if (!Rt.On || def.Throwable == "smoke") { Detonate(thrower, def, x, y); return; }   // the old turn-based game (and a smoke can, which has nothing to dodge) lands at once
            Fuses.Add(new Pending { Thrower = thrower, Def = def, X = x, Y = y, At = Time.time + dist / ThrowSpeed + fuse });
        }

        /// <summary>Every frame in real time (Rt.WorldTick): grenades whose fuse has run out go off.</summary>
        public static void Tick()
        {
            for (int i = Fuses.Count - 1; i >= 0; i--)
                if (Time.time >= Fuses[i].At) { var f = Fuses[i]; Fuses.RemoveAt(i); Detonate(f.Thrower, f.Def, f.X, f.Y); }
        }

        static void Detonate(ICombatant thrower, ConsumableDef def, int x, int y)
        {
            switch (def.Throwable)
            {
                case "frag": Blast(x, y, ThrowRadius, 400, thrower, "frag blast"); break;
                case "smoke": CombatFx.PxAt("smoke_bomb", x, y); TerrainMap.AddSmoke(x, y, ThrowRadius, 3); GameLog.Write("Thick smoke blocks sight through the area."); break;
                case "flash": Flash(x, y, FlashRadius); break;
            }
        }

        /// <summary>Flashbang: everyone within `radius` with a clear line to the flash is blinded - no roll. No damage. A human or mutant can't shoot, walk or strike for FlashSeconds; a player is concussed (wild aim).</summary>
        public static void Flash(int cx, int cy, int radius)
        {
            Sfx.Play("explosion", TacticalGridView.GridToWorld(cx, cy), 0.6f, 0.08f);
            var units = new List<ICombatant>();
            foreach (var pl in Player.All) if (pl.IsAlive()) units.Add(pl);
            foreach (var e in EncounterState.Enemies) if (e.IsAlive()) units.Add(e);
            var grid = GameServices.Grid;
            foreach (var u in units)
            {
                if (System.Math.Max(System.Math.Abs(u.X - cx), System.Math.Abs(u.Y - cy)) > radius || Rt.Untouchable(u)) continue;
                if (grid != null && !grid.CheckLineOfSight(cx, cy, u.X, u.Y)) continue;
                string who = u is Player ? "Player" : u.Name;
                GameLog.Write($"{who} is blinded by the flash.");
                if (u is Player p) Body.Concuss(p, FlashSeconds);
                else if (u is HumanEnemy h) { RtAi.Hold(h, FlashSeconds); RtAi.Pin(h, FlashSeconds); }
                else if (u is Mutant m) RtAi.Pin(m, FlashSeconds);
            }
        }

        public static void Blast(int cx, int cy, int radius, int damage, ICombatant attacker, string label)
        {
            Sfx.Play("explosion", TacticalGridView.GridToWorld(cx, cy), 1f, 0.08f);
            CombatFx.PxAt("small_explosion", cx, cy);   // RT fx 4: the frag, the barrel, a cooked-off grenade finally have a picture
            Noise.Make(cx, cy, Noise.Blast, attacker);   // Batch 47
            var victims = new List<ICombatant>();
            if (Player.Current != null && Player.Current.IsAlive()) victims.Add(Player.Current);
            foreach (var e in EncounterState.Enemies) if (e.IsAlive()) victims.Add(e);

            var combat = GameServices.RequireCombat();
            foreach (var v in victims)
            {
                if (System.Math.Max(System.Math.Abs(v.X - cx), System.Math.Abs(v.Y - cy)) > radius || Rt.Untouchable(v)) continue;
                var (vx, vy) = Ballistics.Pos(v); float bd = Mathf.Sqrt((vx - (cx + 0.5f)) * (vx - (cx + 0.5f)) + (vy - (cy + 0.5f)) * (vy - (cy + 0.5f)));
                float mult = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(bd / (radius + 0.5f)));          // full in the middle, half at the edge
                float raw = damage * mult;
                int dealt = (int)combat.CalculateDamage(raw, "Standard", v.PhysDR, 0f);
                v.Hp = System.Math.Max(0, v.Hp - dealt);
                Boss.OnDamaged(v); Preacher.OnBlast(v);   // Tier 4: a blast on the Preacher cracks his core
                string who = v is Player ? "Player" : v.Name;
                GameLog.Write($"{who} is caught in the {label} - {dealt} damage.");
                if (v.IsAlive()) { if (v is Player kp) Rt.Knockdown(kp, KnockSeconds); else v.StatusEffects.Prone = true; }   // thrown to the ground
                CombatRules.CheckWound(v, dealt, false);   // "Can cause Wounds" (file 01)
                if (!v.IsAlive())
                {
                    var k = CombatFx.Push("kill", null, v);   // Batch 24: blown outward from the blast, in pieces
                    k.FromX = cx; k.FromY = cy; k.Killed = true; k.Overkill = 99;
                    Weapon.OnTargetKilled(attacker, v);
                }
            }

            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (Barrier.Sturdy(x, y)) { TerrainMap.DamageTile(x, y, Mathf.Max(1, Mathf.RoundToInt(damage * Barrier.BlastWear)), attacker); continue; }   // the Barrier takes a share of the blast
                    var t = TerrainMap.GetTile(x, y);
                    int max = TileRules.MaxIntegrity(t);
                    if (max <= 0) continue;
                    TerrainMap.DamageTile(x, y, t.Type == TileType.HardCover ? 3 : max, attacker);
                }
            Props.BlastShove(cx, cy, radius);   // Batch 103
        }
    }
}
