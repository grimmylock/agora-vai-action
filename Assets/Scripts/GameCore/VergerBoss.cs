// Boss 6: the Verger (Yan, 2026-11-20: "a gothic beast hunter using an axe" -> a scythe; "he comes every run at the end" of the cathedral job, MISSION_CHOIR.md; "he starts as a sprite swinging his scythe ... at 50% he transforms into a beast").
// The fight is Boss.cs (Boss.Verger): the Visor's duel on a war scythe, then at half health the change (Def.Change: hittable at a tenth of the damage for 4 s) and the claws. This file is his entrance and his end.
// NOT built yet: the cathedral job that sends him in at the end of a run (the bell, the Husk waves); the beast's own sprite (until it exists he fights on in the human body); the co-op sync of the change (host-side only).
using System.Linq;
using UnityEngine;

namespace GameCore
{
    public static class VergerBoss
    {
        /// <summary>Puts him on the map and starts the fight (the Testing Ground calls this too; the cathedral job will, at the end of every run).</summary>
        public static Boss.Fight Spawn(int x, int y, bool intro)
        {
            var f = Boss.Spawn(Boss.Verger, x, y); var h = f.Unit; Rt.SetOffset(h, 0f, 0f);
            f.NextMoveAt = Time.time + (intro ? 3.0f : 1.8f);
            if (intro)
            {
                GameLog.Event("The bell goes quiet. From the altar end, a tall man in a long coat walks down the nave with a scythe over his shoulder, and does not hurry.");
                Story.Cutscene(x + 0.5f, y + 0.5f, 2.2f);
            }
            Banter.Say(h, "This is a church. You do not ring that bell in a church.", 1, 0.3f);
            return f;
        }

        /// <summary>His execution (Boss.FinishNow): the scythe and the coat are on the body.</summary>
        public static void Defeated(Player p, HumanEnemy h)
        {
            Story.Journal("The Verger: the cathedral's last hunter kept the bell and the dead apart. He changed at the end, and he is down. His scythe and his coat are yours.");
            GameLog.Event("The beast shrinks into a man in a torn coat, and lies still. The scythe is in his pack.");
        }
    }
}
