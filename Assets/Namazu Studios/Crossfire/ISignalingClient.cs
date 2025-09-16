using System;

namespace Elements.Crossfire
{
    using Model;

    /// <summary>
    /// Signaling abstraction
    /// </summary>
    public interface ISignalingClient
    {
        event Action OnConnected;
        event Action<SignalingMessage> OnMessageReceived;
        event Action OnDisconnected;

        void Connect(string url, string profileId, string sessionToken);
        void SendWSMessage(string message);
        void Disconnect();
        bool IsConnected { get; }
    }
}