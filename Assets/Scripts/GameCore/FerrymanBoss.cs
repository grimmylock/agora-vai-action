// Boss: the Ferryman (Tier 3, Stranded Ferry: the Car Deck; BOSS_CONCEPTS_V3.md 3, BOSS_ROSTER.md 4). REDONE 2026-11-21 from Yan's own ChatGPT drawing (ArtSource/mutants_anim/boss_concepts/gpt/ferryman_native.png, ferryman_from_gpt.py): a drowned
// corpse that became an anglerfish - a split torso for a mouth, rib teeth, four long human limbs bent backwards, a spine growing out of its back with a ship's lantern on it and a drowned face in the glass. Side on, crawls. Drawn from Pixel Lab clips (BossArt key "ferryman").
// The fight is Boss.cs with effects of its own:
//  - the Bite (white: parry it; a lunge across the deck) and the Claw (a long arm);
//  - the Lure (red): the lantern blazes: every player within FlareRange whose crosshair is ON the light (within FlareCone degrees) is dazzled: aim gone, dazed 2 s. "Don't look at it": a dive does not help;
//  - the Hands: it strikes the deck and drowned hands come up round it: everyone within HandsRange not diving is pulled down (knocked down, a little hurt);
//  - phase 2: the Reel (red: the lantern swings and the light hauls you toward the mouth, then the bite), the Double Claw.
// Co-op: it fights the nearest player; the lure and the hands catch everyone. Host-side only.
// NOT built from the brief yet: the phantom lake (water slow zones), the deck tilting and rolling cars, the dive and ambush from the water (the dive / emerge clips exist), the lantern as a breakable weak point.
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class FerrymanBoss
    {
        public const string Lair = "Car Deck", Zone = "stranded_ferry", Done = "ferryman_done", Trophy = "Ferry Lantern";
        public const float WakeRange = 12f, FlareRange = 10f, FlareCone = 30f, HandsRange = 6f;

        public static readonly Boss.Def Def = new Boss.Def
        {
            Name = "The Ferryman", Title = "THE LAKE THAT IS NOT THERE", Weapon = "FerrymanHook", Armor = null, Hp = 6000, Faction = "Mutants", Rank = "Expert", DoneFlag = Done, Vanishes = false,
            Art = "ferryman", Creature = true, StepSeconds = 0.42f, Recover = 1.0f, Phase2Line = "The lantern burns green-white. Water runs up the rails that should run down.",
            Moves = new[]
            {
                new Boss.Move { Name = "Bite", Wind = 0.8f, Lunge = 3f, Mult = 1.1f, Weight = 3, Open = 1.3f, Clip = "bite" },
                new Boss.Move { Name = "Claw", Wind = 0.7f, MaxDist = 2.5f, Mult = 0.8f, Weight = 3, Open = 1.1f, Clip = "claw" },
                new Boss.Move { Name = "Lure", Wind = 1.2f, Danger = true, MaxDist = FlareRange, Effects = new[] { "flare" }, Weight = 2, Open = 1.5f, Clip = "lure" },
                new Boss.Move { Name = "Hands", Wind = 1.0f, Danger = true, MaxDist = HandsRange, Mult = 0.4f, Effects = new[] { "hands" }, Weight = 2, Open = 1.6f, Clip = "claw" },
                new Boss.Move { Name = "Reel", Wind = 1.1f, Danger = true, Drag = 4.5f, Mult = 1.0f, Weight = 2, Open = 1.6f, Phase2Only = true, Clip = "lure" },
                new Boss.Move { Name = "Double Claw", Wind = 0.6f, Hits = 2, Gap = 0.5f, MaxDist = 2.5f, Mult = 0.8f, Weight = 2, Open = 1.6f, Phase2Only = true, Clip = "claw" },
            },
        };
        static FerrymanBoss() { Def.Defeated = Defeated; }

        static bool _spawnedHere;
        public static void OnEnterZone(string zoneId) { _spawnedHere = false; }

        public static void Tick(float dt)
        {
            if (Net.Mode == Net.Role.Client || TerrainMap.ZoneId != Zone || Story.Has(Done) || Player.Current == null) return;
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
                GameLog.Event("Water is running across the car deck from nowhere. In the middle of it something pale crawls toward you on long wrong arms, its chest split open for a mouth, and over its back a lantern burns green with a face pressed to the glass.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.2f);
            }
            return f;
        }

        /// <summary>Is this player's crosshair on him (within FlareCone degrees of the line from the player to him)? No crosshair known (a joined friend without one): yes.</summary>
        public static bool LooksAt(Player q, ICombatant u)
        {
            var aim = Rt.Of(q).AimAt; if (aim == null) return true;
            var (qx, qy) = Ballistics.Pos(q); var (ux, uy) = Ballistics.Pos(u);
            float ax = aim.Value.x - qx, ay = aim.Value.y - qy, bx = ux - qx, by = uy - qy;
            float la = Mathf.Sqrt(ax * ax + ay * ay), lb = Mathf.Sqrt(bx * bx + by * by); if (la < 0.01f || lb < 0.01f) return true;
            return (ax * bx + ay * by) / (la * lb) >= Mathf.Cos(FlareCone * Mathf.Deg2Rad);
        }

        /// <summary>The Flare's blow: everyone within FlareRange looking at him is dazzled (aim gone, dazed 2 s).</summary>
        public static void Flare(Boss.Fight f)
        {
            var h = f.Unit; CombatFx.PxAt("thunderclap", h.X, h.Y); Sfx.Play("lantern_flare", TacticalGridView.GridToWorld(h.X, h.Y), 1f, 0.03f); int n = 0;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                if (CombatantUtil.Distance(h, q) > FlareRange || !LooksAt(q, h)) continue;
                var aim = Gunplay.AimOf(q); aim.Steady = 0f; aim.Bloom = Gunplay.PlayerMaxBloom; Body.Concuss(q, 2f); n++;
            }
            GameLog.Write(n > 0 ? "You looked at the lantern. The face in the glass looked back, and the green light burns into your eyes." : "The lantern blazes. You kept your eyes off it.");
        }

        /// <summary>The Hands' blow: drowned hands come up through the deck round him: everyone within HandsRange not diving is pulled down.</summary>
        public static void Hands(Boss.Fight f, Boss.Move m, int hi)
        {
            var h = f.Unit; Sfx.Play("drowned_hands", TacticalGridView.GridToWorld(h.X, h.Y), 1f, 0.04f); if (h.EquippedWeapon == null) return; int n = 0;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                if (CombatantUtil.Distance(h, q) > HandsRange) continue;
                CombatFx.PxAt("splash", q.X, q.Y);
                if (Rt.Of(q).Diving || Rt.Untouchable(q)) { CombatFx.Push("dodge", q, h, null).Now = true; continue; }
                Parry.Unparryable = true; try { Melee.HitUnit(h, h.EquippedWeapon, q, Boss.MultOf(m, hi)); } finally { Parry.Unparryable = false; }
                Rt.Knockdown(q, CombatantUtil.HasPerk(q, "Tumbler") ? 0.5f : 1.0f); n++;
            }
            if (n > 0) GameLog.Write("Cold hands come up out of water that is not there and pull your legs out from under you.");
        }

        static void Defeated(Player p, HumanEnemy h)
        {
            h.Inventory.Items.Add(new InventoryItem { Id = "ferry_lantern", Name = Trophy, Type = "misc", Qty = 1, WeightKg = 2f, Price = 2600 });
            Story.Journal("The Ferryman: on the Stranded Ferry's car deck something drowned and split open crawled through a lake that is not there, and lured with a lantern. The water is gone. The lantern is cold, and yours.");
            GameLog.Event("It folds flat onto the deck. The green light in the lantern gutters and goes out, and the water goes with it.");
        }
    }
}
