// Boss: Mother Hollow (Tier 1, the Secret Cave's Mutant Den; BOSS_CONCEPTS_V3.md 1, BOSS_ROSTER.md 1). The Pallid Prowler grown to horse size, the den mother.
// Yan, 2026-11-21: "use [the Pixel Lab credits] to create the new bosses after you finish the concepts". Drawn from Pixel Lab clips (BossArt.cs, key "mother_hollow").
// The fight is Boss.cs (glints: white = parry the bite, red = dive the pounce and the spit; phase 2 at half; the break and the execution) with two effects of her own:
//  - the brood: she howls and Pallid Prowlers crawl out of the cave's three dark niches (authored landmarks of the cave's plan, so nothing appears next to a player: CLAUDE.md rule 7);
//  - the acid: a glob spat at where you stood when she wound up; step out of it or dive.
// She only comes once the Brute's business in the cave is over (the culvert story past the canister, no Brute rematch pending), and only once (mother_hollow_done).
// Co-op: she fights the nearest player; any player can parry, shoot, finish. Host-side only (boss fights are not synced yet).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class MotherHollow
    {
        public const string Lair = "Mutant Den", Zone = "secret_cave", Done = "mother_hollow_done", Trophy = "Pack-Mother's Skull";
        public const float WakeRange = 10f, AcidRadius = 1.3f, BroodMinDistance = 9f;
        public const int BroodPhase1 = 3, BroodPhase2 = 5, BroodCap = 6;
        static readonly string[] Niches = { "Dark Niche West", "Dark Niche South", "Dark Niche East" };
        public static readonly List<Mutant> Brood = new List<Mutant>();

        public static readonly Boss.Def Def = new Boss.Def
        {
            Name = "Mother Hollow", Title = "THE DEN REMEMBERS HER", Weapon = "MotherJaws", Armor = null, Hp = 30800, Faction = "Mutants", Rank = "Veteran", DoneFlag = Done, Vanishes = false,
            Art = "mother_hollow", Creature = true, StepSeconds = 0.3f, Recover = 1.0f, Phase2Line = "She stops circling. The whole den is hers, and so are you.",
            Moves = new[]
            {
                new Boss.Move { Name = "Bite", Wind = 0.8f, Mult = 1.0f, MaxDist = 2f, Weight = 3, Open = 1.3f, Clip = "bite" },                                                 // white: parry it
                new Boss.Move { Name = "Pounce", Wind = 0.9f, Danger = true, Lunge = 6f, Mult = 1.3f, Weight = 2, Open = 1.8f, Clip = "pounce" },                                // red: dive under the leap
                new Boss.Move { Name = "Howl", Wind = 1.0f, Danger = true, MaxDist = 14f, Effects = new[] { "brood" }, Weight = 1, Open = 1.6f, Clip = "howl" },                   // the brood comes out of the niches
                new Boss.Move { Name = "Spit", Wind = 0.8f, Danger = true, MaxDist = 7f, Mult = 0.7f, Effects = new[] { "acid" }, Weight = 2, Open = 1.2f, Clip = "spit" },       // where you stood: walk out or dive
                new Boss.Move { Name = "Drag", Wind = 1.0f, Danger = true, Lunge = 9f, Mult = 1.5f, Weight = 2, Open = 2.2f, Phase2Only = true, Clip = "pounce" },               // phase 2: the long lunge across the den
                new Boss.Move { Name = "Double Spit", Wind = 0.7f, Hits = 2, Gap = 0.9f, MaxDist = 7f, Mult = 0.6f, Effects = new[] { "acid", "acid" }, RedHits = new[] { true, true }, Weight = 2, Open = 1.4f, Phase2Only = true, Clip = "spit" },
            },
        };
        static MotherHollow() { Def.Defeated = Defeated; }

        static bool _spawnedHere;
        public static void OnEnterZone(string zoneId) { _spawnedHere = false; Brood.Clear(); }

        /// <summary>Rt.WorldTick: wake her when a player comes near the den (once her time has come).</summary>
        public static void Tick(float dt)
        {
            Brood.RemoveAll(m => m == null || !m.IsAlive() || !EncounterState.Enemies.Contains(m));
            if (Net.Mode == Net.Role.Client || TerrainMap.ZoneId != Zone || Story.Has(Done) || Player.Current == null) return;
            if (!(Story.Step("culvert") >= 4 && Story.Step("brute2") != 1)) return;                                    // the Brute's den first
            Boss.Fights.RemoveAll(f => f.Def == Def && !f.Finished && !EncounterState.Enemies.Contains(f.Unit));   // you left and came back: she starts again
            if (_spawnedHere || Boss.Fights.Any(f => f.Def == Def && !f.Gone)) return;
            var l = TerrainMap.Landmarks?.FirstOrDefault(m => m.Name == Lair); if (l == null) return;
            if (!Player.All.Any(p => p.IsAlive() && Mathf.Sqrt((p.X - l.X) * (p.X - l.X) + (p.Y - l.Y) * (p.Y - l.Y)) <= WakeRange)) return;
            var (x, y) = TerrainMap.FindNearestEnterable(l.X, l.Y - 1);
            Spawn(x, y, true); _spawnedHere = true;
        }

        public static Boss.Fight Spawn(int x, int y, bool intro)
        {
            var f = Boss.Spawn(Def, x, y); Rt.SetOffset(f.Unit, 0f, 0f);
            f.NextMoveAt = Time.time + (intro ? 2.8f : 1.6f);
            if (intro)
            {
                GameLog.Event("Something in the dark of the den unfolds. It is a Prowler the size of a horse, and its belly drags in the dust. Green light leaks out of its jaw.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.2f);
            }
            return f;
        }

        /// <summary>The Howl's blow: Pallid Prowlers crawl out of the cave's dark niches (the ones at least BroodMinDistance tiles from every player), up to BroodCap living at once.</summary>
        public static void Howl(Boss.Fight f)
        {
            var h = f.Unit; Sfx.Play("mother_howl", TacticalGridView.GridToWorld(h.X, h.Y), 1f, 0.04f); CombatFx.Push("stagger", h, null, null).Now = true;
            var db = MutantDatabase.Tiers["tier1"]["PallidProwler"]; int want = Mathf.Min(f.Phase == 2 ? BroodPhase2 : BroodPhase1, BroodCap - Brood.Count), made = 0;
            var spots = (TerrainMap.Landmarks ?? new List<LandmarkInstance>()).Where(l => Niches.Contains(l.Name) && Player.All.Where(p => p.IsAlive()).All(p => Mathf.Sqrt((p.X - l.X) * (p.X - l.X) + (p.Y - l.Y) * (p.Y - l.Y)) >= BroodMinDistance)).ToList();
            if (spots.Count == 0) spots = (TerrainMap.Landmarks ?? new List<LandmarkInstance>()).Where(l => Niches.Contains(l.Name)).ToList();   // everyone is near a niche: they still answer
            var points = spots.Select(l => (l.X, l.Y)).ToList();
            if (points.Count == 0)                                                                                     // no niches (the Testing Ground): a ring 10 tiles round her
                for (int k = 0; k < 8; k++) { float ang = k * Mathf.PI / 4f; points.Add((h.X + Mathf.RoundToInt(10f * Mathf.Cos(ang)), h.Y + Mathf.RoundToInt(10f * Mathf.Sin(ang)))); }
            for (int i = 0, tries = 0; made < want && points.Count > 0 && tries < want * 4; tries++, i++)
            {
                var pt = points[i % points.Count]; var (x, y) = TerrainMap.FindNearestEnterable(pt.Item1, pt.Item2); if (TerrainMap.UnitAt(x, y, null) != null) continue;
                var m = EncounterGeneration.CreateMutantFromDb(db, x, y); m.IsAggroed = true; EncounterState.Enemies.Add(m); Brood.Add(m); made++;
            }
            GameLog.Write(made > 0 ? $"The den answers her: {made} pale shapes come crawling out of the dark." : "She howls. Nothing more is left in the dark to answer.");
        }

        /// <summary>The Spit's blow: the glob comes down where its target stood when she wound up (Fight.AimX/AimY): everyone within AcidRadius of it, unless diving, takes the hit and a Toxic Bleed.</summary>
        public static void Acid(Boss.Fight f, Boss.Move m, int hi)
        {
            var h = f.Unit; CombatFx.PxAt("acid_splash", f.AimX, f.AimY); Sfx.Play("acid_spit", TacticalGridView.GridToWorld(f.AimX, f.AimY), 0.9f, 0.05f);
            if (h.EquippedWeapon == null) return;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                var (qx, qy) = Ballistics.Pos(q); float dx = qx - (f.AimX + 0.5f), dy = qy - (f.AimY + 0.5f);
                if (dx * dx + dy * dy > AcidRadius * AcidRadius) continue;
                if (Rt.Of(q).Diving || Rt.Untouchable(q)) { GameLog.Write("DODGED!"); CombatFx.Push("dodge", q, h, null).Now = true; continue; }
                int hp0 = q.Hp; Parry.Unparryable = true; try { Melee.HitUnit(h, h.EquippedWeapon, q, Boss.MultOf(m, hi)); } finally { Parry.Unparryable = false; }
                if (q.Hp < hp0 && q.IsAlive()) { q.StatusEffects.ToxicBleed = true; GameLog.Write("The acid burns: you suffer Toxic Bleed."); }
            }
        }

        /// <summary>Her execution: the trophy, and the den's hoard is safe to take now.</summary>
        static void Defeated(Player p, HumanEnemy h)
        {
            h.Inventory.Items.Add(new InventoryItem { Id = "mother_hollow_skull", Name = Trophy, Type = "misc", Qty = 1, WeightKg = 2.5f, Price = 1800 });
            foreach (var b in Brood.Where(b => b.IsAlive())) b.IsAggroed = false;
            Story.Journal("Mother Hollow: the Prowlers' den mother lay in the Secret Cave's Mutant Den. She is dead. Her skull is worth something to the right buyer.");
            GameLog.Event("She folds into the dust of her den. In the dark the brood stops moving, then scatters.");
        }
    }
}
