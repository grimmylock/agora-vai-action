// New (Batch 5): weapon/armor condition ("Working"/"Dirty"/"Damaged") existed as flavor text only
// (HumanLootSystem rolled it into the dropped item's Name string) - nothing ever read it back out.
// Centralizes the three real effects the Economy file ties to condition so Weapon.Fire, the armor
// DR calc, and ShopSystem's sell-back all agree on the same rule instead of drifting: Damaged cuts
// output (damage/DR) by 25%; each wear step adds 2% to the jam chance (CombatRules.JamChance); the
// sell-back price multiplier is the one place all three tiers differ.
// Null/empty Condition (every item added before this batch, or bought fresh from a shop) is
// treated as "Working" - pristine, no penalty.
namespace GameCore
{
    public static class ItemCondition
    {
        public const string Working = "Working";
        public const string Dirty = "Dirty";
        public const string Damaged = "Damaged";
        public const string Broken = "Broken";   // file 00: weapon unusable, armor -25% DR

        public static bool IsDamaged(string condition) => condition == Damaged;
        public static bool IsBroken(string condition) => condition == Broken;

        /// <summary>One step down the wear ladder (Working > Dirty > Damaged > Broken).</summary>
        public static string Worse(string condition)
        {
            switch (condition)
            {
                case Dirty: return Damaged;
                case Damaged: return Broken;
                case Broken: return Broken;
                default: return Dirty;
            }
        }

        /// <summary>Armor DR multiplier: Damaged -10%, Broken -25% (file 00 Condition table).</summary>
        public static float ArmorDrMultiplier(string condition) => condition == Broken ? 0.75f : condition == Damaged ? 0.9f : 1f;

        /// <summary>Sell-back price multiplier: Working x1.0, Dirty x0.85, Damaged x0.5 (Economy
        /// file's condition modifier table).</summary>
        public static float SellMultiplier(string condition) => condition switch
        {
            Broken => 0.25f,
            Damaged => 0.5f,
            Dirty => 0.85f,
            _ => 1.0f,
        };
    }
}
