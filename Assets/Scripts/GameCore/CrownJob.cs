// Tier 2, "The Crown" (Yan, 2026-11-19; the second half of the Spool story, STORY_STATUS.md): Spool, spared, plots against the Scrap King. Yan's design: "expose him first, then fight - half his men turn on him, half stay
// loyal, a little group fight, he sends waves of raiders at us until we kill all, then we take his crown and exile him (he's gone for good). Spool keeps the Nest and the Tier 2 trade; Rooke can help in the fight;
// you get a unique perk (reflects 10 % of melee damage back at the attacker); you keep the bat and Spool gives the coat."
//  - Who gives it: Spool, at the Dog Kennels, either after you showed the King the bat as proof of death (`spool` step 4), or - if you sided with Rooke, so the King is already your enemy - straight away.
//  - `crown` steps: 1 the tithe cart (a Garrison escort at the Truck Stop: kill it, E at the Truck Stop for the manifest), 2 take the manifest to Spool, 3 Spool walks with you (the Companion slot) to the Nest,
//    4 E at the King: the scene (half his men turn, the rest do not), 5 the waves (three, smaller for the turned men and again if Rooke is with you) while SPOOL DUELS THE KING (Yan: "we hold off the waves, Spool
//    duels the king himself, he wins after we kill all the waves"; scripted clashes between the two held units), 6 the last wave is dead: Spool's bat comes round, the King goes down on a knee: E: take the crown. Done: `king_exiled`, the perk, Spool at the Nest as the Tier 2 trader (QuartermasterDatabase), the bat and the coat are yours.
// ponytail: the turned men and Rooke's men are not units that fight (the live map's AI fights the player only; one Companion slot, Spool's): they make the waves smaller and the log says so.
using System.Collections.Generic;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class CrownJob
    {
        public const string Mission = "crown", Manifest = "story_tithe_manifest", Cart = "Truck Stop", Nest = "Scrap King's Nest", Kennels = "Dog Kennels", PerkKey = "CrownOfScrap";
        public static readonly int[] WaveSizes = { 6, 7, 8 };
        static string Map2 => MacroMapDatabase.MapOf("scrapyard");
        static bool OnTier2 => Tier2Jobs.OnTier2;
        static int _wave; static float _waveAt = -1f; static readonly List<HumanEnemy> _wave_ = new List<HumanEnemy>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { _wave = 0; _waveAt = -1f; _wave_.Clear(); }

        public static int Step => Story.Step(Mission);
        static LandmarkInstance Mark(string n) => TerrainMap.Landmarks?.FirstOrDefault(l => l.Name == n);

        // ------------------------------------------------------------------ the Crown of Scrap

        /// <summary>The perk's bite: a tenth of the melee damage you take goes back to whoever dealt it (a boss never dies of it: he breaks).</summary>
        public static void Reprisal(ICombatant by, Player victim, int dmg)
        {
            if (by == null || victim == null || dmg <= 0 || !CombatantUtil.HasPerk(victim, PerkKey) || by == victim || !by.IsAlive()) return;
            int back = Mathf.Max(1, Mathf.RoundToInt(dmg * 0.1f));
            by.Hp = Mathf.Max(by is HumanEnemy he && he.Unkillable ? 1 : 0, by.Hp - back);
            GameLog.Write($"The crown bites back: {back} damage to {by.Name}.");
            Boss.OnDamaged(by);
        }

        // ------------------------------------------------------------------ talking

        /// <summary>E near Spool at the Dog Kennels (after he was spared, or when you sided with Rooke): the plan, the manifest, the walk to the Nest.</summary>
        public static bool TryInteract(Player p)
        {
            if (!OnTier2 || Net.Mode == Net.Role.Client) return false;
            if (Story.IsDone(Mission)) return false;
            if (Step == 4 && Story.Near(p, Nest, 5) && Story.Has("crown_walk")) return TalkKing(p);
            if (Step == 6 && Story.Near(p, Nest, 5)) { OfferCrown(p); return true; }
            if (!Story.Near(p, Kennels, 5) || !SpoolFriend) return false;
            int s = Step;
            if (s == 0) { Begin(); return true; }
            if (s == 1) { Story.Talk("Spool", new[] { "The cart stops at the Truck Stop. Garrison soldiers, a sergeant, a manifest in his pack with the crown and the stamp on it. Bring me that page." }); return true; }
            if (s == 2 && Tier1Story.Holder(Manifest) is Player h) { Walk(h); return true; }
            if (s == 2) { Story.Talk("Spool", new[] { "You have the page? No? Then the cart is still out there." }); return true; }
            if (s == 3 || s == 4) { Story.Talk("Spool", new[] { "I am behind you. Go to the Nest." }); return true; }
            return false;
        }

        /// <summary>Spool is a friend: he was spared (the King thinks him dead), or the King is already your enemy (you sided with Rooke).</summary>
        static bool SpoolFriend => Story.Step("spool") == 4 && Story.Has("spool_proof_shown") || Story.Has("rooke_sided") && Story.Step("spool") == 0 || Step >= 1;
        // (spared and the King shown the bat; or the King is already your enemy; or the job is under way)

        static void Begin()
        {
            Story.Talk("Spool", Story.Has("spool_spared")
                ? new[] { "The bat is in your hands and the King thinks I am in the ground. Good. Now the hard part: a ledger is a page, and nobody on the yard will turn on a page.",
                          "They will turn on a cart. Once a week a tenth of the King's crates goes to the Gatehouse on a Garrison wagon, and the wagon stops at the Truck Stop. There is a manifest in the sergeant's pack: the crown, and beside it the Garrison's stamp.",
                          "Take it. Then I walk beside you into his Nest and we let his own men read it." }
                : new[] { "The King's hounds put you out too, I hear. Rooke's coin, was it? Good. Then you will want him gone as badly as I do.",
                          "A ledger is a page, and nobody on the yard will turn on a page. They will turn on a cart. Once a week a tenth of his crates goes to the Gatehouse on a Garrison wagon, and the wagon stops at the Truck Stop. There is a manifest in the sergeant's pack: the crown, and beside it the Garrison's stamp.",
                          "Take it. Then I walk beside you into his Nest and we let his own men read it." });
            Story.SetStep(Mission, 1); if (Story.Step("spool") == 4) Story.Finish("spool");
            Story.SetObjective(Mission, "Take the tithe manifest from the Garrison cart at the Truck Stop", Cart);
            Story.Journal("The Crown: Spool wants proof the Raiders will believe - the tithe manifest from the Garrison wagon at the Truck Stop - and then the King's own men read it at the Nest.");
            var guard = new List<Mutant>();
            for (int i = 0; i < 5; i++) { var (w, a) = EncounterGeneration.PickFactionLoadout("Garrison", i == 0 ? "Professional" : "Experienced", 2, i == 0); guard.Add(new HumanEnemy(i == 0 ? "Tithe Sergeant" : "Tithe Escort", 0, 0, "Garrison", i == 0 ? "Professional" : "Experienced", w, a)); }
            Tier1Story.Place("tithe", Cart, "Garrison", guard);
        }

        static bool EscortAlive() => MacroState.Current.OfflineSquads.Where(q => q.QuestId == "story:tithe").SelectMany(q => q.Members).Any(m => m.IsAlive());

        /// <summary>E at the Truck Stop once the escort is down: the manifest.</summary>
        public static bool TryTruckStop(Player p)
        {
            if (!OnTier2 || Step != 1 || !Story.Near(p, Cart, 4)) return false;
            if (EscortAlive()) return false;
            Story.Talk("The Wagon", new[] { "In the sergeant's pack, under the ration tins: a manifest. A column of crates, each with the Scrap King's crown, each with a Garrison stamp beside it. A tenth of everything, every week." });
            p.AddItem(new InventoryItem { Id = Manifest, Name = "Tithe Manifest", Type = "key", Qty = 1, WeightKg = 0.1f, Price = 0 });
            Story.SetStep(Mission, 2); Story.SetObjective(Mission, "Take the manifest to Spool at the Dog Kennels", Kennels);
            Story.Journal("The Crown: you have the tithe manifest. Take it to Spool.");
            return true;
        }

        static void Walk(Player p)
        {
            Story.Talk("Spool", new[] { "That is the page. Come on. I will be a step behind you, and when his men look at it, you do the talking. I will do the rest." });
            Companion.JoinStory(p, "Spool", 100);
            if (Companion.Current != null)
            {
                var bat = WeaponDatabase.Categories["melee"].TryGetValue("BarbedWireBat", out var wd) ? wd : null;
                if (bat != null) Companion.Current.EquipWeaponFromDb(bat);
                Companion.Current.EquippedArmor = ArmorDatabase.Get("tier2", "BarbwireCoat"); Companion.Current.Unkillable = true;
            }
            Story.Set("crown_walk"); Story.SetStep(Mission, 4);
            Story.SetObjective(Mission, "Show the manifest to the Scrap King at his Nest", Nest);
            Story.Journal("The Crown: Spool walks with you to the Nest.");
            StoryNpcs.Tick();
        }

        // ------------------------------------------------------------------ the Nest

        static bool TalkKing(Player p)
        {
            if (Tier1Story.Holder(Manifest) == null) { Story.Talk("The Scrap King", new[] { "You again. I have nothing to say to someone with empty hands." }); return true; }
            bool rooke = Story.Has("rooke_sided");
            Story.Talk("The Scrap King", new[] { "Spool's friend. I was told you had killed him. I see the dead walk now. What is that you are waving at me?",
                "(You hold up the manifest. The crown, and beside it the Garrison's stamp, a column of crates, a tenth of everything.)",
                "Spool: Read it aloud, anyone. Every tenth crate, every week. The King's peace. You all paid it with your own backs.",
                "Half the men in the yard look at the page. They look at him. Some put their rifles down. Some do not." });
            foreach (var pl in Player.All) pl.RemoveItem(Manifest, 1);
            int turned = Mathf.Clamp(WaveSizes.Length, 1, 3);
            GameLog.Event(rooke ? "Rooke's men come in through the gate and take the wall. \"Half of you. The rest, lay down.\" The rest do not."
                                : "Half the Nest turns on him. The other half does not. \"Get them!\" he roars. \"All of them!\"");
            Story.SetStep(Mission, 5); _wave = 0; _waveAt = Time.time + 2f;
            Story.SetObjective(Mission, "Hold off the King's loyal Raiders while Spool fights the King", Nest);
            Story.Journal("The Crown: the Nest split. Spool faces the King one on one; you hold off the King's loyal Raiders until it is over.");
            Companion.Dismiss(); StoryNpcs.Tick(); SetupDuel();
            NextWave();
            return true;
        }

        static int WaveCount(int w) => Mathf.Max(2, WaveSizes[w] / 2 - (Story.Has("rooke_sided") ? 1 : 0) + Mathf.Max(0, Player.All.Count - 1));

        static void NextWave()
        {
            if (_wave >= WaveSizes.Length) return;
            var n = Mark(Nest); if (n == null) { _wave++; return; }
            int count = WaveCount(_wave); _wave_.Clear();
            for (int i = 0; i < count; i++)
            {
                var (w, a) = EncounterGeneration.PickFactionLoadout("Raiders", _wave == 2 && i == 0 ? "Professional" : "Experienced", 2, _wave == 2 && i == 0);
                var (x, y) = TerrainMap.FindNearestEnterable(n.X + (i % 5) * 2 - 4, n.Y - 6 - _wave * 2 - i / 5);
                var h = new HumanEnemy(_wave == 2 && i == 0 ? "The King's Champion" : "Loyal Raider", x, y, "Raiders", _wave == 2 && i == 0 ? "Professional" : "Experienced", w, a) { IsAggroed = true };
                EncounterState.Enemies.Add(h); _wave_.Add(h);
            }
            EncounterState.InCombat = true; _wave++;
            GameLog.Event(_wave == 1 ? "The first of the King's loyal Raiders come at you." : _wave == 2 ? "More of them. The Nest empties of its loyalists." : "The last wave, with the King's champion at its head.");
        }

        // ------------------------------------------------------------------ the duel (scripted: the live AI fights the player only)

        static HumanEnemy KingUnit => StoryNpcs.Find("The Scrap King");
        static HumanEnemy SpoolUnit => StoryNpcs.Find("Spool");
        static float _clashAt; static int _clash;

        static void SetupDuel()
        {
            var k = KingUnit; var sp = SpoolUnit;
            if (WeaponDatabase.Categories["melee"].TryGetValue("Sledgehammer", out var sw) && k != null) k.EquipWeaponFromDb(sw);
            if (WeaponDatabase.Categories["melee"].TryGetValue("BarbedWireBat", out var bw) && sp != null) sp.EquipWeaponFromDb(bw);
            if (k != null && sp != null) { k.FaceToward(sp.X, sp.Y); sp.FaceToward(k.X, k.Y); Banter.Say(sp, "He is mine. You keep the rest off my back.", 1, 0.5f); Banter.Say(k, "Spool. I should have buried you myself.", 1, 3.5f); }
            Boss.NameCard("SPOOL AND THE SCRAP KING", "HOLD OFF THE WAVES");
            var n = Mark(Nest); DuelStart(k, sp, n?.X ?? (k?.X ?? 0), n?.Y ?? (k?.Y ?? 0), Time.time);
        }

        // The duel (Yan, 2026-11-19: "faster; they can be hitting each other, the king using the normal sledge moveset and Spool his boss moveset, two units with technically infinite hp, moving around like actual NPCs fighting"):
        // two scripted duelists run the boss framework's beats on each other - a move from their own Boss.Def (the King's Smash/Sweep/Crush, Spool's Swing/Backswing/Overhead, phase 2 from the second wave), a wind-up,
        // the glint, the swing, the hit through Melee.HitUnit - or a parry (the guard pose, sparks) about 4 times in 10; between moves they close in, back off, sidestep inside the Nest. Hp 1,000,000 each: the hits are real, the fight never ends by itself.
        public static int Hits, Blocks, Clashes; public static bool LastByKing;
        class Duelist
        {
            public HumanEnemy U; public Boss.Def Def; public Boss.Move Doing; public int HitsLeft; public float StrikeAt, NextMoveAt, NextStep, OpenUntil, GlintDue = -1f; public bool Swinging;
        }
        static Duelist _dk, _ds; static int _cx, _cy; static bool _p2; static readonly System.Random Rng = new System.Random(7);
        const float DuelSpeed = 0.6f, DuelPace = 0.75f, BlockChance = 0.4f, ArenaR = 4f;

        static Duelist Mk(HumanEnemy u, Boss.Def def, float now) => new Duelist { U = u, Def = def, NextMoveAt = now + 0.4f };

        /// <summary>The duel starts: the arena is the tiles within a few of (cx, cy); both have all the health there is.</summary>
        internal static void DuelStart(HumanEnemy k, HumanEnemy sp, int cx, int cy, float now)
        {
            _cx = cx; _cy = cy; _p2 = false; Hits = Blocks = Clashes = 0;
            if (k != null) { k.MaxHp = k.Hp = 1000000; _dk = Mk(k, Boss.King, now + 0.8f); }
            if (sp != null) { sp.MaxHp = sp.Hp = 1000000; _ds = Mk(sp, Boss.Spool, now + 0.2f); }
        }
        internal static void DuelWaves(int w) { _wave = w; }          // the preview: which wave the player is on (phase 2 moves from the second)

        static bool Free(HumanEnemy u, int x, int y) => TerrainMap.CanEnterTile(x, y) && TerrainMap.UnitAt(x, y, u) == null && !Player.All.Any(p => p.X == x && p.Y == y)
            && Mathf.Max(Mathf.Abs(x - _cx), Mathf.Abs(y - _cy)) <= ArenaR;

        /// <summary>One tile toward (away from, sideways to) the foe, inside the arena.</summary>
        static void StepBy(HumanEnemy u, HumanEnemy foe, int mode)
        {
            int best = -1; float bs = mode == 0 ? 1e9f : -1e9f; int bx = u.X, by = u.Y;
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; int x = u.X + dx, y = u.Y + dy; if (!Free(u, x, y)) continue;
                float d = Mathf.Sqrt((x - foe.X) * (x - foe.X) + (y - foe.Y) * (y - foe.Y)); float score = mode == 0 ? d : mode == 1 ? d : -Mathf.Abs(d - Mathf.Sqrt((u.X - foe.X) * (u.X - foe.X) + (u.Y - foe.Y) * (u.Y - foe.Y)));
                if (mode == 0 ? score < bs : score > bs) { bs = score; bx = x; by = y; best = 1; }
            }
            if (best > 0) { u.X = bx; u.Y = by; }
        }

        static void StepInTo(HumanEnemy u, HumanEnemy foe, float gap)
        {
            var (hx, hy) = Ballistics.Pos(u); var (px, py) = Ballistics.Pos(foe);
            float dx = hx - px, dy = hy - py, d = Mathf.Sqrt(dx * dx + dy * dy); if (d <= gap + 0.05f || d < 0.01f) return;
            float tx = px + dx / d * gap, ty = py + dy / d * gap; int x = Mathf.FloorToInt(tx), y = Mathf.FloorToInt(ty);
            if (!(x == foe.X && y == foe.Y) && Free(u, x, y)) { u.X = x; u.Y = y; Rt.SetOffset(u, tx - x - 0.5f, ty - y - 0.5f); }
        }

        static void Run(Duelist d, Duelist o, float now)
        {
            var u = d.U; var foe = o.U; if (u == null || foe == null || !u.IsAlive()) return;
            var mc = Melee.ChargeOf(u); mc.PoseKey = null; mc.Pace = DuelPace;
            u.FaceToward(foe.X, foe.Y);
            if (d.Doing != null && mc.SwingFrom >= 0f && !d.Swinging && now >= d.StrikeAt - HeldPoseMath.ChopTime / DuelPace)
            { float held = now - mc.SwingFrom; mc.SwingFrom = -1f; mc.SwungAt = now - Mathf.Min(held, HeldPoseMath.WindUp) / DuelPace; d.Swinging = true; }
            if (d.Doing != null && d.GlintDue >= 0f && now >= d.GlintDue) { d.GlintDue = -1f; var g = CombatFx.Push("glint", u, null, u.EquippedWeapon); if (g != null) { g.Now = true; g.Psi = d.Doing.Danger; } }
            int dist = CombatantUtil.Distance(u, foe);
            if (d.Doing == null)
            {
                if (now < d.OpenUntil) return;
                if (now >= d.NextStep)
                {
                    d.NextStep = now + d.Def.StepSeconds * DuelSpeed * 1.2f;
                    if (dist > 1) StepBy(u, foe, 0);
                    else if (Rng.NextDouble() < 0.25 && now >= d.NextMoveAt) StepBy(u, foe, 2);       // a sidestep
                }
                if (now < d.NextMoveAt) return;
                var opts = d.Def.Moves.Where(m => (!m.Phase2Only || _p2) && (m.Lunge > 0f ? dist >= 2 && dist <= m.Lunge : dist <= 1)).ToList();
                if (opts.Count == 0) { if (dist > 1) StepBy(u, foe, 0); return; }
                int roll = Rng.Next(0, opts.Sum(m => m.Weight)); Boss.Move pick = opts[0];
                foreach (var m in opts) { if (roll < m.Weight) { pick = m; break; } roll -= m.Weight; }
                d.Doing = pick; d.HitsLeft = pick.Hits; d.StrikeAt = now + pick.Wind * DuelSpeed; d.GlintDue = Mathf.Max(now, d.StrikeAt - Boss.GlintLead * DuelSpeed); d.Swinging = false;
                mc.Move = pick.Poses[(pick.Hits - d.HitsLeft) % pick.Poses.Length]; mc.From = -1f; mc.SwingFrom = now; mc.Red = pick.Danger;
                return;
            }
            if (now < d.StrikeAt) return;
            // the hit lands: a lunge rushes in first; the foe parries or takes it
            if (d.Doing.Lunge > 0f) StepInTo(u, foe, 0.8f); else StepInTo(u, foe, 0.9f);
            Clashes++; LastByKing = d == _dk;
            if (Rng.NextDouble() < BlockChance && !d.Doing.Danger)
            {
                Blocks++; Melee.ChargeOf(foe).GuardAt = now; var sp = CombatFx.Push("glint", foe, null, foe.EquippedWeapon); if (sp != null) sp.Now = true;
                var an = CombatFx.Push("melee", u, foe, u.EquippedWeapon); if (an != null) an.Now = true;
            }
            else { Hits++; if (u.EquippedWeapon != null) Melee.HitUnit(u, u.EquippedWeapon, foe, d.Doing.Mult); }
            d.Swinging = false;
            if (--d.HitsLeft > 0) { d.StrikeAt = now + d.Doing.Gap * DuelSpeed; d.GlintDue = Mathf.Max(now, d.StrikeAt - Boss.GlintLead * DuelSpeed); mc.Move = d.Doing.Poses[(d.Doing.Hits - d.HitsLeft) % d.Doing.Poses.Length]; mc.From = mc.Since(now); mc.SwingFrom = now; return; }
            d.OpenUntil = now + d.Doing.Open * DuelSpeed * 0.7f; d.Doing = null; d.NextMoveAt = d.OpenUntil + 0.2f * (float)(0.8 + 0.5 * Rng.NextDouble());
            if (Rng.NextDouble() < 0.35) { for (int s = 0; s < 2; s++) StepBy(u, foe, 1); }          // back off a little
        }

        /// <summary>One step of the duel at time `now` (the Tick, and the harness's preview).</summary>
        internal static void DuelTick(HumanEnemy k, HumanEnemy sp, float now)
        {
            if (k == null || sp == null) return;
            if (_dk == null || _dk.U != k || _ds == null || _ds.U != sp) DuelStart(k, sp, k.X - 1, k.Y, now);
            _p2 = _wave >= 2;
            Run(_dk, _ds, now); Run(_ds, _dk, now);
        }

        /// <summary>Spool's last blow: the King's guard breaks (the kneel pose) and his weapon sinks for good.</summary>
        internal static void DuelFinish(HumanEnemy k, HumanEnemy sp, float now)
        {
            _dk = _ds = null;
            if (k == null || sp == null) return;
            k.MaxHp = k.Hp = 300; sp.MaxHp = sp.Hp = 300;
            StepInTo(sp, k, 0.9f); sp.FaceToward(k.X, k.Y); k.FaceToward(sp.X, sp.Y);
            var m = Melee.ChargeOf(sp); m.PoseKey = null; m.Pace = DuelPace; m.Move = 2; m.From = -1f; m.SwingFrom = -1f; m.SwungAt = now;
            var kc = Melee.ChargeOf(k); kc.BrokenAt = now; kc.OpenFrom = now + 0.25f; kc.OpenUntil = now + 3600f;
            var anim = CombatFx.Push("melee", sp, k, sp.EquippedWeapon); if (anim != null) anim.Now = true;
            var st = CombatFx.Push("stagger", k, null, null); if (st != null) st.Now = true;
        }

        /// <summary>Every frame (Rt.WorldTick): the waves and the duel, then Spool's last blow.</summary>
        public static void Tick(float dt)
        {
            if (Step != 5 || !OnTier2 || Net.Mode == Net.Role.Client) return;
            DuelTick(KingUnit, SpoolUnit, Time.time);
            if (_wave_.Count > 0 && _wave_.Any(h => h.IsAlive())) return;
            if (_wave < WaveSizes.Length) { if (Time.time >= _waveAt) { NextWave(); _waveAt = Time.time + 4f; } return; }
            var k = KingUnit; var sp = SpoolUnit;
            DuelFinish(k, sp, Time.time); Boss.HitStop(0.35f);
            GameLog.Event("The last of the loyal Raiders falls. Behind you, Spool's bat comes round with all the wire in it, and the Scrap King goes down on a knee in his own yard.");
            Banter.Say(SpoolUnit, "That is for the yard.", 1, 0.4f);
            Story.SetStep(Mission, 6); _wave_.Clear();
            Story.SetObjective(Mission, "Take the Scrap King's crown", Nest);
        }

        /// <summary>The King is on a knee (E at the Nest): take the crown, and he is gone for good.</summary>
        public static void OfferCrown(Player p)
        {
            Story.Talk("The Scrap King", new[] { "(On a knee in his own yard, the hammer out of reach, Spool's bat at his neck.) Take it. It was only ever scrap and nerve. ...Put it on, then. See how heavy it is." }, new[] { "(Take the crown. He goes, and does not come back.)" }, _ => Exile(p), KingUnit);
        }

        internal static void Exile(Player p)
        {
            Story.Set("king_exiled"); Story.Set("king_done"); Story.Finish(Mission);
            GameLog.Event("The Scrap King takes the road south on foot, with nothing, and the yard watches him go. He will not be back.");
            foreach (var pl in Player.All) { pl.TakePerk(PerkKey); }
            GiveRewards(p);
            Companion.Dismiss(); StoryNpcs.Tick();
            foreach (var pl in Player.All) { pl.AdjustReputation("Raiders", Player.RepPoint); pl.AdjustReputation("Outcasts", 2 * Player.RepPoint); }
            Story.Journal("The Crown: the Scrap King is exiled. Spool holds the Nest and keeps the trade. You took the crown of scrap: a tenth of the melee damage you take goes back to whoever dealt it.");
        }

        /// <summary>The bat (if you do not carry it yet: the Rooke road, or Spool wore it) and the coat, from Spool's hands.</summary>
        static void GiveRewards(Player p)
        {
            Story.Talk("Spool", new[] { "(He turns the crown over in his hands once, and gives it to you.) Keep it. I want the yard, not the hat.",
                "The Nest is mine now, and the trade with it. Raiders will still sell to you. I will be fairer than he was, or at least quieter.",
                "The bat is yours, and so is the coat. Wear the wire." });
            bool hasBat = p.Inventory.Items.Any(i => i.Key == "BarbedWireBat") || p.EquippedWeapon?.Db?.Name == "Barbed Wire Bat";
            if (!hasBat) { var d = ShopDatabase.Items.FirstOrDefault(i => i.Type == "weapon" && i.Category == "melee" && i.Key == "BarbedWireBat"); if (d != null) { var it = TestingGround.ItemOf(d); it.Condition = ItemCondition.Working; p.AddItem(it); } }
            if (!p.Inventory.Items.Any(i => i.Key == "BarbwireCoat"))
                p.AddItem(new InventoryItem { Id = "loot_armor_barbwire", Name = "Barbwire Coat", Type = "armor", Category = "tier2", Key = "BarbwireCoat", WeightKg = 6f, Qty = 1, Price = 1500, Condition = ItemCondition.Working });
        }
    }
}
