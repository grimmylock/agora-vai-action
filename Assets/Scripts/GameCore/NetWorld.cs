// Co-op: the rest of the world for a joined friend (Yan, 2026-11-08: "sync ordinary enemies to clients; damage numbers and log lines; bullet-time and hit-stop and the shot tracers"). The host runs everything; a client runs no world, so
// every snapshot (Net.EncodeSnapshot, after the players and the eel) also carries, in this order and each within what is left of the packet (an unreliable message must stay under about 1300 bytes):
//   - the clock: bullet time on or off and the hit-stop left (the client sets its own time scale to match)
//   - log lines: the last few of GameLog (what the host would read in the corner), numbered, resent for 1.5 s so a lost packet loses nothing
//   - damage numbers (the Testing Ground's), numbered the same way
//   - effects: shots (muzzle flash, tracer, impact, the cover's reaction is left out), melee, strikes, reloads and the ready-made strips (CombatFx.PxAt), numbered, resent for 0.3 s
//   - enemies: the nearest ones first, 16 bytes each: an id, what it is as INDICES into the databases (the same build has the same databases: mutant = tier+key, human = faction, rank, weapon, armor), where, hit points and whether it is awake.
// The client keeps a mirror of each: a real Mutant / HumanEnemy built from those indices, moved every snapshot, never ticked (a client runs no world). Not carried: what an enemy is doing (aiming, reloading, its "!" and its radio),
// the boss's own units (the eel has its own block, the brain boss none yet), loot on the dead, anything an enemy carries besides its first weapon and its suit.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameData;
using UnityEngine;

namespace GameCore
{
    public static class NetWorld
    {
        public const int Packet = 1280;                    // the most one snapshot may hold
        // ------------------------------------------------------------------ the tables (indices into the databases)
        static List<(string tier, string key)> _mut; static Dictionary<MutantDef, int> _mutIdx;
        static List<(string cat, string key)> _wep; static Dictionary<WeaponDef, int> _wepIdx;
        static List<(string tier, string key)> _arm; static Dictionary<ArmorDef, int> _armIdx;
        static List<string> _fac;
        static void Tables()
        {
            if (_mut != null) return;
            _mut = new List<(string, string)>(); _mutIdx = new Dictionary<MutantDef, int>();
            foreach (var t in MutantDatabase.Tiers) foreach (var m in t.Value) { _mutIdx[m.Value] = _mut.Count; _mut.Add((t.Key, m.Key)); }
            _wep = new List<(string, string)>(); _wepIdx = new Dictionary<WeaponDef, int>();
            foreach (var c in WeaponDatabase.Categories) foreach (var w in c.Value) { _wepIdx[w.Value] = _wep.Count; _wep.Add((c.Key, w.Key)); }
            _arm = new List<(string, string)>(); _armIdx = new Dictionary<ArmorDef, int>();
            foreach (var t in ArmorDatabase.Tiers) foreach (var a in t.Value) { _armIdx[a.Value] = _arm.Count; _arm.Add((t.Key, a.Key)); }
            _fac = FactionDatabase.Factions.Keys.ToList();
        }
        static int Idx<T>(Dictionary<T, int> d, T k) => k != null && d.TryGetValue(k, out var i) ? i : 0xFFFF;

        // ------------------------------------------------------------------ host: ids and taps
        static readonly Dictionary<ICombatant, int> Ids = new Dictionary<ICombatant, int>(); static int _nextId = 1;
        static int IdOf(ICombatant u) { if (!Ids.TryGetValue(u, out var i)) Ids[u] = i = _nextId++ & 0xFFFF; return i; }
        public static void ResetHost() { Ids.Clear(); _nextId = 1; Logs.Clear(); Nums.Clear(); Fx.Clear(); _seq = 0; }

