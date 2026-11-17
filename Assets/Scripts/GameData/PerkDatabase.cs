// Perks, straight from file 05. One at character creation, more at Professional, Expert and
// Legend (file 00 §13). Group is only used to lay the picker out.
using System.Collections.Generic;

namespace GameData
{
    public class PerkDef
    {
        public string Name;
        public string Group;
        public string Description;
        public string Requires;   // Batch 80: a perk key you must already have
    }

    public static class PerkDatabase
    {
        private static PerkDef P(string group, string name, string desc) => new PerkDef { Group = group, Name = name, Description = desc };

        public static readonly Dictionary<string, PerkDef> All = new Dictionary<string, PerkDef>
        {
            ["MarksmansFocus"] = P("Sniper", "Marksman's Focus", "Sniper rifles settle 20% steadier."),
            ["HighVelocityImpact"] = P("Sniper", "High-Velocity Impact", "Sniper shots at 12+ tiles ignore an extra 20% of Phys-DR."),
            ["RecoilControl"] = P("Assault", "Recoil Control", "Assault rifles kick 30% less."),
            ["BulletHose"] = P("SMG", "Bullet Hose", "SMGs kick 30% less."),
            ["Entrenched"] = P("Heavy", "Entrenched", "+20% Phys-DR on turns you didn't move."),
            ["Shredder"] = P("Heavy", "Shredder", "Machine guns are steadier (-25% spread, -15% kick)."),
            ["Breacher"] = P("Shotgun", "Breacher", "+25% shotgun damage within 4 tiles."),
            ["StaggeringBlast"] = P("Shotgun", "Staggering Blast", "Shotgun hits within 4 tiles stagger the target for a second."),
            ["Gunslinger"] = new PerkDef { Group = "Sidearm", Name = "Gunslinger", Requires = "PointBlankExecution", Description = "Needs Point-Blank Execution. Two sidearms (Off-hand button in the pack): single shots fire both, 2 AP, -1 each; reload both for +1 AP. Not with Double Tap." },   // Batch 80 (Yan)
            ["PointBlankExecution"] = P("Sidearm", "Point-Blank Execution", "Sidearms fired from the hip spread 25% less."),
            ["CombatMedic"] = P("Survival", "Combat Medic", "Medkits and stims heal +10 HP."),
            ["QuickHands"] = P("Survival", "Quick Hands", "Reload 40% faster."),
            ["AdrenalineSurge"] = P("Survival", "Adrenaline Surge", "First time you drop below 30 HP in a fight: +20 temp HP."),
            ["Vault"] = P("Survival", "Vault", "Vaulting cover is 30% faster."),
            ["PackMule"] = P("Survival", "Pack Mule", "Base carry capacity 50 kg."),
            ["IronStomach"] = P("Survival", "Iron Stomach", "Food and water restore double."),
            ["AnomalyWhisperer"] = P("Survival", "Anomaly Whisperer", "+2 to anomaly avoidance."),
            ["Scavenger"] = P("Survival", "Scavenger", "Loot counts are rolled twice, keep the higher."),
            ["IronWill"] = P("Survival", "Iron Will", "Temporary HP lasts about 48 seconds."),
            ["ArtifactHandler"] = P("Survival", "Artifact Handler", "Artifact Anom-DR penalties are halved."),
            ["Haggler"] = P("Survival", "Haggler", "Buy 15% cheaper, sell 15% higher."),
            ["Tumbler"] = P("Survival", "Tumbler", "After a dive you're back on your feet faster (0.35 s prone instead of 0.6)."),   // real-time fork
            ["ColdBlood"] = P("Survival", "Cold Blood", "Rounds that just miss you fill twice the Focus."),   // real-time fork
        };
    }
}
