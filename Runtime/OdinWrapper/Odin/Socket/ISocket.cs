using OdinNative.Core.Imports;
using OdinNative.Wrapper.Socket;

namespace OdinNative.Wrapper
{
    public interface ISocket
    {
        /// <summary>
        /// Socket id
        /// </summary>
        ulong Id { get; }
        OdinSocketHandle Handle { get; }
        /// <summary>
        /// Default value <c>null</c> indicates root or not set
        /// </summary>
        IPeer Parent { get; }
        IRoom Room { get; }
        /// <summary>
        /// Indicates whether the socket was opened by the remote side (inbound)
        /// </summary>
        bool IsRemote { get; }

        /// <summary>
        /// Get the underlying <see cref="Socket.Socket"/>
        /// </summary>
        /// <returns>native wrapper socket</returns>
        T GetBaseSocket<T>() where T : OdinNative.Wrapper.ISocket;
        void OnSocket(IRoom room, SocketMessageEventArgs args);
        /// <summary>
        /// Odin socket received message
        /// </summary>
        event OnSocketMessageReceivedDelegate OnMessageReceived;
    }

    #region Events
    /// <summary>
    /// Odin socket received message
    /// </summary>
    /// <param name="sender">Socket object</param>
    /// <param name="room">Room the socket belongs to</param>
    /// <param name="message">message payload</param>
    public delegate void OnSocketMessageReceivedDelegate(object sender, IRoom room, byte[] message);
    #endregion
}