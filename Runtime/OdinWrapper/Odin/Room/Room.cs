using OdinNative.Core;
using OdinNative.Core.Handles;
using OdinNative.Odin.Media;
using OdinNative.Odin.Peer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Core.Imports.NativeMethods;

namespace OdinNative.Odin.Room
{
    /// <summary>
    /// Main Room
    /// </summary>
    public class Room : IDisposable
    {
        /// <summary>
        /// Room configuration
        /// </summary>
        public readonly RoomConfig Config;
        /// <summary>
        /// true on successful <see cref="Join"/> or false
        /// </summary>
        public bool IsJoined { get; private set; }

        /// <summary>
        /// Client Peer
        /// </summary>
        public Peer.Peer Self { get; internal set; }
        private UserData Data;

        /// <summary>
        /// Conatiner of room peers
        /// </summary>
        public PeerCollection RemotePeers { get; private set; }
        /// <summary>
        /// Get all medias of room peers
        /// </summary>
        public IEnumerable<MediaCollection> PlaybackMedias => RemotePeers.Select(p => p.Medias);
        /// <summary>
        /// Current room microphone data route
        /// </summary>
        public MicrophoneStream MicrophoneMedia { get; internal set; }

        private RoomHandle _Handle;
        internal IntPtr Handle { get { return _Handle.IsInvalid || _Handle.IsClosed ? IntPtr.Zero : _Handle.DangerousGetHandle(); } }
        private TokenGeneratorHandle _AuthHandle;
        internal IntPtr AuthHandle { get { return _AuthHandle.IsInvalid || _AuthHandle.IsClosed ? IntPtr.Zero : _AuthHandle.DangerousGetHandle(); } }

        internal Room(string server, string accessKey, string name, OdinRoomConfig apmConfig = null)
            : this(server, accessKey, name, new OdinMediaConfig(MediaSampleRate.Hz48000, MediaChannels.Mono), apmConfig ?? new OdinRoomConfig(), true)
        { }

        /// <summary>
        /// Create a ODIN ffi room 
        /// </summary>
        /// <param name="server">Endpoint</param>
        /// <param name="accessKey">Room access Key</param>
        /// <param name="name">Room name</param>
        /// <param name="playbackMediaConfig">Config to use for <see cref="MediaStream"/> on new medias</param>
        /// <param name="apmConfig">Config to use for <see cref="OdinRoomConfig"/></param>
        /// <param name="registerEventCallback">true for <see cref="RegisterEventCallback"/> or false for no room events</param>
        public Room(string server, string accessKey, string name, OdinMediaConfig playbackMediaConfig, OdinRoomConfig apmConfig, bool registerEventCallback)
            : this(new RoomConfig()
            {
                AccessKey = accessKey,
                Server = server,
                Name = name,
                PlaybackMediaConfig = playbackMediaConfig,
                ApmConfig = apmConfig,
                HasEventCallbacks = registerEventCallback,
            }) { }

        /// <summary>
        /// Create a ODIN ffi room 
        /// </summary>
        /// <param name="config"><see cref="RoomConfig"/> to use for this room</param>
        public Room(RoomConfig config)
        {
            Config = config;
            IsJoined = false;
            Init();
        }

        private void Init()
        {
            RemotePeers = new PeerCollection();
            _Handle = OdinLibrary.Api.RoomCreate();
            _AuthHandle = OdinLibrary.Api.TokenGeneratorCreate(Config.AccessKey);

            if (Config.HasEventCallbacks)
            {
                // Save the room event delegate for the static OdinClient event Proxy
                EventDelegate = new OdinEventCallback(OdinClient.OnEventReceivedProxy);
                RegisterEventCallback(EventDelegate);
            }

            SetApmConfig(Config.ApmConfig);
        }

