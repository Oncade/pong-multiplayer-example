using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.WebRTC;
using UnityEngine;

namespace Elements.Crossfire
{
    /// <summary>
    /// NGO transport over Unity.WebRTC (data channels only).
    /// Requires an external signaling layer to exchange SDP/ICE.
    /// </summary>
    public class WebRtcTransport : NetworkTransport
    {
        public enum Mode
        {
            REMOTE,
            LOCAL // Local testing only
        }

        [SerializeField]
        private Mode mode = Mode.REMOTE;

        public event Action<ulong, RTCSessionDescription> OnSendOffer;
        public event Action<ulong, RTCSessionDescription> OnSendAnswer;
        public event Action<ulong, RTCIceCandidate> OnSendIce;
        public event Action<ulong, RTCIceConnectionState> OnPeerConnectionStateChanged; // now only fired once peer is READY
        public event Action<ulong> OnDataChannelReady;

        private readonly Queue<(NetworkEvent ev, ulong clientId, ArraySegment<byte> payload, float receiveTime)> events = new();

        private readonly Dictionary<ulong, RTCPeerConnection> peerConnections = new();
        private readonly Dictionary<ulong, RTCDataChannel> dataChannels = new();

        private readonly HashSet<ulong> readyPeers = new(); // tracks peers that have reached "ready"

        private ulong serverPeerId;

        private Dictionary<string, ulong> profileIdToClientId = new();
        private Dictionary<ulong, string> clientIdToProfileId = new();

        private bool initialized = false;

        private void Update()
        {
            if (initialized)
                WebRTC.Update();
        }

        // ---- NGO required overrides ----

        public override void Initialize(NetworkManager networkManager = null)
        {
            Debug.Log("Initializing WebRTC");
            initialized = true;
        }

        public override bool StartClient()
        {
            Debug.Log("Starting Client");
            return true;
        }

        public override bool StartServer()
        {
            Debug.Log("Starting Server");
            return true;
        }

        public override void Shutdown()
        {
            Debug.Log("Shutting Down WebRTC");

            foreach (var dataChannel in dataChannels.Values)
                dataChannel.Close();

            foreach (var peerConnection in peerConnections.Values)
                peerConnection.Close();

            peerConnections.Clear();
            dataChannels.Clear();
            readyPeers.Clear();
        }

        public override void DisconnectRemoteClient(ulong clientId)
        {
            Debug.Log($"[Transport] DisconnectRemoteClient({clientId})");

            if (dataChannels.Remove(clientId, out var dataChannel))
                dataChannel.Close();

            if (peerConnections.Remove(clientId, out var peerConnection))
                peerConnection.Close();

            readyPeers.Remove(clientId);
            Enqueue(NetworkEvent.Disconnect, clientId, default);
        }

        public override void DisconnectLocalClient()
        {
            Debug.Log("Disconnecting Local Client");

            if (serverPeerId != 0)
                DisconnectRemoteClient(serverPeerId);
        }

        public override ulong GetCurrentRtt(ulong clientId) => 0;

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (events.Count > 0)
            {
                var (ev, id, data, t) = events.Dequeue();
                clientId = id;
                payload = data;
                receiveTime = t;

                Debug.Log($"Polling event " + ev);
                return ev;
            }

            clientId = 0;
            payload = default;
            receiveTime = 0;
            return NetworkEvent.Nothing;
        }

