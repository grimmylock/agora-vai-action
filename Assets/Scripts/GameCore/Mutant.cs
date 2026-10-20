// Ported from script.js: class Mutant
// moveToward()/moveAwayFrom()/attack() depend on the grid/combat systems from later port steps
// (GameServices.Grid/Combat) — full logic ported here, not stubbed.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine; // Random.value — swap for System.Random if this data layer is ever used outside Unity

namespace GameCore
{
    public class Mutant : ICombatant
    {
        public string Name { get; protected set; }
        public int X { get; set; }
        public int Y { get; set; }

        public virtual string Faction => "Mutant";

        public MutantDef Db;

        public int MaxHp;
        public int Hp { get; set; }
        public float PhysDR { get; set; }
        public float AnomDR { get; set; }
        public int Ap { get; set; }
        public float Mp { get; set; }
        public int MaxMp { get; set; } // not set by the original Mutant ctor (defaults to 0); HumanEnemy sets it via armor
        public int Ac { get; set; }
        public int AttackDamage;

        // New: MutantLootSystem.GrantDrop guards against a double-grant (e.g. if something else
        // still fires damage at an already-0-HP target) with this flag - set true the moment loot
        // is granted, checked before granting again.
        public bool LootGranted;
        public bool Named;          // file 09: a Named mutant - x1.5 HP, +2 to hit, a borrowed trait, better drops
        public int AttackBonus;
        public bool Interrogated;   // surrendered humans: one interrogation each

        /// <summary>Turns this mutant into a Named one (file 09, "After the ending").</summary>
        public void MakeNamed(string title)
        {
            Named = true;
            Name = $"{title} ({Name})";
            MaxHp = (int)(MaxHp * 1.5f);
            Hp = MaxHp;
            AttackBonus = 2;
            switch (Random.Range(0, 3))
            {
                case 0: ActiveCamouflage = true; IsCloaked = true; break;   // a Dread Prowler that can Cloak
                case 1: _packTacticsEnabled = true; break;
                default: ThroatLacerationChance = System.Math.Max(ThroatLacerationChance, 0.35f); break;
            }
            PanicThreshold = 0f;
        }

        // Stagger (file 04): 40%+ of max HP lost in one player turn costs a non-berserk mutant
        // its next turn. Counted by Weapon.Fire / Explosions, checked and reset by TurnLoop.
        public int DamageTakenThisRound;
        public int ChaseTurns;         // Batch 130: Blight Brute turns started without reaching you (the 4th throws a boulder)
        public bool SlamUsed;          // Blight Brute's Ground Slam is once per encounter
        public bool LeapAdvantage;     // set for the duration of a Dread Prowler's Leap Attack

        protected float _agilityMod;
        public float ThroatLacerationChance;
        protected bool _packTacticsEnabled;
        public bool ActiveCamouflage;

        /// <summary>Cloak (Veil Prowler/Alpha Prowler/Void Weaver): starts true for
        /// ActiveCamouflage mutants, cleared by Attack(). See RequiresDetectionCheck for what it
        /// means mechanically - untargetable beyond 4 tiles without a Perception check, always
        /// visible within 3 regardless of this flag.
        /// ponytail: nothing currently re-sets this true after Attack() clears it (that re-cloak-
        /// on-retreat behavior belongs in the master turn loop, out of scope here) - once a cloaked
        /// mutant swings once, it stays visible for the rest of the fight rather than fading back
        /// out. Upgrade path: TurnLoop.PlayerEndTurn's mutant-turn branch, symmetric with how it
        /// already resets Mp/aggro per turn.</summary>
        public bool IsCloaked;
        public int StalkRounds;   // Batch 61: rounds spent circling without a way to strike; after 2 it walks the long way in

        public float PanicThreshold;
        public string State = "normal"; // "normal" | "fleeing"

        public List<string> ActivePerks { get; } = new List<string>(); // Mutants don't have perks in the source data

