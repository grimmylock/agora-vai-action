// New (file 00 §13): rank-up milestones and perk picks. The player ranks up the moment every
// milestone for the next rank is met; Professional, Expert and Legend each grant a perk
// (picked in CharacterUI). Master and Legend need the Main Story (file 09), which isn't in the
// game yet, so those two can't be reached for now.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public partial class Player
    {
        public int MaxTierReached = 1;
        public int QuestsCompleted;
        public int MaxMutantTierKilled;
        public int MaxNpcRankKilled = -1;      // CombatRules.RankIndex of the toughest human you killed
        public bool HiddenNodeCleared;
        public int ArcsFinished;
        public bool MainStoryAct3Done, MainStoryFinaleDone;   // set by the story once file 09 is in
        public int PendingPerkPicks;
        public int BonusMaxHp;   // permanent (the "Take it" ending)

        // per-turn / per-fight perk trackers
        public bool MovedThisTurn, HitAndRunUsed, RunAndGunUsed, OverwatchHeld, ParryHeld, AdrenalineUsed;
        public int MeleeAttacksThisTurn, SidearmShotsThisTurn, AdrenalineApNextTurn;
        public string ArmedAction;   // "suppress" while Suppressive Fire waits for a target tile

        private static readonly string[] PerkRanks = { "Professional", "Expert", "Legend" };

        /// <summary>The milestones for `rank` (the one after the current), each with met/not met.</summary>
        public List<(string text, bool met)> MilestonesFor(string rank)
        {
            int prof = CombatRules.RankIndex("Professional"), legend = CombatRules.RankIndex("Legend");
            switch (rank)
            {
                case "Experienced": return new List<(string, bool)> { ("Reach a Tier 2 zone", MaxTierReached >= 2), ("Complete 1 quest", QuestsCompleted >= 1) };
                case "Professional": return new List<(string, bool)> { ("Complete 3 quests", QuestsCompleted >= 3), ("Kill a Tier 2 mutant or a Professional+ NPC", MaxMutantTierKilled >= 2 || MaxNpcRankKilled >= prof) };
                case "Veteran": return new List<(string, bool)> { ("Reach a Tier 3 zone", MaxTierReached >= 3), ("Clear a hidden sub-zone or finish an NPC storyline", HiddenNodeCleared || ArcsFinished >= 1) };
                case "Expert": return new List<(string, bool)> { ("Reach a Tier 4 zone", MaxTierReached >= 4), ("Kill a Tier 4 mutant", MaxMutantTierKilled >= 4), ("Finish an NPC storyline", ArcsFinished >= 1) };
                case "Master": return new List<(string, bool)> { ("Reach a Tier 5 zone", MaxTierReached >= 5), ("Main Story Act 3 (not in the game yet)", MainStoryAct3Done), ("Kill a Tier 5 mutant or a Legend NPC", MaxMutantTierKilled >= 5 || MaxNpcRankKilled >= legend) };
                case "Legend": return new List<(string, bool)> { ("Main Story finale (not in the game yet)", MainStoryFinaleDone) };
                default: return new List<(string, bool)>();
            }
        }

        public string NextRank
        {
            get
            {
                int i = CombatRules.RankIndex(Rank) + 1;
                return i < CombatRules.RankOrder.Length ? CombatRules.RankOrder[i] : null;
            }
        }

        /// <summary>Ranks up as far as the milestones allow. Call after anything that can meet one.</summary>
        public void CheckRankUp()
        {
            while (NextRank != null)
            {
                var next = NextRank;
                var ms = MilestonesFor(next);
                if (ms.Count == 0 || ms.Any(m => !m.met)) return;
                Rank = next;
                GameLog.Write($"[RANK UP] You are now {next}. ({string.Join("; ", ms.Select(m => m.text))})");
                GameLog.Write($"  AP {MaxAp}, attack/AC modifier {CombatRules.RankMod(Rank):+0;-0;0}.");
                if (PerkRanks.Contains(next))
                {
                    PendingPerkPicks++;
                    GameLog.Write("  You can pick a new perk (P).");
                    CharacterUI.ShowPerkPick();
                }
                ApplyCarryEffects();
            }
        }

        public void NoteZoneTier(int tier)
        {
            if (tier <= MaxTierReached) return;
            MaxTierReached = tier;
            CheckRankUp();
        }

        public void NoteKill(ICombatant target)
        {
            if (target is HumanEnemy h) MaxNpcRankKilled = System.Math.Max(MaxNpcRankKilled, CombatRules.RankIndex(h.Rank));
            else if (target is Mutant m && m.Db != null) MaxMutantTierKilled = System.Math.Max(MaxMutantTierKilled, m.Db.Tier);
            var zone = MacroMapDatabase.Find(MacroState.Current.CurrentZoneId);
            if (zone?.AccessCondition != null && !HiddenNodeCleared && !EncounterState.Enemies.Any(e => e.IsAlive()))
            {
                HiddenNodeCleared = true;
                GameLog.Write($"You've cleared {zone.Name}.");
            }
            CheckRankUp();
        }

        public void NoteQuestDone(QuestDef q)
        {
            QuestsCompleted++;
            if (q.Stage > 0 && !QuestDatabase.All.Any(o => o.NpcId == q.NpcId && o.Stage > q.Stage))
            {
                ArcsFinished++;
                GameLog.Write("That was the end of their story.");
            }
            CheckRankUp();
        }

        /// <summary>Run and Gun (H): once per turn, 1 AP for +2 MP.</summary>
        public void RunAndGun()
        {
            if (!CombatantUtil.HasPerk(this, "RunAndGun")) { GameLog.Write("You need the Run and Gun perk."); return; }
            if (RunAndGunUsed || Ap < 1) { GameLog.Write(RunAndGunUsed ? "Already used Run and Gun this turn." : "Not enough AP."); return; }
            RunAndGunUsed = true; Ap -= 1; Mp += 2;
            GameLog.Write("Run and Gun: 1 AP for +2 MP.");
        }

        /// <summary>Suppressive Fire (S, then click): 3 AP with any automatic gun (Batch 127; was the perk's and rifles' only);
        /// every enemy within 2 tiles of the tile (4 with the Suppressive Fire perk) is Suppressed and must pass Agility DC 13
        /// or lose 2 AP next turn.</summary>
        public static int SuppressRadius(Player p) => CombatantUtil.HasPerk(p, "SuppressiveFire") ? 4 : 2;

        public void ArmSuppressiveFire()
        {
            if (!CombatRules.CanBurst(EquippedWeapon)) { GameLog.Write("Suppressing fire needs an automatic gun."); return; }
            ArmedAction = ArmedAction == "suppress" ? null : "suppress";
            GameLog.Write(ArmedAction == null ? "Suppressive Fire cancelled." : "Suppressive Fire: click a tile in range (3 AP). Right-click cancels.");
        }

        public void SuppressAt(int x, int y)
        {
            ArmedAction = null;
            var w = EquippedWeapon;
            int rounds = CombatRules.CanBurst(w) ? CombatRules.BurstRounds(w) : 3;
            if (Ap < 3) { GameLog.Write("Suppressive Fire needs 3 AP."); return; }
            if (w == null || w.IsJammed || w.CurrentMagazine < rounds) { GameLog.Write($"Suppressive Fire needs {rounds} rounds loaded."); return; }
            if (System.Math.Max(System.Math.Abs(x - X), System.Math.Abs(y - Y)) > w.MaxReach || !GameServices.RequireGrid().CheckLineOfSight(X, Y, x, y))
            { GameLog.Write("That spot is out of range or out of sight."); return; }
            Ap -= 3; w.CurrentMagazine -= rounds;
            EncounterState.InCombat = true;
            GameLog.Write($"You hose the area with {rounds} rounds!");
            foreach (var e in EncounterState.Enemies.Where(e => e.IsAlive() && System.Math.Max(System.Math.Abs(e.X - x), System.Math.Abs(e.Y - y)) <= SuppressRadius(this)))
            {
                e.IsAggroed = true;
                e.StatusEffects.Suppressed = true;
                int roll = UnityEngine.Random.Range(1, 21) + (int)e.AgilityMod;
                if (roll < 13) { e.PendingApDrain += 2; GameLog.Write($"{e.Name} is pinned down (Suppressed, -2 AP next turn)."); }
                else GameLog.Write($"{e.Name} is Suppressed but keeps moving.");
            }
        }

        // ---- Gunslinger (file 05): a second sidearm in the off hand ----
        public Weapon OffhandWeapon;
        public string OffhandKey;

        public bool Gunslinging => OffhandWeapon != null && CombatantUtil.HasPerk(this, "Gunslinger")
            && CombatRules.Category(EquippedWeapon?.Db) == "sidearm";

        /// <summary>Puts a sidearm from the pack into the off hand (Gunslinger only); the old
        /// off-hand gun goes back into the pack.</summary>
        public void EquipOffhandFromInventory(string itemId)
        {
            var item = Inventory.Items.FirstOrDefault(i => i.Id == itemId && i.Type == "weapon" && i.Category == "sidearm");
            if (item == null) return;
            if (!CombatantUtil.HasPerk(this, "Gunslinger")) { GameLog.Write("You need the Gunslinger perk to fight two-handed."); return; }
            StowOffhand();
            RemoveItem(itemId, 1);
            OffhandWeapon = MakeWeapon("sidearm", item.Key);
            if (OffhandWeapon == null) return;
            OffhandWeapon.Condition = string.IsNullOrEmpty(item.Condition) ? ItemCondition.Working : item.Condition;
            OffhandWeapon.Upgrades = Upgrades.Parse(item.Upgrades);
            OffhandWeapon.ApplyUpgrades();
            OffhandKey = item.Key;
            GameLog.Write($"Off hand: {OffhandWeapon.Name}. Single shots now fire both guns (2 AP, -1 each).");
            ApplyCarryEffects();
        }

        public void StowOffhand()
        {
            if (OffhandWeapon == null) return;
            var d = ShopDatabase.Items.FirstOrDefault(i => i.Type == "weapon" && i.Category == "sidearm" && i.Key == OffhandKey);
            if (d != null) AddItem(new InventoryItem { Id = d.Id, Name = d.Name, WeightKg = d.WeightKg, Qty = 1, Type = "weapon", Price = d.Price,
                Category = "sidearm", Key = OffhandKey, Condition = OffhandWeapon.Condition, Upgrades = Upgrades.Join(OffhandWeapon.Upgrades) });
            OffhandWeapon = null; OffhandKey = null;
            ApplyCarryEffects();
        }

        public Weapon MakeWeapon(string category, string key)
        {
            var db = WeaponDatabase.Get(category, key);
            if (db == null) return null;
            int mag = db.MagCapacity ?? db.CylinderCapacity ?? 1;
            var w = new Weapon(db.Name ?? key, db.BaseDamage > 0 ? db.BaseDamage : 1, mag, "Standard",
                new Dictionary<string, int> { ["Standard"] = 0, ["HollowPoint"] = 0, ["ArmorPiercing"] = 0 });
            w.Owner = this; w.Db = db; w.RangeTiles = RangeUtil.ParseRangeToTiles(db.Range);
            return w;
        }

        public bool TakePerk(string key)
        {
            if (!PerkDatabase.All.ContainsKey(key) || ActivePerks.Contains(key)) return false;
            var req = PerkDatabase.All[key].Requires;
            if (req != null && !ActivePerks.Contains(req)) { GameLog.Write($"{PerkDatabase.All[key].Name} needs {PerkDatabase.All[req].Name} first."); return false; }
            ActivePerks.Add(key);
            GameLog.Write($"Perk gained: {PerkDatabase.All[key].Name} - {PerkDatabase.All[key].Description}");
            ApplyCarryEffects();
            return true;
        }
    }
}
