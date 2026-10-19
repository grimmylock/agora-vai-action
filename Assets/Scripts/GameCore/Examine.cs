// Batch 77 (Yan: "when an enemy is in range or visible add an 'examine' prompt similar to BG3, where you can see his
// equipped weapon and armor, resistances and if he is a boss, perks"): the text of the examine panel (HudView.Targeting).
// Only what you could tell by looking: gear, how hurt they look, their stance - and a boss's perks.
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace GameCore
{
    public static class Examine
    {
        static string Pct(float f) => $"{System.Math.Round(f * 100)}%";

        public static string Title(Mutant e)
        {
            if (e is HumanEnemy h) return $"{h.Name}  <color=#7d7a70>{h.FactionName} {h.Rank}</color>{(h.Named ? "  <color=#e8c547>BOSS</color>" : "")}";
            return $"{e.Name}  <color=#7d7a70>Tier {e.Db?.Tier ?? 1} mutant</color>{(e.Named ? "  <color=#e8c547>BOSS</color>" : "")}";
        }

        public static string Body(Mutant e, Player p)
        {
            var lines = new List<string>();
            lines.Add($"<color=#c8443a>HP {e.Hp}/{e.MaxHp}</color>   AC {e.Ac}   AP {(e is HumanEnemy ? CombatRules.NpcAp(e) : e.Db?.Ap ?? 4)}   MP {e.MaxMp}");
            if (e is HumanEnemy h)
            {
                lines.Add(h.Morale == "Steady" ? "Steady" : h.Morale == "Shaken" ? "<color=#e8c547>Shaken</color> (-2 to hit)" : $"<color=#62b25a>{h.Morale}</color>");
                var w = h.EquippedWeapon;
                lines.Add("");
                lines.Add("<color=#e8c547>WEAPON</color>");
                if (w == null || h.GunDropped) lines.Add(h.GunDropped ? "Gun on the ground (2 AP to pick up)" : "Unarmed");
                else
                {
                    string cat = CombatRules.Category(w.Db);
                    lines.Add($"{w.Name}  <color=#7d7a70>{cat}</color>");
                    lines.Add(CombatRules.IsMelee(w) ? $"{w.BaseDamage} damage, melee"
                        : $"{w.BaseDamage} damage, range {w.RangeTiles} (reach {w.MaxReach}), {w.CurrentMagazine}/{w.MagCapacity} {w.CurrentAmmoType}"
                          + (CombatRules.CanBurst(w) ? $", bursts of {CombatRules.BurstRounds(w)}" : ""));
                    if (w.Condition != ItemCondition.Working) lines.Add($"Condition: {w.Condition}");
                }
                lines.Add("");
                lines.Add("<color=#e8c547>ARMOR</color>");
                var a = h.EquippedArmor;
                lines.Add(a == null ? "None" : $"{a.Name}" + (h.ArmorCondition != ItemCondition.Working ? $" ({h.ArmorCondition})" : ""));
                lines.Add($"Phys-DR {Pct(h.PhysDR * CombatRules.ArmorScale(h))}   Anom-DR {Pct(h.AnomDR)}");
                if (h.Frags > 0) lines.Add("Carries a grenade (aim for the pouch)");
                if (h.Named && h.ActivePerks != null && h.ActivePerks.Count > 0)
                {
                    lines.Add("");
                    lines.Add("<color=#e8c547>PERKS</color>");
                    foreach (var k in h.ActivePerks)
                        lines.Add(PerkDatabase.All.TryGetValue(k, out var perk) ? $"{perk.Name}: <color=#7d7a70>{perk.Description}</color>" : k);
                }
            }
            else
            {
                var d = e.Db;
                lines.Add("");
                lines.Add($"Attack {e.AttackDamage} damage{(d?.Behavior != null ? $", {d.Behavior}" : "")}");
                lines.Add($"Phys-DR {Pct(e.PhysDR)}   Anom-DR {Pct(e.AnomDR)}");
                var traits = new List<string>();
                if (d != null)
                {
                    if (d.PackTactics) traits.Add("hunts in packs");
                    if (d.Leap) traits.Add("leaps");
                    if (d.ToxicBite) traits.Add("toxic bite");
                }
                if (e.ActiveCamouflage) traits.Add("cloaks");
                if (traits.Count > 0) lines.Add(string.Join(", ", traits));
            }
            var st = new List<string>();
            if (e.StatusEffects.Wound != null) st.Add($"{e.StatusEffects.Wound} wound");
            if (e.StatusEffects.Bleeding) st.Add("bleeding");
            if (e.StatusEffects.Suppressed) st.Add("pinned");
            if (e.StatusEffects.Prone) st.Add("prone");
            if (e.StatusEffects.Hunkered) st.Add("hunkered");
            if (!e.IsAggroed) st.Add("<color=#62b25a>hasn't seen you</color>");
            if (st.Count > 0) { lines.Add(""); lines.Add(string.Join(", ", st)); }
            if (p != null)
            {
                var cover = CoverUtil.GetCoverAt(p.X, p.Y, e.X, e.Y);
                lines.Add(cover.AcBonus > 0 ? $"In {(cover.AcBonus >= 2 ? "hard" : "light")} cover from you (+{cover.AcBonus} AC)" : cover.Flanked ? "<color=#62b25a>Flanked from where you stand (+2 to hit)</color>" : "In the open from where you stand");
            }
            return string.Join("\n", lines);
        }
    }
}
