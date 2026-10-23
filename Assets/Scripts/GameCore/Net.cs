// RT 7y: the networking test's game side (ACTION_DESIGN "Co-op": the host runs the world, the others send inputs). Everything
// here is plain C# the offline harness checks; the Unity side (sessions, the relay, Netcode messages) is Net/NgoLink.cs, which
// plugs itself in as Net.Link when the packages are installed.
// - Host: a friend who connects gets a Player beside the host (Player.Join, a starting pistol); their Input packets land in
//   Rt.Remote and PlayerController ticks them like the local player; ~20 times a second every client gets a snapshot.
// - Client: sends its Input every frame, runs no world of its own (no clock, no AI, no squads), and puts every player where the
//   host's snapshot says - itself included.
// ponytail: the test covers players only. Enemies, shots and effects, items, quests, characters travelling with their owner,
// the "everyone at the exit" vote and the host saving everyone's character are the next steps (WORK_LOG RT 7y).
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GameCore
{
    /// <summary>What the Unity transport does for the game. Net/NgoLink.cs is the real one; the harness uses a fake.</summary>
    public interface INetLink
    {
        void Host(System.Action<string> onCode, System.Action<string> onError);   // gives the join code
        void Join(string code, System.Action onJoined, System.Action<string> onError);
        void Stop();
        ulong LocalId { get; }
        void SendToHost(byte[] data);
        void SendToClients(byte[] data);
    }

    public static class Net
    {
        public enum Role { Off, Host, Client }
        public static Role Mode = Role.Off;
        public static INetLink Link;               // null until the Netcode packages are in the project
        public static string Code = "";            // the join code while hosting
        public static string Status = "";          // one line for the co-op panel and the corner of the screen

        public const float SnapshotHz = 20f;
        static float _nextSnapshot;
        static readonly Dictionary<ulong, Player> ByOwner = new Dictionary<ulong, Player>();   // host: who each connection drives; client: mirrors

        public static bool Client => Mode == Role.Client;

        public static void Reset() { Mode = Role.Off; Code = ""; Status = ""; ByOwner.Clear(); _nextSnapshot = 0f; }

        // ---------------- starting and stopping ----------------

        /// <summary>Host the game you're in: a join code for friends.</summary>
        public static void Host()
        {
            if (Link == null) { Status = "Networking isn't installed (Unity packages missing)."; return; }
            if (Player.Current == null) { Status = "Start or load a game first."; return; }
            Status = "Opening a session...";
            Link.Host(code => { Mode = Role.Host; Code = code; Status = $"Hosting - join code {code}"; GameLog.Write($"Co-op: hosting. Join code: {code}"); },
                      err => { Mode = Role.Off; Status = "Couldn't host: " + err; });
        }

        /// <summary>Join a friend's game by its code. `start` begins a local game for the client to stand in (no saving, no world).</summary>
        public static void Join(string code, System.Action start)
        {
            if (Link == null) { Status = "Networking isn't installed (Unity packages missing)."; return; }
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length == 0) { Status = "Type the host's join code."; return; }
            Status = "Joining " + code + "...";
            Link.Join(code, () => { start?.Invoke(); BecomeClient(); Status = "Joined " + code; GameLog.Write("Co-op: joined the host's game."); },
                      err => { Mode = Role.Off; Status = "Couldn't join: " + err; });
        }

        /// <summary>The client's own game goes quiet: the host's world is the only one.</summary>
        public static void BecomeClient()
        {
            Mode = Role.Client; ByOwner.Clear();
            SaveSystem.CharacterId = SaveSystem.WorldId = null;          // a client never saves the host's world
            EncounterState.Enemies.Clear();
        }

        public static void Leave()
        {
            Link?.Stop();
            foreach (var p in ByOwner.Values) { Player.All.Remove(p); Rt.Remote.Remove(p); }
            Reset();
        }

        // ---------------- host ----------------

        /// <summary>A friend connected: a body for them next to the host, with a starting pistol.</summary>
        public static Player OnJoin(ulong owner)
        {
            if (Mode != Role.Host || ByOwner.ContainsKey(owner)) return ByOwner.TryGetValue(owner, out var had) ? had : null;
            var host = Player.Current;
            var p = Player.Join();
            var spot = TerrainMap.FindNearestEnterable(host.X + 1, host.Y);
            p.X = spot.x; p.Y = spot.y;
            p.EquipWeapon("sidearm", "PSh18Pchela");
            if (p.EquippedWeapon != null) Ammo.Add(p.Inventory, Ammo.Key(p.EquippedWeapon.Db, "Standard"), 30);
            ByOwner[owner] = p;
            GameLog.Write("Co-op: a friend joined.");
            return p;
        }

        public static void OnLeave(ulong owner)
        {
            if (!ByOwner.TryGetValue(owner, out var p)) return;
            ByOwner.Remove(owner); Player.All.Remove(p); Rt.Remote.Remove(p);
            GameLog.Write("Co-op: a friend left.");
        }

        /// <summary>An Input packet arrived. Held keys and the stick take the newest; a press (fire, jump, reload) is kept until a
        /// tick has used it, so a press between two host frames is never lost.</summary>
        public static void OnInput(ulong owner, byte[] data)
        {
            if (Mode != Role.Host || !ByOwner.TryGetValue(owner, out var p)) return;
            var i = DecodeInput(data);
            if (Rt.Remote.TryGetValue(p, out var prev)) { i.FireDown |= prev.FireDown; i.Jump |= prev.Jump; i.Reload |= prev.Reload; }
            Rt.Remote[p] = i;
        }

        /// <summary>The input a joined player's tick uses; its presses are spent.</summary>
        public static Rt.Input TakeInput(Player p)
        {
            if (!Rt.Remote.TryGetValue(p, out var i)) return default;
            var held = i; held.FireDown = held.Jump = held.Reload = false;
            Rt.Remote[p] = held;
            return i;
        }

        public static ulong OwnerOf(Player p) => p == Player.Current ? 0UL : ByOwner.FirstOrDefault(kv => kv.Value == p).Key;

        // ---------------- every frame (PlayerController) ----------------

        public static void Tick(float now, Rt.Input mine)
        {
            if (Link == null) return;
            if (Mode == Role.Client) Link.SendToHost(EncodeInput(mine));
            else if (Mode == Role.Host && now >= _nextSnapshot) { _nextSnapshot = now + 1f / SnapshotHz; Link.SendToClients(EncodeSnapshot()); }
        }

        // ---------------- client ----------------

        /// <summary>Put every player where the host says. New friends get a mirror; ones who left go.</summary>
        public static void OnSnapshot(byte[] data)
        {
            if (Mode != Role.Client || Player.Current == null) return;
            var s = DecodeSnapshot(data);
            if (!string.IsNullOrEmpty(s.Map) && s.Map != TerrainMap.ZoneId) { ZoneTravel.EnterZone(s.Map); EncounterState.Enemies.Clear(); }
            MacroState.Current.Hour = s.Hour;
            var seen = new HashSet<ulong>();
            ulong me = Link?.LocalId ?? ulong.MaxValue;
            foreach (var e in s.Players)
            {
                Player p;
                if (e.Owner == me) p = Player.Current;
                else if (!ByOwner.TryGetValue(e.Owner, out p)) ByOwner[e.Owner] = p = Player.Join();
                seen.Add(e.Owner);
                p.X = e.X; p.Y = e.Y; Rt.SetOffset(p, e.Ox, e.Oy);
                p.Hp = e.Hp;
                var st = Rt.Of(p); float now = Time.time;
                st.Aiming = e.Aiming;
                if (e.HasAim && p != Player.Current) st.AimAt = (e.AimX, e.AimY);   // your own gun follows your own mouse
                st.DiveUntil = e.DiveT >= 0f ? now + Rt.DiveTime - e.DiveT : -1f;
                st.ProneUntil = now + e.ProneLeft; st.DiveX = e.DiveRight ? 1f : -1f;
            }
            foreach (var gone in ByOwner.Keys.Where(k => !seen.Contains(k)).ToList()) { Player.All.Remove(ByOwner[gone]); ByOwner.Remove(gone); }
        }

        // ---------------- the wire format ----------------

        public static byte[] EncodeInput(Rt.Input i)
        {
            using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
            w.Write(i.X); w.Write(i.Y);
            int bits = (i.Sneak ? 1 : 0) | (i.Sprint ? 2 : 0) | (i.Aim ? 4 : 0) | (i.Fire ? 8 : 0) | (i.FireDown ? 16 : 0) | (i.Jump ? 32 : 0)
                     | (i.Reload ? 64 : 0) | (i.Use ? 128 : 0) | (i.Mouse.HasValue ? 256 : 0);
            w.Write((short)bits);
            if (i.Mouse.HasValue) { w.Write(i.Mouse.Value.x); w.Write(i.Mouse.Value.y); }
            return ms.ToArray();
        }

        public static Rt.Input DecodeInput(byte[] data)
        {
            using var r = new BinaryReader(new MemoryStream(data));
            var i = new Rt.Input { X = r.ReadSingle(), Y = r.ReadSingle() };
            int b = r.ReadInt16();
            i.Sneak = (b & 1) != 0; i.Sprint = (b & 2) != 0; i.Aim = (b & 4) != 0; i.Fire = (b & 8) != 0; i.FireDown = (b & 16) != 0;
            i.Jump = (b & 32) != 0; i.Reload = (b & 64) != 0; i.Use = (b & 128) != 0;
            if ((b & 256) != 0) i.Mouse = (r.ReadSingle(), r.ReadSingle());
            return i;
        }

        public class PlayerEntry { public ulong Owner; public int X, Y, Hp; public float Ox, Oy; public bool Aiming;
            public bool HasAim, DiveRight; public float AimX, AimY, DiveT = -1f, ProneLeft; }   // RT 7z: where they aim, the dive's moment
        public class Snapshot { public string Map; public float Hour; public List<PlayerEntry> Players = new List<PlayerEntry>(); }

        public static byte[] EncodeSnapshot()
        {
            using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
            w.Write(TerrainMap.ZoneId ?? ""); w.Write(MacroState.Current?.Hour ?? 12f);
            w.Write((byte)Player.All.Count);
            foreach (var p in Player.All)
            {
                var (ox, oy) = Rt.Offset(p);
                var st = Rt.Of(p); float now = Time.time;
                w.Write(OwnerOf(p)); w.Write(p.X); w.Write(p.Y); w.Write(ox); w.Write(oy); w.Write(p.Hp); w.Write(st.Aiming);
                w.Write(st.AimAt.HasValue); w.Write(st.AimAt?.x ?? 0f); w.Write(st.AimAt?.y ?? 0f);
                w.Write(st.Diving ? Rt.DiveTime - (st.DiveUntil - now) : -1f); w.Write(Mathf.Max(0f, st.ProneUntil - now)); w.Write(st.DiveRight);
            }
            return ms.ToArray();
        }

        public static Snapshot DecodeSnapshot(byte[] data)
        {
            using var r = new BinaryReader(new MemoryStream(data));
            var s = new Snapshot { Map = r.ReadString(), Hour = r.ReadSingle() };
            int n = r.ReadByte();
            for (int k = 0; k < n; k++)
                s.Players.Add(new PlayerEntry { Owner = r.ReadUInt64(), X = r.ReadInt32(), Y = r.ReadInt32(), Ox = r.ReadSingle(), Oy = r.ReadSingle(), Hp = r.ReadInt32(), Aiming = r.ReadBoolean(),
                    HasAim = r.ReadBoolean(), AimX = r.ReadSingle(), AimY = r.ReadSingle(), DiveT = r.ReadSingle(), ProneLeft = r.ReadSingle(), DiveRight = r.ReadBoolean() });
            return s;
        }
    }
}
