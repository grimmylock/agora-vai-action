using UnityEngine;
using System.Linq;

// 2026-11-23 (Yan: "make it about 10 seconds of cutscene, he sets down his hat, runs his hand through the scythe making it glow red, similar to the bloodflame incantation in elden ring ... not faster,
// they're plenty fast already ... the after-image as a low opacity version of himself, longer combos ... a quick-time event with a button mash similar to the eel"): the Verger's half-health change.
//  0.0  the camera locks on him (letterbox, everyone stands still)          1.6  the hat comes off (the bare coat) and arcs onto the flagstones
//  3.0 - 6.6  his bare hand drags along the blade: the scythe warms to red, embers rise, the camera creeps in      6.6  the flash and the bell stroke
//  6.9 - 9.3  QUICK-TIME: his first red slash comes at you; MASH the parry key (MashNeed presses) to throw it off -> he is staggered 2.6 s; fail -> it lands (unparryable, knocked down)
// Then phase 2 for real: the scythe stays red, red sparks on every swing, a low-opacity ghost of him trails each move, and the combos are longer (Boss.VergerScytheMoves). Co-op: host side only for now (Yan: "we can fix it later");
// the cutscene is not mirrored to clients and any player's press counts toward the mash.
namespace GameCore
{
    public static class VergerPhase2
    {
        public const float HatOff = 1.6f, HatLand = 0.7f, RiteFrom = 3.0f, RiteTo = 6.6f, QteFrom = 6.9f, QteSeconds = 2.4f, TotalCut = QteFrom;
        public const int MashNeed = 8;

        static Boss.Fight _f; static float _t0 = -1f; static bool _hat, _landed, _flash, _qte, _done; static int _presses; static float _nextEmber, _nextGhost;
        public static bool Done;           // phase 2 proper has begun for _f
        static Boss.Fight _glowFight;

        public static bool Active(Boss.Fight f) => f != null && f == _f && _t0 >= 0f && !_done;
        public static bool InQte(Boss.Fight f) => Active(f) && _qte;
        public static void Reset() { Done = false; _f = null; _t0 = -1f; _hat = _landed = _flash = _qte = _done = false; _presses = 0; _glowFight = null; }

        /// <summary>0..1 how red the scythe is: ramps through the rite and stays at 1.</summary>
        public static float Glow(Boss.Fight f, float now)
        {
            if (f == null || f != _f) return 0f;
            if (Done || _done) return 1f;
            if (_t0 < 0f) return 0f; float t = now - _t0; return Mathf.Clamp01((t - RiteFrom) / (RiteTo - RiteFrom));
        }
        /// <summary>The colour the held scythe is tinted (HeldWeaponView.Show): white unless it is the Verger's and he has begun to glow.</summary>
        public static Color Tint(Weapon w)
        {
            var f = _f; if (f == null || w == null || f.Unit == null || f.Gone || f.Broken || f.Finished || !ReferenceEquals(f.Unit.EquippedWeapon, w)) return Color.white;
            float g = Glow(f, Time.time); if (g <= 0f) return Color.white;
            float pulse = 0.88f + 0.12f * Mathf.Sin(Time.time * 9f);
            return Color.Lerp(Color.white, new Color(1f, 0.3f * pulse, 0.22f * pulse), g);
        }
        public static float GlowOf(ICombatant u) { var f = Boss.Of(u); return f != null && f == _f ? Glow(f, Time.time) : 0f; }
        /// <summary>The ghost trail (TacticalGridView): phase 2, mid-move.</summary>
        public static bool Ghosting(ICombatant u) { var f = Boss.Of(u); return f != null && f == _f && Done && f.Doing != null && !f.Broken && !f.Finished; }

        public static void Begin(Boss.Fight f, float now)
        {
            Reset(); _f = f; _t0 = now; f.Doing = null; f.ChangeUntil = now + 60f; f.Phase = 2;
            var h = f.Unit; var (bx, by) = Ballistics.Pos(h);
            Story.CamLock = (bx, by); Story.CamHalf = 6f; Story.CamRate = 3f; Story.Letterbox = true; Story.CutsceneUntil = now + 1f;
            Boss.NameCard("", ""); GameLog.Write("He stops. The scythe comes down point-first onto the stones.");
        }

        /// <summary>The parry key went down: a mash press while the quick-time is up (Parry.Press).</summary>
        public static bool MashInput(Player p)
        {
            var f = _f; if (f == null || !InQte(f) || p == null) return false;
            _presses++; Boss.NameCard("MASH PARRY", _presses + " / " + MashNeed);
            Sfx.Play("parry", null, 0.6f, 0.05f, 1f + _presses * 0.04f); return true;
        }
        /// <summary>ESC during the cutscene part jumps to the flash.</summary>
        public static bool Skip() { var f = _f; if (f == null || _t0 < 0f || _done || _qte || !Story.InCutscene) return false; if (!_hat) HatCome(f); _t0 = Mathf.Min(_t0, Time.time - RiteTo); return true; }

        static void HatCome(Boss.Fight f)
        {
            _hat = true; var h = f.Unit; if (h.EquippedArmor != null) h.EquippedArmor = Boss.BareOf(h.EquippedArmor);
            int tx = h.X + (Random.value < 0.5f ? -1 : 1); int ty = h.Y; if (!TerrainMap.CanEnterTile(tx, ty)) tx = h.X;
            _hatAt = (tx + 0.5f, ty + 0.9f);
            var e = CombatFx.Push("vergerhat", h, null, null); e.Now = true; e.Rounds = 1; e.EndX = _hatAt.x; e.EndY = _hatAt.y;
        }
        static (float x, float y) _hatAt;