        /// <summary>
        /// Set rooms new Apm config
        /// </summary>
        /// <param name="config">new Apm configuration</param>
        /// <returns>true on successful set or false</returns>
        public bool SetApmConfig(OdinRoomConfig config)
        {
            Config.ApmConfig = config;
            return OdinLibrary.Api.RoomConfigure(_Handle, config) == Utility.OK;
        }

        /// <summary>
        /// Join the room direct to a SFU
        /// </summary>
        /// <remarks>Use <see cref="Join(string, UserData)"/> instead</remarks>
        /// <param name="userData">custom userdata</param>
        /// <returns>true on successful join or false</returns>
        public bool Join(UserData userData = null)
        {
            Data = userData;
            byte[] data = Data?.ToBytes() ?? new byte[0];
            return IsJoined = Utility.OK == OdinLibrary.Api.RoomJoin(
                _Handle,
                Config.Server, // server url
                Config.Name,
                string.Empty,
                data,
                data.Length);
        }

        /// <summary>
        /// Join the room via a gateway
        /// </summary>
        /// <remarks>Generates a room token</remarks>
        /// <param name="name">room name</param>
        /// <param name="userId">user id</param>
        /// <param name="customerId">customer id</param>
        /// <param name="userData">custom userdata</param>
        /// <returns>true on successful join or false</returns>
        public bool Join(string name, string userId, string customerId = "", UserData userData = null)
        {
            string token = OdinLibrary.Api.TokenGeneratorCreateToken(_AuthHandle, name, userId, new OdinTokenOptions() 
            { 
                audience = OdinTokenAudience.Gateway,
                customer = customerId,
                lifetime = Config.TokenLifetime
            });

            return Join(token, userData);
        }


        /// <summary>
        /// Join the room via a gateway
        /// </summary>
        /// <remarks>The room token should be generated by <see cref="TokenGeneratorCreateToken"/></remarks>
        /// <param name="token">room token</param>
        /// <param name="userData">custom userdata</param>
        /// <returns>true on successful join or false</returns>
        public bool Join(string token, UserData userData = null)
        {
            Data = userData;
            byte[] data = Data?.ToBytes() ?? new byte[0];
            return IsJoined = Utility.OK == OdinLibrary.Api.RoomJoin(
                _Handle,
                Config.Server, // gateway url
                token,
                data,
                data.Length);
        }

        /// <summary>
        /// Try to add a <see cref="MicrophoneStream"/> to the room and set it to <see cref="MicrophoneMedia"/>
        /// </summary>
        /// <param name="config">Microphone device configuration</param>
        /// <returns>true if media was added to the room or false</returns>
        public bool CreateMicrophoneMedia(OdinMediaConfig config)
        {
            if (!IsJoined) return false;

            MicrophoneStream stream = new MicrophoneStream(config);
            bool result = stream.AddMediaToRoom(_Handle);
            if (result)
                MicrophoneMedia = stream;
            else
                stream.Dispose();

            return result;
        }

        /// <summary>
        /// Send userdata buffer bytes
        /// </summary>
        /// <remarks>Always false if the room is not joined</remarks>
        /// <param name="userData">Userdata to send</param>
        /// <returns>true if userdata was set for the room or false</returns>
        public bool UpdateUserData(UserData userData)
        {
            if (!IsJoined) return false;

            Data = userData;
            byte[] data = Data?.ToBytes();
            if (data == null || data.Length == 0) return false;

            return OdinLibrary.Api.RoomUpdateUserData(_Handle, data, (ulong)data.Length) == Utility.OK;
        }

        /// <summary>
        /// Will set the room <see cref="MicrophoneMedia"/> to mute
        /// </summary>
        /// <remarks>Always false if there is no <see cref="MicrophoneMedia"/> or the room was not joined</remarks>
        /// <param name="mute">true to mute and false to unmute</param>
        /// <returns>true if set or false</returns>
        public bool SetMicrophoneMute(bool mute)
        {
            if (IsJoined == false || MicrophoneMedia == null) return false;
            MicrophoneMedia.IsMuted = mute;
            return true;
        }

