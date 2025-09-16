using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections.Generic;

namespace Elements.Crossfire
{
    using Model;

    public class NetworkSessionManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private GameObject webRtcTransportPrefab;
        [SerializeField] private GameObject webSocketTransportPrefab;

        [Header("Configuration")]
        [SerializeField] private NetworkSessionConfig sessionConfig;
        [SerializeField] private bool useWebRTC = true;

        // Core components
        private ISignalingClient signalingClient;
        private INetworkTransportAdapter transportAdapter;

        // Session state
        private string hostProfileId;
        private bool isHost;
        private bool networkStarted;
        private readonly HashSet<string> connectedPeers = new();
        private readonly HashSet<string> pendingPeers = new();

        // Public API
        public void StartSession(string profileId, string sessionToken, string matchId = null)
        {
            sessionConfig.profileId = profileId;
            sessionConfig.sessionToken = sessionToken;
            if (matchId != null) sessionConfig.matchId = matchId;

            InitializeComponents();
            signalingClient.Connect(sessionConfig.serverHost, profileId, sessionToken);
        }

        public void JoinMatch(string matchId)
        {
            var request = new JoinHandshakeRequest();
            request.setMatchId(matchId);
            request.setProfileId(sessionConfig.profileId);
            request.setSessionKey(sessionConfig.sessionToken);

            signalingClient.SendWSMessage(request.ToJsonString<JoinHandshakeRequest>());
        }

        public void FindOrCreateMatch(string configurationName)
        {
            var request = new FindHandshakeRequest();
            request.setProfileId(sessionConfig.profileId);
            request.setSessionKey(sessionConfig.sessionToken);
            request.setConfiguration(configurationName);

            signalingClient.SendWSMessage(request.ToJsonString<FindHandshakeRequest>());
        }

        // Initialization
        private void InitializeComponents()
        {
            // Create signaling client
            signalingClient = gameObject.AddComponent<WebSocketSignalingClient>();
            signalingClient.OnConnected += HandleSignalingConnected;
            signalingClient.OnMessageReceived += HandleSignalingMessage;
            signalingClient.OnDisconnected += HandleSignalingDisconnected;

            // Create transport adapter based on configuration
            GameObject transportGO;
            if (useWebRTC)
            {
                transportGO = Instantiate(webRtcTransportPrefab, transform);
                transportAdapter = transportGO.GetComponent<WebRtcTransportAdapter>();

                // Pass signaling client reference to WebRTC adapter for outbound messages
                var webRtcAdapter = transportAdapter as WebRtcTransportAdapter;
                webRtcAdapter.SetSignalingClient(signalingClient, sessionConfig.profileId);
            }
            else
            {
                // Future: WebSocket-only transport
                transportGO = Instantiate(webSocketTransportPrefab, transform);
                transportAdapter = transportGO.GetComponent<INetworkTransportAdapter>();
            }

            // Initialize adapter and assign transport to NetworkManager
            transportAdapter.Initialize(networkManager);

            // Subscribe to transport events
            transportAdapter.OnPeerReady += HandlePeerReady;
            transportAdapter.OnPeerDisconnected += HandlePeerDisconnected;
        }

        // Signaling event handlers
        private void HandleSignalingConnected()
        {
            Debug.Log("[SessionManager] Signaling connected");
        }

        private void HandleSignalingDisconnected()
        {
            Debug.Log("[SessionManager] Signaling disconnected");
        }

        private void HandleSignalingMessage(SignalingMessage message)
        {
            Debug.Log($"[SessionManager] Received: {message.type}");

            switch (message.type)
            {
                case MessageType.HOST:
                    HandleHostMessage(message);
                    break;

                case MessageType.MATCHED:
                    HandleMatchedMessage(message);
                    break;

                case MessageType.CONNECT:
                    HandleConnectMessage(message);
                    break;

                case MessageType.SDP_OFFER:
                case MessageType.SDP_ANSWER:
                case MessageType.CANDIDATE:
                    transportAdapter.HandleSignalingMessage(message.type, message.profileId, message.payload);
                    break;

                case MessageType.DISCONNECT:
                    HandleDisconnectMessage(message);
                    break;
            }
        }