        // Ambient-spawn / aggro state (read by the master turn loop, ported in the UI/game-loop
        // step — declared here since EncounterGeneration.cs already needs to set IsAggroed=false
        // on ambient spawns).
        public bool IsAggroed;
        // Batch 127c (Yan: "a detection system for enemies based on line of sight... yellow when they detect something and red
        // when they confirm"): 0-1, how close this unit is to spotting you (Contact.Notice); SpottedAt = Time.time it did.
        public float Alert, SpottedAt = -99f;
        public int VisionRange = 12;   // Batch 65: was 8; your own daylight sight went 13 -> 20
        public (int x, int y)? PatrolTarget;
        public float AlertUntil;
        public int RestUntil;                    // Batch 67: game minute an idle leader rests until (0 = walking); not saved
        /// <summary>Batch 128: turn to look at a tile (spotting you, shooting another squad, talking to you).</summary>
        public void FaceToward(int x, int y) { if (x != X || y != Y) Facing = (System.Math.Sign(x - X), System.Math.Sign(y - Y)); }
        public (int x, int y) Facing = (0, 1);   // Batch 66: last step taken; an idle guard sees far ahead, little behind   // Batch 62: game hour (Day*24+Hour) until which it watches harder after losing you

        // Added for the anomaly system generalization — see ICombatant.cs note.
        public StatusEffects StatusEffects { get; } = new StatusEffects();
        public int PendingApDrain { get; set; }
        public float ArtifactAvoidanceBonus => 0f;
        public bool ArtifactAutoPassAnomalies => false;
        public object ActiveDetector => null;

        public Mutant(string name, int x, int y, MutantDef db = null)
        {
            Name = name;
            var spawn = TerrainMap.FindNearestEnterable(x, y);
            // don't stack on someone already standing there (groups spawn in a tight 3x3)
            for (int r = 1; r <= 3 && TerrainMap.UnitAt(spawn.x, spawn.y) != null; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int nx = spawn.x + dx, ny = spawn.y + dy;
                        if (TerrainMap.UnitAt(spawn.x, spawn.y) == null) break;
                        if (nx < 0 || ny < 0 || nx >= TerrainMap.Width || ny >= TerrainMap.Height) continue;
                        if (TerrainMap.CanEnterTile(nx, ny) && TerrainMap.UnitAt(nx, ny) == null) spawn = (nx, ny);
                    }
            X = spawn.x;
            Y = spawn.y;
            Db = db;
            MaxHp = db?.Hp ?? 300;
            Hp = MaxHp;
            PhysDR = db?.PhysDR ?? 0f;
            AnomDR = db?.AnomDR ?? 0f;
            Ap = db?.Ap ?? 4;
            Mp = db?.Mp ?? 4;
            Ac = db?.Ac ?? 10;
            AttackDamage = db?.AttackDamage ?? 6;
            _agilityMod = db?.AgilityMod ?? 0;
            ThroatLacerationChance = db?.ThroatLacerationChance ?? 0f; // only Veil/Alpha Prowlers cause bleeding (file 04)
            _packTacticsEnabled = db?.PackTactics ?? false;
            ActiveCamouflage = db?.ActiveCamouflage ?? false;
            IsCloaked = ActiveCamouflage; // Cloak-capable mutants start every encounter cloaked
            PanicThreshold = db?.PanicThreshold ?? 0.25f;
            State = "normal";
        }

        public bool IsAlive() => Hp > 0;

        public float AgilityMod => _agilityMod;

        /// <summary>Ported from the `get packTactics()` accessor — true if another living pack member is adjacent.</summary>
        public bool PackTactics =>
            _packTacticsEnabled && EncounterState.Enemies.Any(other => other != this && other.IsAlive() && !(other is HumanEnemy)
                && (other.Db == Db || other._packTacticsEnabled) && CombatantUtil.Distance(this, other) <= 1);

        /// <summary>Ported from isFleeing().</summary>
        public virtual bool IsFleeing() =>
            State == "fleeing" || (PanicThreshold > 0 && (float)Hp / MaxHp <= PanicThreshold);