        /// <summary>
        /// Leave a room and free all remote peers and associated medias
        /// </summary>
        /// <remarks>This resets the room object for a final close use <see cref="Dispose"/></remarks>
        public void Leave()
        {
            RemotePeers.FreeAll();
            _Handle.DangerousRelease();
            _AuthHandle.DangerousRelease();
            IsJoined = false;
            //Reset
            Init(); 
        }

        #region Events
        private OdinEventCallback EventDelegate { get; set; }

        internal delegate void AkiEventHandler(object sender, OdinEvent e);
        internal static event AkiEventHandler OnEvent;
        [Obsolete]
        internal delegate void RoomEventHandler(object sender, RoomEventArgs e);
        /// <summary>
        /// Passthrough event that identified a new PeerJoined event by Event-Tag.
        /// </summary>
        /// <remarks>Default <see cref="Room"/> sender and <see cref="PeerJoinedEvent"/></remarks>
        public event RoomPeerJoinedEventHandler OnPeerJoined;
        public delegate void RoomPeerJoinedEventHandler(object sender, PeerJoinedEvent e);
        /// <summary>
        /// Passthrough event that identified a new PeerLeft event by Event-Tag.
        /// </summary>
        /// <remarks>Default <see cref="Room"/> sender and <see cref="PeerLeftEvent"/></remarks>
        public event RoomPeerLeftEventHandler OnPeerLeft;
        public delegate void RoomPeerLeftEventHandler(object sender, PeerLeftEvent e);
        /// <summary>
        /// Passthrough event that identified a new PeerUpdated event by Event-Tag.
        /// </summary>
        /// <remarks>Default <see cref="Room"/> sender and <see cref="PeerUpdatedEvent"/></remarks>
        public event RoomPeerUpdatedEventHandler OnPeerUpdated;
        public delegate void RoomPeerUpdatedEventHandler(object sender, PeerUpdatedEvent e);
        /// <summary>
        /// Passthrough event that identified a new MediaAdded event by Event-Tag.
        /// </summary>
        /// <remarks>Default <see cref="Room"/> sender and <see cref="MediaAddedEvent"/></remarks>
        public event RoomMediaAddedEventHandler OnMediaAdded;
        public delegate void RoomMediaAddedEventHandler(object sender, MediaAddedEvent e);
        /// <summary>
        /// Passthrough event that identified a new MediaRemoved event by Event-Tag.
        /// </summary>
        /// <remarks>Default <see cref="Room"/> sender and <see cref="MediaRemovedEvent"/></remarks>
        public event RoomMediaRemovedEventHandler OnMediaRemoved;
        public delegate void RoomMediaRemovedEventHandler(object sender, MediaRemovedEvent e);

        internal void RegisterEventCallback(OdinEventCallback eventCallback)
        {
            OdinLibrary.Api.RoomSetEventCallback(_Handle, eventCallback);
        }

