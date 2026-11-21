// Boss 1 (Yan, 2026-10-29: "build the secret lab hatchet ... bossfight ... to be used as baseline for the bossfights in this game,
// im a sucker for HYPE MOMENTS AND AURA stuff like metal gear rising revengeance, dragon ball, sekiro"):
// the boss framework, and the first boss on it.
// - A boss fights with telegraphed moves: a WHITE glint (and its tink) = parry it; a RED glint (and a low drone) = it can't be
//   parried, dodge it (the dive, or just get out of reach). Combos chain several hits.
// - "perfect parries stagger him for a second, normal parries just block the damage" (Parry.cs): while staggered he takes
//   everything; otherwise he swats bullets away in a shower of sparks (Yan left guns to me - this makes the parry the way in
//   and your gun the punish).
// - Phase 2 at half health: his aura flares, a sting, faster and nastier moves.
// - At 0 HP he doesn't die - he drops to a knee, broken: "when hp bar gets to 0 we can press a key to finish him off" - E next to
//   him: the finisher (slow motion, the camera on you both, a hit-stop, the blood). "after he's down and looted, a few seconds
//   later a smoke grenade goes off and he's gone".
// - The HUD (HudView.Boss.cs): his name card when the fight starts, his health bar, STAGGERED / EXECUTE.
// Bosses aren't nerfed for difficulty (rule 6): tune them by giving the player tools.
// Co-op: he fights the nearest player (Rt.TargetOf); any player can parry him, shoot him while he's open, or finish him.
// ponytail: the hit-stop is the host's time scale (fine solo; co-op clients don't share the host's clock yet).
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Boss
    {
        public class Move
        {
            public string Name;
            public float Wind = 0.6f;      // the glint, then this long before the hit lands
            public bool Danger;            // red glint: can't be parried
            public int Hits = 1;           // a combo: each hit has its own glint
            public float Gap = 0.4f;       // between a combo's hits
            public float Lunge;            // > 0: he closes this far in one rush before the hit
            public string[] Effects;       // 2026-11-21 (the Verger's beast): what a blow does instead of a plain swing: "howl" (a cone: aim blown, knocked down), "quake" (a ring round him); null/empty = the weapon's swing
            public float MaxDist = 1f;     // how far from you he may start a move that is not a rush (a Howl reaches across the room)
            public float[] Gaps, Mults;    // 2026-11-21 (the Verger's combos): the pause before each following hit, and each hit's own damage multiplier (null = Gap / Mult for all)
            public bool[] RedHits;         // 2026-11-21: which hits of a combo are red (can't be parried, a dive goes under): the Smash that finishes a combo
            public float BelowHp;          // 2026-11-21: > 0: only once his health is below this share of his full health (the beast's last stretch)
            public string Clip;            // 2026-11-20 (the Verger's beast): which animation clip plays for this move (VergerBoss.Clips)
            public float Drag;             // 2026-11-20 (the Verger's Hook): > 0: he does not rush in, he stops at the blade's tip and, if it cuts you, drags you up to this many tiles toward him
            public float Mult = 1f;        // x the weapon's damage
            public int Weight = 1;         // how often he picks it
            public bool Phase2Only, Phase1Only;     // 2026-11-20 (the Verger): a two-form boss has moves for only one form
            public int[] Poses = { 0 };
            public float Open = 1f;        // Visor 5: after it he stops to get his breath - still, knife low, bullets land - this long (x phase speed)    // Visor 2: his pose for each hit (HeldPoseMath's Visor moves), in order, wrapping
        }

        public class Def
        {
            public string Name, Title, Weapon, Armor;
            public string Faction = "Contractors", Rank = "Legend", DoneFlag; public bool Vanishes = true;      // 2026-11-19 (Spool): who he fights for, the story flag his execution sets, whether a smoke grenade takes his body
            public int Hp;
            public float StepSeconds = 0.35f, Recover = 1.0f;
            public float Pace = 0.5f;          // Visor 4: his swings play at this speed - the strike starts ChopTime/Pace before the hit
            public int ParriesToStagger = 3;  // Yan: "make it so he needs 3 parries to get stunned"
            public Move[] Moves;
            public string Phase2Line;
            public string Art;             // 2026-11-21: a creature boss drawn from Pixel Lab clips (BossArt.cs, BOSS_CONCEPTS_V3.md); null = a body with a held weapon
            public bool Rooted;            // 2026-11-21 (the Singer): it never walks or steps into a blow
            public bool Creature;          // no weapon or armour to loot from it (its execution gives its own spoils)
            public System.Action<Player, HumanEnemy> Defeated;     // its own end (spoils, story), called by the execution
            public string PoseKey;         // 2026-11-21: his own pose set (HeldPoseMath.VisorPoses / VergerPoses), null = his weapon's
            public string[] Phase2Barks;   // lines over his head when phase 2 starts
            public Transform Change;       // 2026-11-20 (the Verger, BOSS_DESIGNS.md): at half health he changes (a window, a new weapon) instead of just flaring
        }
        /// <summary>The half-health change: he is hittable but takes (1 - Reduction) of the damage for `Seconds`, does nothing meanwhile, then fights on with `Weapon` (a melee db key).</summary>
        public class Transform { public string Weapon, Splash, Line; public float Seconds = 4f, Reduction = 0.9f; public string[] Barks; }

        public class Fight
        {
            public Def Def; public HumanEnemy Unit;
            public int Phase = 1;
            public Move Doing; public int HitsLeft; public float StrikeAt = -1f, NextMoveAt, NextStep, StaggerUntil = -1f;
            public bool Broken, Finished, Gone; public float LootedAt = -1f;
            public float GlintAt = -99f; public bool GlintDanger;   // for the view
            public float Started;
            public int Parries; public float LastParryAt = -99f;
            public float GlintDue = -1f;
            public string Clip; public float ClipT0, ClipScale = 1f;   // the animation clip playing (VergerBoss.Clips) and when it started
            public int AimX, AimY;             // 2026-11-21: where the target stood when the move began (a spit lands there)
            public float StruckAt = -99f;      // when his last hit landed (the beast's attack frame)
            public float ChangeUntil = -1f; public bool Beast;   // the half-health change: the window it lasts, and whether he is in his second form
            public float OpenUntil = -1f;  // Visor 5: his opening after a move (Yan: "a few in between pauses between his attacks and combos, window of oportunity")   // Visor 4: the glint comes GlintLead before each hit, the same beat for every move   // perfect parries toward the next stagger (they fade after GuardRecovers s)
        }
        public const float GuardRecovers = 8f, GlintLead = 0.4f;
        /// <summary>2026-11-21: is hit `i` (0-based) of this move red? A move is red whole (Danger) or hit by hit (RedHits).</summary>
        public static bool Red(Move m, int i) => m.Danger || (m.RedHits != null && m.RedHits[System.Math.Min(i, m.RedHits.Length - 1)]);
        public static float GapBefore(Move m, int i) => m.Gaps != null ? m.Gaps[System.Math.Min(System.Math.Max(0, i - 1), m.Gaps.Length - 1)] : m.Gap;      // the pause before hit i (i >= 1)
        public static float MultOf(Move m, int i) => m.Mults != null ? m.Mults[i % m.Mults.Length] : m.Mult;

        public static readonly List<Fight> Fights = new List<Fight>();
        public static Fight Of(ICombatant u) => u == null ? null : Fights.FirstOrDefault(f => f.Unit == u && !f.Gone);
        public static bool Is(ICombatant u) => Of(u) != null;
        public static Fight Current => Fights.FirstOrDefault(f => !f.Gone && !f.Finished) ?? Fights.LastOrDefault(f => !f.Gone && Time.time - (f.LootedAt < 0 ? Time.time : f.LootedAt) < 4f);

        // the name card (HudView.Boss.cs reads it)
        public static string SplashTitle, SplashSub; public static float SplashAt = -99f, SplashSeconds = 3.2f;
        static void Splash(string title, string sub) { SplashTitle = title; SplashSub = sub; SplashAt = Time.unscaledTime; }
        public static void NameCard(string title, string sub) => Splash(title, sub);      // Boss 3's introduction shows its own card

        public static void Reset() { Sleepers.Clear(); Fights.Clear(); _stopUntil = -1f; _slowUntil = -1f; _momentAt = -99f; _holdUntil = -1f; Parry.Unparryable = false; SplashAt = -99f; }

        // ---------------- the first boss ----------------

        // Yan's hatchet fight (ACTION_DESIGN "Melee"): a corpse in black armour with a red visor, the Tactical Hatchet buried in
        // it; take the hatchet and he gets up - a nod to a certain unkillable courier, no names from that game. He draws a knife.
        /// <summary>Yan: "a black suit with a red visor and a gas mask" (ArtSource/make_unique_armor.py) - his alone, not in any loot table.</summary>
        public static readonly ArmorDef BlackVisorSuit = new ArmorDef { Name = "Black Visor Suit", PhysDR = 0.6f, AnomDR = 0.3f, BeltSlots = 2, ModSlots = 2, Mp = 4, WeightKg = 14f };
        static ArmorDef SuitOf(Def def) { if (def.Armor == null) return null; if (def.Armor == BlackVisorSuit.Name) return BlackVisorSuit; if (def.Armor == "The Scrap King") return StoryNpcs.SuitOf("The Scrap King", 0.4f); foreach (var t in new[] { "tier5", "tier4", "tier3", "tier2", "tier1" }) { var a = ArmorDatabase.Get(t, def.Armor); if (a != null) return a; } return null; }

        public static readonly Def Visor = new Def
        {
            Name = "The Visor", Title = "HE ALWAYS GETS UP", Weapon = "SerratedTrenchKnife", Armor = "Black Visor Suit", Hp = 4800, DoneFlag = "visor_done", PoseKey = HeldPoseMath.VisorPoses,   // Yan: "visor got absolutely crushed lol, he needs more hp, at least double"
            StepSeconds = 0.3f, Recover = 0.9f, Phase2Line = "The red visor flares. He stops holding back.",
            Moves = new[]
            {
                // Visor 2: each move strings his own poses (backhand 0, rising 1, ice-pick 2, thrust 3, spin 4, execution 5)
                new Move { Name = "Slash", Wind = 0.6f, Weight = 2, Poses = new[] { 0 }, Open = 1f },
                new Move { Name = "Ice pick", Wind = 0.7f, Weight = 1, Mult = 1.15f, Poses = new[] { 2 }, Open = 1.2f },
                new Move { Name = "Flurry", Wind = 0.5f, Hits = 3, Gap = 0.38f, Mult = 0.7f, Weight = 2, Poses = new[] { 0, 1, 2 }, Open = 1.5f },
                new Move { Name = "Lunge", Wind = 0.75f, Danger = true, Lunge = 5f, Mult = 1.4f, Weight = 1, Poses = new[] { 3 }, Open = 2f },
                new Move { Name = "Feint flurry", Wind = 0.4f, Hits = 5, Gap = 0.3f, Mult = 0.6f, Weight = 2, Phase2Only = true, Poses = new[] { 0, 1, 4, 0, 2 }, Open = 1.8f },
                new Move { Name = "Whirlwind", Wind = 0.55f, Hits = 2, Gap = 0.45f, Mult = 0.9f, Weight = 1, Phase2Only = true, Poses = new[] { 4, 4 }, Open = 1.5f },
                new Move { Name = "Execution", Wind = 0.9f, Danger = true, Lunge = 7f, Mult = 1.8f, Weight = 2, Phase2Only = true, Poses = new[] { 5 }, Open = 2.2f },
            },
        };

        // Boss 4 (Yan, 2026-11-19: "put him in tier 2", "start building the boss similar to how the visor works"): the outcast Raider with the Barbed Wire Bat, in the Dog Kennels' fenced yard (SpoolBoss.cs).
        // The same fight as the Visor on a slower, heavier weapon: long wind-ups, big openings after each move, a two-handed bat that bleeds and concusses.
        public static readonly Def Spool = new Def
        {
            Name = "Spool", Title = "THE SCRAP KING'S EXILE", Weapon = "BarbedWireBat", Armor = "BarbwireCoat", Hp = 3600, Faction = "Outcasts", Rank = "Veteran", DoneFlag = "spool_done", Vanishes = false,
            StepSeconds = 0.4f, Recover = 1.0f, Phase2Line = "He tears the rag off the grip. The wire is bare now, and so is he.",
            Moves = new[]
            {
                new Move { Name = "Swing", Wind = 0.9f, Weight = 3, Mult = 0.9f, Poses = new[] { 1 }, Open = 1.4f },
                new Move { Name = "Backswing", Wind = 0.8f, Hits = 2, Gap = 0.55f, Mult = 0.6f, Weight = 2, Poses = new[] { 1, 2 }, Open = 1.7f },
                new Move { Name = "Overhead", Wind = 1.1f, Danger = true, Mult = 1.5f, Weight = 1, Poses = new[] { 0 }, Open = 2.3f },
                new Move { Name = "Wire rush", Wind = 0.9f, Danger = true, Lunge = 4f, Mult = 1.1f, Weight = 2, Phase2Only = true, Poses = new[] { 0 }, Open = 2.0f },
                new Move { Name = "Spin", Wind = 0.7f, Hits = 3, Gap = 0.45f, Mult = 0.5f, Weight = 2, Phase2Only = true, Poses = new[] { 1, 1, 2 }, Open = 1.8f },
            },
        };

        // Boss 5: the Scrap King himself (CrownJob.cs, "The Crown"): the Tier 2 finale. A sledgehammer, a crown of scrap, the Nest's yard.
        public static readonly Def King = new Def
        {
            Name = "The Scrap King", Title = "KING OF THE HEAP", Weapon = "Sledgehammer", Armor = "The Scrap King", Hp = 5000, Faction = "Raiders", Rank = "Expert", DoneFlag = "king_done", Vanishes = false,
            StepSeconds = 0.4f, Recover = 1.0f, Phase2Line = "He throws the crown down. \"Then you can have the whole yard.\"",
            Moves = new[]
            {
                new Move { Name = "Smash", Wind = 1.0f, Weight = 3, Mult = 0.8f, Poses = new[] { 0 }, Open = 1.5f },
                new Move { Name = "Sweep", Wind = 0.9f, Hits = 2, Gap = 0.6f, Mult = 0.55f, Weight = 2, Poses = new[] { 1, 2 }, Open = 1.8f },
                new Move { Name = "Crush", Wind = 1.2f, Danger = true, Mult = 1.4f, Weight = 1, Poses = new[] { 0 }, Open = 2.4f },
                new Move { Name = "Charge", Wind = 0.9f, Danger = true, Lunge = 5f, Mult = 1.1f, Weight = 2, Phase2Only = true, Poses = new[] { 0 }, Open = 2.0f },
                new Move { Name = "Rubble", Wind = 0.7f, Hits = 3, Gap = 0.45f, Mult = 0.5f, Weight = 2, Phase2Only = true, Poses = new[] { 1, 1, 2 }, Open = 1.9f },
            },
        };

        // Boss 6 (Yan, 2026-11-20: "he starts as a sprite swinging his scythe, similar to visor and other melee fights"; 2026-11-21: "i don't like it [the beast], he looks very ai generated and his attacks don't read well,
        // save it but take him out of the game for now and make the human version the entire boss, elaborate more on the human scythe-moveset"). VergerBoss.cs, BOSS_DESIGNS.md: the cathedral's last verger.
        // The whole fight is the man with the war scythe (the longest reach in the game, it cuts and bleeds), on his own pose set (HeldPoseMath.VergerPoses: reap, sweep, rising, hook, danse, ankle reap,
        // backhand, impale). He fights from the blade's tip (MaxDist 2: he starts swings from two tiles out). Phase 2 at half health: the hat comes off, he is quicker, and the long combos come out;
        // the last quarter adds the Last Rite. The beast (VergerBeast below) is parked: its art, clips, moves and code are kept and tested, nothing spawns it.
        public static readonly Def Verger = new Def
        {
            Name = "The Verger", Title = "THE CATHEDRAL'S LAST HUNTER", Weapon = "VergerScythe", Armor = "VergerCoat", Hp = 7000, Faction = "Outcasts", Rank = "Legend", DoneFlag = "verger_done", Vanishes = false, PoseKey = HeldPoseMath.VergerPoses,
            StepSeconds = 0.33f, Recover = 1.0f, Phase2Line = "He takes off the hat and sets it down on the flagstones. The mask stays on.", Phase2Barks = new[] { "Then I will not be gentle.", "The bell rings for you now." },
            Moves = VergerScytheMoves(),
        };
        /// <summary>2026-11-21: the scythe moveset (poses: 0 reap, 1 sweep, 2 rising, 3 hook, 4 danse, 5 ankle reap, 6 backhand, 7 impale). White = parry, red = dive under or get out of reach.</summary>
        static Move[] VergerScytheMoves() => new[]
        {
            // both phases: the measured hunter
            new Move { Name = "Reap", Wind = 1.0f, Weight = 3, Mult = 1.0f, MaxDist = 2f, Poses = new[] { 0 }, Open = 1.4f },                                                     // overhead, into the floor in front of him
            new Move { Name = "Sweep", Wind = 0.9f, Hits = 2, Gap = 0.55f, Mult = 0.65f, MaxDist = 2f, Weight = 2, Poses = new[] { 1, 6 }, Open = 1.7f },                         // round and back
            new Move { Name = "Rising Cut", Wind = 0.7f, Mult = 0.8f, MaxDist = 2f, Weight = 2, Poses = new[] { 2 }, Open = 1.1f },                                              // the quick one: short wind, short opening
            new Move { Name = "Hook", Wind = 1.0f, Danger = true, Lunge = 4f, Drag = 3.2f, Mult = 1.0f, Weight = 2, Poses = new[] { 3 }, Open = 1.2f },                           // from range: the beard catches you and he hauls you in
            new Move { Name = "Ankle Reap", Wind = 0.9f, Danger = true, Mult = 0.9f, MaxDist = 2f, Weight = 2, Poses = new[] { 5 }, Open = 1.6f },                               // low, at your feet: dive over it
            // phase 1 only
            new Move { Name = "Harvest", Wind = 0.8f, Hits = 3, Gaps = new[] { 0.45f, 0.6f }, Mults = new[] { 0.5f, 0.5f, 0.9f }, MaxDist = 2f, Weight = 2, Poses = new[] { 1, 6, 0 }, Open = 1.8f, Phase1Only = true },
            // phase 2: the hat comes off
            new Move { Name = "Danse", Wind = 0.8f, Hits = 2, Gap = 0.5f, Mult = 0.8f, MaxDist = 2f, Weight = 2, Poses = new[] { 4, 4 }, Open = 1.8f, Phase2Only = true },          // two full turns, all round him
            new Move { Name = "Toll", Wind = 1.0f, Danger = true, MaxDist = 6f, Effects = new[] { "toll" }, Weight = 1, Poses = new[] { 0 }, Open = 1.3f, Phase2Only = true },     // the blade struck on the flagstones rings like the bell: deafened and your aim gone (dive)
            new Move { Name = "Hook-Reap", Wind = 1.0f, Hits = 2, Lunge = 4f, Drag = 3.2f, Gaps = new[] { 0.7f }, Mults = new[] { 1.0f, 1.4f }, RedHits = new[] { true, false }, Weight = 2, Poses = new[] { 3, 0 }, Open = 2.0f, Phase2Only = true },   // hauled in, then the reap: parry the second
            new Move { Name = "Reaper's Rush", Wind = 1.0f, Danger = true, Lunge = 6f, Mult = 1.6f, Weight = 2, Poses = new[] { 7 }, Open = 2.2f, Phase2Only = true },             // across the nave and the point driven down
            new Move { Name = "Widow's Harvest", Wind = 0.7f, Hits = 5, Gaps = new[] { 0.4f, 0.4f, 0.5f, 0.6f }, Mults = new[] { 0.5f, 0.5f, 0.6f, 0.6f, 1.2f }, RedHits = new[] { false, false, true, false, false }, MaxDist = 2f, Weight = 2, Poses = new[] { 1, 6, 5, 2, 0 }, Open = 2.4f, Phase2Only = true },   // sweep, back, the feet (dive), up, down
            new Move { Name = "Last Rite", Wind = 0.9f, Hits = 4, Gaps = new[] { 0.5f, 0.45f, 0.7f }, Mults = new[] { 0.7f, 0.6f, 0.7f, 1.8f }, RedHits = new[] { false, false, true, true }, MaxDist = 2f, BelowHp = 0.25f, Weight = 3, Poses = new[] { 4, 6, 5, 7 }, Open = 2.8f, Phase2Only = true },   // the last quarter: danse, back, the feet, the impale
        };

        // ---- PARKED (Yan, 2026-11-21: "save it but take him out of the game for now"): the beast form. Nothing spawns this def; the harness still runs it so the code keeps working. ----
        // At half health he changed (Def.Change: hittable at a tenth of the damage for 4 s) into the werewolf brawler drawn from Pixel Lab clips (VergerBoss.Clips), with the claws' moves below.
        public static readonly Def VergerBeast = new Def
        {
            Name = "The Verger", Title = "THE CATHEDRAL'S LAST HUNTER", Weapon = "VergerScythe", Armor = "VergerCoat", Hp = 7000, Faction = "Outcasts", Rank = "Legend", DoneFlag = "verger_done", Vanishes = false,
            StepSeconds = 0.33f, Recover = 1.0f, Phase2Line = "The coat splits at the seams. What stands up is not a man.",
            Change = new Transform { Weapon = "VergerClaws", Seconds = 4f, Reduction = 0.9f, Splash = "THE CHANGE", Line = "The hat falls. The mask splits. The scythe drops from hands that are not hands.", Barks = new[] { "Forgive me.", "I kept the bell. I kept the dead out. And you rang it." } },
            Moves = new[]
            {
                new Move { Name = "Reap", Wind = 1.0f, Weight = 3, Mult = 1.0f, Poses = new[] { 0 }, Open = 1.4f, Phase1Only = true },
                new Move { Name = "Sweep", Wind = 0.9f, Hits = 2, Gap = 0.55f, Mult = 0.6f, Weight = 2, Poses = new[] { 1, 2 }, Open = 1.7f, Phase1Only = true },
                new Move { Name = "Hook", Wind = 1.0f, Danger = true, Lunge = 4f, Drag = 3.2f, Mult = 1.0f, Weight = 2, Poses = new[] { 0 }, Open = 1.2f, Phase1Only = true },
                new Move { Name = "Harvest", Wind = 0.8f, Hits = 3, Gap = 0.45f, Mult = 0.5f, Weight = 2, Poses = new[] { 1, 1, 2 }, Open = 1.8f, Phase1Only = true },
                new Move { Name = "Rake", Wind = 0.65f, Mult = 1.1f, Weight = 3, Poses = new[] { 1 }, Open = 1.3f, Phase2Only = true, Clip = "rake" },       // (one fast slash: the regenerated clip shows one forward blow)
                new Move { Name = "Smash", Wind = 1.2f, Danger = true, Mult = 1.9f, Weight = 2, Poses = new[] { 0 }, Open = 2.4f, Phase2Only = true, Clip = "slam" },
                new Move { Name = "Charge", Wind = 1.0f, Danger = true, Lunge = 7f, Mult = 1.5f, Weight = 2, Poses = new[] { 0 }, Open = 2.0f, Phase2Only = true, Clip = "charge" },
                new Move { Name = "Sweep", Wind = 0.9f, Mult = 1.2f, Weight = 2, Poses = new[] { 1 }, Open = 1.8f, Phase2Only = true, Clip = "sweep" },
                new Move { Name = "Howl", Wind = 1.1f, Danger = true, MaxDist = 9f, Effects = new[] { "howl" }, Weight = 2, Poses = new[] { 0 }, Open = 1.8f, Phase2Only = true, Clip = "howl" },
                new Move { Name = "Quake", Wind = 1.4f, Danger = true, Mult = 1.5f, Effects = new[] { "quake" }, Weight = 2, Poses = new[] { 0 }, Open = 2.6f, Phase2Only = true, Clip = "slam" },
                new Move { Name = "Trample", Wind = 1.0f, Hits = 2, Lunge = 7f, Gaps = new[] { 1.30f }, Mults = new[] { 1.3f, 1.8f }, RedHits = new[] { true, true }, Weight = 2, Poses = new[] { 0, 0 }, Open = 2.4f, Phase2Only = true, Clip = "trample" },
                new Move { Name = "Howl-Rake", Wind = 1.1f, Hits = 2, MaxDist = 1.5f, Effects = new[] { "howl", null }, Gaps = new[] { 0.99f }, Mults = new[] { 1f, 1.2f }, RedHits = new[] { true, false }, Weight = 2, Poses = new[] { 0, 1 }, Open = 2.0f, Phase2Only = true, Clip = "howlrake" },
                new Move { Name = "Rake-Sweep", Wind = 0.65f, Hits = 2, Gaps = new[] { 0.49f }, Mults = new[] { 0.9f, 1.0f }, Weight = 2, Poses = new[] { 1, 1 }, Open = 1.6f, Phase2Only = true, Clip = "rakesweep" },
                new Move { Name = "Rake-Smash", Wind = 0.65f, Hits = 2, Gaps = new[] { 1.21f }, Mults = new[] { 0.9f, 1.8f }, RedHits = new[] { false, true }, Weight = 2, Poses = new[] { 1, 0 }, Open = 2.3f, Phase2Only = true, Clip = "rakeslam" },
                new Move { Name = "Rampage", Wind = 0.65f, Hits = 3, Gaps = new[] { 0.49f, 1.59f }, Mults = new[] { 0.8f, 0.9f, 1.9f }, RedHits = new[] { false, false, true }, BelowHp = 0.25f, Weight = 3, Poses = new[] { 1, 1, 0 }, Open = 2.6f, Phase2Only = true, Clip = "rampage" },
            },
        };

        /// <summary>Put a boss on the map at (x, y) and start the fight.</summary>
        public static Fight Spawn(Def def, int x, int y)
        {
            var w = WeaponDatabase.Categories["melee"].TryGetValue(def.Weapon, out var wd) ? wd : null;
            var a = SuitOf(def);
            var h = new HumanEnemy(def.Name, x, y, def.Faction, def.Rank, w, a) { IsAggroed = true, MaxHp = def.Hp, Hp = def.Hp, Unkillable = true };
            EncounterState.Enemies.Add(h); EncounterState.InCombat = true;
            var f = new Fight { Def = def, Unit = h, Started = Time.time, NextMoveAt = Time.time + 1.2f };
            Fights.Add(f);
            Splash(def.Name.ToUpperInvariant(), def.Title);
            Sfx.Play("boss_sting", null, 1f, 0f);
            HitStop(0.4f);
            GameLog.Event($"{def.Name}: {def.Title}");
            return f;
        }

        // ---------------- every frame (Rt.WorldTick, instead of RtAi for this unit) ----------------

        public static void Tick(Fight f, Player p)
        {
            var h = f.Unit; float now = Time.time;
            // Visor 2: his wind-up is held through the glint, and the strike starts just before the hit lands
            var mc = Melee.ChargeOf(h); mc.PoseKey = f.Def.PoseKey; mc.Pace = f.Def.Pace;
            if (f.Doing == null) mc.SwingFrom = -1f;
            else if (mc.SwingFrom >= 0f && now >= f.StrikeAt - HeldPoseMath.ChopTime / mc.Pace) { float held = now - mc.SwingFrom; mc.SwingFrom = -1f; mc.SwungAt = now - Mathf.Min(held, HeldPoseMath.WindUp) / mc.Pace; }
            if (f.Doing != null && f.GlintDue >= 0f && now >= f.GlintDue) { f.GlintDue = -1f; Glint(f, Red(f.Doing, f.Doing.Hits - f.HitsLeft)); }
            if (!f.Broken && !f.Finished && h.Hp <= 1) OnDamaged(h);              // bled (or blown) down to the break
            if (f.Broken || f.Finished || !h.IsAlive() || p == null) return;
            if (now < f.StaggerUntil) return;
            if (now < f.ChangeUntil) return;                                                  // mid-change: he does nothing, and takes a tenth
            if (f.Phase == 1 && h.Hp <= f.Def.Hp / 2 && f.Def.Change != null) { BeginChange(f, now); return; }
            if (f.Phase == 1 && h.Hp <= f.Def.Hp / 2)
            {
                f.Phase = 2; f.Doing = null; f.NextMoveAt = now + 1.4f;
                Splash("PHASE 2", f.Def.Phase2Line);
                Sfx.Play("boss_sting", null, 0.8f, 0f, 1.25f);
                CombatFx.Push("aura", h, null, h.EquippedWeapon).Now = true;
                HitStop(0.3f);
                if (f.Def.Phase2Barks != null) for (int i = 0; i < f.Def.Phase2Barks.Length; i++) Banter.Say(h, f.Def.Phase2Barks[i], 1, 0.3f + i * 1.8f);
            }
            float speed = f.Beast ? 1.1f : f.Phase == 2 ? 0.8f : 1f;     // the Verger's beast is a slow brawler (1.1: his waits and openings are a tenth longer); every other boss's phase 2 is quicker
            int dist = CombatantUtil.Distance(h, p);

            if (f.Doing == null)
            {
                if (now < f.OpenUntil) return;                                  // Visor 5: getting his breath back - he doesn't move
                if (dist > 1 && now >= f.NextStep && !f.Def.Rooted)
                {
                    f.NextStep = now + f.Def.StepSeconds * speed;
                    h.MoveToward(p.X, p.Y);
                }
                if (now < f.NextMoveAt) return;
                var options = f.Def.Moves.Where(m => (!m.Phase2Only || f.Phase == 2) && (!m.Phase1Only || f.Phase == 1) && (m.BelowHp <= 0f || h.Hp <= f.Def.Hp * m.BelowHp) && (m.Lunge > 0f ? dist >= 2 && dist <= m.Lunge : dist <= m.MaxDist)).ToList();
                if (options.Count == 0) return;
                int roll = Random.Range(0, options.Sum(m => m.Weight));
                foreach (var m in options) { if (roll < m.Weight) { Begin(f, m, now, speed); break; } roll -= m.Weight; }
                return;
            }
            if (now < f.StrikeAt) return;
            Land(f, p);
            if (f.Doing == null) return;                                    // parried mid-combo: the stagger already cut it short
            if (--f.HitsLeft > 0) { f.StrikeAt = now + GapBefore(f.Doing, f.Doing.Hits - f.HitsLeft) * speed; f.GlintDue = Mathf.Max(now, f.StrikeAt - GlintLead); WindPose(f, now); return; }
            OpenUp(f, f.Doing.Open * speed, now);
            f.Doing = null; f.NextMoveAt = f.OpenUntil + 0.3f * Random.Range(0.8f, 1.3f);
        }

        /// <summary>2026-11-20: the half-health change (Def.Change): the scythe goes into his pack, the claws come out, the camera finds him, and for a few seconds he is hittable at a tenth of the damage.</summary>
        static void BeginChange(Fight f, float now)
        {
            var h = f.Unit; var c = f.Def.Change;
            f.Phase = 2; f.Beast = true; f.Doing = null; f.OpenUntil = -1f; f.ChangeUntil = now + c.Seconds; VergerBoss.StartClip(f, "roar", now + VergerBoss.Clips["roar"].PreHitSeconds); f.NextMoveAt = f.ChangeUntil + 0.6f;
            HumanLootSystem.StashWeaponNow(h, ItemCondition.Working);                           // the weapon he held drops into his pack: it is yours when he falls
            if (WeaponDatabase.Categories["melee"].TryGetValue(c.Weapon, out var wd)) h.EquipWeaponFromDb(wd);
            Splash(c.Splash ?? "THE CHANGE", c.Line ?? f.Def.Phase2Line);
            Sfx.Play("boss_sting", null, 0.8f, 0f, 0.7f);
            CombatFx.Push("aura", h, null, h.EquippedWeapon).Now = true;
            var (x, y) = Ballistics.Pos(h); Story.Cutscene(x, y, c.Seconds * 0.8f);
            HitStop(0.5f);
            if (c.Barks != null) for (int i = 0; i < c.Barks.Length; i++) Banter.Say(h, c.Barks[i], 1, 0.3f + i * 1.6f);
            GameLog.Event($"{f.Def.Name}: {c.Line ?? f.Def.Phase2Line}");
        }
        /// <summary>2026-11-20: the damage multiplier a boss takes right now (Melee.Strike and Gunplay.Resolve): a tenth while he changes.</summary>
        public static float DamageScale(ICombatant u) => Of(u) is Fight f && f.Def.Change != null && Time.time < f.ChangeUntil ? 1f - f.Def.Change.Reduction : 1f;

        /// <summary>Visor 5: the window after a move - he stands, knife low, breathing; bullets get through (Deflects).</summary>
        static void OpenUp(Fight f, float seconds, float now)
        {
            f.OpenUntil = now + seconds;
            var mc = Melee.ChargeOf(f.Unit); mc.OpenFrom = now + HeldPoseMath.Follow / mc.Pace; mc.OpenUntil = f.OpenUntil;   // the pose sinks after the follow-through
        }
        public static bool Open(ICombatant u) => Of(u) is Fight f && Time.time < f.OpenUntil && !f.Broken && !f.Finished;

        internal static void Begin(Fight f, Move m, float now, float speed)
        {
            f.Doing = m; f.HitsLeft = m.Hits; f.StrikeAt = now + m.Wind * speed;
            { Player t = null; int bd = int.MaxValue; foreach (var q in Player.All) { if (!q.IsAlive()) continue; int d = CombatantUtil.Distance(f.Unit, q); if (d < bd) { bd = d; t = q; } } if (t != null) { f.AimX = t.X; f.AimY = t.Y; } }
            f.GlintDue = Mathf.Max(now, f.StrikeAt - GlintLead); WindPose(f, now);
            if (m.Effects != null && m.Effects.Contains("drip")) OrderlyBoss.DripBegins(f);         // the Orderly: how hurt he is when he starts squeezing the bag
            if (m.Clip != null && !BossArt.Start(f, m.Clip, f.StrikeAt, speed)) VergerBoss.StartClip(f, m.Clip, f.StrikeAt, speed);                 // the beast's animation, timed so its hit frame lands on the strike
            if (m.Danger) Banter.Say(f.Unit, m.Name.ToUpperInvariant() + "!", 1);
        }

        /// <summary>Visor 2: the next hit's pose winds up (from wherever the last hit left the knife, in a combo).</summary>
        static void WindPose(Fight f, float now)
        {
            var mc = Melee.ChargeOf(f.Unit); var m = f.Doing; int i = m.Hits - f.HitsLeft;
            mc.From = i > 0 ? mc.Since(now) : -1f;
            mc.Move = m.Poses[i % m.Poses.Length]; mc.SwingFrom = now; mc.Red = Red(m, i);
        }

        static void Glint(Fight f, bool danger)
        {
            f.GlintAt = Time.time; f.GlintDanger = danger;
            var fx = CombatFx.Push("glint", f.Unit, null, f.Unit.EquippedWeapon); fx.Now = true; fx.Psi = danger;
        }

        /// <summary>The hit lands now: a lunge rushes in first; the swing takes whoever is in its arc (parry, block or blood).</summary>
        static void Land(Fight f, Player p)
        {
            var h = f.Unit; var m = f.Doing; int hi0 = m.Hits - f.HitsLeft;
            string fx = m.Effects != null && m.Effects.Length > 0 ? m.Effects[System.Math.Min(hi0, m.Effects.Length - 1)] : null;
            if (fx == "howl") { Howl(f, p, Red(m, hi0)); return; }                                  // 2026-11-21: a sound, not a swing: it reaches across the room
            if (fx == "quake") { Quake(f, p, m, hi0); return; }                                      // a ring round him
            if (fx == "toll") { Toll(f); return; }
            if (fx == "note") { f.StruckAt = Time.time; SingerBoss.Note(f, m, hi0); return; }          // 2026-11-21 the Singer
            if (fx == "grasp") { f.StruckAt = Time.time; SingerBoss.Grasp(f, p, m, hi0); return; }
            if (fx == "flare") { f.StruckAt = Time.time; FerrymanBoss.Flare(f); return; }               // 2026-11-21 the Ferryman
            if (fx == "hands") { f.StruckAt = Time.time; FerrymanBoss.Hands(f, m, hi0); return; }
            if (fx == "mind") { f.StruckAt = Time.time; SpecimenGrown.Mind(f); return; }                // 2026-11-21 the Specimen, grown
            if (fx == "digslam") { f.StruckAt = Time.time; ExcavatorBoss.DigSlam(f, m, hi0); return; }   // 2026-11-21 the Excavator
            if (fx == "rocks") { f.StruckAt = Time.time; ExcavatorBoss.Rocks(f, m, hi0); return; }
            if (fx == "slab") { f.StruckAt = Time.time; OrderlyBoss.Slab(f, p, m, hi0); return; }         // 2026-11-21 the Orderly
            if (fx == "drip") { OrderlyBoss.Drip(f); return; }
            if (fx == "brood") { f.StruckAt = Time.time; MotherHollow.Howl(f); return; }                // 2026-11-21 Mother Hollow: the den answers
            if (fx == "acid") { f.StruckAt = Time.time; MotherHollow.Acid(f, m, hi0); if (f.Doing != null && f.HitsLeft > 1) { var t2 = p; f.AimX = t2.X; f.AimY = t2.Y; } return; }   // a glob where you stood (the next one aims anew)                                                   // 2026-11-21 the Verger: the blade rung on the stone
            f.StruckAt = Time.time;
            if (!f.Def.Rooted) StepIn(h, p, m.Drag > 0f && hi0 == 0 ? HookGap : m.Lunge > 0f ? 0.8f : 0.9f);   // the hit comes with a step (a lunge: the whole rush; a hook: he stops at the blade's tip)
            var (hx, hy) = Ballistics.Pos(h); var (px, py) = Ballistics.Pos(p);
            float dx = px - hx, dy = py - hy, len = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dy * dy));
            var anim = CombatFx.Push("melee", h, p, h.EquippedWeapon); anim.Now = true;
            int hi = m.Hits - f.HitsLeft; bool red = Red(m, hi);
            if (red && Rt.Of(p).Diving) { GameLog.Write("DODGED!"); CombatFx.Push("dodge", p, h, null).Now = true; return; }   // the dive goes under it
            if (h.EquippedWeapon == null) return;
            Parry.Unparryable = red; int hp0 = p.Hp;
            try { Melee.Swing(h, h.EquippedWeapon, dx / len, dy / len, 0.45f, MultOf(m, hi)); }
            finally { Parry.Unparryable = false; }
            if (VergerBoss.IsBeast(h)) { var sf = CombatFx.Push("beastslash", h, p, null); sf.Now = true; sf.Hit = p.Hp < hp0; if (sf.Hit) HitStop(0.05f); }      // 2026-11-21: the claws' slash is drawn, and a landed blow has weight
            if (m.Drag > 0f && hi == 0) { bool caught = p.Hp < hp0; var cf = CombatFx.Push("hookchain", h, p, null); cf.Now = true; cf.Hit = caught; if (caught) DragIn(h, p, m.Drag); }   // the chain: thrown, and if it cuts, hauled
        }

        public const float TollRange = 6f;
        /// <summary>2026-11-21 (the Verger's phase 2): he strikes the blade on the flagstones and it rings like the cathedral bell: every player within TollRange who is not under it (a dive) is deafened for 1.5 s and has their aim blown. No damage: the setup for the Hook.</summary>
        static void Toll(Fight f)
        {
            var h = f.Unit; f.StruckAt = Time.time; CombatFx.PxAt("thunderclap", h.X, h.Y); Sfx.Play("boss_sting", TacticalGridView.GridToWorld(h.X, h.Y), 0.8f, 0.02f, 1.6f); CombatFx.Push("stagger", h, null, null).Now = true;
            int n = 0;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                if (CombatantUtil.Distance(h, q) > TollRange || Rt.Untouchable(q) || Rt.Of(q).Diving) continue;
                var aim = Gunplay.AimOf(q); aim.Steady = 0f; aim.Bloom = Gunplay.PlayerMaxBloom; Body.Concuss(q, 1.5f); n++;
            }
            GameLog.Write(n > 0 ? "The scythe rings on the stone like the bell. Your ears give out." : "The scythe rings on the stone. You were under it.");
        }

        public const float HowlRange = 9f, HowlCone = 100f, QuakeRadius = 4.2f;
        /// <summary>2026-11-21 (the Verger's beast, "he needs more moves"): the Howl: in a wide cone in front of him everyone not under it (a dive) has their aim blown, is deafened for two seconds and knocked down for a second (a Tumbler gets half): the setup for what follows. Like the Gaunt's scream.</summary>
        static void Howl(Fight f, Player target, bool red)
        {
            var h = f.Unit; CombatFx.PxAt("thunderclap", h.X, h.Y); Sfx.Play("gaunt_scream", TacticalGridView.GridToWorld(h.X, h.Y), 1f, 0.03f, 0.55f); CombatFx.Push("howlwave", h, target, null).Now = true;
            float dx = target.X - h.X, dy = target.Y - h.Y, l = Mathf.Max(0.01f, Mathf.Sqrt(dx * dx + dy * dy)); dx /= l; dy /= l; int n = 0;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                float px = q.X - h.X, py = q.Y - h.Y, d = Mathf.Sqrt(px * px + py * py);
                if (d > HowlRange || d < 0.01f || (px * dx + py * dy) / d < Mathf.Cos(HowlCone * 0.5f * Mathf.Deg2Rad) || Rt.Untouchable(q)) continue;
                var aim = Gunplay.AimOf(q); aim.Steady = 0f; aim.Bloom = Gunplay.PlayerMaxBloom; Body.Concuss(q, 2f);
                Rt.Knockdown(q, CombatantUtil.HasPerk(q, "Tumbler") ? 0.5f : 1.0f); n++;
                GameLog.Write("The howl hits you like a wall: your ears give out and you go down.");
            }
            if (n == 0) GameLog.Write("The howl goes over you.");
        }
        /// <summary>2026-11-21: the Quake: the slam shakes the floor in a ring (QuakeRadius tiles) round him: every player in it takes the blow (a dive goes under) and is knocked down.</summary>
        static void Quake(Fight f, Player target, Move m, int hi)
        {
            var h = f.Unit; f.StruckAt = Time.time; CombatFx.PxAt("thunderclap", h.X, h.Y); Sfx.Play("boss_sting", null, 0.6f, 0f, 0.5f); CombatFx.Push("stagger", h, null, null).Now = true; HitStop(0.06f);
            if (h.EquippedWeapon == null) return;
            foreach (var q in Player.All.Where(q => q.IsAlive()))
            {
                if (CombatantUtil.Distance(h, q) > QuakeRadius) continue;
                if (Rt.Of(q).Diving || Rt.Untouchable(q)) { GameLog.Write("DODGED!"); CombatFx.Push("dodge", q, h, null).Now = true; continue; }
                Parry.Unparryable = true; try { Melee.HitUnit(h, h.EquippedWeapon, q, MultOf(m, hi)); } finally { Parry.Unparryable = false; }
                Rt.Knockdown(q, 0.6f);
            }
        }

        public const float HookGap = 2.0f, DragStop = 0.9f;
        /// <summary>2026-11-20 (the Verger's Hook): the blade's beard catches you and hauls you in: a slide toward him (Rt.Shove: eased, walls and friends stop it) to just in front of him, and a moment on the floor.</summary>
        public static void DragTo(HumanEnemy h, Player p, float maxTiles) => DragIn(h, p, maxTiles);
        internal static void DragIn(HumanEnemy h, Player p, float maxTiles)
        {
            var (hx, hy) = Ballistics.Pos(h); var (px, py) = Ballistics.Pos(p);
            float dx = hx - px, dy = hy - py, d = Mathf.Sqrt(dx * dx + dy * dy); float pull = Mathf.Min(maxTiles, d - DragStop);
            if (pull < 0.15f || d < 0.01f) return;
            Rt.Shove(p, dx / d * pull, dy / d * pull, 0.3f); Rt.Knockdown(p, 0.5f);
            GameLog.Write("The hook catches you and drags you in.");   // (the chain's sounds and its drawing are the view's: CombatFxView "hookchain")
        }

        /// <summary>Close in to `gap` tiles from p before a hit lands - you stand anywhere inside a tile, so "the next tile" can still
        /// be out of a knife's reach (Melee.Stats.Reach 1.3). Only toward you, never through a wall. Rt.Glide turns it into a slide.</summary>
        public static void StepIn(HumanEnemy h, Player p, float gap)
        {
            var (hx, hy) = Ballistics.Pos(h); var (px, py) = Ballistics.Pos(p);
            float dx = hx - px, dy = hy - py, d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d <= gap + 0.05f || d < 0.01f) return;
            float tx = px + dx / d * gap, ty = py + dy / d * gap;
            int x = Mathf.FloorToInt(tx), y = Mathf.FloorToInt(ty);
            if (!(x == p.X && y == p.Y) && TerrainMap.CanEnterTile(x, y)) { h.X = x; h.Y = y; Rt.SetOffset(h, tx - x - 0.5f, ty - y - 0.5f); return; }
            int sx = System.Math.Sign(h.X - p.X), sy = System.Math.Sign(h.Y - p.Y);   // the spot is in your tile: the edge of the next one
            var (ax, ay) = TerrainMap.FindNearestEnterable(p.X + sx, p.Y + sy);
            if (CombatantUtil.Distance(p, ax, ay) <= 1)
            { h.X = ax; h.Y = ay; Rt.SetOffset(h, Mathf.Clamp(tx - ax - 0.5f, -0.45f, 0.45f), Mathf.Clamp(ty - ay - 0.5f, -0.45f, 0.45f)); }
        }

        // ---------------- what the rest of the game asks ----------------

        /// <summary>A perfect parry (or anything else that staggers): open for `seconds`. A plain enemy just loses that long.</summary>
        /// <summary>A boss needs Def.ParriesToStagger perfect parries (within GuardRecovers s of each other) before he's open; the ones
        /// before it are clean deflections that don't break his combo. True if this one staggered him.</summary>
        public static bool Stagger(ICombatant u, float seconds)
        {
            var f = Of(u);
            if (f == null)
            {
                if (u is HumanEnemy he) RtAi.Hold(he, seconds);
                else if (u is Mutant mu) { MutantAttack.Interrupt(mu); RtAi.Pin(mu, seconds); }   // a parried bite, claw or slam: the strike is cut short and it reels for a second
                return true;
            }
            if (f.Broken) return false;
            if (Time.time - f.LastParryAt > GuardRecovers) f.Parries = 0;
            f.LastParryAt = Time.time;
            if (++f.Parries < f.Def.ParriesToStagger) return false;
            f.Parries = 0;
            f.Clip = null; f.StaggerUntil = Time.time + seconds; f.Doing = null; f.NextMoveAt = f.StaggerUntil + 0.3f; f.OpenUntil = -1f;
            CombatFx.Push("stagger", u, null, null).Now = true;
            return true;
        }
        /// <summary>For the HUD: how many perfect parries toward the next stagger (0 once they've faded).</summary>
        public static int GuardDamage(Fight f) => Time.time - f.LastParryAt > GuardRecovers ? 0 : f.Parries;

        public static bool Staggered(ICombatant u) => Of(u) is Fight f && Time.time < f.StaggerUntil;

        /// <summary>Rounds glance off him unless he's staggered (or already broken).</summary>
        public static bool Deflects(ICombatant u) => Of(u) is Fight f && !f.Broken && !f.Finished && Time.time >= f.StaggerUntil && Time.time >= f.OpenUntil && Time.time >= f.ChangeUntil;      // (mid-change rounds land, at a tenth)

        /// <summary>After any damage (Gunplay.Resolve, Melee.Strike): a boss at 0 doesn't die, he breaks.</summary>
        public static void OnDamaged(ICombatant u)
        {
            GreyRobes.OnDamaged(u);                                        // Tier 1: "those" never die: the smoke goes up
            BrainBoss.OnDamaged(u);                                        // Boss 2: cracks, and no death at 0
            EelBoss.OnDamaged(u);                                          // Boss 3: wounds at 75/50/25%, the vomit at 0
            var f = Of(u);
            if (f != null && !f.Finished && !f.Broken && u.Hp > 1) BossArt.Hurt(f);       // 2026-11-21: a creature boss flinches (its hurt clip)
            if (f == null || f.Finished || f.Broken || u.Hp > 1) return;   // Unkillable holds him at 1: that's the break
            f.Broken = true; f.Doing = null;
            Splash("EXECUTE", "E - next to him");
            CombatFx.Push("stagger", u, null, null).Now = true;
            HitStop(0.35f);
            GameLog.Event($"{f.Def.Name} drops to one knee. E next to him: execute.");
        }

        public static Fight BrokenNear(Player p) => Fights.FirstOrDefault(f => f.Broken && !f.Finished && CombatantUtil.Distance(p, f.Unit) <= 2);

        /// <summary>E next to a broken boss: the finisher.</summary>
        public static bool TryFinish(Player p)
        {
            var f = BrokenNear(p);
            if (f == null) return false;
            if (f.Def == Spool && Story.Step("spool") == 1) { SpoolBoss.Offer(p, f); return true; }   // the Scrap King's job: finish him, or hear him out
            return FinishNow(p, f);
        }

        /// <summary>The finisher itself (slow motion, the camera on you both): `TryFinish` calls it, or a choice made in a dialogue.</summary>
        internal static bool FinishNow(Player p, Fight f)
        {
            f.Finished = true;
            var (x, y) = Ballistics.Pos(f.Unit);
            Story.Cutscene(x, y, 1.6f);
            HitStop(0.45f);
            var fx = CombatFx.Push("finisher", p, f.Unit, p.EquippedWeapon); fx.Now = true; fx.Hit = true; fx.Killed = true;
            if (f.Def.Change != null) HumanLootSystem.StashArmorOnly(f.Unit, ItemCondition.Working);   // the Verger: his coat (the scythe went into his pack at the change)
            else if (f.Def == Spool) HumanLootSystem.StashWornGear(f.Unit, ItemCondition.Working);   // before the death hook rolls his loot: the bat and the coat, in good order
            if (f.Def.Creature) { f.Unit.EquippedWeapon = null; f.Unit.EquippedArmor = null; f.Unit.Inventory.Items.Clear(); }   // 2026-11-21: a creature has no gun or coat to drop: its spoils come from Def.Defeated
            f.Beast = false;                                                        // the Verger: the beast shrinks back into the man (his corpse is the man in the torn coat)
            f.Unit.Unkillable = false; f.Unit.Hp = 0;
            Weapon.OnTargetKilled(p, f.Unit);
            Splash(f.Def.Name.ToUpperInvariant(), "DEFEATED");
            Rt.AddFocus(p, Rt.FocusMax);                                           // Yan: "executing gives slow-mo energy" - Focus full
            GameLog.Event($"You execute {f.Def.Name}. Your Focus surges.");
            if (f.Def.DoneFlag != null) Story.Set(f.Def.DoneFlag);
            if (f.Def == Spool) SpoolBoss.Defeated(p, f.Unit);
            if (f.Def == Verger || f.Def == VergerBeast) VergerBoss.Defeated(p, f.Unit);
            f.Def.Defeated?.Invoke(p, f.Unit);
            return true;
        }

        // ---------------- the Visor's lab: Secret Lab Beta's secret wing (ArtSource/plans/lab_plans.py visor_wing) ----------------
        // ACTION_DESIGN "Melee": "found in a secret room of a Tier 4-5 lab: break a wall, walk a long corridor; it's stuck in a
        // corpse in black armour with a red visor"; Yan: "the ... corpse gets up when you take the hatchet and fights you in melee".

        public const string VisorLab = "secret_lab_beta", VisorMark = "The Black Visor", CrackedWall = "Cracked Wall";
        static readonly HashSet<HumanEnemy> Sleepers = new HashSet<HumanEnemy>();

        /// <summary>A map was entered: the body lies in its room until the fight is won.</summary>
        public static void OnEnterZone(string zoneId)
        {
            Sleepers.Clear();
            if (zoneId != VisorLab || Story.Has("visor_done")) return;
            var l = TerrainMap.Landmarks?.FirstOrDefault(x => x.Name == VisorMark);
            if (l == null) return;
            var body = new HumanEnemy("Body in black armour", l.X, l.Y, "Contractors", "Legend", null, BlackVisorSuit) { Hp = 0 };
            EncounterState.Enemies.Add(body); Sleepers.Add(body);
        }

        /// <summary>E at the cracked wall: it gives - behind it, a corridor nobody was meant to find.</summary>
        public static bool TryBreakWall(Player p)
        {
            var l = TerrainMap.Landmarks?.FirstOrDefault(x => x.Name == CrackedWall && CombatantUtil.Distance(p, x.X, x.Y) <= 1);
            if (l == null || TerrainMap.GetTile(l.X, l.Y).Type != TileType.Solid) return false;
            TerrainMap.SetTileType(l.X, l.Y, TileType.Difficult, "rubble");
            Sfx.Play("door_break", TacticalGridView.GridToWorld(l.X, l.Y));
            Noise.Make(l.X, l.Y, 10, p);
            GameLog.Event("The plaster was hollow. It caves in - behind it, a long dark corridor that isn't on any plan.");
            return true;
        }

        /// <summary>E on the body (before it's looted like any other): you pull the hatchet out, and he gets up.</summary>
        public static bool TryWake(Player p, HumanEnemy corpse)
        {
            if (corpse == null || !Sleepers.Remove(corpse)) return false;
            EncounterState.Enemies.Remove(corpse);
            WakeVisor(p, corpse.X, corpse.Y);
            return true;
        }

        /// <summary>The hatchet comes free in your hand; the visor lights up red; he rises (the Testing Ground calls this too).</summary>
        public static Fight WakeVisor(Player p, int x, int y)
        {
            var d = ShopDatabase.Items.FirstOrDefault(i => i.Type == "weapon" && i.Category == "melee" && i.Key == "TacticalHatchet");
            if (d != null) { p.AddItem(TestingGround.ItemOf(d)); p.EquipWeaponFromInventory(d.Id); }
            GameLog.Event("You work the hatchet loose from the black armour. It comes free with a wet sound.");
            GameLog.Event("Behind the visor, a red light comes on.");
            Story.Cutscene(x + 0.5f, y + 0.5f, 2.4f);
            var f = Spawn(Visor, x, y);
            f.NextMoveAt = Time.time + 2.6f;                                       // he gets up slowly. Then he doesn't do anything slowly.
            return f;
        }

        /// <summary>His body was opened for looting (CorpseLootUI): the clock to the smoke grenade starts.</summary>
        public static void Looted(HumanEnemy body) { var f = Of(body); if (f != null && f.Finished && f.LootedAt < 0f) f.LootedAt = Time.time; }

        public const float SmokeAfter = 3f;

        /// <summary>Every frame: the hit-stop wears off; a looted boss's smoke grenade.</summary>
        public static void TickAll()
        {
            if (_stopUntil >= 0f && Time.unscaledTime >= _stopUntil)
            {
                _stopUntil = -1f;
                if (!Rt.Paused) Time.timeScale = Time.unscaledTime < _slowUntil ? Mathf.Min(MomentScale, Rt.SlowMo ? Rt.SlowScale : 1f) : Rt.SlowMo ? Rt.SlowScale : 1f;
            }
            else if (_stopUntil < 0f && _slowUntil >= 0f && Time.unscaledTime >= _slowUntil)
            {
                _slowUntil = -1f;
                if (!Rt.Paused) Time.timeScale = Rt.SlowMo ? Rt.SlowScale : 1f;
            }
            foreach (var f in Fights.Where(f => f.Finished && !f.Gone && f.Def.Vanishes && f.LootedAt >= 0f && Time.time - f.LootedAt >= SmokeAfter).ToList())
            {
                f.Gone = true;
                CombatFx.Push("smoke", f.Unit, null, null).Now = true;
                EncounterState.Enemies.Remove(f.Unit);
                GameLog.Event("A smoke grenade hisses out of nowhere. When it clears, the body is gone.");
            }
        }

        // ---------------- hit-stop and the perfect parry's moment ----------------

        static float _stopUntil = -1f, _slowUntil = -1f, _momentAt = -99f, _holdUntil = -1f;
        public const float MomentStop = 0.14f, MomentSlow = 1.0f, MomentScale = 0.25f;

        /// <summary>A moment of near-frozen time (real seconds): the weight on a parry, a phase change, the finisher.</summary>
        public static float StopLeft => _stopUntil < 0f ? 0f : Mathf.Max(0f, _stopUntil - Time.unscaledTime);      // co-op: what is left of the hit-stop, for the friends
        public static void HitStopPublic(float seconds) => HitStop(seconds);
        public static void HitStop(float seconds)
        {
            if (Rt.Paused) return;
            Time.timeScale = 0.05f;
            _stopUntil = Mathf.Max(_stopUntil, Time.unscaledTime + seconds);
        }

        /// <summary>A perfect parry: a hit-stop, then a second of slow motion while the camera leans in and a cold edge rings
        /// the screen (Punch drives the zoom and HudView's vignette).</summary>
        public static void ParryMoment()
        {
            HitStop(MomentStop);
            _momentAt = Time.unscaledTime;
            _slowUntil = Time.unscaledTime + MomentStop + MomentSlow;
        }

        /// <summary>Boss 3 (the eel's jaw-hold): a hit-stop, then slow motion held for `seconds` with the camera leaning in, until Release.</summary>
        public static void Hold(float seconds)
        {
            HitStop(0.12f); _momentAt = Time.unscaledTime; _slowUntil = Time.unscaledTime + 0.12f + seconds; _holdUntil = _slowUntil;
        }
        public static void Release()
        {
            _holdUntil = -1f; _slowUntil = -1f; _momentAt = -99f;
            if (_stopUntil < 0f && !Rt.Paused) Time.timeScale = Rt.SlowMo ? Rt.SlowScale : 1f;
        }

        /// <summary>0..1: how far into the parry moment's lean-in we are (fast in, held, eased out).</summary>
        public static float Punch
        {
            get
            {
                if (Time.unscaledTime < _holdUntil) return Mathf.Clamp01((Time.unscaledTime - _momentAt) / 0.12f);   // the held scene: leaned in the whole time
                float t = Time.unscaledTime - _momentAt, len = MomentStop + MomentSlow;
                if (t < 0f || t > len) return 0f;
                return Mathf.Clamp01(t / 0.08f) * Mathf.Clamp01((len - t) / 0.45f);
            }
        }
    }
}