        public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery)
        {
            if (!readyPeers.Contains(clientId))
            {
                Debug.LogWarning($"[Transport.Send] Peer {clientId} not ready yet, skipping send.");
                return;
            }

            Debug.Log($"[Transport.Send] to={clientId} len={payload.Count}");

            if (clientId == 0)
            {
                Debug.LogWarning("[Transport.Send] Ignoring send to self (host id=0)");
                return;
            }

            if (dataChannels.TryGetValue(clientId, out var dc) && dc.ReadyState == RTCDataChannelState.Open)
            {
                dc.Send(payload.ToArray());
            }
            else
            {
                Debug.LogWarning($"[Transport.Send] data channel for {clientId} not found or not open. ReadyState={(dataChannels.TryGetValue(clientId, out var ch) ? ch.ReadyState.ToString() : "missing")}");
            }
        }

        public override bool IsSupported => true;
        public override ulong ServerClientId => serverPeerId;

        public void SetServerClientId(ulong id)
        {
            serverPeerId = id;
        }

        // ---- WebRTC-specific helpers ----

        public void BeginConnection(ulong remoteId, bool isOfferer)
        {
            Debug.Log($"Beginning WebRTC Connection with remoteId: {remoteId} and isOfferer: {isOfferer}");

            var peerConnection = CreateAndConfigurePeerConnection(remoteId);

            if (isOfferer)
            {
                Debug.Log("Creating data channel");
                var dc = peerConnection.CreateDataChannel("game");
                WireDataChannel(remoteId, dc);
                StartCoroutine(CreateOfferAndSend(peerConnection, remoteId));
            }
        }

        public void InjectEvent(NetworkEvent ev, ulong id, ArraySegment<byte> data)
        {
            Enqueue(ev, id, data);
        }

        public void OnRemoteOffer(ulong remoteId, RTCSessionDescription offer)
        {
            Debug.Log($"[Transport] OnRemoteOffer from {remoteId}");

            if (!peerConnections.TryGetValue(remoteId, out var pc))
            {
                Debug.Log($"[Transport] No PC for {remoteId} - creating one now");
                pc = CreateAndConfigurePeerConnection(remoteId);
                peerConnections[remoteId] = pc;
            }

            StartCoroutine(RespondToOffer(pc, remoteId, offer));
        }


        public void OnRemoteAnswer(ulong remoteId, RTCSessionDescription answer)
        {
            if (peerConnections.TryGetValue(remoteId, out var pc))
            {
                Debug.Log("Handling remote answer from id " + remoteId);
                StartCoroutine(SetRemoteDesc(pc, answer));
            }
        }

        public void OnRemoteIce(ulong remoteId, RTCIceCandidate candidate)
        {
            if (candidate == null) return;

            if (peerConnections.TryGetValue(remoteId, out var pc))
            {
                pc.AddIceCandidate(candidate);
            }
        }

        public ulong ProfileIdToClientId(string profileId, string matchId)
        {
            if (profileIdToClientId.TryGetValue(profileId, out var clientId))
            {
                return clientId;
            }

            var newId = NetworkIdMapper.DeterministicClientId(profileId, matchId);
            profileIdToClientId[profileId] = newId;
            clientIdToProfileId[newId] = profileId;

            return newId;
        }

        public string ClientIdToProfileId(ulong id)
        {
            return clientIdToProfileId.TryGetValue(id, out var sid) ? sid : null;
        }

        // ---- Data Channel ----

        private void WireDataChannel(ulong remoteId, RTCDataChannel channel)
        {
            Debug.Log($"[Transport] Wiring data channel for {remoteId}, label={channel.Label}");
            dataChannels[remoteId] = channel;

            channel.OnMessage = bytes =>
                Enqueue(NetworkEvent.Data, remoteId, new ArraySegment<byte>(bytes));

            channel.OnClose = () =>
            {
                Debug.Log($"[Transport] DataChannel closed for {remoteId}");
                dataChannels.Remove(remoteId);
                readyPeers.Remove(remoteId);
                Enqueue(NetworkEvent.Disconnect, remoteId, default);
            };

            channel.OnOpen = () =>
            {
                Debug.Log($"[Transport] DataChannel open for {remoteId}");

            // Mark peer ready
            TryMarkPeerReady(remoteId);

            // Tell CrossfireClient that a channel is ready.
            // CrossfireClient will ensure NGO is started first.
            OnDataChannelReady?.Invoke(remoteId);
                OnPeerConnectionStateChanged?.Invoke(remoteId, RTCIceConnectionState.Completed);

            };

            if (channel.ReadyState == RTCDataChannelState.Open)
            {
                Debug.Log($"[Transport] DataChannel for {remoteId} already open, manually firing OnOpen");
                channel.OnOpen?.Invoke();
            }
        }

        // ---- "Peer Ready" detection ----

        public bool IsPeerReady(ulong clientId)
        {
            return readyPeers.Contains(clientId);
        }

        private bool TryMarkPeerReady(ulong remoteId)
        {
            if (!peerConnections.ContainsKey(remoteId)) return false;
            if (!dataChannels.TryGetValue(remoteId, out var dc)) return false;
            if (dc.ReadyState != RTCDataChannelState.Open) return false;

            if (readyPeers.Add(remoteId))
            {
                Debug.Log($"[Transport] Peer {remoteId} is fully ready, enqueuing Connect");
                Enqueue(NetworkEvent.Connect, remoteId, default);
                return true;
            }

            return false;
        }

        // ---- Coroutines ----

        private IEnumerator CreateOfferAndSend(RTCPeerConnection pc, ulong remoteId)
        {
            var op = pc.CreateOffer();
            yield return op;
            if (op.IsError) yield break;

            var desc = op.Desc;
            var setOp = pc.SetLocalDescription(ref desc);
            yield return setOp;
            if (setOp.IsError) yield break;

            OnSendOffer?.Invoke(remoteId, desc);
        }

        private IEnumerator RespondToOffer(RTCPeerConnection pc, ulong remoteId, RTCSessionDescription offer)
        {
            var setOp = pc.SetRemoteDescription(ref offer);
            yield return setOp;
            if (setOp.IsError) yield break;

            var answerOp = pc.CreateAnswer();
            yield return answerOp;
            if (answerOp.IsError) yield break;

            var desc = answerOp.Desc;
            var setLocalOp = pc.SetLocalDescription(ref desc);
            yield return setLocalOp;
            if (setLocalOp.IsError) yield break;

            OnSendAnswer?.Invoke(remoteId, desc);
        }

        private IEnumerator SetRemoteDesc(RTCPeerConnection pc, RTCSessionDescription desc)
        {
            var op = pc.SetRemoteDescription(ref desc);
            yield return op;
        }

        private RTCPeerConnection CreateAndConfigurePeerConnection(ulong remoteId)
        {
            var cfg = GetRTCConfiguration();
            var pc = new RTCPeerConnection(ref cfg);
            peerConnections[remoteId] = pc;

            pc.OnIceCandidate = cand => { if (cand != null) OnSendIce?.Invoke(remoteId, cand); };
            pc.OnDataChannel = ch => WireDataChannel(remoteId, ch);
            pc.OnIceConnectionChange = state =>
            {
                Debug.Log($"[PC:{remoteId}] ICE state changed: {state}");
                if (state == RTCIceConnectionState.Connected || state == RTCIceConnectionState.Completed)
                    TryMarkPeerReady(remoteId);
            };

            return pc;
        }

        private RTCConfiguration GetRTCConfiguration()
        {
            return mode == Mode.LOCAL
                ? new RTCConfiguration { iceServers = new RTCIceServer[0] }
                : new RTCConfiguration
                {
                    iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } }
                };
        }

        private void Enqueue(NetworkEvent @event, ulong id, ArraySegment<byte> data)
        {
            events.Enqueue((@event, id, data, Time.realtimeSinceStartup));
        }
    }
}