        /// <summary>Ported from checkPanic().</summary>
        public void CheckPanic()
        {
            if (State != "fleeing" && PanicThreshold > 0 && (float)Hp / MaxHp <= PanicThreshold)
            {
                State = "fleeing";
                GameLog.Write($"{Name} panics and tries to fall back!");
            }
        }

        /// <summary>Ported from moveToward(target). Returns whether a step was actually taken.</summary>
        public string Carries;     // Batch 126: the key to a locked stash on this map (Stashes)
        public int RestFor = 45;   // Batch 110: how long the squad rests at its next stop (Posts.NextStop)

        public virtual bool MoveToward(ICombatant target) => MoveToward(target.X, target.Y);

        /// <summary>
        /// Overload accepting a raw coordinate, since the original's moveToward(target) was called
        /// both with a full unit (target.x/target.y) and with plain {x,y} patrol-point objects
        /// (see the ambient-wander loop, ported in the game-loop step). Same logic either way.
        /// </summary>
        public virtual bool MoveToward(int targetX, int targetY)
        {
            if (Mp <= 0) return false;

            var grid = GameServices.RequireGrid();
            var path = grid.FindPath(X, Y, targetX, targetY, this);

            if (path.Count > 1)
            {
                var nextStep = path[1];
                float cost = grid.GetMoveCost(this, nextStep.x, nextStep.y);
                if (Mp >= cost && grid.AttemptMove(this, nextStep.x, nextStep.y))
                    return true;
                // Batch 126: a squadmate in the way (out of a fight): trade places with it
                if (Mp >= cost && !EncounterState.InCombat && Pathfinding.SquadLeader(this) && TerrainMap.UnitAt(nextStep.x, nextStep.y, this) is Mutant mate
                    && MacroState.Current.OfflineSquads.Any(q => q.Members.Contains(this) && q.Members.Contains(mate)))
                {
                    mate.X = X; mate.Y = Y; X = nextStep.x; Y = nextStep.y; Mp -= cost;
                    return true;
                }
            }

            // Fallback linear movement if A* found no valid route (e.g. no path exists at all).
            // Only attempt it when the guessed tile is actually enterable - previously this called
            // AttemptMove blind, which logs its own "blocked by solid obstacle" message on every
            // miss. Since a solid guess is common (any diagonal-ish direction has good odds of
            // hitting a wall), that spammed the log every time A* legitimately found nothing AND
            // silently ate the unit's move attempt for the turn, which is why enemies could look
            // like they were closing in (real A* steps taken earlier the same turn/tick were
            // silent successes) but then stall out short of attack range.
            int dx = System.Math.Sign(targetX - X);
            int dy = System.Math.Sign(targetY - Y);
            int fallbackX = X + dx, fallbackY = Y + dy;
            if (!grid.CanEnterTile(fallbackX, fallbackY)) return false;
            return grid.AttemptMove(this, fallbackX, fallbackY);
        }

        /// <summary>Ported from moveAwayFrom(target). Returns whether a step was actually taken.</summary>
        public virtual bool MoveAwayFrom(ICombatant target)
        {
            if (Mp <= 0) return false;

            int dx = System.Math.Sign(X - target.X);
            if (dx == 0) dx = Random.value < 0.5f ? -1 : 1;
            int dy = System.Math.Sign(Y - target.Y);
            if (dy == 0) dy = Random.value < 0.5f ? -1 : 1;

            int newX = System.Math.Min(System.Math.Max(0, X + dx), TerrainMap.Width - 1);
            int newY = System.Math.Min(System.Math.Max(0, Y + dy), TerrainMap.Height - 1);

            if (!GameServices.RequireGrid().AttemptMove(this, newX, newY))
                return false;

            GameLog.Write($"{Name} retreats to ({X}, {Y}). MP remaining: {Mp}.");
            return true;
        }

