// New (Batch 9): shared remoteness/reputation price-and-reward adjustment - the gap flagged in
// three places this session (QuartermasterDatabase.cs's header note, ShopSystem.cs's
// SellPercentByType doc comment, and QuestSystem.ComputeQuestReward's doc comment): the source
// ruleset layers a further ±10-30% price/reward swing on top of the flat numbers this project had
// been using, and nothing modeled it. One shared formula here so buying, selling, and quest
// rewards all read the same numbers instead of three independent guesses.
//
// ponytail: the source's exact per-outpost/per-tier calibration (e.g. "Ghost Town's squatter
// always sits at the high end of the band") wasn't available to re-derive this session - this
// uses zone/outpost Tier as the remoteness proxy (Tier 1 -> +10%, Tier 5+ -> +30%, linear between)
// and a flat ±15% for a Friendly relationship, both inside the source's stated ±10-30% band.
// Revisit against the source doc's actual table if/when it's available.
namespace GameCore
{
    public static class ReputationPricing
    {
        /// <summary>Remoteness markup applied to what you pay to buy - how far into the Zone the
        /// current trader/quest is, by tier.</summary>
        public static float RemotenessMarkup(int tier)
        {
            int clamped = System.Math.Min(5, System.Math.Max(1, tier));
            return 1f + 0.10f + (clamped - 1) * 0.05f;
        }

        /// <summary>Friendly relationship price break: cheaper to buy (isSelling: false -> 0.85x),
        /// better payout to sell or a bigger quest reward (isSelling: true -> 1.15x) - both
        /// favorable to the player, just pulling the price in opposite directions depending on
        /// which side of the transaction they're on. Neutral or no-faction trades are unaffected
        /// (Hostile can't trade at all - already gated by QuartermasterDatabase.CanTrade).</summary>
        public static float FriendlyMultiplier(string repLevel, bool isSelling) =>
            repLevel == "Friendly" ? (isSelling ? 1.15f : 0.85f) : 1f;
    }
}
