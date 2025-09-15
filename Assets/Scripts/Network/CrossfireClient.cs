using UnityEngine;
using Unity.WebRTC;
using Unity.Netcode;
using WebSocketSharp;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

public class CrossfireClient : MonoBehaviour
{
    public enum TopologyMode
    {
        //Mesh, //Mesh not supported at this time
        Host
    }


    [Header("Connection Settings")]

    public string serverHost = "ws://localhost:8080/app/ws/crossfire";

    [Tooltip("The id of the match from Elements.")]
    public string matchId = "1234";

    [Tooltip("The profile id from Elements.")]
    public string profileId = "abcd";

    [Tooltip("The id of this player relative to the match that they've joined.")]
    public ulong PlayerMatchId;

    [Tooltip("The connection mode")]
    public TopologyMode mode = TopologyMode.Host;    


    [Header("Reconnect Settings")]

    [Tooltip("Seconds before connection retry if the connection wasn't intentionally closed.")]
    public float reconnectDelay = 5f;   


    [Header("Network Managers")]

    [SerializeField]
    private WebRtcTransport transport;

    [SerializeField]
    private NetworkManager networkManager;

    private bool networkStarted = false;

    // Outbound message buffer
    private ConcurrentQueue<string> outboundQueue = new();

    // Peers that we know exist but haven’t fully negotiated WebRTC yet
    private readonly HashSet<string> pendingPeers = new();

    private readonly Dictionary<ulong, NetworkManager.ConnectionApprovalResponse> pendingApprovals = new();
    private readonly HashSet<string> connectedPeers = new();

    // The player match id of the host
    private ulong hostClientId;

    private string hostProfileId;

    private string sessionToken;

    private WebSocket ws;

    private bool intentionalClose = false;

    // Some actions need to occur on the main thread, so we queue them up for processing here
    private readonly ConcurrentQueue<Action> mainThreadMessageQueue = new();


    private void Start()
    {
        transport.OnSendOffer += SendOffer;
        transport.OnSendAnswer += SendAnswer;
        transport.OnSendIce += SendIce;
        transport.OnDataChannelReady += OnDataChannelReady;
        transport.OnPeerConnectionStateChanged += OnPeerConnectionStateChanged;
    }


    private void Update()
    {
        while (mainThreadMessageQueue.TryDequeue(out var action))
        {
            action();
        }
    }

    private void OnDestroy()
    {
        intentionalClose = true;

        transport.OnSendOffer -= SendOffer;
        transport.OnSendAnswer -= SendAnswer;
        transport.OnSendIce -= SendIce;
        transport.OnDataChannelReady -= OnDataChannelReady;
        transport.OnPeerConnectionStateChanged -= OnPeerConnectionStateChanged;

        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }

