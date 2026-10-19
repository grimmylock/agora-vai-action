// New (file 09 Part 3): the Main Story, "The Heart of the Zone". Five acts tracked as flags in
// MacroState.StoryFlags; every step happens with E next to a canon landmark (file 06):
//   1 Origin      Secret Lab Alpha: keycard at the Reception Airlock, Fragment Alpha at Sample Storage
//   2 Guardians   Secret Lab Beta: the Research Drive in the Archive Room, decoded by Dr. Osei at the
//                 Sub-Station House (or Intelligence DC 18 at any lab landmark)
//   3 The Signal  Secret Lab Gamma: Fragment Gamma + The Lodestone in the Sealed Vault (clear the room first)
//   4 Allegiance  pick a sponsor at the Garrison Barricade (Abandoned City); they escort you through
//                 the Siege Ward
//   5 Last Gate   clear the Reactor Core, then open The Last Gate - the ending follows the sponsor
// Fragments and the keycard are "key" items: they can't be sold or dropped for Ru.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class StorySystem
    {
        public static readonly string[] SponsorFactions = { "Surveyors", "Contractors", "Sentinels", "Garrison", "Liberators", "Obelisk" };

        private static List<string> Flags => MacroState.Current.StoryFlags;
        public static bool Has(string f) => Flags.Contains(f);
        private static void Set(string f) { if (!Flags.Contains(f)) Flags.Add(f); }

        public static int Act =>
            Has("ending") ? 6 : Has("sponsor") ? 5 : Has("gamma") ? 4 : Has("beta") ? 3 : Has("alpha") ? 2 : 1;

        public static string Objective
        {
            get
            {
                switch (Act)
                {
                    case 1: return Has("keycard") ? "Open Sample Storage's sealed terminal in Secret Lab Alpha." : "Find Secret Lab Alpha (under the Industrial Basin's Loading Dock) and get the access keycard.";
                    case 2: return Has("beta_drive") ? "Have the Lab Beta drive decoded: Dr. Osei at the Sub-Station House (Dried Lake), or crack it yourself at a lab terminal (Intelligence DC 18)." : "Recover the Research Drive from Secret Lab Beta's Archive Room (under Ghost Town's Town Hall).";
                    case 3: return "Get into Secret Lab Gamma (under the Antenna Grid) and open the Sealed Vault.";
                    case 4: return "Choose who backs you at the Garrison Barricade in the Abandoned City - or go alone.";
                    case 5: return Has("control_room") ? "Open The Last Gate in the Reactor Core." : "Fight through the Reactor Core's Containment Hall and Control Room.";
                    default: return $"Done. Ending: {MacroState.Current.Ending}.";
                }
            }
        }

        public static string Fragments =>
            $"Alpha {(Has("alpha") ? "[x]" : "[ ]")}  Beta {(Has("beta") ? "[x]" : "[ ]")}  Gamma {(Has("gamma") ? "[x]" : "[ ]")}";

        /// <summary>Is this landmark where the story (or a legendary artifact) wants you next?</summary>
        public static bool IsObjective(string zone, string landmark)
        {
            if (zone == "outskirts" && landmark == "The Wound") return !Has("wound_heart");
            if (zone == "siege_catacombs" && landmark == "Sealed Crypt") return !Has("lucifer");
            switch (Act)
            {
                case 1: return zone == "secret_lab_alpha" && landmark == (Has("keycard") ? "Sample Storage" : "Reception Airlock");
                case 2: return Has("beta_drive") ? zone == "dried_lake" && landmark == "Sub-Station House" : zone == "secret_lab_beta" && landmark == "Archive Room";
                case 3: return zone == "secret_lab_gamma" && landmark == "Sealed Vault";
                case 4: return zone == "abandoned_city" && landmark == "Garrison Barricade";
                case 5: return zone == "reactor_core" && landmark == "The Last Gate";
                default: return false;
            }
        }

        private static LandmarkInstance Near(Player p, string name) =>
            TerrainMap.Landmarks.FirstOrDefault(l => l.Name == name && System.Math.Max(System.Math.Abs(l.X - p.X), System.Math.Abs(l.Y - p.Y)) <= 2);

        private static bool HostilesNear(Player p, int range) =>
            EncounterState.Enemies.Any(e => e.IsAlive() && e.IsAggroed && CombatantUtil.Distance(p, e) <= range && !(e is HumanEnemy h && EnemyAI.IsOut(h)));

        /// <summary>E next to a story landmark. Returns true if the story handled the key press.</summary>
        public static bool TryInteract(Player p)
        {
            string zone = MacroState.Current.CurrentZoneId;
            if (zone == "secret_lab_alpha" && !Has("keycard") && Near(p, "Reception Airlock") != null)
            {
                Set("keycard");
                GiveKey(p, "story_keycard", "Alpha Access Keycard");
                GameLog.Write("[STORY] A dead guard by the airlock still has his keycard clipped on: ALPHA ACCESS - PROJECT HEARTH.");
                return true;
            }
            if (zone == "secret_lab_alpha" && !Has("alpha") && Near(p, "Sample Storage") != null)
            {
                if (!Has("keycard")) { GameLog.Write("[STORY] A sealed terminal blinks: ACCESS CARD REQUIRED."); return true; }
                Set("alpha");
                p.RemoveItem("story_keycard", 1);
                GiveKey(p, "story_fragment_alpha", "Fragment Alpha (Origin Log)");
                GameLog.Write("[STORY] Act 1 - Origin. The terminal spits out a data core: the Origin Log.");
                GameLog.Write("  'Project Hearth, day 412. The Source responds to the lattice. Director approves full-power test.'");
                GameLog.Write("  The accident wasn't an accident. Somebody was studying something under the reactor before the Zone existed.");
                RewardCrate(p, 4, 2);
                p.CheckRankUp();
                return true;
            }
            if (zone == "secret_lab_beta" && Has("alpha") && !Has("beta_drive") && !Has("beta") && Near(p, "Archive Room") != null)
            {
                Set("beta_drive");
                GiveKey(p, "story_drive_beta", "Lab Beta Research Drive (encrypted)");
                GameLog.Write("[STORY] Act 2 - Guardians. Behind the archive's fire door, a hardened drive with the Hearth logo. Encrypted.");
                GameLog.Write("  Dr. Osei at the Dried Lake could crack it. So could you, at a working lab terminal, with a lot of patience.");
                return true;
            }
            if (Has("beta_drive") && !Has("beta"))
            {
                if (zone == "dried_lake" && Near(p, "Sub-Station House") != null)
                {
                    DecodeBeta(p, "Dr. Osei plugs the drive into a battered laptop and goes very quiet.");
                    p.AdjustReputation("Surveyors", Player.RepPoint);
                    return true;
                }
                if (zone.StartsWith("secret_lab") && TerrainMap.Landmarks.Any(l => System.Math.Max(System.Math.Abs(l.X - p.X), System.Math.Abs(l.Y - p.Y)) <= 2))
                {
                    if (p.Ap < 2) { GameLog.Write("Not enough AP."); return true; }
                    p.Ap -= 2;
                    int roll = UnityEngine.Random.Range(1, 21) + p.IntelligenceMod;
                    GameLog.Write($"You work the lab terminal on the drive: {roll} vs DC 18.");
                    if (roll >= 18) DecodeBeta(p, "The encryption gives way.");
                    else GameLog.Write("Gibberish. You'll have to try again (or find Dr. Osei).");
                    return true;
                }
            }
            if (zone == "secret_lab_gamma" && Has("beta") && !Has("gamma") && Near(p, "Sealed Vault") != null)
            {
                if (HostilesNear(p, 10)) { GameLog.Write("[STORY] Not with them still on you. Clear the room first."); return true; }
                Set("gamma");
                p.MainStoryAct3Done = true;
                GiveKey(p, "story_fragment_gamma", "Fragment Gamma");
                var compass = ArtifactDatabase.All["theLodestone"];
                p.AddItem(new InventoryItem { Id = "theLodestone", Name = compass.Name, Type = "artifact", Qty = 1, WeightKg = compass.WeightKg, Price = 20000 });
                GameLog.Write("[STORY] Act 3 - The Signal. The vault opens on a hum you feel in your teeth.");
                GameLog.Write("  The array isn't broadcasting to anyone. It's relaying the Source's voice - and Obelisk answers it like a god.");
                GameLog.Write("  You take Fragment Gamma, and a legendary artifact from its cradle: The Lodestone.");
                p.CheckRankUp();
                return true;
            }
            if (zone == "abandoned_city" && Has("gamma") && !Has("sponsor") && Near(p, "Garrison Barricade") != null)
            {
                ChooseSponsor(p);
                return true;
            }
            // Legendary artifacts with a fixed place (file 09)
            if (zone == "outskirts" && !Has("wound_heart") && Near(p, "The Wound") != null)
            {
                Set("wound_heart");
                GiveArtifact(p, "heartOfTheWound", 22000);
                GameLog.Write("[LEGEND] At the very edge of The Wound, where the air folds over itself, something pulses like a heartbeat: the Heart of the Wound.");
                return true;
            }
            if (zone == "siege_catacombs" && !Has("lucifer") && Near(p, "Sealed Crypt") != null)
            {
                if (EncounterState.Enemies.Any(e => e.Named && e.IsAlive())) { GameLog.Write("[LEGEND] Not with its guardian still breathing down your neck."); return true; }
                if (p.Ap < 2) { GameLog.Write("Not enough AP."); return true; }
                p.Ap -= 2;
                int roll = UnityEngine.Random.Range(1, 21) + p.StrengthMod;
                GameLog.Write($"You put your shoulder to the crypt's stone lid: {roll} vs DC 18.");
                if (roll < 18) { GameLog.Write("It doesn't budge. Yet."); return true; }
                Set("lucifer");
                GiveArtifact(p, "lucifer", 24000);
                GameLog.Write("[LEGEND] The lid grinds aside. In the dust of a very old grave: Lucifer, warm to the touch.");
                return true;
            }
            if (zone == "reactor_core" && Has("sponsor") && !Has("ending") && Near(p, "The Last Gate") != null)
            {
                if (!Has("control_room") && ReactorClear()) OnKill();
                if (!Has("control_room") || HostilesNear(p, 12)) { GameLog.Write("[STORY] Obelisk still holds the Control Room. You won't get the Gate open under their guns."); return true; }
                if (!(Has("alpha") && Has("beta") && Has("gamma"))) { GameLog.Write("[STORY] Three sockets in the Gate. You don't have all three Fragments."); return true; }
                LastGate(p);
                return true;
            }
            return false;
        }

        private static void DecodeBeta(Player p, string how)
        {
            Set("beta");
            p.RemoveItem("story_drive_beta", 1);
            GiveKey(p, "story_fragment_beta", "Fragment Beta (decoded drive)");
            GameLog.Write($"[STORY] {how}");
            GameLog.Write("  The labs bred the mutants on purpose - Dread Prowlers and worse, guardians for the Source.");
            GameLog.Write("  And the drive names a relay: the Antenna Grid. Secret Lab Gamma is under it.");
        }

        /// <summary>Called on every kill: the Reactor Core's Control Room counts as taken once the
        /// zone's hostiles are down.</summary>
        public static void OnKill(ICombatant target = null)
        {
            if (target is Mutant nm && nm.Named) OnNamedKilled(nm);
            if (MacroState.Current.CurrentZoneId != "reactor_core" || Has("control_room") || !ReactorClear()) return;
            Set("control_room");
            GameLog.Write("[STORY] The Control Room falls silent. Nothing stands between you and The Last Gate.");
        }

        // Obelisk's own people stand aside for their prophet-to-be (a Friendly faction doesn't block).
        private static bool ReactorClear() =>
            !EncounterState.Enemies.Any(e => e.IsAlive() && !EnemyAI.FriendlyToPlayer(e) && !(e is HumanEnemy h && EnemyAI.IsOut(h)));

        private static void ChooseSponsor(Player p)
        {
            var friends = SponsorFactions.Where(f => p.GetRepLevel(f) == "Friendly").ToList();
            var options = friends.Select(f => $"{f}: {EndingFor(f).name}").ToList();
            options.Add("Go alone: no help, no debts");
            ChoiceUI.Show("ACT 4 - ALLEGIANCE",
                "Everyone wants the Source. The Surveyors would contain it, the Contractors sell it, the soldiers destroy it, Obelisk wake it.\n" +
                "Only factions that trust you (Friendly) will back you. Whoever you pick escorts you through the Siege Ward - and decides what happens at the Gate.",
                options, i =>
                {
                    string sponsor = i < friends.Count ? friends[i] : "Alone";
                    MacroState.Current.Sponsor = sponsor;
                    Set("sponsor");
                    GameLog.Write(sponsor == "Alone"
                        ? "[STORY] Act 4 - you go alone. Nobody will help you through the Siege Ward, and nobody will own you after."
                        : $"[STORY] Act 4 - the {sponsor} back you. Their people will hold the Siege Ward open for you.");
                });
        }

        public static (string name, string text) EndingFor(string sponsor)
        {
            switch (sponsor)
            {
                case "Surveyors": return ("Contain it", "The Source is studied and sealed under watch. Surges only on a 20; Surveyor outposts trade at Friendly prices everywhere.");
                case "Contractors": return ("Sell it", "The Source is shipped out to the highest bidder. You get 20,000 Ru; the Contractors run every outpost and prices rise 20%.");
                case "Obelisk": return ("Wake it", "You become the Source's voice. The Zone grows; every other faction turns Hostile. You are Obelisk's prophet.");
                case "Alone": return ("Take it", "You carry part of the Source out yourself. Every faction is wary of you, but you keep The Black Star and gain +10 Max HP for good.");
                default: return ("Destroy it", "The Source is detonated. The Zone starts to shrink: the deepest zones fall to Tier 5, and artifact searches get 2 harder.");
            }
        }

        private static void LastGate(Player p)
        {
            var m = MacroState.Current;
            var (name, text) = EndingFor(m.Sponsor ?? "Alone");
            Set("ending");
            m.Ending = name;
            foreach (var id in new[] { "story_fragment_alpha", "story_fragment_beta", "story_fragment_gamma" }) p.RemoveItem(id, 1);
            var star = ArtifactDatabase.All["theBlackStar"];
            p.AddItem(new InventoryItem { Id = "theBlackStar", Name = star.Name, Type = "artifact", Qty = 1, WeightKg = star.WeightKg, Price = 25000 });
            GameLog.Write("[STORY] Act 5 - The Last Gate. Three Fragments, three sockets. The Gate opens on the Source.");
            GameLog.Write($"[ENDING] {name.ToUpperInvariant()}. {text}");
            GameLog.Write("  Behind the Gate you find The Black Star. The Zone goes on - and now everyone knows your name.");
            ApplyEnding(p, apply: true);
            p.MainStoryFinaleDone = true;
            p.CheckRankUp();
            ChoiceUI.Show($"ENDING - {name.ToUpperInvariant()}", text + "\n\nThe campaign doesn't stop here. Keep playing: the factions keep fighting over what's left.",
                new List<string> { "Continue" }, _ => { });
        }

        /// <summary>World changes of the ending (file 09). apply=true is the one-off part (Ru,
        /// reputation, HP); the persistent part (zone tiers) is re-applied after a load.</summary>
        public static void ApplyEnding(Player p, bool apply)
        {
            var m = MacroState.Current;
            if (string.IsNullOrEmpty(m.Ending)) return;
            if (m.Ending == "Destroy it")
                foreach (var z in MacroMapDatabase.Zones.Where(z => z.Tier > 5)) z.Tier = 5;
            if (!apply) return;
            switch (m.Ending)
            {
                case "Sell it": p.Ru += 20000; break;
                case "Wake it":
                    foreach (var f in FactionDatabase.Factions.Keys.Where(f => f != "Obelisk" && f != "Mutant"))
                        p.AdjustReputation(f, -100);
                    p.AdjustReputation("Obelisk", 100);
                    break;
                case "Take it":
                    foreach (var f in FactionDatabase.Factions.Keys.Where(f => f != "Mutant")) p.AdjustReputation(f, -Player.RepPoint);
                    p.BonusMaxHp += 10;
                    p.ApplyArtifactStatBonuses();
                    break;
            }
        }

        /// <summary>Price swing from the ending: Contractors' monopoly +20%; Surveyors' Contain
        /// gives Friendly prices at every outpost (handled by ShopSystem via ContainPrices).</summary>
        public static float PriceMultiplier => MacroState.Current.Ending == "Sell it" ? 1.2f : 1f;
        public static bool ContainPrices => MacroState.Current.Ending == "Contain it";
        public static int ArtifactSearchMod => MacroState.Current.Ending == "Destroy it" ? -2 : 0;

        /// <summary>The sponsor's escort: their people in the Siege Ward don't shoot at you, and
        /// its hazard drops by 1.</summary>
        public static bool Escorted(string zoneId) => zoneId == "hospital" && Has("sponsor") && !Has("ending") && MacroState.Current.Sponsor != "Alone";

        private static Dictionary<string, int> _originalTiers;

        /// <summary>Undo a previous run's world changes (ZoneDef is shared static data) - call on
        /// new game and load, before re-applying the current ending.</summary>
        public static void ResetWorld()
        {
            if (_originalTiers == null) _originalTiers = MacroMapDatabase.Zones.ToDictionary(z => z.Id, z => z.Tier);
            foreach (var z in MacroMapDatabase.Zones) z.Tier = _originalTiers[z.Id];
        }

        private static void GiveArtifact(Player p, string key, int price)
        {
            var a = ArtifactDatabase.All[key];
            p.AddItem(new InventoryItem { Id = key, Name = a.Name, Type = "artifact", Qty = 1, WeightKg = a.WeightKg, Price = price });
        }

        private static readonly string[] Titles = { "Old Scar", "the Widowmaker", "Grandmother", "the Gravedigger", "Hollow Jack", "the Choir", "Rust Tooth", "the Long Quiet", "Black Tongue", "the Ferryman" };

        /// <summary>On entering a zone: the Siege Catacombs' crypt guardian (until Lucifer is taken),
        /// and after the ending one Named mutant per Tier 5+ zone until it's killed (file 09).</summary>
        public static void OnEnterZone(ZoneDef zone)
        {
            if (zone == null || zone.IsSafeZone) return;
            bool guardian = zone.Id == "siege_catacombs" && !Has("lucifer") && !Has("named:siege_catacombs");
            int tier = _originalTiers != null && _originalTiers.TryGetValue(zone.Id, out var t) ? t : zone.Tier;
            bool afterEnding = Has("ending") && tier >= 5 && !Has("named:" + zone.Id);
            if (!guardian && !afterEnding) return;
            var pool = MutantDatabase.Tiers["tier5"].Concat(MutantDatabase.Tiers["tier4"]).ToList();
            var pick = pool[UnityEngine.Random.Range(0, pool.Count)];
            var spot = guardian ? TerrainMap.Landmarks.FirstOrDefault(l => l.Name == "Sealed Crypt") : null;
            int x = spot?.X ?? UnityEngine.Random.Range(4, TerrainMap.Width - 4), y = spot?.Y ?? UnityEngine.Random.Range(4, TerrainMap.Height - 4);
            var m = new Mutant(pick.Value.Name, x + 1, y, pick.Value);
            m.MakeNamed(Titles[System.Math.Abs((zone.Id + MacroState.Current.Day).GetHashCode()) % Titles.Length]);
            EncounterState.Enemies.Add(m);
            GameLog.Write(guardian
                ? $"[LEGEND] Something old is curled around the Sealed Crypt: {m.Name}."
                : $"[LEGEND] scavengers whisper about this place now. {m.Name} hunts here.");
            WorldEvents.NoteNpc(m.Name, "Hostile", "alive");
        }

        /// <summary>A Named mutant died: it stays dead, and the log remembers.</summary>
        public static void OnNamedKilled(Mutant m)
        {
            Set("named:" + MacroState.Current.CurrentZoneId);
            WorldEvents.NoteNpc(m.Name, "Hostile", "dead - you killed it");
            GameLog.Write($"[LEGEND] {m.Name} is dead. People will talk about this.");
        }

        private static void GiveKey(Player p, string id, string name) =>
            p.AddItem(new InventoryItem { Id = id, Name = name, Type = "key", Qty = 1, WeightKg = 0.1f, Price = 0 });

        /// <summary>Major-quest reward crate (file 07): GM picks - here, `count` real items from the
        /// Tier `tier` table (no junk or cash) plus a Ru purse.</summary>
        private static void RewardCrate(Player p, int tier, int count)
        {
            GameLog.Write("[STORY] Sample Storage's back room holds a Hearth supply crate:");
            for (int i = 0; i < count; i++)
            {
                LootRoll r = null;
                for (int k = 0; k < 10 && (r == null || r.Type == "junk" || r.Type == "cash" || r.Type == "consumable"); k++) r = Economy.Roll(tier, trader: true);
                if (r == null || r.Type == "junk" || r.Type == "cash") continue;
                p.AddItem(r.ToItem(ItemCondition.Working));
                GameLog.Write($"  - {r.Name}");
            }
            p.Ru += tier * 500;
            GameLog.Write($"  - {tier * 500} Ru in old Hearth scrip, still good with the Contractors.");
        }
    }
}
