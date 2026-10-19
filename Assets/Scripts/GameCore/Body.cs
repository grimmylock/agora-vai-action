// Real-time fork, RT batch 2 (ACTION_DESIGN.md "Health, wounds, death"): limb health, Tarkov-lite, on top of the one HP pool.
//  - Humans (you and human enemies) have six parts: head, torso, both arms, both legs, each with its own health
//    (a share of max HP). A round's damage comes off the part it hit AND the HP pool (which the rest of the game reads).
//  - Head or torso at 0 = down. An arm or leg at 0 is broken; more damage to it spills into the torso.
//  - Wounds (Yan's rule): a hit that leaves a part under 60% may wound it - the chance rises as the part weakens,
//    (60 - part%) / 60 * 0.75 (50% ~13%, 40% 25%, 20% 50%); hollow points raise it, AP rounds lower it.
//    Head: concussion (blur and sway for 10 s, Yan). Torso: bleeding. Arm/leg: a fracture or bleeding.
//  - Debuffs only from wounds and broken parts: a broken leg limps (no sprint), a broken arm sways and reloads slowly.
//  - You, downed (Yan): at 0 you drop and bleed out - lying still slowly, crawling (to cover) faster. A stim gets you up.
//    Enemies stop shooting someone who is down; a human enemy at 0 is dead (ponytail: NPC downed states later).
// Mutants keep the one pool (their head still takes x1.6 in Ballistics.PartMult).
// ponytail: part health isn't saved; a load starts every part full (the HP pool is saved as before).
using System.Collections.Generic;
using System.Linq;

namespace GameCore
{
    public static class Body
    {
        public static readonly string[] Parts = { "Head", "Torso", "ArmL", "ArmR", "LegL", "LegR" };
        static readonly Dictionary<string, float> Share = new Dictionary<string, float>
            { ["Head"] = 0.35f, ["Torso"] = 0.8f, ["ArmL"] = 0.45f, ["ArmR"] = 0.45f, ["LegL"] = 0.5f, ["LegR"] = 0.5f };

        public class State
        {
            public readonly Dictionary<string, float> Hp = new Dictionary<string, float>();
            public readonly HashSet<string> Broken = new HashSet<string>(), Fractured = new HashSet<string>();
            public float ConcussedUntil = -1f;
            public bool Down;
            public float BleedOut;                  // seconds left once down
        }
        static readonly Dictionary<ICombatant, State> States = new Dictionary<ICombatant, State>();
        public static void Reset() => States.Clear();

        public static float Now = -1f;              // the harness sets it; the game uses Time.time
        static float T => Now >= 0f ? Now : UnityEngine.Time.time;

        public static bool Human(ICombatant u) => u is Player || u is HumanEnemy;
        public static int MaxHp(ICombatant u) => u is Player p ? p.MaxHp : u is Mutant m ? System.Math.Max(1, m.MaxHp) : 100;
        public static float PartMax(ICombatant u, string part) => MaxHp(u) * Share[part];

        public static State Of(ICombatant u)
        {
            if (!States.TryGetValue(u, out var s))
            {
                States[u] = s = new State();
                foreach (var p in Parts) s.Hp[p] = PartMax(u, p);
            }
            return s;
        }

        public static float Pct(ICombatant u, string part) => Of(u).Hp[part] / PartMax(u, part);
        public static bool Down(ICombatant u) => u != null && States.TryGetValue(u, out var s) && s.Down;
        public static bool LegHurt(ICombatant u) => u != null && States.TryGetValue(u, out var s) && (s.Fractured.Contains("LegL") || s.Fractured.Contains("LegR"));
        public static int ArmsHurt(ICombatant u) => u != null && States.TryGetValue(u, out var s) ? (s.Fractured.Contains("ArmL") ? 1 : 0) + (s.Fractured.Contains("ArmR") ? 1 : 0) : 0;
        public static bool Concussed(ICombatant u) => u != null && States.TryGetValue(u, out var s) && T < s.ConcussedUntil;

        /// <summary>A round landed. part: Ballistics' "Head" | "Torso" | "Arm" | "Legs"; side: + = the shooter's right.
        /// `dmg` has already come off the HP pool. Returns the part name hit (ArmL, LegR...).</summary>
        public static string Hit(ICombatant u, string part, float side, int dmg, string ammo, float woundMult = 1f)
        {
            if (!Human(u) || dmg <= 0) return part;
            var s = Of(u);
            string p = part == "Arm" ? (side >= 0 ? "ArmR" : "ArmL") : part == "Legs" ? (side >= 0 ? "LegR" : "LegL") : part == "Head" ? "Head" : "Torso";
            float left = s.Hp[p] - dmg;
            if (left <= 0f && (p.StartsWith("Arm") || p.StartsWith("Leg")))
            {
                if (s.Broken.Add(p)) GameLog.Write($"{Who(u)} {(u is Player ? "have" : "has")} a broken {Name(p)}!");
                s.Fractured.Add(p);
                s.Hp["Torso"] = System.Math.Max(0f, s.Hp["Torso"] + left * 0.7f);   // the rest spills into the torso
                left = 0f;
            }
            s.Hp[p] = System.Math.Max(0f, left);
            if (s.Hp["Head"] <= 0f || s.Hp["Torso"] <= 0f) { Drop(u, s); return p; }
            if (!s.Broken.Contains(p)) Wound(u, s, p, ammo, woundMult);
            Sync(u, s);
            return p;
        }

