// RT 7y: the Unity side of the networking test - a session with a join code over Unity's relay (Unity Multiplayer Services) and
// Netcode for GameObjects carrying two named messages: Input packets up to the host, snapshots down to the clients. All game
// logic is in GameCore/Net.cs; this file only moves bytes. It needs the packages in Packages/manifest.json and the project
// linked to a Unity Cloud project (Edit > Project Settings > Services). The offline harness skips this folder.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace GameCore
{
    public class NgoLink : INetLink
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install() => Net.Link = new NgoLink();

        const string InputMsg = "av_input", SnapshotMsg = "av_snapshot";
        ISession _session;
        NetworkManager _nm;
        bool _registered;

        public ulong LocalId => _nm != null ? _nm.LocalClientId : 0UL;

        NetworkManager Manager()
        {
            if (_nm != null) return _nm;
            var go = new GameObject("NetworkManager");
            UnityEngine.Object.DontDestroyOnLoad(go);
            var transport = go.AddComponent<UnityTransport>();
            _nm = go.AddComponent<NetworkManager>();
            _nm.NetworkConfig = new NetworkConfig { NetworkTransport = transport };
            _nm.OnServerStarted += Register;
            _nm.OnClientStarted += Register;
            _nm.OnClientConnectedCallback += id => { if (_nm.IsServer && id != NetworkManager.ServerClientId) Net.OnJoin(id); };
            _nm.OnClientDisconnectCallback += id =>
            {
                if (_nm.IsServer) Net.OnLeave(id);
                else { Net.Status = "Disconnected from the host."; GameLog.Write("Co-op: disconnected from the host."); }
            };
            return _nm;
        }

        void Register()
        {
            if (_registered) return;
            _registered = true;
            var cmm = _nm.CustomMessagingManager;
            cmm.RegisterNamedMessageHandler(InputMsg, (sender, reader) => { reader.ReadValueSafe(out byte[] b); Net.OnInput(sender, b); });
            cmm.RegisterNamedMessageHandler(SnapshotMsg, (sender, reader) => { reader.ReadValueSafe(out byte[] b); Net.OnSnapshot(b); });
        }

        /// <summary>Unity Services and an anonymous sign-in. Each Multiplayer Play Mode window gets its own profile, so two editor
        /// windows count as two players.</summary>
        static async Task SignIn()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions();
                options.SetProfile("p" + UnityEngine.Random.Range(0, 1000000));
                await UnityServices.InitializeAsync(options);
            }
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public async void Host(Action<string> onCode, Action<string> onError)
        {
            try
            {
                Manager();
                await SignIn();
                _session = await MultiplayerService.Instance.CreateSessionAsync(new SessionOptions { MaxPlayers = 4, IsPrivate = true }.WithRelayNetwork());
                onCode(_session.Code);
            }
            catch (Exception e) { Debug.LogException(e); onError(e.Message); }
        }

        public async void Join(string code, Action onJoined, Action<string> onError)
        {
            try
            {
                Manager();
                await SignIn();
                _session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
                onJoined();
            }
            catch (Exception e) { Debug.LogException(e); onError(e.Message); }
        }

        public void Stop()
        {
            try { _session?.LeaveAsync(); } catch (Exception e) { Debug.LogWarning(e.Message); }
            _session = null;
            if (_nm != null && _nm.IsListening) _nm.Shutdown();
            _registered = false;
        }

        readonly List<ulong> _others = new List<ulong>();

        void Send(string name, byte[] data, IReadOnlyList<ulong> to, NetworkDelivery delivery)
        {
            if (_nm == null || !_nm.IsListening || _nm.CustomMessagingManager == null || to.Count == 0) return;
            using var w = new FastBufferWriter(data.Length + 16, Allocator.Temp);
            w.WriteValueSafe(data);
            _nm.CustomMessagingManager.SendNamedMessage(name, to, w, delivery);
        }

        public void SendToHost(byte[] data) { if (_nm != null && _nm.IsConnectedClient && !_nm.IsServer) Send(InputMsg, data, new[] { NetworkManager.ServerClientId }, NetworkDelivery.ReliableSequenced); }

        /// <summary>Every client but the host itself; a snapshot may drop (the next one is 50 ms behind).</summary>
        public void SendToClients(byte[] data)
        {
            if (_nm == null || !_nm.IsServer) return;
            _others.Clear();
            foreach (var id in _nm.ConnectedClientsIds) if (id != NetworkManager.ServerClientId) _others.Add(id);
            Send(SnapshotMsg, data, _others, NetworkDelivery.UnreliableSequenced);
        }
    }
}