        private void HandleHostMessage(SignalingMessage message)
        {
            hostProfileId = message.profileId;
            isHost = (hostProfileId == sessionConfig.profileId);

            Debug.Log($"[SessionManager] Host identified: {hostProfileId}, I am host: {isHost}");

            // Connect to host if we're a client
            if (!isHost)
            {
                BeginConnectionWithPeer(hostProfileId);
            }

            // Process any pending peers
            foreach (var peerId in pendingPeers)
            {
                BeginConnectionWithPeer(peerId);
            }

            pendingPeers.Clear();
        }

        private void HandleMatchedMessage(SignalingMessage message)
        {
            sessionConfig.matchId = message.matchId;

            if (transportAdapter is WebRtcTransportAdapter webRtcAdapter)
            {
                webRtcAdapter.SetMatchId(sessionConfig.matchId);
            }

            Debug.Log($"[SessionManager] Matched to: {sessionConfig.matchId}");
        }

        private void HandleConnectMessage(SignalingMessage message)
        {
            var remoteProfileId = message.profileId;

            // Skip self
            if (remoteProfileId == sessionConfig.profileId)
                return;

            // If host unknown and this is not the host, queue for later
            if (hostProfileId == null && remoteProfileId != hostProfileId)
            {
                pendingPeers.Add(remoteProfileId);
                return;
            }

            BeginConnectionWithPeer(remoteProfileId);
        }

        private void HandleDisconnectMessage(SignalingMessage message)
        {
            var remoteProfileId = message.profileId;

            if (connectedPeers.Remove(remoteProfileId))
            {
                transportAdapter.DisconnectPeer(remoteProfileId);
                Debug.Log($"[SessionManager] Peer disconnected: {remoteProfileId}");
            }
        }

        // Connection management
        private void BeginConnectionWithPeer(string remoteProfileId)
        {
            if (remoteProfileId == sessionConfig.profileId)
                return;

            if (connectedPeers.Contains(remoteProfileId))
                return;

            connectedPeers.Add(remoteProfileId);

            bool shouldOffer = DetermineIfShouldOffer(remoteProfileId);
            transportAdapter.BeginConnection(remoteProfileId, shouldOffer);

            Debug.Log($"[SessionManager] Beginning connection with {remoteProfileId}, offering: {shouldOffer}");
        }

        private bool DetermineIfShouldOffer(string remoteProfileId)
        {
            // Host always offers
            if (isHost) return true;

            // Client answers to host
            if (remoteProfileId == hostProfileId) return false;

            // For P2P connections, use lexicographic comparison
            return string.Compare(sessionConfig.profileId, remoteProfileId, StringComparison.Ordinal) < 0;
        }

        // Transport event handlers
        private void HandlePeerReady(string peerId)
        {
            Debug.Log($"[SessionManager] Peer ready: {peerId}");

            if (!networkStarted)
            {
                StartNetworkManager();
            }
        }

        private void HandlePeerDisconnected(string peerId)
        {
            connectedPeers.Remove(peerId);
            Debug.Log($"[SessionManager] Peer disconnected: {peerId}");
        }

        private void StartNetworkManager()
        {
            if (isHost)
            {
                Debug.Log("[SessionManager] Starting as Host");
                networkManager.StartHost();
            }
            else
            {
                Debug.Log("[SessionManager] Starting as Client");
                // Set server peer for transport
                if (transportAdapter is WebRtcTransportAdapter webRtcAdapter)
                {
                    var serverNgoId = NetworkIdMapper.DeterministicClientId(hostProfileId, sessionConfig.matchId);
                    webRtcAdapter.GetComponent<WebRtcTransport>().SetServerClientId(serverNgoId);
                }

                networkManager.StartClient();
            }

            networkStarted = true;
        }

        private void OnDestroy()
        {
            if (networkStarted)
            {
                networkManager.Shutdown();
            }

            transportAdapter?.Shutdown();

            if (signalingClient != null)
            {
                signalingClient.OnConnected -= HandleSignalingConnected;
                signalingClient.OnMessageReceived -= HandleSignalingMessage;
                signalingClient.OnDisconnected -= HandleSignalingDisconnected;
                signalingClient.Disconnect();
            }
        }
    }
}