        static int _seq;
        class Tapped<T> { public int Seq; public float At; public T V; }
        static readonly List<Tapped<string>> Logs = new List<Tapped<string>>();
        static readonly List<Tapped<(ICombatant who, string text, int kind)>> Nums = new List<Tapped<(ICombatant, string, int)>>();
        static readonly List<Tapped<FxEvent>> Fx = new List<Tapped<FxEvent>>();
        static bool Hosting => Net.Mode == Net.Role.Host;
        /// <summary>GameLog.Write (the host's lines go to the friends too).</summary>
        public static void TapLog(string line) { if (!Hosting || string.IsNullOrEmpty(line)) return; Logs.Add(new Tapped<string> { Seq = ++_seq, At = Time.time, V = line.Length > 90 ? line.Substring(0, 90) : line }); if (Logs.Count > 20) Logs.RemoveAt(0); }
        /// <summary>TestingGround's damage numbers.</summary>
        public static void TapNumber(ICombatant who, string text, int kind) { if (!Hosting) return; Nums.Add(new Tapped<(ICombatant, string, int)> { Seq = ++_seq, At = Time.time, V = (who, text, kind) }); if (Nums.Count > 20) Nums.RemoveAt(0); }
        /// <summary>CombatFx.Push / Add: the event itself (its fields are filled in after it is pushed, so it is only read when the snapshot is made).</summary>
        public static void TapFx(FxEvent e) { if (!Hosting || e == null) return; Fx.Add(new Tapped<FxEvent> { Seq = ++_seq, At = Time.time, V = e }); if (Fx.Count > 40) Fx.RemoveAt(0); }

        // ------------------------------------------------------------------ the wire
        const int Shot = 1, Melee = 2, Strike = 3, Reload = 4, Impact = 5, Px = 6;
        static int KindCode(string k) => k == "shot" ? Shot : k == "melee" ? Melee : k == "strike" ? Strike : k == "reload" ? Reload : k == "impact" ? Impact : k == "px" ? Px : 0;
        static string KindName(int c) => c == Shot ? "shot" : c == Melee ? "melee" : c == Strike ? "strike" : c == Reload ? "reload" : c == Impact ? "impact" : c == Px ? "px" : null;
        static void WriteRef(BinaryWriter w, ICombatant u)
        {
            if (u is Player p) { w.Write((byte)1); w.Write(Net.OwnerOf(p)); }
            else if (u != null && Ids.ContainsKey(u)) { w.Write((byte)2); w.Write((ulong)Ids[u]); }
            else { w.Write((byte)0); w.Write(0UL); }
        }

