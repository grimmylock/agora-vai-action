// Batch 88 (Yan: "the g-7 is either used by an enemy npc boss for a hidden sidequest/lab (not any of the already made ones)";
// "special synthesizer in a northern area ... lab that turns artifacts into ammo, give a reason to revisit lab's"; "yes for
// the g-7 proposal"). The Lattice Foundry: a hidden Tier 5 lab under the Ship Graveyard's old slipway (found like the other
// hidden ways: Search near the Flooded Slipway, rumours point at it). An Obelisk crew holds it, led by a boss who carries
// the G-7 Rail Prototype and spare cells - kill him and both are on his body. The Cell Synthesizer there turns one artifact
// into rail cells (the better the artifact, the more), for as long as you bring artifacts.
using System.Linq;
using GameData;

namespace GameCore
{
    public static class G7Lab
    {
        // Batch 91: two floors. The test level (upstairs) has a guard squad; the Foreman, his crew and the synthesizer are on
        // the synthesis level below.
        public const string Upper = "lattice_foundry", Zone = "lattice_foundry_lower", Machine = "Cell Synthesizer", CellKey = "cell|Standard";
        const string CrewFlag = "g7_crew", GuardFlag = "g7_guards";
        public const int SpareCells = 12;

        /// <summary>ZoneTravel, on arrival: the first time, the crew is there (afterwards they're a squad that holds the lab).</summary>
        public static void OnEnterZone(ZoneDef zone)
        {
            var flags = MacroState.Current.StoryFlags;
            if (zone?.Id == Upper && !flags.Contains(GuardFlag))
            {
                flags.Add(GuardFlag);
                var guards = SquadManager.CreateOfflineSquad(Upper, "Obelisk", 4);
                if (guards != null)
                {
                    guards.Job = "hold";
                    foreach (var m in guards.Members.Where(m => m.IsAlive())) { m.IsAggroed = false; if (!EncounterState.Enemies.Contains(m)) EncounterState.Enemies.Add(m); }
                    SquadManager.PositionSquadsInZone(Upper);
                }
                return;
            }
            if (zone?.Id != Zone || flags.Contains(CrewFlag)) return;
            flags.Add(CrewFlag);
            var sq = SquadManager.CreateOfflineSquad(Zone, "Obelisk", 4);
            if (sq == null) return;
            sq.Job = "hold";
            var lead = sq.Members.OfType<HumanEnemy>().OrderByDescending(h => CombatRules.RankIndex(h.Rank)).First();
            lead.EquipWeaponFromDb(WeaponDatabase.Categories["sniper"]["G7RailPrototype"]);
            lead.MakeBoss("Foreman");
            lead.Inventory.AmmoReserve[CellKey] = SpareCells;   // on his body when he falls
            foreach (var m in sq.Members.Where(m => m.IsAlive())) { m.IsAggroed = false; if (!EncounterState.Enemies.Contains(m)) EncounterState.Enemies.Add(m); }
            SquadManager.PositionSquadsInZone(Zone);
            GameLog.Write($"[LEGEND] Voices below, and a sound like a capacitor charging. {lead.Name} runs this place.");
        }

        /// <summary>Rail cells for one artifact: 2 per tier, 15 for a legendary.</summary>
        public static int CellsFor(ArtifactDef a) => a == null ? 0 : a.Type == "Legendary" ? 15 : 2 * System.Math.Max(1, a.Tier);

        /// <summary>E at the synthesizer (InteractionEngine).</summary>
        public static bool TryInteract(Player p)
        {
            if (MacroState.Current.CurrentZoneId != Zone || !TerrainMap.Landmarks.Any(l => l.Name == Machine && System.Math.Max(System.Math.Abs(l.X - p.X), System.Math.Abs(l.Y - p.Y)) <= 2)) return false;
            if (EncounterState.InCombat) { GameLog.Write("Not with bullets flying."); return true; }
            var item = p.Inventory.Items.Where(i => i.Type == "artifact" && ArtifactDatabase.All.ContainsKey(i.Id))
                .OrderBy(i => ArtifactDatabase.All[i.Id].Tier).FirstOrDefault();   // the cheapest first
            if (item == null) { GameLog.Write("The synthesizer's hopper is empty. It needs an artifact to work with."); return true; }
            var art = ArtifactDatabase.All[item.Id];
            p.RemoveItem(item.Id, 1);
            int n = CellsFor(art);
            Ammo.Add(p.Inventory, CellKey, n);
            GameLog.Write($"The synthesizer swallows the {art.Name}, whines, and spits out {n} rail cells.");
            return true;
        }
    }
}
