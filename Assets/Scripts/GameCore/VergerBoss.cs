// Boss 6: the Verger (Yan, 2026-11-20: "a gothic beast hunter using an axe" -> a scythe; "he comes every run at the end" of the cathedral job, MISSION_CHOIR.md; "he starts as a sprite swinging his scythe ... at 50% he transforms into a beast").
// The fight is Boss.cs (Boss.Verger): the Visor's duel on a war scythe, then at half health the change (Def.Change: hittable at a tenth of the damage for 4 s) and the claws. This file is his entrance and his end.
// NOT built yet: the cathedral job that sends him in at the end of a run (the bell, the Husk waves); the beast's own sprite (until it exists he fights on in the human body); the co-op sync of the change (host-side only).
using System.Collections.Generic;
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

        // ---- the beast's drawing (ArtSource/export_verger_beast.py -> Sprites/Mutants/anim/verger_beast.bytes: stand, change, run, attack) ----
        public const float BeastScale = 0.5f;                 // drawn at half the strip's size: about 2.8 tiles tall, next to the Gaunt's 3 (it grew a lot, 1 tile -> ~3)
        const int Stand = 0, Change = 1, Run = 2, Attack = 3;
        static readonly Dictionary<HumanEnemy, (float x, float y)> _last = new Dictionary<HumanEnemy, (float, float)>();
        public static bool IsBeast(ICombatant u) => u is HumanEnemy && Boss.Of(u) is Boss.Fight f && f.Def == Boss.Verger && f.Beast && !f.Finished;
        public static bool FacesLeft(ICombatant u) { var p = Player.Current; return p != null && p.X < u.X; }

        /// <summary>The beast's picture this frame: the change while it lasts, the attack from the wind-up's end to a third of a second after the hit, the trot (run and stand alternating) while he moves, the stand otherwise. Null when the art is missing (he stays a man).</summary>
        public static Sprite BeastFrame(HumanEnemy h)
        {
            var strip = UnitSprites.HDFx("verger_beast", 4, new Vector2(0.5f, 16f / 190f)); if (strip == null) return null;
            var f = Boss.Of(h); float now = Time.time; int i = Stand;
            var (x, y) = Ballistics.Pos(h); bool moving = _last.TryGetValue(h, out var l) && (Mathf.Abs(l.x - x) + Mathf.Abs(l.y - y)) > 0.01f; _last[h] = (x, y);
            if (now < f.ChangeUntil) i = Change;
            else if (f.Doing != null && f.StrikeAt - now < 0.3f || now - f.StruckAt < 0.3f) i = Attack;
            else if (moving) i = (int)(now / 0.16f) % 2 == 0 ? Run : Stand;
            return strip[i];
        }

        /// <summary>His execution (Boss.FinishNow): the scythe and the coat are on the body.</summary>
        public static void Defeated(Player p, HumanEnemy h)
        {
            Story.Journal("The Verger: the cathedral's last hunter kept the bell and the dead apart. He changed at the end, and he is down. His scythe and his coat are yours.");
            GameLog.Event("The beast shrinks into a man in a torn coat, and lies still. The scythe is in his pack.");
        }
    }
}