        /// <summary>The host's part of a snapshot. `used` = bytes already written (the players and the eel).</summary>
        public static void Write(BinaryWriter w, int used)
        {
            Tables(); float now = Time.time; w.Flush();
            var near = Enemies().ToList(); foreach (var e in near) IdOf(e);                           // every enemy has an id before anything names one
            w.Write(Rt.SlowMo); w.Write(Boss.StopLeft);
            // log lines, numbers: the last second and a half, at most 3 and 4
            int Room() { w.Flush(); return Packet - (int)w.BaseStream.Length - 6; }                         // what is left; each section takes only what its share allows, the enemies get the rest
            int nLog = Room() > 700 ? 3 : Room() > 450 ? 1 : 0;
            var logs = Logs.Where(l => now - l.At < 1.5f).ToList(); if (logs.Count > nLog) logs = logs.Skip(logs.Count - nLog).ToList(); w.Write((byte)logs.Count);
            foreach (var l in logs) { w.Write(l.Seq); w.Write(l.V); }
            int nNum = Room() > 500 ? 4 : Room() > 300 ? 2 : 0;
            var nums = Nums.Where(n => now - n.At < 1.5f && n.V.who != null).ToList(); if (nums.Count > nNum) nums = nums.Skip(nums.Count - nNum).ToList(); w.Write((byte)nums.Count);
            foreach (var n in nums) { w.Write(n.Seq); WriteRef(w, n.V.who); w.Write((byte)n.V.kind); w.Write(n.V.text.Length > 24 ? n.V.text.Substring(0, 24) : n.V.text); }
            // the enemies come first in id terms (an effect may name one): give every enemy in the list an id before the effects are written
            Fx.RemoveAll(f => now - f.At > 0.6f);
            var fx = Fx.Where(f => now - f.At < 0.3f && KindCode(f.V.Kind) > 0 && !(f.V.Kind != "px" && f.V.Actor == null)).ToList(); int nFx = System.Math.Max(0, System.Math.Min(8, (Room() - 150) / 42)); if (fx.Count > nFx) fx = fx.Skip(fx.Count - nFx).ToList();
            w.Write((byte)fx.Count);
            foreach (var f in fx)
            {
                var e = f.V; w.Write(f.Seq); w.Write((byte)KindCode(e.Kind)); WriteRef(w, e.Actor); WriteRef(w, e.Target);
                var wd = e.Weapon?.Db ?? (e.Actor as HumanEnemy)?.EquippedWeapon?.Db ?? (e.Actor as Player)?.EquippedWeapon?.Db; w.Write((ushort)Idx(_wepIdx, wd));
                int fl = (e.Hit ? 1 : 0) | (e.Jammed ? 2 : 0) | (e.NoLos ? 4 : 0) | (e.Psi ? 8 : 0) | (e.Killed ? 16 : 0) | (e.Pellet ? 32 : 0) | (e.Flying ? 64 : 0) | (e.Landed ? 128 : 0); w.Write((byte)fl);
                w.Write((byte)e.Rounds); w.Write((short)e.FromX); w.Write((short)e.FromY); w.Write((short)e.ToX); w.Write((short)e.ToY); w.Write(e.EndX); w.Write(e.EndY); w.Write((short)System.Math.Min(e.Overkill, 30000)); w.Write(e.Seconds);
                if (e.Kind == "px") w.Write(e.Px ?? "");
            }
            // the enemies, nearest first, as many as fit
            w.Flush(); int budget = Packet - (int)w.BaseStream.Length - 4; int cap = System.Math.Max(0, System.Math.Min(60, budget / 16)); var send = near.Take(cap).ToList(); w.Write((byte)send.Count);
            foreach (var e in send)
            {
                bool human = e is HumanEnemy; var h = e as HumanEnemy; int a = human ? Idx(_wepIdx, h.EquippedWeapon?.Db) : Idx(_mutIdx, e.Db), b = human ? Idx(_armIdx, h.EquippedArmor) : 0xFFFF;
                int fr = human ? ((System.Math.Max(0, _fac.IndexOf(h.FactionName)) << 3) | System.Math.Max(0, CombatRules.RankIndex(h.Rank))) : 0;
                var (ox, oy) = Rt.Offset(e); bool alive = e.IsAlive() && e.Hp > 0;
                w.Write((ushort)IdOf(e)); w.Write((byte)((human ? 1 : 0) | (alive ? 2 : 0) | (e.IsAggroed ? 4 : 0))); w.Write((ushort)a); w.Write((ushort)b); w.Write((byte)fr);
                w.Write((short)e.X); w.Write((short)e.Y); w.Write((sbyte)Mathf.Clamp(Mathf.RoundToInt(ox * 127f), -127, 127)); w.Write((sbyte)Mathf.Clamp(Mathf.RoundToInt(oy * 127f), -127, 127)); w.Write((ushort)Mathf.Clamp(e.Hp, 0, 65535));
            }
            WriteBrain(w);
            // forget the ids of enemies that are gone
            foreach (var gone in Ids.Keys.Where(k => !(k is Player) && !EncounterState.Enemies.Contains(k as Mutant)).ToList()) Ids.Remove(gone);
        }
        // ------------------------------------------------------------------ the brain boss (Boss 2): its vat's state and every effect the view draws
        static void WriteBrain(BinaryWriter w)
        {
            var f = BrainBoss.Current; w.Flush(); bool has = f != null; w.Write(has); if (!has) return;
            float now = Time.time; float wind = f.Moves.Count == 0 ? 0f : Mathf.Max(0f, f.Moves.Max(m => m.WindUntil) - now);
            w.Write((byte)((f.Started ? 1 : 0) | (f.Drained ? 2 : 0) | (f.Gone ? 4 : 0) | (BrainBoss.Pulse != null ? 8 : 0) | (f.PulseAt >= 0f ? 16 : 0))); w.Write((byte)f.Phase); w.Write(f.Stage ?? ""); w.Write(now - f.StageAt); w.Write(f.Unit.Hp);
            w.Write(f.Doing ?? ""); w.Write(wind); w.Write(BrainBoss.Pulse?.R ?? 0f);
            var beams = BrainBoss.Beams.Take(4).ToList(); w.Write((byte)beams.Count); foreach (var b in beams) { w.Write(b.Angle); w.Write(b.Length); }
            int room = Packet - (int)w.BaseStream.Length - 4; int nOrb = System.Math.Max(0, System.Math.Min(24, room / 14)); var orbs = BrainBoss.Orbs.Take(nOrb).ToList(); w.Write((byte)orbs.Count);
            foreach (var o in orbs) { w.Write(o.X); w.Write(o.Y); w.Write((sbyte)Mathf.Clamp(Mathf.RoundToInt(o.Dx * 100f), -100, 100)); w.Write((sbyte)Mathf.Clamp(Mathf.RoundToInt(o.Dy * 100f), -100, 100)); w.Write((byte)(o.Kind == "bolt" ? 1 : o.Kind == "void" ? 2 : 0)); w.Write((byte)Mathf.Clamp(Mathf.RoundToInt(o.Age * 50f), 0, 255)); }
            w.Flush(); room = Packet - (int)w.BaseStream.Length - 2; var throws = BrainBoss.Throws.Take(System.Math.Max(0, System.Math.Min(6, room / 24))).ToList(); w.Write((byte)throws.Count);
            foreach (var t in throws) { w.Write(t.Look ?? ""); w.Write(t.X); w.Write(t.Y); w.Write(t.Flying); w.Write(now - t.Born); }
        }
        public class BrainIn { public bool Has, Started, Drained, Gone, Pulse, PulseAt; public int Phase, Hp; public string Stage, Doing; public float StageAge, Wind, PulseR; public List<(float a, float l)> Beams = new List<(float, float)>(); public List<(float x, float y, float dx, float dy, int kind, float age)> Orbs = new List<(float, float, float, float, int, float)>(); public List<(string look, float x, float y, bool flying, float age)> Throws = new List<(string, float, float, bool, float)>(); }
        public static BrainIn ReadBrain(BinaryReader r)
        {
            var b = new BrainIn { Has = r.ReadBoolean() }; if (!b.Has) return b;
            int fl = r.ReadByte(); b.Started = (fl & 1) != 0; b.Drained = (fl & 2) != 0; b.Gone = (fl & 4) != 0; b.Pulse = (fl & 8) != 0; b.PulseAt = (fl & 16) != 0; b.Phase = r.ReadByte(); b.Stage = r.ReadString(); b.StageAge = r.ReadSingle(); b.Hp = r.ReadInt32();
            b.Doing = r.ReadString(); b.Wind = r.ReadSingle(); b.PulseR = r.ReadSingle();
            int n = r.ReadByte(); for (int i = 0; i < n; i++) b.Beams.Add((r.ReadSingle(), r.ReadSingle()));
            n = r.ReadByte(); for (int i = 0; i < n; i++) { float x = r.ReadSingle(), y = r.ReadSingle(); float dx = r.ReadSByte() / 100f, dy = r.ReadSByte() / 100f; int kind = r.ReadByte(); b.Orbs.Add((x, y, dx, dy, kind, r.ReadByte() / 50f)); }
            n = r.ReadByte(); for (int i = 0; i < n; i++) b.Throws.Add((r.ReadString(), r.ReadSingle(), r.ReadSingle(), r.ReadBoolean(), r.ReadSingle()));
            return b;
        }
        /// <summary>On a client: the dormant brain fight (spawned when the client entered the map) is made to look like the host's; the view reads these same statics.</summary>
        public static void ApplyBrain(BrainIn b)
        {
            var f = BrainBoss.Current; if (b != null && !b.Has && f != null && !f.Gone) { f.Gone = true; EncounterState.Enemies.Remove(f.Unit); return; }       // the host has none (it escaped, or it is not here): the client's dormant copy goes
            if (b == null || !b.Has || f == null) return; float now = Time.time;
            f.Started = b.Started; f.Drained = b.Drained; f.Phase = b.Phase; f.Stage = string.IsNullOrEmpty(b.Stage) ? null : b.Stage; f.StageAt = now - b.StageAge; f.Unit.Hp = b.Hp; f.Unit.IsAggroed = b.Started;
            if (b.Gone && !f.Gone) { f.Gone = true; EncounterState.Enemies.Remove(f.Unit); }
            f.Moves.Clear(); if (!string.IsNullOrEmpty(b.Doing)) f.Moves.Add(new BrainBoss.Move { Kind = b.Doing, WindUntil = now + b.Wind });
            f.PulseAt = b.PulseAt ? now : -1f;
            BrainBoss.Pulse = b.Pulse ? (BrainBoss.Pulse ?? new BrainBoss.Wave()) : null; if (BrainBoss.Pulse != null) BrainBoss.Pulse.R = b.PulseR;
            BrainBoss.Beams.Clear(); foreach (var (a, l) in b.Beams) BrainBoss.Beams.Add(new BrainBoss.Beam { Angle = a, Length = l, Until = now + 1f });
            BrainBoss.Orbs.Clear(); foreach (var o in b.Orbs) BrainBoss.Orbs.Add(new BrainBoss.Orb { X = o.x, Y = o.y, Dx = o.dx, Dy = o.dy, Kind = o.kind == 1 ? "bolt" : o.kind == 2 ? "void" : "ring", Age = o.age });
            BrainBoss.Throws.Clear(); foreach (var t in b.Throws) BrainBoss.Throws.Add(new BrainBoss.Thrown { Look = t.look, X = t.x, Y = t.y, Flying = t.flying, Born = now - t.age });
        }
        static IEnumerable<Mutant> Enemies()
        {
            Tables(); var ps = Player.All.Where(p => p.IsAlive()).ToList(); if (ps.Count == 0) ps = Player.All.ToList();
            return EncounterState.Enemies.Where(e => e != null && !BrainBoss.Is(e) && !EelBoss.Is(e) && !(EelBoss.Current?.Body == e) && (e is HumanEnemy || e.Db != null && _mutIdx.ContainsKey(e.Db)))
                .OrderBy(e => ps.Count == 0 ? 0 : ps.Min(p => (p.X - e.X) * (p.X - e.X) + (p.Y - e.Y) * (p.Y - e.Y)));
        }