        /// <summary>
        /// Ported from the `typeof enemy.executeSpecial === 'function'` check in playerEndTurn().
        /// NOTE (pre-existing dead code, preserved): no class in the original script.js ever
        /// actually defines executeSpecial — not Mutant, not any subclass — so this branch never
        /// ran in the original either. It reads as a hook left in place for a special-ability
        /// system that was never implemented. Kept here as a virtual returning false so the turn
        /// loop's shape matches exactly; override it on a subclass if you want to actually wire up
        /// mutant special abilities (psi blast, ground slam, etc. currently only affect flavor
        /// text/log lines inside Attack(), not real bonus effects).
        /// </summary>
        public virtual bool ExecuteSpecial(ICombatant target) => false;

        /// <summary>New: Behavior=="ranged" mutants (MindWeaver/PsiBlast, VoidWeaver/NeuralOverload)
        /// deliberately hold position instead of closing to melee when the player is 2-5 tiles away
        /// in LoS (see TurnLoop's movementTarget==null branch) - but Attack() used to require strict
        /// adjacency regardless, so those mutants just stood there doing nothing for as long as the
        /// player stayed in that band. Shared by TurnLoop's engage check and Attack()'s own guard so
        /// both agree on what "in range" means for a given mutant.
        /// ponytail: same flat AttackDamage/to-hit as a melee hit, not a distinct psi damage type
        /// or an AnomDR-specific formula - upgrade if you want psi attacks to feel mechanically
        /// different from a claw swipe, not just longer-ranged.</summary>
        public const int RangedEngageTiles = 5;

        public bool IsInEngageRange(ICombatant target) =>
            Db?.Behavior == "ranged"
                ? CombatantUtil.Distance(this, target) <= RangedEngageTiles && GameServices.RequireGrid().CheckLineOfSight(X, Y, target.X, target.Y)
                : CombatantUtil.IsAdjacent(this, target);

        /// <summary>New: the Mutants &amp; Factions reference's 12+Tier DC formula - used both for
        /// Cloak detection (Veil Prowler 14, Alpha Prowler 15, Void Weaver 17) and for the Agility
        /// dodge check against a Dread Prowler's Leap Attack. Read by PlayerController's target-
        /// selection guard.</summary>
        public int MutantCheckDC => 12 + (Db?.Tier ?? 1);

        /// <summary>True if a cloaked mutant at this distance from you can't be made out (Batch 49: beyond CloakSpotRange,
        /// no roll). Vision.Shows uses it, so it's neither drawn nor targetable.</summary>
        public bool RequiresDetectionCheck(int attackerDistance) => IsCloaked && attackerDistance > CloakSpotRange();

        /// <summary>Batch 49: how close you must be to make out a cloaked mutant - 3 tiles plus your Perception modifier,
        /// shortened by weather and dark like enemy sight (at least 1). Beyond it the mutant isn't drawn or targetable;
        /// there's no Perception roll any more.</summary>
        public static int CloakSpotRange()
        {
            var p = Player.Current;
            return System.Math.Max(1, (int)System.Math.Round((3 + (p?.PerceptionMod ?? 0)) * Atmosphere.SightFactor()));
        }

        public bool HiddenByCloak() => Player.Current != null && RequiresDetectionCheck(CombatantUtil.Distance(Player.Current, this));

