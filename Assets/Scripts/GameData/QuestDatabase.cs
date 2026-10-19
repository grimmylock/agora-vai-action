// New (Batch 6): ported from the Quest Bank reference file - 30 standalone jobs (Part 1, Tiers
// 1-7) plus 10 three-stage NPC storyline arcs (Part 2, one per Faction Outpost Quartermaster from
// the Zone Map file / QuartermasterDatabase.cs) = 60 individually-trackable quest entries, exactly
// matching the source's own "~60 pre-written quests" count once storyline stages are counted
// individually.
//
// ponytail: the source's quest verbs (Escort/Protect, Retrieve, Sabotage/Investigate) assume a GM
// improvising NPC movement, dialogue, and guarded landmarks - none of which this project has (no
// escort AI, no per-landmark interactables yet - that's Batch 7). QuestSystem.cs collapses every
// quest down to the 3 mechanics this game actually has: Kill (a tiered target, in-zone), SkillCheck
// (a Perception roll in-zone, or "discover this exact hidden node" when the source quest already
// ties to one of MacroMapDatabase's real hidden connections), and ClearZone (every hostile squad
// in the zone is dead - the honest stand-in for "protect/escort," which this game has no NPC-AI
// mechanic for at all). Every quest below still carries its full source checklist text
// (ObjectiveText) for flavor/display even where several display lines resolve together off one
// real mechanical trigger - see QuestSystem.AcceptQuest's comment for why.
//
// Ambiguous source values (a range like "NPC Rookie/Experienced" always maps to the same reward
// bucket regardless of which half you pick; an unnumbered "pack"/"nest" kill quota defaults to 3;
// a "Target: NPC squad if contested" with no rank given defaults to the lowest bucket) are resolved
// with the most conservative reasonable reading rather than guessed precisely - noted inline only
// where a real judgment call was made, not for every mechanical field.
using System.Collections.Generic;
using System.Linq;

namespace GameData
{
    public enum QuestCategory { Elimination, KillQuota, Retrieve, Escort, Investigate }

    public class QuestDef
    {
        public string Id;
        public string Hook;                 // flavor/title line
        public string[] ObjectiveText;      // source checklist lines, display-only
        public string ZoneId;               // null = "any zone at ZoneTier" (bound to the current zone on accept)
        public int ZoneTier;
        public QuestCategory Category;
        public string TargetKind;           // "Mutant" | "NPC" | null (no guaranteed target)
        public int TargetTier;              // Mutant: tier 1-5. NPC: bucket 0=Rookie/Experienced,
                                             // 1=Professional/Veteran, 2=Expert/Master, 3=Legend
                                             // (see QuestSystem.NpcRankBucket - the Economy file's
                                             // Target Difficulty Bonus table already pairs ranks
                                             // this way, so it doubles as the reward-lookup key).
        public int KillCount = 1;           // KillQuota only
        public bool Guarded;                 // Retrieve only - also requires killing TargetKind/TargetTier first
        public string HiddenNodeId;         // Investigate/Retrieve only - ties completion to an
                                             // existing MacroMapDatabase hidden connection instead
                                             // of a fresh skill-check roll, when the source quest
                                             // already describes finding one of those exact nodes

        // NPC storyline only (Stage 0 = standalone quest, ignore the rest of this block)
        public string NpcId;                // the Quartermaster's zone id (QuartermasterDatabase.Find key)
        public int Stage;
        public string Faction;              // reputation nudged +5 here on stage completion
        public bool FriendlyGate;           // must already be Friendly with Faction to see this stage at all
    }

    public static class QuestDatabase
    {
        private static QuestDef Q(string id, string zoneId, int zoneTier, QuestCategory cat, string hook, string[] obj,
            string targetKind = null, int targetTier = 0, int count = 1, bool guarded = false, string hiddenNode = null,
            string npcId = null, int stage = 0, string faction = null, bool friendlyGate = false) => new QuestDef
        {
            Id = id, ZoneId = zoneId, ZoneTier = zoneTier, Category = cat, Hook = hook, ObjectiveText = obj,
            TargetKind = targetKind, TargetTier = targetTier, KillCount = count, Guarded = guarded, HiddenNodeId = hiddenNode,
            NpcId = npcId, Stage = stage, Faction = faction, FriendlyGate = friendlyGate,
        };