        // ------------------------------------------------------------------ the client
        public class Entry { public int Id, A, B, Fr, X, Y, Hp; public bool Human, Alive, Aggro; public float Ox, Oy; }
        public class FxIn { public int Seq, Kind, Weapon, Rounds, FromX, FromY, ToX, ToY, Overkill; public byte ActorT, TargetT; public ulong Actor, Target; public int Flags; public float EndX, EndY, Seconds; public string Px; }
        public class WorldIn { public BrainIn Brain; public bool Slow; public float Stop; public List<(int seq, string text)> Logs = new List<(int, string)>(); public List<(int seq, byte t, ulong who, int kind, string text)> Nums = new List<(int, byte, ulong, int, string)>(); public List<FxIn> Fx = new List<FxIn>(); public List<Entry> Enemies = new List<Entry>(); }
        public static WorldIn Read(BinaryReader r)
        {
            var o = new WorldIn { Slow = r.ReadBoolean(), Stop = r.ReadSingle() };
            int n = r.ReadByte(); for (int i = 0; i < n; i++) o.Logs.Add((r.ReadInt32(), r.ReadString()));
            n = r.ReadByte(); for (int i = 0; i < n; i++) { int seq = r.ReadInt32(); byte t = r.ReadByte(); ulong who = r.ReadUInt64(); int kind = r.ReadByte(); o.Nums.Add((seq, t, who, kind, r.ReadString())); }
            n = r.ReadByte();
            for (int i = 0; i < n; i++)
            {
                var f = new FxIn { Seq = r.ReadInt32(), Kind = r.ReadByte(), ActorT = r.ReadByte(), Actor = r.ReadUInt64(), TargetT = r.ReadByte(), Target = r.ReadUInt64(), Weapon = r.ReadUInt16(), Flags = r.ReadByte(), Rounds = r.ReadByte() };
                f.FromX = r.ReadInt16(); f.FromY = r.ReadInt16(); f.ToX = r.ReadInt16(); f.ToY = r.ReadInt16(); f.EndX = r.ReadSingle(); f.EndY = r.ReadSingle(); f.Overkill = r.ReadInt16(); f.Seconds = r.ReadSingle();
                if (f.Kind == Px) f.Px = r.ReadString(); o.Fx.Add(f);
            }
            n = r.ReadByte();
            for (int i = 0; i < n; i++)
            {
                var e = new Entry { Id = r.ReadUInt16() }; int fl = r.ReadByte(); e.Human = (fl & 1) != 0; e.Alive = (fl & 2) != 0; e.Aggro = (fl & 4) != 0; e.A = r.ReadUInt16(); e.B = r.ReadUInt16(); e.Fr = r.ReadByte();
                e.X = r.ReadInt16(); e.Y = r.ReadInt16(); e.Ox = r.ReadSByte() / 127f; e.Oy = r.ReadSByte() / 127f; e.Hp = r.ReadUInt16(); o.Enemies.Add(e);
            }
            o.Brain = ReadBrain(r);
            return o;
        }

