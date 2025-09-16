using UnityEngine;
using Unity.Netcode;
using Unity.WebRTC;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Elements.Crossfire
{
    using Model;

    public class WebRtcTransportAdapter : MonoBehaviour, INetworkTransportAdapter
    {
        public event Action<string> OnPeerReady;
        public event Action<string> OnPeerDisconnected;

        [SerializeField] private WebRtcTransport transport;

        private Dictionary<string, ulong> profileToNgoId = new();
        private Dictionary<ulong, string> ngoIdToProfile = new();
        private string matchId;
        private string localProfileId;
        private ISignalingClient signalingClient;

        public void Initialize(NetworkManager networkManager)
        {
            if (transport == null)
                transport = GetComponent<WebRtcTransport>();

            networkManager.NetworkConfig.NetworkTransport = transport;

            transport.Initialize(networkManager);

            // Subscribe to transport events
            transport.OnDataChannelReady += HandleDataChannelReady;
            transport.OnPeerConnectionStateChanged += HandlePeerStateChanged;

            // Subscribe to WebRTC signaling events
            transport.OnSendOffer += HandleSendOffer;
            transport.OnSendAnswer += HandleSendAnswer;
            transport.OnSendIce += HandleSendIce;
        }

        public void SetSignalingClient(ISignalingClient client, string profileId)
        {
            signalingClient = client;
            localProfileId = profileId;
        }

        public void SetMatchId(string id)
        {
            matchId = id;
        }

        public void BeginConnection(string peerId, bool isOfferer)
        {
            var ngoId = GetOrCreateNgoId(peerId);
            transport.BeginConnection(ngoId, isOfferer);
        }

        public void DisconnectPeer(string peerId)
        {
            if (profileToNgoId.TryGetValue(peerId, out var ngoId))
            {
                transport.DisconnectRemoteClient(ngoId);
                profileToNgoId.Remove(peerId);
                ngoIdToProfile.Remove(ngoId);
            }
        }

        public bool IsPeerReady(string peerId)
        {
            return profileToNgoId.TryGetValue(peerId, out var ngoId) &&
                   transport.IsPeerReady(ngoId);
        }

        public void HandleSignalingMessage(MessageType messageType, string fromPeerId, string payload)
        {
            var ngoId = GetOrCreateNgoId(fromPeerId);
            var json = JObject.Parse(payload);

            switch (messageType)
            {
                case MessageType.SDP_OFFER:
                    var offer = new RTCSessionDescription
                    {
                        type = RTCSdpType.Offer,
                        sdp = (string)json["peerSdp"]
                    };
                    transport.OnRemoteOffer(ngoId, offer);
                    break;

                case MessageType.SDP_ANSWER:
                    var answer = new RTCSessionDescription
                    {
                        type = RTCSdpType.Answer,
                        sdp = (string)json["peerSdp"]
                    };
                    transport.OnRemoteAnswer(ngoId, answer);
                    break;

                case MessageType.CANDIDATE:
                    var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                    {
                        candidate = (string)json["candidate"],
                        sdpMid = (string)json["mid"]
                    });
                    transport.OnRemoteIce(ngoId, candidate);
                    break;
            }
        }

        public void Shutdown()
        {
            // Unsubscribe from events
            if (transport != null)
            {
                transport.OnDataChannelReady -= HandleDataChannelReady;
                transport.OnPeerConnectionStateChanged -= HandlePeerStateChanged;
                transport.OnSendOffer -= HandleSendOffer;
                transport.OnSendAnswer -= HandleSendAnswer;
                transport.OnSendIce -= HandleSendIce;
            }

            transport.Shutdown();
            profileToNgoId.Clear();
            ngoIdToProfile.Clear();
        }

        private ulong GetOrCreateNgoId(string profileId)
        {
            if (!profileToNgoId.TryGetValue(profileId, out var ngoId))
            {
                ngoId = NetworkIdMapper.DeterministicClientId(profileId, matchId);
                profileToNgoId[profileId] = ngoId;
                ngoIdToProfile[ngoId] = profileId;
            }
            return ngoId;
        }

        // Outbound WebRTC signaling
        private void HandleSendOffer(ulong to, RTCSessionDescription offer)
        {
            if (!ngoIdToProfile.TryGetValue(to, out var recipientProfileId))
            {
                Debug.LogError($"[TransportAdapter] Cannot send offer - unknown NGO ID: {to}");
                return;
            }

            // Don't send to self
            if (recipientProfileId == localProfileId) return;

            var signal = new SdpOfferDirectSignal();
            signal.setPeerSdp(offer.sdp);
            signal.setProfileId(localProfileId);
            signal.setRecipientProfileId(recipientProfileId);

            var msg = signal.ToJsonString<SdpOfferDirectSignal>();
            Debug.Log($"[TransportAdapter] Sending offer to {recipientProfileId}");

            signalingClient?.SendWSMessage(msg);
        }

        private void HandleSendAnswer(ulong to, RTCSessionDescription answer)
        {
            if (!ngoIdToProfile.TryGetValue(to, out var recipientProfileId))
            {
                Debug.LogError($"[TransportAdapter] Cannot send answer - unknown NGO ID: {to}");
                return;
            }

            // Don't send to self
            if (recipientProfileId == localProfileId) return;

            var signal = new SdpAnswerDirectSignal();
            signal.setPeerSdp(answer.sdp);
            signal.setProfileId(localProfileId);
            signal.setRecipientProfileId(recipientProfileId);

            var msg = signal.ToJsonString<SdpAnswerDirectSignal>();
            Debug.Log($"[TransportAdapter] Sending answer to {recipientProfileId}");

            signalingClient?.SendWSMessage(msg);
        }

        private void HandleSendIce(ulong to, RTCIceCandidate candidate)
        {
            if (!ngoIdToProfile.TryGetValue(to, out var recipientProfileId))
            {
                Debug.LogError($"[TransportAdapter] Cannot send ICE - unknown NGO ID: {to}");
                return;
            }

            // Don't send to self
            if (recipientProfileId == localProfileId) return;

            var signal = new CandidateDirectSignal();
            signal.setProfileId(localProfileId);
            signal.setRecipientProfileId(recipientProfileId);
            signal.setMid(candidate.SdpMid);
            signal.setCandidate(candidate.Candidate);

            var msg = signal.ToJsonString<CandidateDirectSignal>();
            Debug.Log($"[TransportAdapter] Sending ICE candidate to {recipientProfileId}");

            signalingClient?.SendWSMessage(msg);
        }

        private void HandleDataChannelReady(ulong ngoId)
        {
            if (ngoIdToProfile.TryGetValue(ngoId, out var profileId))
            {
                OnPeerReady?.Invoke(profileId);
            }
        }

        private void HandlePeerStateChanged(ulong ngoId, RTCIceConnectionState state)
        {
            if (state == RTCIceConnectionState.Failed ||
                state == RTCIceConnectionState.Disconnected)
            {
                if (ngoIdToProfile.TryGetValue(ngoId, out var profileId))
                {
                    OnPeerDisconnected?.Invoke(profileId);
                }
            }
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}