        public static readonly List<QuestDef> All = new List<QuestDef>
        {
            // ---- Part 1: Standalone quests ----
            Q("T1-01", "perimeter", 1, QuestCategory.Investigate, "Yefim noticed movement at the fence line and wants to know what's coming.",
                new[] { "Investigate the breached fence near Watchtower 3", "Determine who/what caused it" }),
            Q("T1-02", "marshes", 1, QuestCategory.Retrieve, "A buyer wants the drum's contents analyzed, no questions asked.",
                new[] { "Recover a sealed chemical drum from the Sunken Tanker", "Return it unopened" }, "Mutant", 1, guarded: true),
            Q("T1-03", "marshes", 1, QuestCategory.Elimination, "Caravans keep getting hit at the same spot.",
                new[] { "Eliminate the Raider using the Reed Blind to ambush travelers" }, "NPC", 0),
            Q("T1-04", "smugglers_run", 1, QuestCategory.KillQuota, "The Runner's couriers keep getting swarmed.",
                new[] { "Clear 3 Rot Swarm mutants nesting near Blind Curve" }, "Mutant", 1, count: 3),
            Q("T1-05", null, 1, QuestCategory.Escort, "Low-stakes mentorship job - good campaign-opener filler.",
                new[] { "Escort a greenhorn scavenger safely to their first destination" }),

            Q("T2-01", "industrial_basin", 2, QuestCategory.KillQuota, "Wanderers want the silos safe to scavenge.",
                new[] { "Clear the Pallid Prowler pack denning in Silo Row" }, "Mutant", 1, count: 3),
            Q("T2-02", "scrapyard", 2, QuestCategory.Retrieve, "A minor job unrelated to (or a lead-in for) the Scrap King's own storyline.",
                new[] { "Recover a stolen trade ledger from a rival camp in The Maze", "Return it" }, "NPC", 0, guarded: true),
            Q("T2-03", "black_valley", 2, QuestCategory.Escort, "Liberators need the route open.",
                new[] { "Protect a supply convoy crossing the Chokepoint Bridge" }),
            Q("T2-04", null, 2, QuestCategory.Elimination, "Generic bounty-board filler.",
                new[] { "Eliminate a named bounty target (GM picks identity/faction)" }, "NPC", 1),
            Q("T2-05", "rail_yards", 2, QuestCategory.Investigate, "Contractors are spooked by an unidentified broadcast.",
                new[] { "Investigate strange radio chatter from the Signal Tower", "Report or silence its source" }),

            Q("T3-01", "dried_lake", 3, QuestCategory.Retrieve, "Surveyors want working gear, not scrap.",
                new[] { "Recover intact equipment from the Tower Line before mutants strip it" }),
            Q("T3-02", "dried_lake", 3, QuestCategory.KillQuota, "Cloaked ambush predators - a good showcase for the Cloak mechanic.",
                new[] { "Eliminate 2 Veil Prowlers preying on travelers crossing The Flats" }, "Mutant", 2, count: 2),
            Q("T3-03", "rail_yards", 3, QuestCategory.Investigate, "Ties directly into using a hidden sub-node.",
                new[] { "Find the Maintenance Tunnels (Perception DC 15)", "Investigate movement below the yard" }, hiddenNode: "maintenance_tunnels"),
            Q("T3-04", null, 3, QuestCategory.Escort, "Good Sentinel-reputation builder, zone-agnostic.",
                new[] { "Escort a Sentinel patrol through hostile territory" }),
            Q("T3-05", "dried_lake", 3, QuestCategory.Elimination, "Campaign note: may already be resolved by an ongoing Dried Lake questline.",
                new[] { "Eliminate the Alpha Prowler claiming territory near the Sub-Station House" }, "Mutant", 3),

            Q("T4-01", "vanguard_barracks", 4, QuestCategory.Retrieve, "Whichever faction currently holds the barracks needs this.",
                new[] { "Recover ammo crates from the Motor Pool before Raiders scavenge them" }, "NPC", 0, guarded: true),
            Q("T4-02", "ghost_town", 4, QuestCategory.KillQuota, "Squatters want the block livable again.",
                new[] { "Clear 2 Alpha Prowlers or Mind Weavers denning in the Row Houses" }, "Mutant", 3, count: 2),
            Q("T4-03", "crimson_woods", 4, QuestCategory.Investigate, "Espionage-flavored - careful, non-combat play rewarded.",
                new[] { "Investigate the Obelisk Shrine without provoking a fight", "Learn what Obelisk is doing at Deadfall Clearing" }),
            Q("T4-04", "ghost_town", 4, QuestCategory.Retrieve, "A solid payoff for actively searching a zone.",
                new[] { "Find Secret Lab Beta (Perception DC 17)", "Recover intact samples from the Isolation Ward" }, hiddenNode: "secret_lab_beta"),
            Q("T4-05", null, 4, QuestCategory.Elimination, "Zone-agnostic escalation bounty.",
                new[] { "Eliminate a high-value enemy officer" }, "NPC", 2),

            Q("T5-01", "quarry_basin", 5, QuestCategory.Investigate, "Contractors are worried about their equipment.",
                new[] { "Investigate anomalous activity concentrated at the Pit Floor (bring a Detector)" }),
            Q("T5-02", "ship_graveyard", 5, QuestCategory.Retrieve, "Salvage job with a real fight attached.",
                new[] { "Clear a Hulk of nesting mutants", "Recover cargo from its hold" }, "Mutant", 3, guarded: true),
            Q("T5-03", "antenna_grid", 5, QuestCategory.Elimination, "Good showcase for the tank vs. Leap mutant archetypes.",
                new[] { "Eliminate the mutant disrupting the Array's equipment" }, "Mutant", 4),
            Q("T5-04", null, 5, QuestCategory.KillQuota, "Zone-agnostic filler at the high end.",
                new[] { "Eliminate 3 hostile squad members raiding supply convoys" }, "NPC", 1, count: 3),

            Q("T6-01", "abandoned_city", 6, QuestCategory.KillQuota, "Time-pressure flavor - the push is coming regardless.",
                new[] { "Clear Highrise Row of a mutant nest before a Garrison push" }, "Mutant", 4, count: 3),
            Q("T6-02", "outskirts", 6, QuestCategory.Investigate, "Extremely dangerous dead-end zone - treat with real weight.",
                new[] { "Reach the Crater Rim", "Retrieve data from Scattered Relics without approaching The Wound" }),
            Q("T6-03", "hospital", 6, QuestCategory.Escort, "Trench-warfare set piece.",
                new[] { "Protect a Garrison medic team resupplying the Field Hospital Ruins" }),
            Q("T6-04", "hospital", 6, QuestCategory.Elimination, "Apex human-NPC bossfight.",
                new[] { "Eliminate the Obelisk sniper controlling the Bell Tower" }, "NPC", 3),

            Q("T7-01", "reactor_core", 7, QuestCategory.Elimination, "Penultimate campaign beat.",
                new[] { "Fight through the Containment Hall", "Eliminate whatever Obelisk has stationed at the Control Room" }, "Mutant", 5),
            Q("T7-02", "reactor_core", 7, QuestCategory.Investigate, "Campaign ending - narrative payoff, not really about the Ru.",
                new[] { "Reach The Last Gate", "Make the choice the campaign has been building toward" }),

            // ---- Part 2: NPC storyline arcs (NpcId = the Quartermaster's zone id) ----
            Q("YEFIM-1", "perimeter", 1, QuestCategory.Investigate, "Establishes Yefim as the camp's quiet caretaker.",
                new[] { "Clear debris blocking the Checkpoint Gate", "Report back" }, npcId: "bonfire_village", stage: 1, faction: "Wanderers"),
            Q("YEFIM-2", "perimeter", 1, QuestCategory.Retrieve, "Reveals Yefim used to run with a Raider crew.",
                new[] { "Recover a personal keepsake near Watchtower 3, guarded by a small Raider group", "Return it" },
                "NPC", 0, guarded: true, npcId: "bonfire_village", stage: 2, faction: "Wanderers"),
            Q("YEFIM-3", "perimeter", 2, QuestCategory.Elimination, "Closes the arc: Bogdan, a Raider warlord who wronged Yefim years ago.",
                new[] { "Track down Bogdan, a Raider warlord who wronged Yefim years ago", "Confront him (combat or persuasion, GM's call)" },
                "NPC", 1, npcId: "bonfire_village", stage: 3, faction: "Wanderers"),

            Q("RUNNER-1", "smugglers_run", 1, QuestCategory.Escort, "Establishes trust (or tests the player's restraint).",
                new[] { "Carry a sealed package to a contact in the next zone without opening it" }, npcId: "smugglers_run", stage: 1, faction: "Wanderers"),
            Q("RUNNER-2", "smugglers_run", 1, QuestCategory.Elimination, "The Runner is being leaned on by someone bigger.",
                new[] { "Survive an ambush at Blind Curve tied to the package", "Learn who set it up" },
                "NPC", 0, npcId: "smugglers_run", stage: 2, faction: "Wanderers"),
            Q("RUNNER-3", "smugglers_run", 2, QuestCategory.Elimination, "Success makes The Runner a better-stocked, fully independent trader.",
                new[] { "Help The Runner break free of his patron, violently or otherwise" },
                "NPC", 1, npcId: "smugglers_run", stage: 3, faction: "Wanderers"),

            Q("SCRAPKING-1", "scrapyard", 2, QuestCategory.Elimination, "Establishes the working relationship (may already be done).",
                new[] { "Clear the Industrial Basin loading dock of a rival raider presence" },
                "NPC", 1, npcId: "scrapyard", stage: 1, faction: "Raiders"),
            Q("SCRAPKING-2", "scrapyard", 2, QuestCategory.Investigate, "Someone's challenging the King's hold on the yard.",
                new[] { "Investigate a rival scavenger crew moving into Crusher Row", "Identify their backer" }, npcId: "scrapyard", stage: 2, faction: "Raiders"),
            Q("SCRAPKING-3", "scrapyard", 3, QuestCategory.Elimination, "Cements the Scrap King's control (or, GM's option, backs Rooke instead).",
                new[] { "Defeat Rooke and his enforcers at The Maze", "Report back" },
                "NPC", 2, npcId: "scrapyard", stage: 3, faction: "Raiders"),

            Q("VOSS-1", "rail_yards", 3, QuestCategory.Escort, "Standard contract work.",
                new[] { "Escort a supply train through the Container Stacks" }, npcId: "rail_yards", stage: 1, faction: "Contractors"),
            Q("VOSS-2", "rail_yards", 3, QuestCategory.Investigate, "Someone inside is feeding the enemy.",
                new[] { "Investigate repeated sabotage on Contractor shipments", "Identify the leak" }, npcId: "rail_yards", stage: 2, faction: "Contractors"),
            Q("VOSS-3", "rail_yards", 4, QuestCategory.Elimination, "Loyalty is rewarded with permanent first-pick access to Voss's stock.",
                new[] { "Confront the named turncoat at the Signal Tower" }, "NPC", 1, npcId: "rail_yards", stage: 3, faction: "Contractors"),

            Q("OSEI-1", "dried_lake", 3, QuestCategory.Elimination, "Campaign note: may already be resolved - use that outcome as this stage's result.",
                new[] { "Eliminate the Alpha Prowler guarding the Sub-Station", "Recover the dead researcher's hardened drive" },
                "Mutant", 3, npcId: "dried_lake", stage: 1, faction: "Surveyors"),
            Q("OSEI-2", "dried_lake", 3, QuestCategory.Investigate, "Plants a lingering mystery thread.",
                new[] { "Deliver the drive to Dr. Osei", "Investigate the Sub-Station Depths (Perception DC 16) for what the researcher was really tracking" },
                hiddenNode: "sub_station_depths", npcId: "dried_lake", stage: 2, faction: "Surveyors"),
            Q("OSEI-3", "dried_lake", 4, QuestCategory.Elimination, "Sets up a thread that can pay off later in the campaign.",
                new[] { "Deal with whatever the Depths investigation stirred up", "Decide whether to report the discovery or suppress it" },
                "Mutant", 4, npcId: "dried_lake", stage: 3, faction: "Surveyors"),

            Q("KADE-1", "vanguard_barracks", 4, QuestCategory.Escort, "Establishes the barracks' precarious situation.",
                new[] { "Defend the Parade Ground during a Raider probe" }, npcId: "vanguard_barracks", stage: 1, faction: "Sentinels"),
            Q("KADE-2", "vanguard_barracks", 4, QuestCategory.Investigate, "Internal-threat escalation.",
                new[] { "Identify a defector feeding the enemy faction barracks positions" }, npcId: "vanguard_barracks", stage: 2, faction: "Sentinels"),
            Q("KADE-3", "vanguard_barracks", 5, QuestCategory.Elimination, "The player's choice here can flip which faction holds this outpost.",
                new[] { "Break the enemy push", "Decide who ultimately controls Vanguard Barracks" },
                "NPC", 3, npcId: "vanguard_barracks", stage: 3, faction: "Sentinels"),

            Q("MARA-1", "ghost_town", 4, QuestCategory.Investigate, "Low-key opener.",
                new[] { "Disarm booby traps in an adjacent shopfront so Mara can expand" }, npcId: "ghost_town", stage: 1),
            Q("MARA-2", "ghost_town", 4, QuestCategory.Elimination, "Raises the stakes on her situation.",
                new[] { "Deal with a faction shaking Mara down for \"protection money\"" }, "NPC", 1, npcId: "ghost_town", stage: 2),
            Q("MARA-3", "ghost_town", 5, QuestCategory.Escort, "Success permanently upgrades Mara into a proper Friendly-rate static trader.",
                new[] { "Defend Main Street during a full raid" }, npcId: "ghost_town", stage: 3),

            Q("IRYNA-1", "quarry_basin", 5, QuestCategory.Retrieve, "Introduces the pit's danger.",
                new[] { "Recover equipment lost to Pit Floor anomalies" }, npcId: "quarry_basin", stage: 1, faction: "Contractors"),
            Q("IRYNA-2", "quarry_basin", 5, QuestCategory.Investigate, "Foreshadows a psychic threat.",
                new[] { "Investigate erratic worker behavior near the Crane Rig" }, npcId: "quarry_basin", stage: 2, faction: "Contractors"),
            Q("IRYNA-3", "quarry_basin", 6, QuestCategory.Elimination, "Good showcase for Psi Blast/Neural Overload as \"the reveal.\"",
                new[] { "Eliminate whatever's psychically influencing the crew" }, "Mutant", 5, npcId: "quarry_basin", stage: 3, faction: "Contractors"),

            Q("BOSCH-1", "ship_graveyard", 5, QuestCategory.KillQuota, "Community-focused opener.",
                new[] { "Clear a beached freighter of nesting mutants" }, "Mutant", 3, count: 3, npcId: "ship_graveyard", stage: 1, faction: "Wanderers"),
            Q("BOSCH-2", "ship_graveyard", 5, QuestCategory.Retrieve, "Personal stakes.",
                new[] { "Recover a family heirloom from a sunken/dangerous hull" }, npcId: "ship_graveyard", stage: 2, faction: "Wanderers"),
            Q("BOSCH-3", "ship_graveyard", 5, QuestCategory.Escort, "Emotional community-defense finale.",
                new[] { "Defend Captain's Row from a raid" }, npcId: "ship_graveyard", stage: 3, faction: "Wanderers"),

            Q("ANATOLY-1", "antenna_grid", 5, QuestCategory.Elimination, "Tests commitment; morally loaded by design. Friendly Obelisk reputation required.",
                new[] { "Complete a task proving loyalty to Obelisk" }, "NPC", 1, npcId: "antenna_grid", stage: 1, faction: "Obelisk", friendlyGate: true),
            Q("ANATOLY-2", "antenna_grid", 5, QuestCategory.Retrieve, "Ties into a hidden sub-node.",
                new[] { "Find Secret Lab Gamma (Perception DC 18)", "Recover a \"holy\" relic from the Sealed Vault" },
                hiddenNode: "secret_lab_gamma", npcId: "antenna_grid", stage: 2, faction: "Obelisk"),
            Q("ANATOLY-3", "antenna_grid", 6, QuestCategory.Investigate, "Foreshadows the Reactor Core endgame - should leave the player unsettled, not triumphant.",
                new[] { "Witness/participate in an Obelisk ritual at the Array", "Learn what the Antenna Grid is really broadcasting" },
                npcId: "antenna_grid", stage: 3, faction: "Obelisk"),
        };

        public static QuestDef Find(string id) => All.FirstOrDefault(q => q.Id == id);
    }
}