        static readonly Dictionary<int, Mutant> Mirror = new Dictionary<int, Mutant>(); static int _seenLog, _seenNum, _seenFx;
        public static void ResetClient() { Mirror.Clear(); _seenLog = _seenNum = _seenFx = 0; }
        static ICombatant Resolve(byte t, ulong id) => t == 1 ? Net.PlayerOf(id) : t == 2 && Mirror.TryGetValue((int)id, out var m) ? m : null;

        /// <summary>On a client: the clock, the lines, the numbers, the effects and the enemies of the host's snapshot.</summary>
        public static void Apply(WorldIn o)
        {
            if (o == null) return; Tables();
            if (o.Slow != Rt.SlowMo) Rt.SetSlow(o.Slow);                                              // the host's bullet time is everyone's
            if (o.Stop > 0.02f) Boss.HitStop(o.Stop);
            // the enemies first, so that effects and numbers can name them
            var seen = new HashSet<int>();
            foreach (var e in o.Enemies)
            {
                seen.Add(e.Id);
                if (!Mirror.TryGetValue(e.Id, out var m))
                {
                    if (e.Human)
                    {
                        var wd = e.A < _wep.Count ? WeaponDatabase.Categories[_wep[e.A].cat][_wep[e.A].key] : null; var ad = e.B < _arm.Count ? ArmorDatabase.Tiers[_arm[e.B].tier][_arm[e.B].key] : null;
                        string fac = _fac[System.Math.Min(_fac.Count - 1, e.Fr >> 3)], rank = CombatRules.RankOrder[System.Math.Min(CombatRules.RankOrder.Length - 1, e.Fr & 7)];
                        m = new HumanEnemy(fac == "Raiders" ? "Raider" : fac, e.X, e.Y, fac, rank, wd, ad);
                    }
                    else { if (e.A >= _mut.Count) continue; var (tier, key) = _mut[e.A]; m = new Mutant(key, e.X, e.Y, MutantDatabase.Get(tier, key)); }
                    Mirror[e.Id] = m; EncounterState.Enemies.Add(m);
                }
                m.X = e.X; m.Y = e.Y; Rt.SetOffset(m, e.Ox, e.Oy); m.Hp = e.Alive ? System.Math.Max(1, e.Hp) : 0; m.IsAggroed = e.Aggro;
            }
            foreach (var id in Mirror.Keys.Where(k => !seen.Contains(k)).ToList()) { EncounterState.Enemies.Remove(Mirror[id]); Mirror.Remove(id); }
            ApplyBrain(o.Brain);
            foreach (var (seq, text) in o.Logs) if (seq > _seenLog) { _seenLog = seq; GameLog.Write(text); }
            foreach (var n in o.Nums)
                if (n.seq > _seenNum) { _seenNum = n.seq; var who = Resolve(n.t, n.who); if (who != null) TestingGround.Numbers.Add(new TestingGround.Number { Who = who, Text = n.text, Kind = n.kind, At = Time.time }); }
            foreach (var f in o.Fx.OrderBy(x => x.Seq))
            {
                if (f.Seq <= _seenFx) continue; _seenFx = f.Seq;
                var actor = Resolve(f.ActorT, f.Actor); if (f.Kind != Px && actor == null) continue;
                var ev = new FxEvent { Kind = KindName(f.Kind), Actor = actor, Target = Resolve(f.TargetT, f.Target), Rounds = f.Rounds, Hit = (f.Flags & 1) != 0, Jammed = (f.Flags & 2) != 0, NoLos = (f.Flags & 4) != 0, Psi = (f.Flags & 8) != 0, Killed = (f.Flags & 16) != 0,
                    Pellet = (f.Flags & 32) != 0, Flying = (f.Flags & 64) != 0, Landed = (f.Flags & 128) != 0, FromX = f.FromX, FromY = f.FromY, ToX = f.ToX, ToY = f.ToY, EndX = f.EndX, EndY = f.EndY, Overkill = f.Overkill, Seconds = f.Seconds, Px = f.Px, Now = true };
                if (f.Weapon < _wep.Count && Player.Current != null) { var (cat, key) = _wep[f.Weapon]; ev.Weapon = Player.Current.MakeWeapon(cat, key); }
                CombatFx.Pending.Add(ev);
            }
        }
    }
}