        /// <summary>
        /// Main entry for Room OdinEvents to identify the appropriate event to further passthrough and wrap the arguments.
        /// </summary>
        /// <remarks>Events: PeerJoined, PeerLeft, PeerUpdated, MediaAdded, MediaRemoved</remarks>
        /// <param name="_">this instance</param>
        /// <param name="event">OdinEvent struct</param>
        /// <param name="userDataPtr">userdata pointer</param>
        internal void OnEventReceived(Room _, OdinEvent @event, IntPtr userDataPtr)
        {
            byte[] GetUserData(IntPtr data, ulong size)
            {
                byte[] buffer = new byte[0];
                if (@event.peer_joined.user_data != IntPtr.Zero)
                {
                    buffer = new byte[@event.peer_joined.user_data_len];
                    System.Runtime.InteropServices.Marshal.Copy(@event.peer_joined.user_data, buffer, 0, (int)@event.peer_joined.user_data_len);
                }

                return buffer;
            }

            switch (@event.tag)
            {
                case OdinEventTag.OdinEvent_PeerJoined:
                    // Create peer with userdata
                    byte[] data = GetUserData(@event.peer_joined.user_data, @event.peer_joined.user_data_len);
                    UserData userData = new UserData(data);
                    var peer = new Peer.Peer(@event.peer_left.id, this.Config.Name, userData);
                    RemotePeers.Add(peer);

                    OnPeerJoined?.Invoke(this, new PeerJoinedEvent()
                    {
                        PeerId = @event.peer_left.id,
                        Peer = peer
                    });
                    break;
                case OdinEventTag.OdinEvent_PeerLeft:
                    //remove dangling medias
                    var leavingPeer = RemotePeers[@event.peer_left.id];
                    if (leavingPeer != null)
                    {
                        foreach(var closingMedia in leavingPeer.Medias)
                            OnMediaRemoved?.Invoke(this, new MediaRemovedEvent()
                            {
                                MediaId = (ushort)closingMedia.Id,
                                Peer = leavingPeer,
                                Media = closingMedia
                            });
                    }
                    //remove peer
                    RemotePeers.Free(@event.peer_left.id);

                    OnPeerLeft?.Invoke(this, new PeerLeftEvent()
                    {
                        PeerId = @event.peer_left.id
                    });
                    break;
                case OdinEventTag.OdinEvent_PeerUpdated:
                    // Set new userdata to peer
                    byte[] newData = GetUserData(@event.peer_updated.user_data, @event.peer_updated.user_data_len);
                    RemotePeers[@event.peer_updated.id]?.SetUserData(newData);

                    OnPeerUpdated?.Invoke(this, new PeerUpdatedEvent()
                    {
                        PeerId = @event.peer_updated.id,
                        UserData = newData,
                    });
                    break;
                case OdinEventTag.OdinEvent_MediaAdded:
                    var playbackStream = new PlaybackStream(
                        @event.media_added.media_id,
                        Config.PlaybackMediaConfig,
                        new StreamHandle(@event.media_added.stream));

                    // Add media to peer
                    var mediaPeer = RemotePeers[@event.media_added.peer_id];
                    if (mediaPeer == null) // should only happen if this client (Self) added a media 
                    {
                        //Skip Self medias
                        if (@event.media_added.peer_id == Self?.Id) break;
                        //still add an unknown peer that never joined the room but created a media
                        mediaPeer = new Peer.Peer(@event.media_added.peer_id, this.Config.Name, Data);
                        RemotePeers.Add(mediaPeer);
                    }

                    mediaPeer.AddMedia(playbackStream);

                    OnMediaAdded?.Invoke(this, new MediaAddedEvent()
                    {
                        PeerId = @event.media_added.peer_id,
                        Peer = mediaPeer,
                        Media = playbackStream,
                    });
                    break;
                case OdinEventTag.OdinEvent_MediaRemoved:
                    MediaStream removingMedia = null;
                    // Remove media from peer
                    var peerWithMedia = RemotePeers.FirstOrDefault(p => p.Medias.Any(m => m.Id == @event.media_removed.media_id));
                    if (peerWithMedia == null)
                    {
                        Self?.RemoveMedia(@event.media_removed.media_id);
                    }
                    else
                    {
                        removingMedia = peerWithMedia?.Medias[@event.media_removed.media_id];
                        peerWithMedia?.RemoveMedia(@event.media_removed.media_id);
                    }
                    OnMediaRemoved?.Invoke(this, new MediaRemovedEvent()
                    {
                        MediaId = @event.media_removed.media_id,
                        Peer = peerWithMedia,
                        Media = removingMedia
                    });
                    break;
                case OdinEventTag.OdinEvent_None:
                default:
                    OnEvent?.Invoke(this, @event);
                    break;
            }
        }
        #endregion Events

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    EventDelegate = null;
                    RemotePeers.FreeAll();
                    Self?.Dispose();
                    MicrophoneMedia?.Dispose();
                }

                _Handle.Close();
                disposedValue = true;
            }
        }

        ~Room()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
