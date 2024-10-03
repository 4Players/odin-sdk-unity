using OdinNative.Core.Imports;
using OdinNative.Wrapper.Media;
using OdinNative.Wrapper.Peer;
using System.Collections.ObjectModel;

namespace OdinNative.Wrapper
{
    public interface IRoom
    {
        /// <summary>
        /// Room id
        /// </summary>
        ulong Id { get; }
        /// <summary>
        /// Default value <code>null</code> indicates root or not set
        /// </summary>
        object Parent { get; }
        /// <summary>
        /// The current room is initialized
        /// </summary>
        bool IsJoined { get; }
        /// <summary>
        /// Encryption handle container
        /// </summary>
        /// <remarks>Can be null for unencrypted</remarks>
        Crypto CryptoCipher { get; }

        /// <summary>
        /// Join a room
        /// </summary>
        /// <param name="token">jwt to send</param>
        /// <returns>true on success</returns>
        bool Join(string token);
        /// <summary>
        /// Join a room with encryption
        /// </summary>
        /// <param name="token">jwt to send</param>
        /// <param name="cipher">crypto cipher</param>
        /// <returns>true on success</returns>
        bool Join(string token, OdinCipherHandle cipher);
        /// <summary>
        /// Get the underlying <see cref="Room.Room"/>
        /// </summary>
        /// <returns>native wrapper room</returns>
        T GetBaseRoom<T>() where T : OdinNative.Wrapper.IRoom;

        /// <summary>
        /// Odin connection status
        /// </summary>
        event OnRoomStatusChangedDelegate OnRoomStatusChanged;
        /// <summary>
        /// Odin room joined
        /// </summary>
        event OnRoomJoinedDelegate OnRoomJoined;
        /// <summary>
        /// Odin room left
        /// </summary>
        event OnRoomLeftDelegate OnRoomLeft;
        /// <summary>
        /// Odin peer joined
        /// </summary>
        event OnPeerJoinedDelegate OnPeerJoined;
        /// <summary>
        /// Odin peer left
        /// </summary>
        event OnPeerLeftDelegate OnPeerLeft;
        /// <summary>
        /// Odin media started
        /// </summary>
        event OnMediaStartedDelegate OnMediaStarted;
        /// <summary>
        /// Odin media stopped
        /// </summary>
        event OnMediaStoppedDelegate OnMediaStopped;
        /// <summary>
        /// Odin peer changed userdata
        /// </summary>
        event OnUserDataChangedDelegate OnUserDataChanged;
        /// <summary>
        /// Odin room received message
        /// </summary>
        event OnMessageReceivedDelegate OnMessageReceived;
    }

    #region Events
    /// <summary>
    /// Odin connection status
    /// </summary>
    /// <param name="sender">Room object</param>
    /// <param name="roomStatus">string status</param>
    public delegate void OnRoomStatusChangedDelegate(object sender, string roomStatus);
    /// <summary>
    /// Odin room joined bookkeeping
    /// </summary>
    /// <param name="sender">Room object</param>
    /// <param name="ownPeerId">Self id</param>
    /// <param name="name">Room name</param>
    /// <param name="customer">customer id</param>
    /// <param name="roomUserData">arbitrary datas</param>
    /// <param name="mediaIds">raw media ids in remote room</param>
    /// <param name="peers">Parsed peer rpc data</param>
    public delegate void OnRoomJoinedDelegate(object sender, ulong ownPeerId, string name, string customer, byte[] roomUserData, ushort[] mediaIds, ReadOnlyCollection<PeerRpc> peers);
    /// <summary>
    /// Odin room left
    /// </summary>
    /// <remarks>The Left event is usually only received by server side force</remarks>
    /// <param name="sender">Room object</param>
    /// <param name="reason">indicate leaving reason</param>
    public delegate void OnRoomLeftDelegate(object sender, string reason);
    /// <summary>
    /// Odin peer joined wrapped data
    /// </summary>
    /// <param name="sender">Room object</param>
    /// <param name="peerId"></param>
    /// <param name="userId"></param>
    /// <param name="userData"></param>
    /// <param name="medias"></param>
    public delegate void OnPeerJoinedDelegate(object sender, ulong peerId, string userId, byte[] userData, MediaRpc[] medias);
    /// <summary>
    /// Odin peer left
    /// </summary>
    /// <param name="sender">Room object</param>
    /// <param name="peerId"></param>
    public delegate void OnPeerLeftDelegate(object sender, ulong peerId);
    /// <summary>
    /// Odin media started
    /// </summary>
    /// <remarks>encoder/decoder</remarks>
    /// <param name="sender">Room object</param>
    /// <param name="peerId"></param>
    /// <param name="media"></param>
    public delegate void OnMediaStartedDelegate(object sender, ulong peerId, MediaRpc media);
    /// <summary>
    /// Odin media stopped
    /// </summary>
    /// <remarks>encoder/decoder</remarks>
    /// <param name="sender">Room object</param>
    /// <param name="peerId"></param>
    /// <param name="mediaId"></param>
    public delegate void OnMediaStoppedDelegate(object sender, ulong peerId, ushort mediaId);
    /// <summary>
    /// Odin peer changed userdata
    /// </summary>
    /// <param name="sender">Room object</param>
    /// <param name="peerId"></param>
    /// <param name="userData"></param>
    public delegate void OnUserDataChangedDelegate(object sender, ulong peerId, byte[] userData);
    /// <summary>
    /// Odin room received message
    /// </summary>
    /// <param name="sender">Room object</param>
    /// <param name="peerId"></param>
    /// <param name="message"></param>
    public delegate void OnMessageReceivedDelegate(object sender, ulong peerId, byte[] message);
    #endregion
}