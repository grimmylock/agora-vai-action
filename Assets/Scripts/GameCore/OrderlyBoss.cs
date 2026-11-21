// Boss: the Orderly (Tier 6, the Hospital Morgue; BOSS_CONCEPTS_V3.md 4, BOSS_ROSTER.md 11). REDONE 2026-11-21 from Yan's own ChatGPT drawing (ArtSource/mutants_anim/boss_concepts/gpt/orderly_native.png): an insect made of a hospital patient -
// a long pale torso stretched into a thorax, both arms grown into bone-saw blades held up like a mantis, a black body bag fused over its back like wing cases, six thin legs of forearms, a face on its belly, an IV pole
// with a bag of dark blood growing out of its back. Side on. Drawn from Pixel Lab clips (BossArt key "orderly"). The fight is a heavy duel on Boss.cs:
//  - the Saw (white: parry the sweep), the Chop (red: both blades down: dive or step out), the Grab (red: caught, slammed down and pulled to it);
//  - the Drip: it squeezes the IV bag and mends DripHeal of its health, unless you hurt it by DripBreak of its health while it does it (the bag bursts: no heal, and it bleeds).
//    It can drip DripMax times a fight. A tool for the player (rule 6): stop it, and it is the weaker for the rest of the fight;
//  - phase 2: the Double Saw (white, white) and the Saw Rush (red, a lunge).
// It is woken when a player comes near the Autopsy Theatre, once. Co-op: the nearest player. Host-side only.
// NOT built from the brief yet: the lights going out (the cave darkness), the bag as a separate target on its back.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class OrderlyBoss
    {
        public const string Lair = "Autopsy Theatre", Done = "orderly_done", Trophy = "Ward Name Tag";
        public const float WakeRange = 10f, DripHeal = 0.06f, DripBreak = 0.03f; public const int DripMax = 3;
        static readonly Dictionary<Boss.Fight, (int hp, int used, bool burst)> _drip = new Dictionary<Boss.Fight, (int, int, bool)>();

        public static readonly Boss.Def Def = new Boss.Def
        {
            Name = "The Orderly", Title = "HE IS STILL ON SHIFT", Weapon = "OrderlySaw", Armor = null, Hp = 12000, Faction = "Mutants", Rank = "Legend", DoneFlag = Done, Vanishes = false,
            Art = "orderly", Creature = true, StepSeconds = 0.45f, Recover = 1.0f, Phase2Line = "It tears the drip out of its back. What runs out of the tube is black.",
            Moves = new[]
            {
                new Boss.Move { Name = "Saw", Wind = 0.9f, MaxDist = 2f, Mult = 1.0f, Weight = 3, Poses = new[] { 1 }, Open = 1.4f, Clip = "saw" },
                new Boss.Move { Name = "Chop", Wind = 1.1f, Danger = true, MaxDist = 2f, Mult = 1.6f, Weight = 2, Poses = new[] { 0 }, Open = 2.0f, Clip = "chop" },
                new Boss.Move { Name = "Grab", Wind = 1.0f, Danger = true, MaxDist = 2f, Mult = 1.1f, Effects = new[] { "slab" }, Weight = 2, Open = 1.8f, Clip = "grab" },
                new Boss.Move { Name = "Drip", Wind = 1.6f, MaxDist = 20f, Effects = new[] { "drip" }, Weight = 1, Open = 0.8f, Phase1Only = true, Clip = "drip" },
                new Boss.Move { Name = "Double Saw", Wind = 0.8f, Hits = 2, Gap = 0.6f, MaxDist = 2f, Mult = 0.8f, Weight = 3, Poses = new[] { 1, 1 }, Open = 1.8f, Phase2Only = true, Clip = "saw" },
                new Boss.Move { Name = "Saw Rush", Wind = 1.0f, Danger = true, Lunge = 6f, Mult = 1.4f, Weight = 2, Poses = new[] { 1 }, Open = 2.2f, Phase2Only = true, Clip = "saw" },
            },
        };
        static OrderlyBoss() { Def.Defeated = Defeated; }

        static bool _spawnedHere; static string _map;
        public static void OnEnterZone(string zoneId) { _spawnedHere = false; _drip.Clear(); }

        public static void Tick(float dt)
        {
            if (Net.Mode == Net.Role.Client || Story.Has(Done) || Player.Current == null) return;
            if (_map != TerrainMap.ZoneId) { _map = TerrainMap.ZoneId; _spawnedHere = false; }
            var l = TerrainMap.Landmarks?.FirstOrDefault(m => m.Name == Lair); if (l == null) return;
            Boss.Fights.RemoveAll(f => f.Def == Def && !f.Finished && !EncounterState.Enemies.Contains(f.Unit));
            if (_spawnedHere || Boss.Fights.Any(f => f.Def == Def && !f.Gone)) return;
            if (!Player.All.Any(p => p.IsAlive() && Mathf.Sqrt((p.X - l.X) * (p.X - l.X) + (p.Y - l.Y) * (p.Y - l.Y)) <= WakeRange)) return;
            var (x, y) = TerrainMap.FindNearestEnterable(l.X, l.Y);
            Spawn(x, y, true); _spawnedHere = true;
        }

        public static Boss.Fight Spawn(int x, int y, bool intro)
        {
            var f = Boss.Spawn(Def, x, y); Rt.SetOffset(f.Unit, 0f, 0f); _drip.Remove(f);
            f.NextMoveAt = Time.time + (intro ? 3.0f : 1.8f);
            if (intro)
            {
                GameLog.Event("Between the two steel tables something unfolds from the ceiling: a patient stretched into an insect, both arms grown into saw blades, a black body bag split open on its back. A bag of dark blood swings on a pole out of its spine, and the blades scream on the tiles.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.4f); Sfx.Play("saw_scrape", TacticalGridView.GridToWorld(x, y), 1f, 0f);
            }
            return f;
        }

        /// <summary>Boss.Begin of the Drip: remember his health, to see whether the bag bursts.</summary>
        public static void DripBegins(Boss.Fight f) { _drip.TryGetValue(f, out var d); _drip[f] = (f.Unit.Hp, d.used, d.burst); }
        public static int DripsUsed(Boss.Fight f) => _drip.TryGetValue(f, out var d) ? d.used : 0;
        public static bool Burst(Boss.Fight f) => _drip.TryGetValue(f, out var d) && d.burst;
        /// <summary>The Drip's blow: he mends DripHeal of his health, unless he lost DripBreak of it since he began (the bag burst), or he has used it DripMax times.</summary>
        public static void Drip(Boss.Fight f)
        {
            var h = f.Unit; _drip.TryGetValue(f, out var d);
            if (d.burst || d.used >= DripMax) { GameLog.Write("It squeezes the bag. Nothing comes out of it."); return; }
            if (d.hp - h.Hp >= Def.Hp * DripBreak) { _drip[f] = (h.Hp, d.used, true); h.StatusEffects.Bleeding = true; GameLog.Event("The IV bag bursts. Black fluid sprays over the tiles, and it bleeds."); CombatFx.PxAt("acid_splash", h.X, h.Y); return; }
            Sfx.Play("drip_squeeze", TacticalGridView.GridToWorld(h.X, h.Y), 1f, 0.05f);
            int heal = Mathf.RoundToInt(Def.Hp * DripHeal); h.Hp = Mathf.Min(h.MaxHp, h.Hp + heal); _drip[f] = (h.Hp, d.used + 1, false);
            GameLog.Write("It squeezes the bag on its pole. Dark fluid runs down the tube into it, and it straightens.");
        }

        /// <summary>The Grab's blow: whoever is in front of him is caught, slammed down (knocked down 1.2 s) and pulled to his feet.</summary>
        public static void Slab(Boss.Fight f, Player p, Boss.Move m, int hi)
        {
            var h = f.Unit; if (p == null || h.EquippedWeapon == null || CombatantUtil.Distance(h, p) > 2) return;
            if (Rt.Of(p).Diving || Rt.Untouchable(p)) { GameLog.Write("DODGED!"); CombatFx.Push("dodge", p, h, null).Now = true; return; }
            int hp0 = p.Hp; Parry.Unparryable = true; try { Melee.HitUnit(h, h.EquippedWeapon, p, Boss.MultOf(m, hi)); } finally { Parry.Unparryable = false; }
            if (p.Hp < hp0) { Rt.Knockdown(p, 1.2f); Boss.DragTo(h, p, 1.5f); Sfx.Play("hit_hard", null, 1f, 0.03f); GameLog.Write("Thin grey legs close on you, lift you and slam you down on the tiles."); }
        }

        static void Defeated(Player p, HumanEnemy h)
        {
            h.Inventory.Items.Add(new InventoryItem { Id = "orderly_tag", Name = Trophy, Type = "misc", Qty = 1, WeightKg = 0.1f, Price = 4200 });
            Story.Journal("The Orderly: something that had been a patient and had become an insect kept the Hospital Morgue. It is down. Its ward name tag reads a name nobody has said in years.");
            GameLog.Event("Its legs fold one by one. The IV pole tips over and rings on the tiles, and the face on its belly goes on screaming without a sound.");
        }
    }
}