        static void Ember(Boss.Fight f, int strength)
        {
            var e = CombatFx.Push("scytheember", f.Unit, null, null); e.Now = true; e.Rounds = strength;
        }

        /// <summary>Called every frame from Boss.Tick for the Verger; true = he is in the cutscene (does nothing else).</summary>
        public static bool Tick(Boss.Fight f, Player p, float now)
        {
            if (f != _f) return false;
            var h = f.Unit;
            if (Done)                                                    // phase 2 proper: red sparks on every swing
            {
                if (f.Doing != null && now >= _nextEmber) { _nextEmber = now + 0.09f; Ember(f, 2); }
                return false;
            }
            if (_done || _t0 < 0f) return false;
            float t = now - _t0; var (bx, by) = Ballistics.Pos(h);
            f.Doing = null; f.ChangeUntil = now + 60f; f.StrikeAt = now + 99f;
            if (t < TotalCut) { Story.CutsceneUntil = now + 0.5f; Story.Letterbox = true; }
            if (!_hat && t >= HatOff) HatCome(f);
            if (_hat && !_landed && t >= HatOff + HatLand)
            {
                _landed = true; var e = CombatFx.Push("vergerhat", h, null, null); e.Now = true; e.Rounds = 2; e.EndX = _hatAt.x; e.EndY = _hatAt.y; Sfx.Play("hook_catch", null, 0.5f, 0.05f, 0.7f);
            }
            if (t < QteFrom)
            {
                float k = Mathf.Clamp01((t - RiteFrom) / (RiteTo - RiteFrom));
                Story.CamLock = (bx, by); Story.CamHalf = Mathf.Lerp(6f, 3.4f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / RiteTo))); Story.CamRate = 3f;
                if (t >= RiteFrom && t < RiteTo && now >= _nextEmber) { _nextEmber = now + Mathf.Lerp(0.2f, 0.06f, k); Ember(f, 1 + (int)(k * 3f)); }
                if (!_flash && t >= RiteTo)
                {
                    _flash = true; Story.Fade(new Color(1f, 0.55f, 0.45f), 0.85f, 0.06f); Boss.HitStop(0.25f); CombatFx.PxAt("thunderclap", h.X, h.Y);
                    Sfx.Play("boss_sting", null, 0.9f, 0f, 1.25f); Sfx.Play("boss_sting", null, 0.8f, 0f, 0.5f);          // ponytail: Yan finds the track later; this is the hook
                    CombatFx.Push("aura", h, null, h.EquippedWeapon).Now = true; Ember(f, 4);
                }
                if (_flash && t >= RiteTo + 0.25f && Story.FadeAlpha > 0.05f) Story.Fade(new Color(1f, 0.55f, 0.45f), 0f, 0.5f);
                return true;
            }
            if (!_qte)                                                    // the quick-time: input flows again, slow motion, the camera stays close
            {
                _qte = true; _presses = 0; Story.CutsceneUntil = -1f; Boss.Hold(QteSeconds + 0.3f);
                Boss.Glint(f, true);
                Boss.NameCard("MASH PARRY", "0 / " + MashNeed); GameLog.Write("The red scythe comes down: MASH the parry key to turn it aside!");
            }
            Story.CamLock = (bx, by); Story.CamHalf = 3.4f; Story.Letterbox = true;
            bool won = _presses >= MashNeed;
            if (won || t >= QteFrom + QteSeconds)
            {
                Boss.Release();
                if (won)
                {
                    CombatFx.Push("parry", h, p, null).Hit = true; Boss.HitStop(0.2f); GameLog.Write("You turn it aside! He reels.");
                }
                else
                {
                    GameLog.Write("The red scythe lands.");
                    foreach (var q in Player.All.Concat(new[] { p }).Distinct().ToList()) if (q != null && q.IsAlive() && CombatantUtil.Distance(h, q) <= 4)
                    { Parry.Unparryable = true; try { Melee.HitUnit(h, h.EquippedWeapon, q, 1.6f * 0.7f); } finally { Parry.Unparryable = false; } Rt.Knockdown(q, 1.1f); }
                    CombatFx.Push("stagger", h, null, null).Now = true; Boss.HitStop(0.15f);
                }
                Finish(f, now, won);
            }
            return true;
        }

        static void Finish(Boss.Fight f, float now, bool won)
        {
            _done = true; Done = true; _glowFight = f; Story.CineEnd(); Boss.Release();
            f.Phase = 2; f.Doing = null; f.ChangeUntil = -1f; f.OpenUntil = -1f; f.NextMoveAt = now + 1.6f;
            if (won) { f.Clip = null; f.StaggerUntil = now + 2.6f; f.NextMoveAt = f.StaggerUntil + 0.3f; CombatFx.Push("stagger", f.Unit, null, null).Now = true; }          // thrown off his own slash: open for 2.6 s
            Boss.NameCard("PHASE 2", f.Def.Phase2Line);
            if (f.Def.Phase2Barks != null) for (int i = 0; i < f.Def.Phase2Barks.Length; i++) Banter.Say(f.Unit, f.Def.Phase2Barks[i], 1, 0.3f + i * 1.8f);
        }
    }
}