        /// <summary>Ported from attack(target), on the design's rules (file 04): d20 + Agility vs
        /// AC (advantage with Pack Tactics), natural 20 doubles. Claws and bites are physical -
        /// armor Phys-DR reduces them (Alpha Prowler ignores 20% of it); psi blasts (Mind/Void
        /// Weaver) only see Anom-DR, and cover applies against them. Hits can wound humans.</summary>
        public virtual void Attack(ICombatant target)
        {
            if (!IsInEngageRange(target)) return;

            if (ActiveCamouflage) IsCloaked = false; // attacking breaks Cloak

            bool psi = Db != null && (Db.PsiBlast || Db.NeuralOverload);
            bool advantage = PackTactics || LeapAdvantage;
            string who = target is Player ? "Player" : target.Name;
            GameLog.Write($"{Name} {(psi ? "lashes out with a psi blast at" : "attacks")} {who}{(advantage ? " with Pack Tactics" : "")}.");
            var fx = CombatFx.Push("strike", this, target);
            fx.Psi = psi;

            int ac = target.Ac;
            if (psi)
            {
                var cover = CoverUtil.GetCover(this, target, null);
                ac += cover.AcBonus;
                if (cover.AcBonus > 0 && target.StatusEffects.TakeCoverAc > 0 && !target.StatusEffects.EvasiveMelee) ac += target.StatusEffects.TakeCoverAc;
            }
            else if (target.StatusEffects.EvasiveMelee) ac += target.StatusEffects.TakeCoverAc;

            if (Rt.On && target is Player diver && Rt.Of(diver).Diving) { GameLog.Write($"You dive under {Name}'s strike."); return; }
            var result = Rt.On ? new AttackRollResult { Hit = true, ChosenRoll = 0 }   // real time: no dice - in reach when it strikes = hit
                : GameServices.RequireCombat().RollAttack(advantage ? "advantage" : "standard", (int)AgilityMod + AttackBonus - (StatusEffects.Wound == "Arm" ? 2 : 0), ac);   // Batch 57: maimed claws
            bool crit = result.ChosenRoll == CombatRules.Headshot;
            if (!result.Hit && !crit)
            {
                GameLog.Write($"{Name} misses {who}.");
                return;
            }

            if (!psi && target is Player parrier && parrier.ParryHeld)
            {
                parrier.ParryHeld = false;
                int pr = Random.Range(1, 21) + (int)parrier.AgilityMod;
                GameLog.Write($"Desperate Parry: {pr} vs DC 14 - {(pr >= 14 ? "you knock the blow aside!" : "too slow.")}");
                if (pr >= 14) return;
            }
            float tierMult = Rt.On ? 1f : CombatRules.HitMultiplier(result.ChosenRoll, result.ChosenRoll + (int)AgilityMod + AttackBonus - (StatusEffects.Wound == "Arm" ? 2 : 0), ac);
            float raw = AttackDamage * tierMult;
            int damage = psi
                ? CombatRules.AnomalousDamage(raw, target.AnomDR)
                : CombatRules.PhysicalDamage(raw, "Standard", target.PhysDR, Db?.ArmorPierce ?? 0f);
            int hpBefore = target.Hp;
            target.Hp = System.Math.Max(0, target.Hp - damage);
            fx.Hit = true; fx.Killed = !target.IsAlive(); fx.Overkill = damage - hpBefore;
            GameLog.Write($"{Name} hits {who} for {damage} {(psi ? "psi " : "")}damage{(crit ? " (headshot!)" : "")}. {who} HP is now {target.Hp}.");
            if (!psi) CombatRules.CheckWound(target, 0, CombatRules.HitWounds(tierMult, null));   // Batch 60

            if (target.IsAlive())
            {
                if (ThroatLacerationChance > 0 && Random.value < ThroatLacerationChance)
                {
                    target.StatusEffects.Bleeding = true;
                    GameLog.Write($"{who} is slashed open and starts bleeding.");
                }
                if (Db != null && Db.ToxicBite && Random.Range(1, 21) >= 17)
                {
                    target.StatusEffects.ToxicBleed = true;
                    GameLog.Write($"The bite festers: {who} suffers Toxic Bleed.");
                }
                if (Db != null && Db.NeuralOverload && target is Player p)
                {
                    int roll = Random.Range(1, 21) + p.IntelligenceMod;
                    if (roll < 14)
                    {
                        p.PendingApDrain += 1;
                        GameLog.Write($"Neural Overload (Intelligence {roll} vs 14): you lose 1 AP next turn.");
                    }
                }
            }
            else if (target is HumanEnemy) Weapon.OnTargetKilled(this, target);

            if (ActiveCamouflage)   // Batch 51: a cloaked hunter always strikes and slips back (was a 15% coin flip)
            {
                GameLog.Write($"{Name} fades back into cover after the strike.");
                MoveAwayFrom(target);
            }
        }
    }
}
