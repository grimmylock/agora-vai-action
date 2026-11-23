// Tier 4 boss-ish: "The Preacher" (STORY.md, STORY_DIALOGUE.md "The Siege", Yan 2026-10-29: "sends waves of purple energy out round him (an area attack); dodge them with the dive or stay out of range").
// A tall Obelisk figure in white with a glowing violet psi-core on his back. Unarmed: his only weapon is the pulse - every 7 s (every 4.5 s under half health) he raises his arms
// (a 1.4 s tell: the line "Be still. Be quiet. Be one." and the void-pull effect), then a violet wave rolls out 9 tiles: everyone in it takes 35 damage (less with anomaly resistance) unless
// they are mid-dive (Rt.Untouchable). He is tough (2,500 HP) and walks slowly at you (RtAi). Rule: do NOT break the core - an explosion that touches him cracks it (`Exploded`); he drops
// the core as an item on death (intact: "Tuned Psi-Core" `story_core`; cracked: "Cracked Psi-Core" `story_core_cracked`). Hooks: `Explosions.Blast` (OnBlast), `Rt.WorldTick` (Tick).
// ponytail: the pulse has no art of its own (a pixelfx void pull at the tell, a thunderclap at the wave); no vision-swim screen effect yet; the core isn't a separate target.
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class Preacher
    {
        public const string Name = "The Preacher";
        public const int Hp = 1000, Damage = 35, Radius = 9;   // 2026-11-23 (Yan: "normal HP human but forcefield"): a human's health, the field does the rest
        public const float ShieldSeconds = 15f;                 // the force field: up for 15 s, every third of his health lost (at 67% and at 34%), and it calls another wave
        public const int ExtraWaveBatches = 3;
        public const float Gap = 7f, GapHurt = 4.5f, Tell = 1.4f;
        public static HumanEnemy Current; public static bool Exploded, Dead;
        static float _nextAt, _waveAt = -1f, _shieldUntil = -1f; static int _shieldHp, _shields; static bool _dropped;
        /// <summary>The force field is up: nothing touches him (rounds are deflected, blows and blasts take nothing off).</summary>
        public static bool Shielded(ICombatant u) => Is(u) && Time.time < _shieldUntil;
        public static bool Is(ICombatant u) => Current != null && ReferenceEquals(u, Current);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Current = null; Exploded = Dead = _dropped = false; _nextAt = 0f; _waveAt = -1f; _shieldUntil = -1f; _shields = 0; _shieldHp = 0; }

        public static HumanEnemy Spawn(int x, int y)
        {
            Reset();
            var armor = StoryNpcs.Fixed("Obelisk Exoskeleton") ?? StoryNpcs.Fixed("Obelisk Scientific");
            var h = new HumanEnemy(Name, x, y, "Obelisk", "Master", null, armor) { IsAggroed = true };
            h.MaxHp = h.Hp = Hp; h.Morale = "Steady";
            EncounterState.Enemies.Add(h); Current = h; _nextAt = Time.time + 4f;
            return h;
        }

        /// <summary>Explosions.Blast: a blast on the Preacher cracks the core.</summary>
        public static void OnBlast(ICombatant v) { if (Is(v) && !Shielded(v)) Exploded = true; }

        public static void Tick(float dt)
        {
            if (Current == null) return;
            float now = Time.time;
            if (!Current.IsAlive())
            {
                if (!_dropped)
                {
                    _dropped = true; Dead = true;
                    var p = Player.Current;
                    if (p != null)
                    {
                        string id = Exploded ? "story_core_cracked" : "story_core";
                        p.AddItem(new InventoryItem { Id = id, Name = Exploded ? "Cracked Psi-Core" : "Tuned Psi-Core", Type = "key", Qty = 1, WeightKg = 3f, Price = 0 });
                        GameLog.Event(Exploded ? "The violet sphere is cracked, flickering, half dead." : "The violet sphere is warm, humming, intact.");
                    }
                }
                return;
            }
            if (Net.Mode == Net.Role.Client) return;
            // 2026-11-23 (Yan: "forcefield 15 second cooldown, activates every 33% HP lost, spawns more waves"): the field
            if (now < _shieldUntil) { Current.Hp = System.Math.Max(Current.Hp, _shieldHp); }
            else if (_shieldUntil > 0f) { _shieldUntil = -1f; GameLog.Event("The violet shell round the Preacher gutters out."); }
            float frac = Current.Hp / (float)Current.MaxHp; int crossed = (frac <= 0.67f ? 1 : 0) + (frac <= 0.34f ? 1 : 0);
            if (crossed > _shields && Current.IsAlive())
            {
                _shields = crossed; _shieldUntil = now + ShieldSeconds; _shieldHp = Current.Hp;
                CombatFx.PxAt("void_pull", Current.X, Current.Y); CombatFx.PxAt("thunderclap", Current.X, Current.Y); Sfx.Play("boss_sting", TacticalGridView.GridToWorld(Current.X, Current.Y), 1f, 0f);
                GameLog.Event("A violet shell closes round the Preacher. Nothing gets through it, and more of them are coming out of the woods.");
                Banter.Say(Current, "Be one.", 2); Tier4Story.ExtraWave(ExtraWaveBatches);
            }
            bool hurt = Current.Hp * 2 < Current.MaxHp;
            if (_waveAt < 0f && now >= _nextAt)
            {
                _waveAt = now + Tell;
                Banter.Say(Current, "Be still. Be quiet. Be one.", 2);
                CombatFx.PxAt("void_pull", Current.X, Current.Y);
                Story.Radio("Osei", "That's it - the core on his back! That's what we need! Don't hit the sphere! Whatever you do, don't break it!");
            }
            if (_waveAt >= 0f && now >= _waveAt)
            {
                _waveAt = -1f; _nextAt = now + (hurt ? GapHurt : Gap);
                CombatFx.PxAt("thunderclap", Current.X, Current.Y);
                GameLog.Write("A wave of violet light rolls out from him. Your vision swims.");
                foreach (var p in Player.All.Where(q => q.IsAlive()))
                {
                    if (System.Math.Max(System.Math.Abs(p.X - Current.X), System.Math.Abs(p.Y - Current.Y)) > Radius || Rt.Untouchable(p)) continue;
                    int dealt = Mathf.RoundToInt(Damage * (1f - Mathf.Min(0.75f, p.AnomDR)));
                    p.Hp = System.Math.Max(0, p.Hp - dealt);
                    if (p.IsAlive()) Body.Hit(p, "Torso", 0f, dealt, "Psi");
                    GameLog.Write($"The wave hits you for {dealt}.");
                    if (p.Hp <= 0) TurnLoop.CheckCombatEnd();
                }
            }
        }
    }
}
