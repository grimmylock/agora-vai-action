// Ported from script.js: rollD20(options) and formatRollLog(roll, modifier, targetAC).
using UnityEngine;

namespace GameCore
{
    public struct D20Roll
    {
        public int Roll1;
        public int? Roll2;   // null unless Advantage or Disadvantage
        public int Chosen;
        public bool Advantage;
        public bool Disadvantage;
        public string Dice1, Dice2;   // Batch 73: the two d10s behind each 2d10 total ("4+2"), shown in the log
    }

    public static class DiceRoller
    {
        /// <summary>
        /// Ported from rollD20(options). advantageState is "advantage", "disadvantage", or
        /// anything else (including "standard") for a plain roll — matches the JS's loose string
        /// comparison (options === 'advantage' / 'disadvantage').
        /// </summary>
        public static D20Roll RollD20(string advantageState = "standard")
        {
            int roll1 = Random.Range(1, 21); // Range(1,21) is [1,20], matching Math.floor(Math.random()*20)+1
            int roll2 = Random.Range(1, 21);
            return Pick(roll1, roll2, advantageState);
        }

        /// <summary>Batch 56: attack rolls are 2d10 (2-20, most often 11). Advantage rolls the pair twice.</summary>
        public static D20Roll Roll2D10(string advantageState = "standard")
        {
            int a1 = Random.Range(1, 11), b1 = Random.Range(1, 11), a2 = Random.Range(1, 11), b2 = Random.Range(1, 11);
            var r = Pick(a1 + b1, a2 + b2, advantageState);
            r.Dice1 = $"{a1}+{b1}"; r.Dice2 = $"{a2}+{b2}";
            return r;
        }

        private static D20Roll Pick(int roll1, int roll2, string advantageState)
        {
            bool advantage = advantageState == "advantage";
            bool disadvantage = advantageState == "disadvantage";

            if (advantage)
            {
                return new D20Roll { Roll1 = roll1, Roll2 = roll2, Chosen = System.Math.Max(roll1, roll2), Advantage = true, Disadvantage = false };
            }
            if (disadvantage)
            {
                return new D20Roll { Roll1 = roll1, Roll2 = roll2, Chosen = System.Math.Min(roll1, roll2), Advantage = false, Disadvantage = true };
            }
            return new D20Roll { Roll1 = roll1, Roll2 = null, Chosen = roll1, Advantage = false, Disadvantage = false };
        }

        /// <summary>Ported from formatRollLog(roll, modifier, targetAC).</summary>
        public static string FormatRollLog(D20Roll roll, int modifier, int targetAC, string breakdown = null)
        {
            // Batch 73 (Yan: "why is it 6-3 = 3, are there not 2 d10's being rolled?"): show both dice and what the modifier is made of
            if (roll.Dice1 != null)
            {
                string mods = string.IsNullOrEmpty(breakdown) ? (modifier == 0 ? "" : $" {modifier:+0;-0}") : " " + breakdown;
                string dice = roll.Advantage || roll.Disadvantage
                    ? $"2d10 {roll.Dice1}={roll.Roll1} and {roll.Dice2}={roll.Roll2} ({(roll.Advantage ? "Advantage" : "Disadvantage")}, keeps {roll.Chosen})"
                    : $"2d10 {roll.Dice1}={roll.Chosen}";
                return $"Rolled {dice}{mods} = {roll.Chosen + modifier} vs AC {targetAC}";
            }
            if (roll.Advantage)
                return $"Rolled {roll.Roll1} and {roll.Roll2} (Advantage) + {modifier} = {roll.Chosen + modifier} vs AC {targetAC}";
            if (roll.Disadvantage)
                return $"Rolled {roll.Roll1} and {roll.Roll2} (Disadvantage) + {modifier} = {roll.Chosen + modifier} vs AC {targetAC}";
            return $"Rolled {roll.Chosen} + {modifier} = {roll.Chosen + modifier} vs AC {targetAC}";
        }
    }
}
