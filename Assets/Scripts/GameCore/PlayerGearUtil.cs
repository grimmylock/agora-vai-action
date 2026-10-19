// Ported from script.js: getDetectorBonus(unit), hasArmorMod(modId), removeRandomMedicalConsumable().
// hasArmorMod() referenced the global `player` directly in the original (not a generic unit
// param) — reproduced as-is via Player.Current.
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class PlayerGearUtil
    {
        /// <summary>Ported from getDetectorBonus(unit).</summary>
        public static int GetDetectorBonus(ICombatant unit)
        {
            return (unit?.ActiveDetector as GameData.DetectorDef)?.Modifier ?? 0;
        }

        /// <summary>Ported from hasArmorMod(modId) — always checks the global player, matching the JS.</summary>
        public static bool HasArmorMod(string modId)
        {
            var p = Player.Current;
            return p != null && p.ArmorMods != null && (p.ArmorMods.SlotA == modId || p.ArmorMods.SlotB == modId);
        }

        private static readonly string[] MedicalItemIds =
        {
            "basicMedkit", "militaryMedkit", "scientificMedkit", "bandage", "radBlockPill", "mre", "energyDrink",
        };

        /// <summary>Ported from removeRandomMedicalConsumable().</summary>
        public static bool RemoveRandomMedicalConsumable()
        {
            var p = Player.Current;
            if (p == null) return false;

            var medicalItems = p.Inventory.Items.Where(it => it.Type == "consumable" && MedicalItemIds.Contains(it.Id)).ToList();
            if (medicalItems.Count == 0) return false;

            var choice = medicalItems[Random.Range(0, medicalItems.Count)];
            p.RemoveItem(choice.Id, 1);
            GameLog.Write($"Pyre crit destroys one {choice.Name} from your inventory.");
            return true;
        }
    }
}