        StopNetworkSession();
    }

    public void Connect(string profileId, string sessionToken)
    {
        this.profileId = profileId;
        this.sessionToken = sessionToken;

        DoConnect();
    }

    public void FindOrCreateMatch(string configurationName)
    {
        var request = new FindHandshakeRequest();
        request.setProfileId(profileId);
        request.setSessionKey(sessionToken);
        request.setConfiguration(configurationName);

        var requestString = request.ToJsonString<FindHandshakeRequest>();
        EnqueueMessage(requestString);
    }

    public void JoinMatch(string matchId)
    {
        var request = new JoinHandshakeRequest();
        request.setMatchId(matchId);
        request.setProfileId(profileId);
        request.setSessionKey(sessionToken);
        request.setVersion(HandshakeRequest.VERSION_1_0);

        var requestString = request.ToJsonString<JoinHandshakeRequest>();
        EnqueueMessage(requestString);
    }

    private void DoConnect()
    {
        //string url = $"{serverHost}/sdp/{matchId}/{profileId}";
        string url = $"{serverHost}/match";
        Debug.Log("Connecting to " + url);

        ws = new WebSocket(url);

        ws.OnOpen += (s, e) =>
        {
            Debug.Log("Connected to " + url);
            FlushOutboundQueue();
        };

        ws.OnMessage += (s, e) =>
        {
            // Necessary to perform Unity functions, such as starting coroutines, from the main thread
            mainThreadMessageQueue.Enqueue(() => HandleMessage(e.Data));
        };

        ws.OnError += (s, e) =>
        {
            Debug.LogError("WebSocket Error: " + e.Message + "\n" + e.Exception);
        };

        ws.OnClose += (s, e) =>
        {
            Debug.Log("WebSocket closed: " + e.Reason);

            if (!intentionalClose)
            {
                Debug.Log("Attempting reconnect in " + reconnectDelay + "s...");
                StartCoroutine(TryReconnect());
            }
        };

        ws.ConnectAsync();
    }

    private IEnumerator TryReconnect()
    {
        yield return new WaitForSeconds(reconnectDelay);
        DoConnect();
    }

    /// <summary>
    /// Enqueues a message. If socket is live, sends immediately. Otherwise stores until reconnect.
    /// </summary>
    public void EnqueueMessage(string message)
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Send(message);
            Debug.Log("Sent immediately: " + message);
        }
        else
        {
            outboundQueue.Enqueue(message);
            Debug.Log("Queued (offline): " + message);
        }
    }

    /// <summary>
    /// Flushes queued messages once the socket is reconnected.
    /// </summary>
    private void FlushOutboundQueue()
    {
        while (outboundQueue.TryDequeue(out string queuedMsg))
        {
            if (ws != null && ws.IsAlive)
            {
                ws.Send(queuedMsg);
                Debug.Log("Sent from queue: " + queuedMsg);
            }
            else
            {
                // If connection dropped mid-flush, re-enqueue and stop
                outboundQueue.Enqueue(queuedMsg);
                break;
            }
        }
    }

    private void HandleMessage(string msg)
    {
        var json = JObject.Parse(msg);
        var type = (string)json["type"];

        Debug.Log($"Received message {type}: {json}");

        if (!Enum.TryParse(type, out MessageType messageType))
        {
            Debug.LogError($"Received unmapped message type {type}");
            return;
        }

        switch (messageType)
        {
            case MessageType.HOST:
                {
                    var hostSignal = json.ToObject<HostBroadcastSignal>();
                    hostProfileId = hostSignal.getProfileId();

                    Debug.Log($"Host identified: {hostProfileId}");

                    // If we are a client, connect to the host
                    if (!IsHost(profileId))
                        BeginConnectionWithHost(hostProfileId);

                    // Connect to any pending peers (non-host)
                    foreach (var peerId in pendingPeers)
                        BeginConnectionWithHost(peerId);

                    pendingPeers.Clear();
                    break;
                }

            case MessageType.MATCHED:
                {
                    var matchedResponse = json.ToObject<MatchedResponse>();
                    matchId = matchedResponse.getMatchId();
                    PlayerMatchId = NetworkIdMapper.DeterministicClientId(profileId, matchId);
                    break;
                }

            case MessageType.CONNECT:
                {
                    var connectSignal = json.ToObject<ConnectBroadcastSignal>();
                    var remoteProfileId = connectSignal.getProfileId();

                    // Skip self
                    if (remoteProfileId == profileId)
                        return;

                    // If host unknown and this is not the host, enqueue
                    if (hostProfileId == null && !IsHost(remoteProfileId))
                    {
                        pendingPeers.Add(remoteProfileId);
                        return;
                    }

                    BeginConnectionWithHost(remoteProfileId);
                    break;
                }

            case MessageType.SDP_OFFER:
                {
                    var offerSignal = json.ToObject<SdpOfferDirectSignal>();
                    var ngoId = transport.ProfileIdToClientId(offerSignal.getProfileId(), matchId);
                    transport.OnRemoteOffer(ngoId, new RTCSessionDescription
                    {
                        type = RTCSdpType.Offer,
                        sdp = offerSignal.getPeerSdp()
                    });
                    break;
                }

            case MessageType.SDP_ANSWER:
                {
                    var answerSignal = json.ToObject<SdpAnswerDirectSignal>();
                    var ngoId = transport.ProfileIdToClientId(answerSignal.getProfileId(), matchId);
                    transport.OnRemoteAnswer(ngoId, new RTCSessionDescription
                    {
                        type = RTCSdpType.Answer,
                        sdp = answerSignal.getPeerSdp()
                    });
                    break;
                }

            case MessageType.CANDIDATE:
                {
                    var candidateSignal = json.ToObject<CandidateDirectSignal>();
                    var ngoId = transport.ProfileIdToClientId(candidateSignal.getProfileId(), matchId);
                    transport.OnRemoteIce(ngoId, new RTCIceCandidate(new RTCIceCandidateInit
                    {
                        candidate = candidateSignal.getCandidate(),
                        sdpMid = candidateSignal.getMid()
                    }));
                    break;
                }

            // Ignore other messages for now
            case MessageType.BINARY_BROADCAST:
            case MessageType.BINARY_RELAY:
            case MessageType.DISCONNECT:
            case MessageType.ERROR:
            case MessageType.FIND:
            case MessageType.JOIN:
                break;
        }
    }


    private void BeginConnectionWithHost(string remoteProfileId)
    {
        // Never connect to self
        if (remoteProfileId == profileId)
            return;

        if (connectedPeers.Contains(remoteProfileId))
            return; // Already connected

        connectedPeers.Add(remoteProfileId);

        ulong ngoId = transport.ProfileIdToClientId(remoteProfileId, matchId);
        bool remoteIsHost = IsHost(remoteProfileId);

        if (IsHost(profileId))
        {
            // Host always offers
            transport.BeginConnection(ngoId, true);
        }
        else if (remoteIsHost)
        {
            // Client answers host
            transport.BeginConnection(ngoId, false);
        }
        else
        {
            // Optionally handle other peer-to-peer connections in the future
            Debug.Log($"Skipping non-host peer {remoteProfileId}");
        }
    }

    private void StopNetworkSession()
    {
        if (networkStarted)
        {
            networkManager.Shutdown();
        }
    }

    private bool IsHost(string id)
    {        
        return id == hostProfileId;
    }

    private void OnDataChannelReady(ulong remoteId)
    {
        if (!networkStarted)
        {
            if (IsHost(profileId))
            {
                Debug.Log("Starting as Host...");
                networkManager.StartHost();
            }
            else
            {
                Debug.Log("Starting as Client...");
                networkManager.StartClient();

                // Set the host/server peer ID for NGO transport
                transport.SetServerClientId(
                    transport.ProfileIdToClientId(hostProfileId, matchId)
                );
            }

            networkStarted = true;
        }

        Debug.Log($"[CrossfireClient] NGO is now started, peer {remoteId} will connect");
    }

    private void OnPeerConnectionStateChanged(ulong remoteId, RTCIceConnectionState state)
    {
        if (state == RTCIceConnectionState.Completed)
        {
            Debug.Log($"[CrossfireClient] Peer {remoteId} reached ICE Completed");

            if (pendingApprovals.TryGetValue(remoteId, out var pendingResponse))
            {
                pendingResponse.Approved = true;
                pendingResponse.CreatePlayerObject = true;
                pendingResponse.Pending = false;

                pendingApprovals.Remove(remoteId);
                Debug.Log($"[Approval] Now approved client {remoteId}");
            }
        }
    }

    private void SendOffer(ulong to, RTCSessionDescription offer)
    {
        var recipientProfileId = transport.ClientIdToProfileId(to);

        if (profileId == recipientProfileId) return;

        SdpOfferDirectSignal signal = new SdpOfferDirectSignal();
        signal.setPeerSdp(offer.sdp);
        signal.setProfileId(profileId);
        signal.setRecipientProfileId(recipientProfileId);

        var msg = signal.ToJsonString<SdpOfferDirectSignal>();

        Debug.Log("Sending offer " + msg);

        EnqueueMessage(msg);
    }

    private void SendAnswer(ulong to, RTCSessionDescription answer)
    {
        var recipientProfileId = transport.ClientIdToProfileId(to);

        if (profileId == recipientProfileId) return;

        SdpAnswerDirectSignal signal = new SdpAnswerDirectSignal();
        signal.setPeerSdp(answer.sdp);
        signal.setProfileId(profileId);
        signal.setRecipientProfileId(recipientProfileId);

        var msg = signal.ToJsonString<SdpAnswerDirectSignal>();

        Debug.Log("Sending answer " + msg);

        EnqueueMessage(msg);
    }

    private void SendIce(ulong to, RTCIceCandidate cand)
    {
        var recipientProfileId = transport.ClientIdToProfileId(to);

        if (profileId == recipientProfileId) return;

        CandidateDirectSignal signal = new CandidateDirectSignal();
        signal.setProfileId(profileId);
        signal.setRecipientProfileId(recipientProfileId);
        signal.setMid(cand.SdpMid);
        signal.setCandidate(cand.Candidate);

        var msg = signal.ToJsonString<CandidateDirectSignal>();

        Debug.Log("Sending candidate " + msg);

        EnqueueMessage(msg);
    }


    private void ApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        ulong clientId = request.ClientNetworkId;

        if (transport.IsPeerReady(clientId))
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Pending = false;
            Debug.Log($"[Approval] Approved client {clientId}");
        }
        else
        {
            // Store the response object to approve later
            pendingApprovals[clientId] = response;
            response.Approved = false;
            response.Pending = true;
            Debug.Log($"[Approval] Pending client {clientId}, waiting for WebRTC ready");
        }
    }

}
