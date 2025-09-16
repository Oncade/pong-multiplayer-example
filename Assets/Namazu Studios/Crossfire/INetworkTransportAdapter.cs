using System;
using Unity.Netcode;

namespace Elements.Crossfire
{
    using Model;

    /// <summary>
    /// Core transport abstraction
    /// </summary>
    public interface INetworkTransportAdapter
    {
        event Action<string> OnPeerReady;
        event Action<string> OnPeerDisconnected;

        void Initialize(NetworkManager networkManager);
        void BeginConnection(string peerId, bool isOfferer);
        void DisconnectPeer(string peerId);
        void Shutdown();
        bool IsPeerReady(string peerId);

        // For handling signaling messages
        void HandleSignalingMessage(MessageType messageType, string fromPeerId, string payload);
    }
}