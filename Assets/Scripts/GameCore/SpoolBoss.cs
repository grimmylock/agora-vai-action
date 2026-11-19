// Boss 4: Spool, the Scrap King's exile (Yan, 2026-11-19: "put him in tier 2", "start building the boss similar to how the visor works"; the Barbed Wire Bat's outcast Raider, ACTION_DESIGN "Melee").
// He lives in the Dog Kennels' fenced yard in the Scrapyard (Tier 2): a Raider the Scrap King threw out of his crew. The fight is the Visor's (Boss.cs: white glint = parry it, red glint = dodge it, three perfect parries stagger him,
// phase 2 at half health, the finisher) on a slower, heavier weapon, so every move is a long wind-up and a long opening. He fights as an Outcast, keeps his body (no smoke grenade) and drops the Barbed Wire Bat
// (a Tier 4 bat that bleeds) and his coat; `spool_done` is set by the execution. Co-op: he fights the nearest player. Host-side only (the networking does not carry boss fights yet).
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class SpoolBoss
    {
        public const string Lair = "Dog Kennels", Done = "spool_done", Ledger = "story_kennel_ledger";
        public const float WakeRange = 13f;

        public static void Tick(float dt)
        {
            if (Story.Step("spool") == 1 && Tier1Story.Holder(Ledger) != null && Net.Mode != Net.Role.Client) ReadLedger();      // picked up off the body (anywhere)
            if (!Tier2Jobs.OnTier2 || Net.Mode == Net.Role.Client || Story.Has(Done) || Player.Current == null) return;
            Boss.Fights.RemoveAll(f => f.Def == Boss.Spool && !f.Finished && !EncounterState.Enemies.Contains(f.Unit));          // you left and came back: he starts again
            if (Boss.Fights.Any(f => f.Def == Boss.Spool && !f.Gone)) return;
            var l = TerrainMap.Landmarks?.FirstOrDefault(m => m.Name == Lair); if (l == null) return;
            if (!Player.All.Any(p => p.IsAlive() && Mathf.Sqrt((p.X - l.X) * (p.X - l.X) + (p.Y - l.Y) * (p.Y - l.Y)) <= WakeRange)) return;
            var (x, y) = TerrainMap.FindNearestEnterable(l.X, l.Y + 1);
            Spawn(x, y, true);
        }

        /// <summary>Puts him on the map and starts the fight (`intro`: the lair's few lines first).</summary>
        public static Boss.Fight Spawn(int x, int y, bool intro)
        {
            var f = Boss.Spawn(Boss.Spool, x, y); var h = f.Unit; Rt.SetOffset(h, 0f, 0f);
            f.NextMoveAt = Time.time + (intro ? 2.6f : 1.6f);
            if (intro)
            {
                GameLog.Event("Behind the kennel fence a man sits on an upturned crate, a bat across his knees. Barbed wire is wound round the barrel, rag over the grip.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.0f);
            }
            Banter.Say(h, "The King sent you. He always sends someone.", 1, 0.2f);
            if (Story.Step("spool") == 1) { Banter.Say(h, "Did he tell you about the papers?", 1, 3.5f); Banter.Say(h, "He did. Course he did.", 1, 6.5f); }
            return f;
        }

        /// <summary>The ledger in the hand, read once (the dialogue panel, like the Annex's papers): what Spool kept and why he was thrown out.</summary>
        static void ReadLedger()
        {
            if (Story.Step("spool") != 1) return;
            Story.SetStep("spool", 2); Story.SetObjective("spool", "Take the Kennel Ledger to the Scrap King - or think about it first", King2);
            Story.Talk("THE KENNEL LEDGER", new[] { "Spool's hand: columns of dates and crate counts, each line a cart out of the Basin, each with the King's crown and, beside it, a second stamp. A Garrison stamp.",
                "The last page: 'Every tenth crate goes up the road to the Gatehouse, and nobody on that road is robbed. That is the King's peace. I said what it was. He called it exile. - S.'",
                "Under it, in another pen: 'The Garrison asked for the hatch under the Loading Dock. We told them where. - the Crown.'" });
            Story.Set("doc_kennel"); Story.Journal("The Kennel Ledger: the Scrap King pays the Garrison a tenth of every cart for a quiet road, and sold them the hatch under the Loading Dock. Spool said so, and was put out for it.");
        }
        const string King2 = "Scrap King's Nest";

        /// <summary>His execution (Boss.TryFinish): what the Dog Kennels were hiding, and the bat.</summary>
        public static void Defeated(Player p, HumanEnemy h)
        {
            if (!h.Inventory.Items.Any(i => i.Id == Ledger)) h.Inventory.Items.Add(new InventoryItem { Id = Ledger, Name = "Kennel Ledger", Type = "key", Qty = 1, WeightKg = 0.1f, Price = 0 });   // sewn into the coat's lining
            GameLog.Event("In the coat's lining, sewn flat: a folded ledger.");
            Story.Journal("Spool: the Scrap King's exile held the Dog Kennels with a barbed wire bat. He is down. The bat and his coat are yours.");
            GameLog.Event("Spool kneels in the dirt, and does not get up again. The bat is still wet. Take it from his hands.");
        }
    }
}
