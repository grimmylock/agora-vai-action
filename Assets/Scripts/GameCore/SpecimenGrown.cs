// Boss: the Specimen, grown (Tier 4, Deep Caverns: the Crystal Pool island; BOSS_CONCEPTS_V3.md 5, BOSS_ROSTER.md 5): the rematch. The brain that escaped Secret Lab Alpha
// (BrainBoss.Escaped) has fed on the pool and walks now, on a spray of long spinal legs. Drawn from Pixel Lab clips (BossArt key "specimen").
// The fight is Boss.cs with effects of its own:
//  - Lash (white: parry the leg), Double Lash (white, white), Skitter-Lash (red: it rushes across the island and lashes);
//  - the Mind-Lash: its eye flares: every player within MindRange it can see has their aim torn away and is dazed 1.5 s (a dive does not help; break its line of sight);
//  - phase 2: the Scream (the Howl's cone: the mouths on its tendrils scream together) and the Frenzy (three lashes, the last red).
// It only comes if it escaped the lab (the flag set when its first fight ended with it fleeing). Co-op: the nearest player; the mind-lash catches everyone in its sight. Host-side only.
// NOT built from the brief yet: "it remembers" (opening with the counter to how you beat it the first time), drinking from the pool to heal and the crystals that stop it, the Husks on the shore in phase 2.
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class SpecimenGrown
    {
        public const string Lair = "Crystal Pool", Zone = "deep_caverns", Done = "specimen_grown_done", Trophy = "Specimen Tissue";
        public const float WakeRange = 12f, MindRange = 11f;

        public static readonly Boss.Def Def = new Boss.Def
        {
            Name = "The Specimen", Title = "IT REMEMBERS YOU", Weapon = "SpecimenLegs", Armor = null, Hp = 8000, Faction = "Mutants", Rank = "Master", DoneFlag = Done, Vanishes = false,
            Art = "specimen", Creature = true, StepSeconds = 0.25f, Recover = 1.0f, Phase2Line = "Every mouth on its tendrils opens at once.",
            Moves = new[]
            {
                new Boss.Move { Name = "Lash", Wind = 0.7f, MaxDist = 2f, Mult = 1.0f, Weight = 3, Open = 1.1f, Clip = "lash" },
                new Boss.Move { Name = "Double Lash", Wind = 0.7f, Hits = 2, Gap = 0.45f, MaxDist = 2f, Mult = 0.75f, Weight = 2, Open = 1.4f, Clip = "lash2" },
                new Boss.Move { Name = "Skitter", Wind = 0.8f, Danger = true, Lunge = 7f, Mult = 1.2f, Weight = 2, Open = 1.6f, Clip = "lash" },
                new Boss.Move { Name = "Mind-Lash", Wind = 1.0f, Danger = true, MaxDist = MindRange, Effects = new[] { "mind" }, Weight = 2, Open = 1.3f, Clip = "mind" },
                new Boss.Move { Name = "Scream", Wind = 1.1f, Danger = true, MaxDist = 9f, Effects = new[] { "howl" }, Weight = 2, Open = 1.8f, Phase2Only = true, Clip = "scream" },
                new Boss.Move { Name = "Frenzy", Wind = 0.6f, Hits = 3, Gaps = new[] { 0.4f, 0.55f }, Mults = new[] { 0.6f, 0.6f, 1.3f }, RedHits = new[] { false, false, true }, MaxDist = 2f, Weight = 2, Open = 2.0f, Phase2Only = true, Clip = "lash2" },
            },
        };
        static SpecimenGrown() { Def.Defeated = Defeated; }

        static bool _spawnedHere;
        public static void OnEnterZone(string zoneId) { _spawnedHere = false; }

        public static void Tick(float dt)
        {
            if (Net.Mode == Net.Role.Client || TerrainMap.ZoneId != Zone || Story.Has(Done) || !Story.Has(BrainBoss.Escaped) || Player.Current == null) return;
            Boss.Fights.RemoveAll(f => f.Def == Def && !f.Finished && !EncounterState.Enemies.Contains(f.Unit));
            if (_spawnedHere || Boss.Fights.Any(f => f.Def == Def && !f.Gone)) return;
            var l = TerrainMap.Landmarks?.FirstOrDefault(m => m.Name == Lair); if (l == null) return;
            if (!Player.All.Any(p => p.IsAlive() && Mathf.Sqrt((p.X - l.X) * (p.X - l.X) + (p.Y - l.Y) * (p.Y - l.Y)) <= WakeRange)) return;
            var (x, y) = TerrainMap.FindNearestEnterable(l.X, l.Y);
            Spawn(x, y, true); _spawnedHere = true;
        }

        public static Boss.Fight Spawn(int x, int y, bool intro)
        {
            var f = Boss.Spawn(Def, x, y); Rt.SetOffset(f.Unit, 0f, 0f);
            f.NextMoveAt = Time.time + (intro ? 2.8f : 1.6f);
            if (intro)
            {
                GameLog.Event("Something stands up out of the glowing pool on its long, wet legs. It is the brain from the lab's vat, the size of a bed now, and it turns its one eye on you.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.2f);
            }
            return f;
        }

        /// <summary>The Mind-Lash's blow: every player within MindRange it can see has their aim torn away and is dazed for 1.5 s. Breaking its line of sight is the only cover.</summary>
        public static void Mind(Boss.Fight f)
        {
            var h = f.Unit; Sfx.Play("psi", TacticalGridView.GridToWorld(h.X, h.Y), 1f, 0.02f, 0.8f); CombatFx.Push("stagger", h, null, null).Now = true; int n = 0;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                if (CombatantUtil.Distance(h, q) > MindRange) continue;
                if (GameServices.Grid != null && !GameServices.Grid.CheckLineOfSight(h.X, h.Y, q.X, q.Y)) continue;
                var aim = Gunplay.AimOf(q); aim.Steady = 0f; aim.Bloom = Gunplay.PlayerMaxBloom; Body.Concuss(q, 1.5f); n++;
            }
            GameLog.Write(n > 0 ? "Its eye finds you, and for a moment you forget which way your gun points." : "Its eye searches for you through the rock.");
        }

        static void Defeated(Player p, HumanEnemy h)
        {
            h.Inventory.Items.Add(new InventoryItem { Id = "specimen_tissue", Name = Trophy, Type = "misc", Qty = 1, WeightKg = 1.5f, Price = 3200 });
            Story.Journal("The Specimen: the brain that fled Secret Lab Alpha fed on the Crystal Pool in the Deep Caverns and grew legs. It is dead. A piece of it is worth a great deal to a lab.");
            GameLog.Event("Its legs fold under it one by one, and it settles into the shallows of the pool. The eye stays open, and stays still.");
        }
    }
}
