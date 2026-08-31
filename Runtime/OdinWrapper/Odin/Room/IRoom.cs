using OdinNative.Core.Imports;
using OdinNative.Wrapper.Media;
using OdinNative.Wrapper.Peer.Rpc;
using OdinNative.Wrapper.Room.Rpc;

namespace OdinNative.Wrapper
{
    public interface IRoom
    {
        /// <summary>
        /// Room id
        /// </summary>
        ulong Id { get; }
        OdinRoomHandle Handle { get; }
        /// <summary>
        /// Default value <c>null</c> indicates root or not set
        /// </summary>
        object Parent { get; }
        /// <summary>
        /// Indicates whether the room is currently joined
        /// </summary>
        bool IsJoined { get; }
        /// <summary>
        /// The local peer for this client in this room.
        /// Null until the room is joined.
        /// Allows SDK components to reference the local peer through the same <see cref="IPeer"/> abstraction
        /// used for remote peers, enabling the native wrapper to be replaced by a web bridge without changing SDK code.
        /// </summary>
        IPeer Self { get; }
        /// <summary>
        /// Encryption handle container
        /// </summary>
        /// <remarks>Can be null for unencrypted</remarks>
        Crypto CryptoCipher { get; }
        /// <summary>
        /// Default encoder/decoder samplerate for this room.
        /// </summary>
        uint Samplerate { get; }
        /// <summary>
        /// Default encoder/decoder stereo flag for this room.
        /// </summary>
        bool Stereo { get; }
        /// <summary>
        /// Room name as reported by the server. Empty until joined.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Join a room with encryption
        /// </summary>
        /// <param name="token">jwt to send</param>
        /// <param name="cipher">crypto cipher</param>
        /// <returns>true on success</returns>
        bool Join(string token, OdinCipherHandle cipher = null);
        /// <summary>
        /// Push audio samples to the room via a specific encoder.
        /// </summary>
        bool SendAudio(float[] samples, MediaEncoder encoder);
        /// <summary>
        /// Get or create a decoder for the given peer and media, using the supplied samplerate and stereo flag.
        /// On create calls through peer: <see cref="OdinNative.Wrapper.PeerEntity.CreateDecoder"/>
        /// </summary>
        bool GetOrCreateDecoder(uint peerId, ulong mediaId, uint samplerate, bool stereo, out MediaDecoder decoder);
        /// <summary>
        /// Create a decoder for the given peer, using the room supplied samplerate and stereo.
        /// </summary>
        MediaDecoder CreateMediaDecoder(uint peerId);
        /// <summary>
        /// Remove the decoder for the given peer and media and dispose media.
        /// </summary>
        void RemoveMediaDecoder(uint peerId, ulong mediaId);
        /// <summary>
        /// Remove the decoder for the given peer and media from room.
        /// </summary>
        bool RemoveDecoder(uint peerId, ulong mediaId, out MediaDecoder decoder);

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
        /// Odin peer changed userdata
        /// </summary>
        event OnPeerChangedDelegate OnPeerChanged;
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
    /// <param name="args">Parsed joined rpc data</param>
    public delegate void OnRoomJoinedDelegate(object sender, JoinedObjectContainer args);
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
    /// <param name="args">Parsed peer joined rpc data</param>
    public delegate void OnPeerJoinedDelegate(object sender, PeerJoinedObjectContainer args);
    /// <summary>
    /// Odin peer left
    /// </summary>
    /// <param name="sender">Room object</param>
    /// <param name="args">Parsed peer left rpc data</param>
    public delegate void OnPeerLeftDelegate(object sender, PeerLeftObjectContainer args);
    /// <summary>
    /// Odin decoder created
    /// </summary>
    /// <remarks>fired when a native decoder is created for a peer</remarks>
    /// <param name="sender">Room object</param>
    /// <param name="peerId"></param>
    /// <param name="mediaId"></param>
    public delegate void OnDecoderCreatedDelegate(object sender, uint peerId, ulong mediaId);
    /// <summary>
    /// Odin decoder removed
    /// </summary>
    /// <remarks>fired when a native decoder is removed for a peer</remarks>
    /// <param name="sender">Room object</param>
    /// <param name="peerId"></param>
    /// <param name="mediaId"></param>
    public delegate void OnDecoderRemovedDelegate(object sender, uint peerId, ulong mediaId);
    /// <summary>
    /// Odin peer changed
    /// </summary>
    /// <param name="sender">Room object</param>
    /// <param name="args">Parsed peer changed rpc data</param>
    public delegate void OnPeerChangedDelegate(object sender, PeerChangedObjectContainer args);
    /// <summary>
    /// Odin room received message
    /// </summary>
    /// <param name="sender">Room object</param>
    /// <param name="peerId"></param>
    /// <param name="message"></param>
    public delegate void OnMessageReceivedDelegate(object sender, uint peerId, byte[] message);
    #endregion
}