        /// <summary>Yan's wound rule: nothing above 60%; below it the chance climbs as the part weakens.</summary>
        public static float WoundChance(float pct, string ammo)
        {
            if (pct >= 0.6f) return 0f;
            float c = (0.6f - pct) / 0.6f * 0.75f;
            if (ammo != null && ammo.Contains("HollowPoint")) c *= 1.4f;
            if (ammo != null && ammo.Contains("ArmorPiercing")) c *= 0.6f;
            return System.Math.Min(1f, c);
        }

        static void Wound(ICombatant u, State s, string p, string ammo, float mult)
        {
            if (UnityEngine.Random.value >= WoundChance(Pct(u, p), ammo) * mult) return;
            if (p == "Head") { s.ConcussedUntil = T + 10f; GameLog.Write($"{Who(u)} {(u is Player ? "are" : "is")} concussed!"); return; }
            bool limb = p != "Torso";
            if (limb && UnityEngine.Random.value < 0.5f) { if (s.Fractured.Add(p)) GameLog.Write($"{Who(u)}: fractured {Name(p)}."); }
            else if (!u.StatusEffects.Bleeding) { u.StatusEffects.Bleeding = true; GameLog.Write($"{Who(u)} {(u is Player ? "are" : "is")} bleeding."); }
        }

        /// <summary>Old systems read StatusEffects.Wound (NPC move and aim penalties, the HUD chip): keep it in step.</summary>
        static void Sync(ICombatant u, State s)
        {
            if (u is Player) return;                            // you: Rt reads Body directly
            u.StatusEffects.Wound = s.Fractured.Any(f => f.StartsWith("Leg")) ? "Leg" : s.Fractured.Any(f => f.StartsWith("Arm")) ? "Arm" : null;
        }

        /// <summary>Head or torso gone, or the HP pool empty: you go down, anyone else dies.</summary>
        static void Drop(ICombatant u, State s)
        {
            if (u is Player p) { GoDown(p); return; }
            u.Hp = 0;
        }

        public const float BleedOutSeconds = 40f, CrawlDrain = 2.5f;

        /// <summary>You go down instead of dying (Player.TakeDamage calls this on lethal damage while Rt is on).</summary>
        public static void GoDown(Player p)
        {
            var s = Of(p);
            if (s.Down) return;
            s.Down = true; s.BleedOut = BleedOutSeconds;
            p.StatusEffects.Bleeding = false;                   // the bleed-out timer is the bleeding now
            p.Sneaking = true;
            GameLog.Write("[DOWN] You're down and bleeding out. Crawl to cover, use a stim to get up.");
            Sfx.Play("hurt");
        }

        /// <summary>Per frame while down: the clock runs out faster while crawling. True = bled out.</summary>
        public static bool TickDown(Player p, bool crawling, float dt)
        {
            var s = Of(p);
            if (!s.Down) return false;
            s.BleedOut -= dt * (crawling ? CrawlDrain : 1f);
            return s.BleedOut <= 0f;
        }

        /// <summary>Back on your feet (a stim, or a partner later): head and torso get a little back.</summary>
        public static void GetUp(Player p)
        {
            var s = Of(p);
            if (!s.Down) return;
            s.Down = false;
            foreach (var part in new[] { "Head", "Torso" }) s.Hp[part] = System.Math.Max(s.Hp[part], PartMax(p, part) * 0.25f);
            p.Sneaking = false;
            GameLog.Write("You drag yourself back up.");
        }

        /// <summary>Healing reaches the parts too (medkits, resting): spread by share; fractures need a splint.</summary>
        public static void Heal(ICombatant u, int amount)
        {
            if (!States.TryGetValue(u, out var s) || amount <= 0) return;
            foreach (var p in Parts)
            {
                float max = PartMax(u, p);
                s.Hp[p] = System.Math.Min(max, s.Hp[p] + amount * Share[p]);
                if (s.Hp[p] > 0f) s.Broken.Remove(p);
            }
        }

        /// <summary>A splint: the worst fracture (legs first).</summary>
        public static bool Splint(ICombatant u)
        {
            if (!States.TryGetValue(u, out var s)) return false;
            var f = new[] { "LegL", "LegR", "ArmL", "ArmR" }.FirstOrDefault(s.Fractured.Contains);
            if (f == null) return false;
            s.Fractured.Remove(f); s.Broken.Remove(f);
            s.Hp[f] = System.Math.Max(s.Hp[f], PartMax(u, f) * 0.3f);
            GameLog.Write($"You splint your {Name(f)}.");
            Sync(u, s);
            return true;
        }

        public static string Name(string p) => p switch { "ArmL" => "left arm", "ArmR" => "right arm", "LegL" => "left leg", "LegR" => "right leg", _ => p.ToLowerInvariant() };
        static string Who(ICombatant u) => u is Player ? "You" : u.Name;

        /// <summary>The HUD line: each part's health in %, broken parts marked.</summary>
        public static string Summary(ICombatant u)
        {
            var s = Of(u);
            string P(string part, string tag) => s.Fractured.Contains(part) ? $"<color=#c8443a>{tag}X</color>" : $"{tag}{UnityEngine.Mathf.RoundToInt(100f * s.Hp[part] / PartMax(u, part))}";
            return $"{P("Head", "H")} {P("Torso", "T")} {P("ArmL", "A")}/{P("ArmR", "")} {P("LegL", "L")}/{P("LegR", "")}";
        }
    }
}
