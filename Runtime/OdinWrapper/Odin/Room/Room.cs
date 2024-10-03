using OdinNative.Core;
using OdinNative.Core.Imports;
using OdinNative.Utils.MessagePack;
using OdinNative.Wrapper.Media;
using OdinNative.Wrapper.Peer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Wrapper.Room
{
    /// <summary>
    /// Main Room
    /// </summary>
    public class Room : IRoom, IDisposable
    {
        /// <summary>
        /// Room default samplerate
        /// </summary>
        public readonly uint Samplerate;
        /// <summary>
        /// Room default stereo flag
        /// </summary>
        public readonly bool Stereo;
        /// <summary>
        /// Room server gateway endpoint
        /// </summary>
        public string EndPoint { get; private set; }
        internal OdinRoomHandle Handle { get { return _handle; } private set { _handle = value; } }
        private OdinRoomHandle _handle;

        /// <summary>
        /// RoomId
        /// </summary>
        public ulong Id => IsClosed ? _Id : _Id = GetRoomId();
        private ulong _Id;
        /// <summary>
        /// PeerId of self
        /// </summary>
        public ulong OwnPeerId { get; private set; }
        /// <summary>
        /// Room name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// RoomStatus
        /// </summary>
        public string RoomStatus { get; private set; }
        /// <summary>
        /// IsJoined
        /// </summary>
        public bool IsJoined => RoomStatus?.Equals("Joined") ?? false;
        /// <summary>
        /// IsClosed
        /// </summary>
        public bool IsClosed => disposedValue || (RoomStatus?.Equals("Closed") ?? true);
        private bool _InTransition;
        /// <summary>
        /// Inital position of self on join
        /// </summary>
        public float PositionX { get; private set; }
        /// <summary>
        /// Inital position of self on join
        /// </summary>
        public float PositionY { get; private set; }
        /// <summary>
        /// Inital position of self on join
        /// </summary>
        public float PositionZ { get; private set; }
        /// <summary>
        /// Crypto cipher
        /// </summary>
        public Crypto CryptoCipher { get; private set; }
        /// <summary>
        /// Msgpack writer for RPC
        /// </summary>
        public IMsgPackWriter RpcWriter { get; set; }
        protected internal uint RpcId { get; private set; }
        /// <summary>
        /// Toggle message type for <c>Room.SendRpc</c> 
        /// true is request and false is notification.
        /// <remarks>Currently calls to "UpdatePeer" and "SetPeerPosition" needs to be requests!</remarks>
        /// </summary>
        public bool RpcAckActive { get; set; }
        /// <summary>
        /// Msgpack results to RPC requests
        /// </summary>
        public ConcurrentDictionary<uint, TaskCompletionSource<RpcResult>> RpcTableThunk { get; private set; }

        /// <summary>
        /// Odin UserData helper for marshal byte arrays on Room level
        /// </summary>
        public IUserData RoomUserData { get; private set; }

        /// <summary>
        /// Conatiner of room peers
        /// </summary>
        public ConcurrentDictionary<ulong, PeerEntity> RemotePeers { get; private set; }
        /// <summary>
        /// Container of room input medias
        /// </summary>
        public ConcurrentDictionary<ushort, MediaEncoder> Encoders { get; private set; }
        /// <summary>
        /// Elements of room output medias
        /// </summary>
        public IEnumerable<MediaDecoder> Decoders => RemotePeers.Values.SelectMany(p => p.Medias.Values);
        /// <summary>
        /// Room joining token
        /// </summary>
        public string Token { get; private set; }

        /// <summary>
        /// Default value <code>null</code> indicates root or not set
        /// </summary>
        public object Parent { get; set; }
        /// <summary>
        /// Available media ids that are reserved for the room
        /// </summary>
        /// <remarks>Contains a set of reserved ids that are free to use with input medias by the server.</remarks>
        public Queue<ushort> AvailableEncoderIds;

        #region Events
        /// <summary>
        /// Call on audio data
        /// </summary>
        public event EventHandler<DatagramEventArgs> OnDatagram;
        protected internal void OnDatagramReceived(DatagramEventArgs e) => OnDatagram?.Invoke(this, e);
        /// <summary>
        /// Call on rpc data
        /// </summary>
        public event EventHandler<RpcEventArgs> OnRpc;
        /// <summary>
        /// Call on response to a rpc request
        /// </summary>
        public event EventHandler<RpcResult> OnSendRpcResponse;
        protected internal void OnRPCReceived(RpcEventArgs e) => OnRpc?.Invoke(this, e);
        /// <summary>
        /// Odin connection status
        /// </summary>
        public event OnRoomStatusChangedDelegate OnRoomStatusChanged;
        /// <summary>
        /// Odin room joined
        /// </summary>
        public event OnRoomJoinedDelegate OnRoomJoined;
        /// <summary>
        /// Odin room left
        /// </summary>
        /// <remarks>The Left event is usually only received by server side force</remarks>
        public event OnRoomLeftDelegate OnRoomLeft;
        /// <summary>
        /// Odin peer joined
        /// </summary>
        public event OnPeerJoinedDelegate OnPeerJoined;
        /// <summary>
        /// Odin peer left
        /// </summary>
        public event OnPeerLeftDelegate OnPeerLeft;
        /// <summary>
        /// Odin media started
        /// </summary>
        /// <remarks>encoder/decoder</remarks>
        public event OnMediaStartedDelegate OnMediaStarted;
        /// <summary>
        /// Odin media stopped
        /// </summary>
        /// <remarks>encoder/decoder</remarks>
        public event OnMediaStoppedDelegate OnMediaStopped;
        /// <summary>
        /// Odin peer changed userdata
        /// </summary>
        public event OnUserDataChangedDelegate OnUserDataChanged;
        /// <summary>
        /// Odin room received message
        /// </summary>
        public event OnMessageReceivedDelegate OnMessageReceived;
        #endregion

        private OdinConnectionPoolSettings _connectionPoolSettings;
        private OdinConnectionPoolHandle _connectionPoolHandle;
        /// <summary>
        /// Initialise dangling room
        /// </summary>
        /// <remarks>For creating an independent room use <see cref="Room.Create"/></remarks>
        /// <param name="connectionPoolHandle">Rooms connection handle</param>
        /// <param name="endPoint">Gateway server</param>
        /// <param name="samplerate">sets default samplerate</param>
        /// <param name="stereo">sets default stereo flag</param>
        public Room(OdinConnectionPoolHandle connectionPoolHandle, string endPoint, uint samplerate, bool stereo)
        {
            _handle = (OdinRoomHandle)IntPtr.Zero;
            Name = string.Empty;
            RoomStatus = string.Empty;
            Token = string.Empty;
            PositionX = 0f;
            PositionY = 0f;
            PositionZ = 0f;
            CryptoCipher = null;
            Encoders = new ConcurrentDictionary<ushort, MediaEncoder>();
            RemotePeers = new ConcurrentDictionary<ulong, PeerEntity>();
            RoomUserData = new UserData();
            AvailableEncoderIds = new Queue<ushort>();
            Samplerate = samplerate;
            Stereo = stereo;
            _connectionPoolSettings = new OdinConnectionPoolSettings()
            {
                OnDatagram = this.OnNativeDatagramReceived,
                OnRPC = this.OnNativeRPCReceived,
            };
            _connectionPoolHandle = connectionPoolHandle;
            EndPoint = endPoint;
            RpcWriter = new MsgPackWriter();
            RpcId = 0;
            RpcAckActive = true; // default send rpc as request
            RpcTableThunk = new ConcurrentDictionary<uint, TaskCompletionSource<RpcResult>>();

            SubscribeEvents();
        }

        /// <summary>
        /// Initialise independent room
        /// </summary>
        /// <param name="endPoint">Gateway server</param>
        /// <param name="samplerate">sets default samplerate</param>
        /// <param name="stereo">sets default stereo flag</param>
        /// <returns>Room object</returns>
        public static Room Create(string endPoint, uint samplerate = OdinDefaults.SampleRate, bool stereo = OdinDefaults.Stereo)
        {
            Room result = new Room(null, endPoint, samplerate, stereo);
            result.CreateConnectionPool();

            return result;
        }
        /// <summary>
        /// This will always return itself
        /// </summary>
        public T GetBaseRoom<T>() where T : IRoom
        {
            return (T)GetBaseRoom();
        }
        private IRoom GetBaseRoom()
        {
            return this;
        }

        private void CreateConnectionPool() => CreateConnectionPool(_connectionPoolSettings);
        private void CreateConnectionPool(OdinConnectionPoolSettings settings)
        {
            _connectionPoolSettings = settings;
            var connectionResult = Odin.Library.Methods.ConnectionPoolCreate(settings, out _connectionPoolHandle);
            Utility.Assert(Utility.IsOk(connectionResult), $"{nameof(Room)} internal {nameof(CreateConnectionPool)} {nameof(Odin.Library.Methods.ConnectionPoolCreate)} failed. {Utility.OdinErrorToString(connectionResult)} {Utility.OdinLastErrorString()}");
        }

        private void SubscribeEvents()
        {
            OnDatagram += Room_OnDatagram;
            OnRpc += Room_OnRPC;
            OnRoomStatusChanged += Room_OnRoomStatusChanged;
            OnRoomJoined += Room_OnRoomJoined;
            OnRoomLeft += Room_OnRoomLeft;
            OnPeerJoined += Room_OnPeerJoined;
            OnPeerLeft += Room_OnPeerLeft;
            OnMediaStarted += Room_OnMediaStarted;
            OnMediaStopped += Room_OnMediaStopped;
            OnUserDataChanged += Room_OnUserDataChanged;
            OnMessageReceived += Room_OnMessageReceived;
        }

        [AOT.MonoPInvokeCallback(typeof(NativeLibraryMethods.OdinConnectionPoolOnDatagramDelegate))]
        private void OnNativeDatagramReceived(ulong room_id, ushort media_id, IntPtr bytesPtr, uint bytes_length, MarshalByRefObject user_data)
        {
            Utility.Assert(bytesPtr != IntPtr.Zero, $"{nameof(Room)} room {room_id} internal {nameof(OdinConnectionPoolHandle)} {nameof(OnNativeDatagramReceived)} datagram pointer should not be zero");
            Utility.Assert(bytes_length > 0, $"{nameof(Room)} room {room_id} internal {nameof(OdinConnectionPoolHandle)} {nameof(OnNativeDatagramReceived)} datagram should not be empty");

            byte[] datagramPayload = Utility.GetNativeBuffer(bytesPtr, bytes_length);

            if (this.Id == room_id)
            {
                this.OnDatagram?.Invoke(this, new DatagramEventArgs()
                {
                    RoomId = room_id,
                    Datagram = bytesPtr,
                    Payload = datagramPayload,
                    Userdata = user_data
                });
            }
        }

        [AOT.MonoPInvokeCallback(typeof(NativeLibraryMethods.OdinConnectionPoolOnRPCDelegate))]
        private void OnNativeRPCReceived(ulong room_id, IntPtr bytesPtr, uint bytes_length, MarshalByRefObject user_data)
        {
            Utility.Assert(bytesPtr != IntPtr.Zero, $"{nameof(Room)} room {room_id} internal {nameof(OdinConnectionPoolHandle)} {nameof(OnNativeRPCReceived)} rpc pointer should not be zero");
            Utility.Assert(bytes_length > 0, $"{nameof(Room)} room {room_id} internal {nameof(OdinConnectionPoolHandle)} {nameof(OnNativeRPCReceived)} rpc data should not be empty");

            byte[] rpcPayload = Utility.GetNativeBuffer(bytesPtr, bytes_length);

            if (this.Id == room_id)
            {
                this.OnRpc?.Invoke(this, new RpcEventArgs()
                {
                    RoomId = room_id,
                    Rpc = rpcPayload,
                    Userdata = user_data
                });
            }
        }

        private void Room_OnPeerJoined(object sender, ulong peerId, string userId, byte[] userData, MediaRpc[] medias)
        {
            RemotePeers.TryAdd(peerId, new PeerEntity(peerId)
            {
                UserId = userId,
                UserData = new UserData(userData)
            });
        }

        /// <summary>
        /// Remove and dispose peer of <see cref="RemotePeers"/>
        /// </summary>
        protected virtual void Room_OnPeerLeft(object sender, ulong peerId)
        {
            if (RemotePeers.TryRemove(peerId, out PeerEntity peer))
                peer?.Dispose();
        }

        /// <summary>
        /// Add new created decoder to <see cref="RemotePeers"/> by id
        /// </summary>
        protected virtual void Room_OnMediaStarted(object sender, ulong peerId, MediaRpc media)
        {
            if (RemotePeers.TryGetValue(peerId, out PeerEntity peer))
            {
                var decoder = MediaDecoder.Create(media.Id, Samplerate, Stereo);
                peer?.Medias.TryAdd(media.Id, decoder);
            }
        }

        /// <summary>
        /// Remove and dispose decoder of <see cref="RemotePeers"/> by media id
        /// </summary>
        protected virtual void Room_OnMediaStopped(object sender, ulong peerId, ushort mediaId)
        {
            if (this.RemoveDecoder(peerId, mediaId, out MediaDecoder decoder))
                decoder?.Dispose();
        }

        /// <summary>
        /// Set userdata of <see cref="RemotePeers"/> by id
        /// </summary>
        protected virtual void Room_OnUserDataChanged(object sender, ulong peerId, byte[] userData)
        {
            if (RemotePeers.TryGetValue(peerId, out PeerEntity peer))
                peer?.SetUserData(userData);
        }

        /// <summary>
        /// Log message in Debug 
        /// </summary>
        protected virtual void Room_OnMessageReceived(object sender, ulong peerId, byte[] message)
        {
            Debug.WriteLine($"{nameof(Room_OnMessageReceived)}(id {Id}) peer {peerId} message {message.Length}");
            Debug.WriteLine($"\t{Encoding.UTF8.GetString(message)}");
        }

        /// <summary>
        /// Set <see cref="RemotePeers"/> for bookkeeping and AvailableEncoderIds for encoders
        /// </summary>
        protected virtual void Room_OnRoomJoined(object sender, ulong ownPeerId, string name, string customer, byte[] roomUserData, ushort[] mediaIds, ReadOnlyCollection<PeerRpc> peers)
        {
            OwnPeerId = ownPeerId;
            Name = name;
            RoomUserData = new UserData(roomUserData);

            RemotePeers = new ConcurrentDictionary<ulong, PeerEntity>(peers.Select(rpc =>
                    new PeerEntity(rpc.Id)
                    {
                        UserId = rpc.UserId,
                        UserData = new UserData(rpc.UserData),
                        Medias = new ConcurrentDictionary<ushort, MediaDecoder>(rpc.Medias.Select(m =>
                        {
                            var decoder = MediaDecoder.Create(m.Id, Samplerate, Stereo);
                            decoder.MediaProperties = m.Properties;
                            decoder.IsPaused = m.Paused;
                            return new KeyValuePair<ushort, MediaDecoder>(m.Id, decoder);
                        }))
                    })
                    .ToDictionary(kvp => kvp.Id));

            AvailableEncoderIds = new Queue<ushort>(mediaIds);
            Utility.Assert(AvailableEncoderIds.Count > 0, $"{nameof(Room)} no available media id for room {Name}");
        }

        /// <summary>
        /// This close the current room
        /// </summary>
        /// <remarks>cleanup the forced leave</remarks>
        /// <param name="sender">room</param>
        /// <param name="reason">event reason</param>
        protected virtual void Room_OnRoomLeft(object sender, string reason)
        {
            _InTransition = true;
            this.Close();
        }

        /// <summary>
        /// Set the RoomStatus and flag for transition
        /// </summary>
        protected virtual void Room_OnRoomStatusChanged(object sender, string connectionStatus)
        {
            RoomStatus = connectionStatus;
            if(RoomStatus.Equals("Joining"))
                _InTransition = true;
            else if (IsJoined || IsClosed)
                _InTransition = false;

            if (_InTransition && string.IsNullOrEmpty(RoomStatus))
                Utility.Assert(_InTransition, "Auto reconnect from hot-reload currently not supported!");
        }

        /// <summary>
        /// Default impl will push a datagram to all <see cref="Decoders"/> of the same mediaId in the current room.
        /// </summary>
        /// <remarks>The room will drop a datagram if there is no decoder to push</remarks>
        /// <param name="sender">Room object</param>
        /// <param name="e">Datagram payload</param>
        public virtual void Room_OnDatagram(object sender, DatagramEventArgs e)
        {
            Utility.Assert(e.Datagram != IntPtr.Zero, $"{nameof(DatagramEventArgs)} {nameof(Room_OnDatagram)} datagram pointer should not be zero");
            if (OdinDefaults.Debug) Debug.WriteLine($"{nameof(Room_OnDatagram)}(id {Id}) room {e.RoomId} datagramSize {e.Payload.Length}");
            foreach (MediaDecoder decoder in Decoders)
                decoder.Push(e.Datagram, e.Payload.Length);
        }

        /// <summary>
        /// Default impl will process all rpc packets.
        /// </summary>
        /// <remarks>Client side processing of <see cref="OdinNative.Utils.MessagePack.MsgPackMessageType.Request"/> is not supported and will always response with <c>"MessagePack requests are not supported!"</c> or drop the request on error</remarks>
        /// <param name="sender">Room object</param>
        /// <param name="e">RPC payload</param>
        public virtual void Room_OnRPC(object sender, RpcEventArgs e)
        {
            if (OdinDefaults.Debug) Debug.WriteLine($"{nameof(Room_OnRPC)}(id {Id}) rpcSize {e.Rpc.Length}\n{BitConverter.ToString(e.Rpc).Replace("-", " ")}");
            ProcessRPC(e.Rpc);
        }

        #region ParseRpc
        /// <summary>
        /// Will process all rpc packets.
        /// </summary>
        /// <remarks>Client side processing of <see cref="OdinNative.Utils.MessagePack.MsgPackMessageType.Request"/> is not supported and will always response with <c>"MessagePack requests are not supported!"</c> or drop the request on error</remarks>
        /// <param name="bytes">raw Msgpack</param>
        public virtual void ProcessRPC(byte[] bytes)
        {
            var reader = Utils.MessagePack.MsgPackReader.Create(bytes);
            int id = -1;
            try { id = reader[RpcFormat.TypeIndex].GetInt(); } catch { return; }
            MsgPackMessageType rpcType = (MsgPackMessageType)id;

            switch (rpcType)
            {
                case MsgPackMessageType.Request:
                    try
                    {
                        using (MsgPackWriter writer = new MsgPackWriter())
                        {
                            writer.WriteArrayHeader(4);
                            writer.Write((int)MsgPackMessageType.Response);
                            writer.Write(reader[RpcFormat.Request.MsgidIndex].GetUInt());
                            writer.WriteString("MessagePack requests are not supported!");
                            writer.WriteByte(MsgPackToken.Nil);
                            SendRpc(writer.GetBytes());
                        };
                    } catch { /* drop error */ }
                    return;
                case MsgPackMessageType.Response:
                    ResponseParse(reader);
                    break;
                case MsgPackMessageType.Notification:
                    NortificationParse(reader);
                    break;
                default:
                    Utility.Assert(false, $"{nameof(ProcessRPC)} type {id} (@{RpcFormat.TypeIndex}) NotImplemented");
                    break;
            }
        }

        private bool ResponseParse(MsgPackReader reader)
        {
            uint msgid = reader[RpcFormat.Response.MsgidIndex].GetUInt();
            string error = reader[RpcFormat.Response.ErrorIndex].GetString(); // Nil == ""
            object result = null;

            var resultField = reader[RpcFormat.Response.ResultIndex];
            if (MsgPackToken.IsFixMap(resultField.GetFormatCode()))
            {
                if (MsgPackToken.IsFixMap(resultField["properties"].GetFormatCode()))
                {
                    result = new MediaRpcProperties()
                    {
                        Kind = resultField["properties"]["kind"].GetString(),
                        uId = resultField["properties"]["uid"].GetString(),
                    };
                }
            }
            else if (MsgPackToken.IsString(resultField.GetFormatCode()))
                result = resultField.GetString(); // Nil == ""

            if (RpcAckActive == false)
                return false;

            if (RpcTableThunk.TryRemove(msgid, out TaskCompletionSource<RpcResult> completion))
            {
                var args = new RpcResult { Id = msgid, Error = error, Value = result };
                var ret = completion.TrySetResult(args);
                OnSendRpcResponse?.Invoke(this, args);
                return ret;
            }

            return false;
        }

        private void NortificationParse(MsgPackReader reader)
        {
            string method = reader[RpcFormat.Notification.MethodIndex].GetString();
            if (string.IsNullOrEmpty(method)) return;

            switch (method)
            {
                case "RoomStatusChanged":
                    var roomStatus = reader[RpcFormat.Notification.ParamsIndex];
                    RoomStatusChangedRPC(method, roomStatus);
                    break;
                case "RoomUpdated":
                    var roomUpdates = reader[RpcFormat.Notification.ParamsIndex]["updates"];
                    RoomUpdatedRpc(method, roomUpdates);
                    break;
                case "PeerUpdated":
                    var peerUpdate = reader[RpcFormat.Notification.ParamsIndex];
                    PeerUpdatedRpc(method, peerUpdate);
                    break;
                case "MessageReceived":
                    var message = reader[RpcFormat.Notification.ParamsIndex];
                    OnMessageReceived?.Invoke(this,
                        message["sender_peer_id"].GetUInt(),
                        message["message"].GetBinary());
                    break;
                default:
                    Utility.Assert(false, $"{nameof(NortificationParse)} method {method} NotImplemented");
                    break;
            }
        }

        private void RoomStatusChangedRPC(string method, MsgPackReader roomStatus)
        {
            string connectionStatus = string.Empty;
            byte statusToken = roomStatus.GetFormatCode();
            if (MsgPackToken.IsFixMap(statusToken) || MsgPackToken.IsMap(statusToken))
            {
                var kvp = roomStatus
                    .AsMapEnumerable()
                    .FirstOrDefault(kvp => kvp.Key.Equals("status"));
                Utility.Assert(!string.IsNullOrEmpty(kvp.Key), $"invalid RPC status key for method \"{method}\"");
                if (string.IsNullOrEmpty(kvp.Key) == false)
                    connectionStatus = kvp.Value.GetString();
            }
            else if (MsgPackToken.IsString(statusToken)) // backwards compatibility
                connectionStatus = roomStatus.GetString();

            OnRoomStatusChanged?.Invoke(this, connectionStatus);
        }

        private void PeerUpdatedRpc(string method, MsgPackReader peerUpdate)
        {
            string submethod = peerUpdate["kind"].GetString();
            switch (submethod)
            {
                case "MediaStarted":
                    OnMediaStarted?.Invoke(this,
                        peerUpdate["peer_id"].GetUInt(),
                        new MediaRpc()
                        {
                            Id = peerUpdate["media"]["id"].GetUShort(),
                            Paused = peerUpdate["media"]["paused"].GetBool()
                        });
                    break;
                case "MediaStopped":
                    OnMediaStopped?.Invoke(this,
                        peerUpdate["peer_id"].GetUInt(),
                        peerUpdate["media_id"].GetUShort());
                    break;
                case "UserDataChanged":
                    OnUserDataChanged?.Invoke(this,
                        peerUpdate["peer_id"].GetUInt(),
                        peerUpdate["user_data"].GetBinary());
                    break;
                default:
                    Utility.Assert(false, $"{nameof(PeerUpdatedRpc)} {method} kind {submethod} NotImplemented");
                    break;
            }
        }

        private void RoomUpdatedRpc(string method, MsgPackReader roomUpdates)
        {
            string submethod = string.Empty;
            foreach (var update in roomUpdates.AsArrayEnumerable())
            {
                submethod = update["kind"].GetString();
                switch (submethod)
                {
                    case "Joined":
                        RoomJoinedRpc(update);
                        break;
                    case "Left":
                        string reason = MsgPackToken.IsString(update["reason"].GetFormatCode()) ? update["reason"].GetString() : update["reason"].GetFormatName();
                        OnRoomLeft?.Invoke(this, reason);
                        break;
                    case "PeerJoined":
                        var joinedPeer = update["peer"];
                        RoomPeerJoinedRpc(joinedPeer);
                        break;
                    case "PeerLeft":
                        OnPeerLeft?.Invoke(this, update["peer_id"].GetUInt());
                        break;
                    case "UserDataChanged":
                        RoomUserData = new UserData(update["user_data"].GetBinary());
                        break;
                    default:
                        Utility.Assert(false, $"{nameof(RoomUpdatedRpc)} {method} kind {submethod} NotImplemented");
                        break;
                }
            }
        }

        private void RoomPeerJoinedRpc(MsgPackReader joinedPeer)
        {
            List<MediaRpc> joinedMedias = new List<MediaRpc>();
            foreach (var joinedMedia in joinedPeer["medias"].AsArrayEnumerable())
            {
                var media = new MediaRpc()
                {
                    Id = joinedMedia["id"].GetUShort(),
                    //Properties = new MediaRpcProperties()
                    //{
                    //    Kind = joinedMedia["properties"]["kind"].GetString(),
                    //    uId = joinedMedia["properties"]["uid"].GetString()
                    //},
                    Paused = joinedMedia["paused"].GetBool()
                };
                joinedMedias.Add(media);
            }

            OnPeerJoined?.Invoke(this,
                joinedPeer["id"].GetUInt(),
                joinedPeer["user_id"].GetString(),
                joinedPeer["user_data"].GetBinary(),
                joinedMedias.ToArray());
        }

        private void RoomJoinedRpc(MsgPackReader update)
        {
            List<PeerRpc> peers = new List<PeerRpc>();
            foreach (var pData in update["room"]["peers"].AsArrayEnumerable())
            {
                var peer = new PeerRpc()
                {
                    Id = pData["id"].GetUInt(),
                    UserId = pData["user_id"].GetString(),
                    UserData = new UserData(pData["user_data"].GetBinary()),
                    Medias = new List<MediaRpc>()
                };

                foreach (var mData in pData["medias"].AsArrayEnumerable())
                {
                    var media = new MediaRpc()
                    {
                        Id = mData["id"].GetUShort(),
                        Paused = mData["paused"].GetBool()
                    };
                    peer.Medias.Add(media);
                }

                peers.Add(peer);
            }

            OnRoomJoined?.Invoke(this,
                update["own_peer_id"].GetUInt(),
                update["room"]["id"].GetString(),
                update["room"]["customer"].GetString(),
                update["room"]["user_data"].GetBinary(),
                update["media_ids"].AsArrayEnumerable().Select(v => v.GetUShort()).ToArray(),
                peers.AsReadOnly());
        }
        #endregion ParseRpc

        /// <summary>
        /// Create and join a Room
        /// </summary>
        /// <param name="endPoint">Gateway server</param>
        /// <param name="token">Join token</param>
        /// <param name="samplerate">sets default samplerate</param>
        /// <param name="stereo">sets default stereo flag</param>
        /// <param name="room">Initialized room</param>
        /// <returns>true on successfully request join or false</returns>
        public static bool Join(string endPoint, string token, uint samplerate, bool stereo, out Room room)
        {
            room = Create(endPoint, samplerate, stereo);
            return room.Join(token);
        }

        /// <summary>
        /// Create and join a Room
        /// </summary>
        /// <param name="connectionPoolHandle">Connection pool for the room</param>
        /// <param name="endPoint">Gateway server</param>
        /// <param name="token">Join token</param>
        /// <param name="samplerate">sets default samplerate</param>
        /// <param name="stereo">sets default stereo flag</param>
        /// <param name="room">Initialized room</param>
        /// <returns>true on successfully request join or false</returns>
        public static bool Join(OdinConnectionPoolHandle connectionPoolHandle, string endPoint, string token, uint samplerate, bool stereo, out Room room)
        {
            room = new Room(connectionPoolHandle, endPoint, samplerate, stereo);
            return room.Join(token);
        }
        /// <summary>
        /// Create and join a Room
        /// </summary>
        /// <param name="connectionPoolHandle">Connection pool for the room</param>
        /// <param name="endPoint">Gateway server</param>
        /// <param name="token">Join token</param>
        /// <param name="roomName">initial room name</param>
        /// <param name="userData">initial userdata</param>
        /// <param name="positionX">initial position</param>
        /// <param name="positionY">initial position</param>
        /// <param name="positionZ">initial position</param>
        /// <param name="samplerate">sets default samplerate</param>
        /// <param name="stereo">sets default stereo flag</param>
        /// <param name="room">Initialized room</param>
        /// <returns>true on successfully request join or false</returns>
        public static bool Join(OdinConnectionPoolHandle connectionPoolHandle, string endPoint, string token, string roomName, byte[] userData, float positionX, float positionY, float positionZ, uint samplerate, bool stereo, out Room room)
        {
            room = new Room(connectionPoolHandle, endPoint, samplerate, stereo);
            return room.Join(token, roomName, userData, positionX, positionY, positionZ);
        }

        /// <summary>
        /// Join a room with token
        /// </summary>
        /// <remarks>Always flase if the room is already connected. Use a new room object!</remarks>
        /// <param name="token">Join token</param>
        /// <returns>true on successfully request join or false</returns>
        public bool Join(string token)
        {
            Utility.Assert(_connectionPoolHandle.IsAlive, $"{nameof(Odin.Library.Methods.RoomCreate)} {nameof(OdinConnectionPoolHandle)} is released");
            Utility.Assert(string.IsNullOrEmpty(EndPoint.ToString()) == false, $"{nameof(Odin.Library.Methods.RoomCreate)} {nameof(EndPoint)} IsNullOrEmpty");
            Utility.Assert(string.IsNullOrEmpty(token) == false, $"{nameof(Odin.Library.Methods.RoomCreate)} {nameof(token)} IsNullOrEmpty");

            if(IsJoined)
                return false;

            this.Token = token;
            var result = Odin.Library.Methods.RoomCreate(_connectionPoolHandle, EndPoint.ToString(), token, out _handle);
            bool ret = Utility.IsOk(result);
            if (ret == false)
                Utility.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomCreate)} in {nameof(Room.Join)} failed (invalid {Handle.IsInvalid}): {Utility.OdinLastErrorString()} (code {result})").ToString());

            return ret;
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// Create and join a Room with initial data
        /// </summary>
        /// <param name="connectionPoolHandle">Connection pool for the room</param>
        /// <param name="endPoint">Gateway server</param>
        /// <param name="token">Join token</param>
        /// <param name="roomName">initial room name</param>
        /// <param name="userData">initial userdata</param>
        /// <param name="position">initial x,y,z position</param>
        /// <param name="samplerate">sets default samplerate</param>
        /// <param name="stereo">sets default stereo flag</param>
        /// <returns>true on successfully request join or false</returns>
        public static Room Join(OdinConnectionPoolHandle connectionPoolHandle, string endPoint, string token, string roomName, byte[] userData, UnityEngine.Vector3 position, uint samplerate, bool stereo, OdinCipherHandle cipher = null) =>
            Join(connectionPoolHandle, endPoint, token, roomName, userData, position.x, position.y, position.z, samplerate, stereo, cipher);
        /// <summary>
        /// Create and join a Room
        /// </summary>
        /// <param name="token">Join token</param>
        /// <param name="roomName">initial room name</param>
        /// <param name="userData">initial userdata</param>
        /// <param name="position">initial x,y,z position</param>
        /// <returns>true on successfully request join or false</returns>
        public bool Join(string token, string roomName, byte[] userData, UnityEngine.Vector3 position) =>
            Join(token, roomName, userData, position.x, position.y, position.z);

        /// <summary>
        /// Set Odin server side culling positioning
        /// </summary>
        /// <param name="position">Will use 2D transformed position X => X and Z => Y </param>
        public virtual Task<RpcResult> SetPosition(UnityEngine.Vector3 position) => SetPosition(position.x, position.z);
#endif
        /// <summary>
        /// Create and join a Room
        /// </summary>
        /// <param name="connectionPoolHandle">Connection pool for the room</param>
        /// <param name="endPoint">Gateway server</param>
        /// <param name="token">Join token</param>
        /// <param name="roomName">initial room name</param>
        /// <param name="userData">initial userdata</param>
        /// <param name="positionX">initial position</param>
        /// <param name="positionY">initial position</param>
        /// <param name="positionZ">initial position</param>
        /// <param name="samplerate">sets default samplerate</param>
        /// <param name="stereo">sets default stereo flag</param>
        /// <returns>true on successfully request join or false</returns>
        public static Room Join(OdinConnectionPoolHandle connectionPoolHandle, string endPoint, string token, string roomName, byte[] userData, float positionX, float positionY, float positionZ, uint samplerate, bool stereo, OdinCipherHandle cipher = null)
        {
            Room room = new Room(connectionPoolHandle, endPoint, samplerate, stereo);
            room.Join(token, roomName, userData, positionX, positionY, positionZ, cipher);
            return room;
        }

        /// <summary>
        /// Join a room
        /// </summary>
        /// <param name="token">room token</param>
        /// <param name="roomName">initial room name</param>
        /// <param name="userData">initial userdata</param>
        /// <param name="positionX">initial position</param>
        /// <param name="positionY">initial position</param>
        /// <param name="positionZ">initial position</param>
        /// <param name="cipher">cypto interface</param>
        /// <returns>true on successfully request join or false</returns>
        public bool Join(string token, string roomName = null, byte[] userData = null, float positionX = 0f, float positionY = 0f, float positionZ = 0f, OdinCipherHandle cipher = null)
        {
            Utility.Assert(_connectionPoolHandle.IsAlive, $"{nameof(Odin.Library.Methods.RoomCreateEx)} {nameof(OdinConnectionPoolHandle)} is released");
            Utility.Assert(string.IsNullOrEmpty(EndPoint.ToString()) == false, $"{nameof(Odin.Library.Methods.RoomCreateEx)} {nameof(EndPoint)} IsNullOrEmpty");
            Utility.Assert(string.IsNullOrEmpty(token) == false, $"{nameof(Odin.Library.Methods.RoomCreateEx)} {nameof(token)} IsNullOrEmpty");

            this.Token = token;
            this.Name = roomName ?? string.Empty;
            this.PositionX = positionX;
            this.PositionY = positionY;
            this.PositionZ = positionZ;

            if (cipher != null && cipher.IsAlive)
                this.CryptoCipher = Crypto.Create(cipher);

            var result = Odin.Library.Methods.RoomCreateEx(_connectionPoolHandle, EndPoint.ToString(), token, out _handle, roomName, userData ?? new byte[0], positionX, positionY, positionZ, cipher);
            bool ret = Utility.IsOk(result);
            if (ret == false)
                Utility.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomCreateEx)} in {nameof(Room.Join)} failed (invalid {Handle.IsInvalid}): {Utility.OdinLastErrorString()} (code {result})").ToString());

            return ret;
        }
        /// <summary>
        /// Join a room with encryption
        /// </summary>
        /// <remarks>If the room is already connected. Use a new room object!</remarks>
        /// <param name="token">Join token</param>
        /// <param name="cipher">cypto cipher</param>
        /// <returns>true on successfully request join or false</returns>
        public bool Join(string token, OdinCipherHandle cipher) => Join(token, null, null, 0, 0, 0, cipher);

        /// <summary>
        /// Retrieves the room id
        /// </summary>
        /// <returns>room id</returns>
        public ulong GetRoomId()
        {
            Utility.Assert(Handle?.IsAlive == true, $"{nameof(Odin.Library.Methods.RoomGetId)} {nameof(OdinRoomHandle)} is released");

            return Odin.Library.Methods.RoomGetId(Handle);
        }

        /// <summary>
        /// Retrieves the room name
        /// </summary>
        /// <remarks>Updates <see cref="Room.Name"/> on success by default</remarks>
        /// <param name="update">update this room name on true</param>
        /// <returns>room name</returns>
        public string GetRoomName(bool update = true)
        {
            Utility.Assert(Handle?.IsAlive == true, $"{nameof(Odin.Library.Methods.RoomGetName)} {nameof(OdinRoomHandle)} is released");

            OdinError result = Odin.Library.Methods.RoomGetName(Handle, out string name);
            
            if (Utility.IsOk(result) == false)
                Utility.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomGetName)} in {nameof(Room.GetRoomName)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());
            else if (update)
                this.Name = name;

            return name;
        }

        /// <summary>
        /// Resend native UserData
        /// </summary>
        /// <returns>error code</returns>
        protected internal OdinError ResendUserData()
        {
            Utility.Assert(Handle?.IsAlive == true, $"{nameof(Odin.Library.Methods.RoomGetName)} {nameof(OdinRoomHandle)} is released");

            OdinError result = Odin.Library.Methods.RoomResendUserData(Handle);

            if (Utility.IsOk(result) == false)
                Utility.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomGetName)} in {nameof(Room.GetRoomName)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());
            return result;
        }

        /// <summary>
        /// Get a encoder from <see cref="Encoders"/> by id
        /// </summary>
        /// <param name="mediaId">id of a input media</param>
        /// <param name="encoder">input object</param>
        /// <returns>true on encoder found or false</returns>
        public bool GetEncoder(ushort mediaId, out MediaEncoder encoder)
        {
            return Encoders.TryGetValue(mediaId, out encoder);
        }
        /// <summary>
        /// Get a encoder from <see cref="Encoders"/> by id. If the encoder is not found create a new one that will be added to <see cref="Encoders"/>.
        /// </summary>
        /// <remarks>Will use the default samplerate and stereo flag set by the current room</remarks>
        /// <param name="mediaId">id of a input media</param>
        /// <param name="encoder">input object</param>
        /// <returns>true or false on error</returns>
        public bool GetOrCreateEncoder(ushort mediaId, out MediaEncoder encoder) => GetOrCreateEncoder(mediaId, Samplerate, Stereo, out encoder);
        /// <summary>
        /// Get a encoder from <see cref="Encoders"/> by id. If the encoder is not found create a new one that will be added to <see cref="Encoders"/>.
        /// </summary>
        /// <param name="mediaId">id of a input media</param>
        /// <param name="samplerate">custom samplerate</param>
        /// <param name="stereo">custom stereo flag</param>
        /// <param name="encoder">input object</param>
        /// <returns>true or false on error</returns>
        public bool GetOrCreateEncoder(ushort mediaId, uint samplerate, bool stereo, out MediaEncoder encoder)
        {
            if (Encoders.TryGetValue(mediaId, out encoder))
                return true;

            encoder = CreateEncoder(mediaId, samplerate, stereo);
            return encoder != null;
        }
        /// <summary>
        /// Create a new input media that will be added to<see cref="Encoders"/>
        /// </summary>
        /// <remarks>Will use the default samplerate and stereo flag set by the current room</remarks>
        /// <param name="mediaId">id of a input media</param>
        /// <returns>input media</returns>
        public MediaEncoder CreateEncoder(ushort mediaId) => CreateEncoder(mediaId, Samplerate, Stereo);
        /// <summary>
        /// Create a new input media that will be added to<see cref="Encoders"/>
        /// </summary>
        /// <param name="mediaId">id of a input media</param>
        /// <param name="samplerate">custom samplerate</param>
        /// <param name="stereo">custom stereo flag</param>
        /// <returns>input media</returns>
        public MediaEncoder CreateEncoder(ushort mediaId, uint samplerate, bool stereo)
        {
            MediaEncoder encoder = MediaEncoder.Create(mediaId, samplerate, stereo);
            Encoders.TryAdd(mediaId, encoder);
            return encoder;
        }
        /// <summary>
        /// Removes the input media from <see cref="Encoders"/>
        /// </summary>
        /// <param name="mediaId">id of a input media</param>
        /// <param name="encoder">input media that was removed</param>
        /// <returns>true on encoder found or false</returns>
        public bool RemoveEncoder(ushort mediaId, out MediaEncoder encoder) => Encoders.TryRemove(mediaId, out encoder);

        /// <summary>
        /// Get a decoder from <see cref="PeerEntity.Medias"/> of <see cref="RemotePeers"/> by id
        /// </summary>
        /// <param name="peerId">id of peer</param>
        /// <param name="mediaId">id of output media</param>
        /// <param name="decoder">output object or null</param>
        /// <returns>true on decoder found or false</returns>
        public bool GetDecoder(ulong peerId, ushort mediaId, out MediaDecoder decoder)
        {
            if (RemotePeers.TryGetValue(peerId, out PeerEntity peer))
                return peer.GetDecoder(mediaId, out decoder);

            decoder = null;
            return false;
        }
        /// <summary>
        /// Get a decoder from <see cref="PeerEntity.Medias"/> of <see cref="RemotePeers"/> by id. If the decoder is not found create a new one that will be added to the Peer
        /// </summary>
        /// <remarks>Will use the default samplerate and stereo flag set by the current room</remarks>
        /// <param name="peerId">id of peer</param>
        /// <param name="mediaId">id of output media</param>
        /// <param name="decoder">output object or null</param>
        /// <returns>true on decoder found or false</returns>
        public bool GetOrCreateDecoder(ulong peerId, ushort mediaId, out MediaDecoder decoder) => GetOrCreateDecoder(peerId, mediaId, Samplerate, Stereo, out decoder);
        /// <summary>
        /// Get a decoder from <see cref="PeerEntity.Medias"/> of <see cref="RemotePeers"/> by id. If the decoder is not found create a new one that will be added to the Peer
        /// </summary>
        /// <param name="peerId">id of peer</param>
        /// <param name="mediaId">id of output media</param>
        /// <param name="samplerate">custom samplerate</param>
        /// <param name="stereo">custom stereo flag</param>
        /// <param name="decoder">output object or null</param>
        /// <returns>true on decoder found or false</returns>
        public bool GetOrCreateDecoder(ulong peerId, ushort mediaId, uint samplerate, bool stereo, out MediaDecoder decoder)
        {
            if (RemotePeers.TryGetValue(peerId, out PeerEntity peer))
                if (peer.GetOrCreateDecoder(mediaId, samplerate, stereo, out decoder))
                    return true;

            decoder = null;
            return false;
        }
        /// <summary>
        /// Create a new output media that will be added to <see cref="PeerEntity.Medias"/>
        /// </summary>
        /// <remarks>Will use the default samplerate and stereo flag set by the current room</remarks>
        /// <param name="peerId">id of peer</param>
        /// <param name="mediaId">id of output media</param>
        /// <returns>output media</returns>
        public MediaDecoder CreateDecoder(ulong peerId, ushort mediaId) => CreateDecoder(peerId, mediaId, Samplerate, Stereo);
        /// <summary>
        /// Create a new output media that will be added to <see cref="PeerEntity.Medias"/>
        /// </summary>
        /// <remarks>Will use the default samplerate and stereo flag set by the current room</remarks>
        /// <param name="peerId">id of peer</param>
        /// <param name="mediaId">id of output media</param>
        /// <param name="samplerate">custom samplerate</param>
        /// <param name="stereo">custom stereo flag</param>
        /// <returns>output media</returns>
        public MediaDecoder CreateDecoder(ulong peerId, ushort mediaId, uint samplerate, bool stereo)
        {
            RemotePeers.TryGetValue(peerId, out var peer);
            return peer?.CreateDecoder(mediaId, samplerate, stereo) ?? null;
        }
        /// <summary>
        /// Removes a output media from a remote peer.
        /// </summary>
        /// <param name="peerId">id of peer</param>
        /// <param name="mediaId">id of a output media</param>
        /// <param name="decoder">output media that was removed</param>
        /// <returns>true on decoder found or false</returns>
        public bool RemoveDecoder(ulong peerId, ushort mediaId, out MediaDecoder decoder)
        {
            bool bRemovedDecoder = false;
            PeerEntity remotePeer = RemotePeers[peerId];
            if (null != remotePeer)
            {
                bRemovedDecoder = remotePeer.RemoveDecoder(mediaId, out decoder);
            }
            else
            {
                decoder = null;
            }
            return bRemovedDecoder;
        }

        #region RPC
        /// <summary>
        /// Send a <c>"StartMedia"</c> RPC to the server to start the encoder for input.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="encoder">input media</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> StartMedia(MediaEncoder encoder) 
        {
            if (encoder == null) return Task.FromResult(new RpcResult { Error = $"invalid encoder null" });

            if (encoder.MediaProperties == null)
                encoder.MediaProperties = new MediaRpcProperties()
                {
                    Kind = "audio",
                    uId = Guid.NewGuid().ToString()
                };

            return StartMedia(encoder.Id, encoder.MediaProperties); 
        }
        /// <summary>
        /// Send a <c>"StartMedia"</c> RPC with custom media-data to the server to start the encoder for input.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="mediaId">id of input media</param>
        /// <param name="properties">arbitrary media data usually to identify the media on lost connections</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> StartMedia(ushort mediaId, MediaRpcProperties properties)
        {
            return SendRpc("StartMedia", (writer) => {
                writer.WriteMapHeader(2);
                writer.WriteString("media_id");
                writer.Write(mediaId);
                writer.WriteString("properties");
                writer.WriteMapHeader(2);
                writer.WriteString("kind");
                writer.WriteString(properties?.Kind ?? "audio");
                writer.WriteString("uid");
                writer.WriteString(properties?.uId ?? Guid.NewGuid().ToString());
            });
        }
        /// <summary>
        /// Send a <c>"StopMedia"</c> to the server to stop the encoder.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="encoder">input media to stop</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> StopMedia(MediaEncoder encoder) => StopMedia(encoder.Id);
        /// <summary>
        /// Send a <c>"StopMedia"</c> to the server to stop the encoder.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="mediaId">raw id to stop</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> StopMedia(ushort mediaId)
        {
            return SendRpc("StopMedia", (writer) => {
                writer.WriteMapHeader(1);
                writer.WriteString("media_id");
                writer.Write(mediaId);
            });
        }

        /// <summary>
        /// Send a <c>"PauseMedia"</c> to the server to stop the decoder for output.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="decoder">output media to stop</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> PauseMedia(MediaDecoder decoder) => PauseMedia(decoder.Id);
        /// <summary>
        /// Send a <c>"PauseMedia"</c> to the server to stop the decoder for output.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="mediaId">raw id to stop</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> PauseMedia(ushort mediaId)
        {
            return SendRpc("PauseMedia", (writer) => {
                writer.WriteMapHeader(1);
                writer.WriteString("media_id");
                writer.Write(mediaId);
            });
        }
        /// <summary>
        /// Send a <c>"ResumeMedia"</c> to the server to start a stopped decoder for output.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="decoder">output media to start</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> ResumeMedia(MediaDecoder decoder) => ResumeMedia(decoder.Id);
        /// <summary>
        /// Send a <c>"ResumeMedia"</c> to the server to start a stopped decoder for output.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="mediaId">raw id to start</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> ResumeMedia(ushort mediaId)
        {
            return SendRpc("ResumeMedia", (writer) => {
                writer.WriteMapHeader(1);
                writer.WriteString("media_id");
                writer.Write(mediaId);
            });
        }

        /// <summary>
        /// Update arbitrary userdata of self (note: <see cref="UserData"/>)
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="userData">arbitrary data</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> UpdateUserData(IUserData userData) => UpdateUserData(userData.ToBytes());
        /// <summary>
        /// Update binary userdata of self (note: <see cref="UpdateUserData(IUserData)"/>)
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="bytes">raw binary data</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> UpdateUserData(byte[] bytes)
        {
            return SendRpc("UpdatePeer", (writer) => {
                writer.WriteMapHeader(1);
                writer.WriteString("user_data");
                writer.WriteBinary(bytes);
            });
        }
        /// <summary>
        /// Set the spatial position for server side culling. Other remote peers outside the boundary will appear as not in the room or leaving the room.
        /// </summary>
        /// <remarks><b>The up-vector should be on the y-axis!</b> Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="x">X of vector</param>
        /// <param name="y">Y of vector</param>
        /// <param name="z">Z of vector</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> SetPosition(float x = 0.0f, float y = 0.0f, float z = 0.0f)
        {
            return SendRpc("SetPeerPosition", (writer) => {
                writer.WriteMapHeader(1);
                writer.WriteString("position");
                writer.WriteArrayHeader(3);
                writer.WriteFloat(x);
                writer.WriteFloat(y);
                writer.WriteFloat(z);
            });
        }

        /// <summary>
        /// Send <c>"SendMessage"</c> to the server to broadcast the message with default UTF8 encoding.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="message">UTF8 string</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> SendMessage(string message) => SendMessage(Native.Encoding.GetBytes(message));
        /// <summary>
        /// Send <c>"SendMessage"</c> to the server to broadcast the message.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="message">string</param>
        /// <param name="encoding">custom encoding</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> SendMessage(string message, Encoding encoding) => SendMessage(encoding.GetBytes(message));
        /// <summary>
        /// Send <c>"SendMessage"</c> to the server to broadcast an arbitrary message.
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="message">arbitrary data</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> SendMessage(byte[] message)
        {
            return SendRpc("SendMessage", (writer) => {
                writer.WriteMapHeader(1);
                writer.WriteString("message");
                writer.WriteBinary(message);
            });
        }

        /// <summary>
        /// Send registered RPCs to the server. (set by <see cref="RpcWriter"/>)
        /// </summary>
        /// <remarks>Be aware that the task is created for resolving Msgpack request/response result as <see cref="System.Threading.Tasks.TaskCompletionSource{RpcResult}"/> on a <see cref="Room"/>. Unless handled correctly <b>should not be awaited by the corresponding context</b>.</remarks>
        /// <param name="method">RPC method</param>
        /// <param name="builder">Msgpack builder</param>
        /// <returns>Thunk task that will not run (see <see cref="RpcTableThunk"/>)</returns>
        public virtual Task<RpcResult> SendRpc(string method, Action<IMsgPackWriter> builder)
        {
            TaskCompletionSource<RpcResult> completionSource = new TaskCompletionSource<RpcResult>();

            if (RpcWriter == null)
            {
                var ex = new NotSupportedException($"{nameof(Room.SendRpc)} needs a valid {nameof(IMsgPackWriter)} \"{nameof(RpcWriter)}\" to build.");
                Utility.Assert(message: ex.ToString());
                completionSource.SetException(ex);
                return completionSource.Task;
            }

            if (RpcAckActive)
            {
                RpcWriter.WriteArrayHeader(4); // [type, msgid, method, params]
                RpcWriter.Write((int)MsgPackMessageType.Request); // type
                RpcWriter.Write(RpcId++); // msgid
            }
            else
            {
                RpcWriter.WriteArrayHeader(3); // [type, method, params]
                RpcWriter.Write((int)MsgPackMessageType.Notification); // type
            }
            RpcWriter.WriteString(method); // method
            builder(RpcWriter); // params

            if (RpcAckActive)
            {
                RpcTableThunk.TryAdd(RpcId, completionSource);
            }

            Debug.WriteLine($"{method} RPC: {string.Join(" ", RpcWriter.GetBytes().Select(s => s.ToString("X2")))}");
            OdinError result = SendRpc(RpcWriter.GetBytes());
            RpcWriter.Clear();

            if (Utility.IsOk(result))
                completionSource.SetResult(new RpcResult() { Id = RpcId, Name = method, Value = string.Empty, Error = string.Empty });
            else
                completionSource.SetException(new OdinException(result, $"{nameof(Room)} {nameof(SendRpc)} failed: {Utility.OdinErrorToString(result)} {Utility.OdinLastErrorString()}"));

            return completionSource.Task;
        }

        private OdinError SendRpc(byte[] bytes)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Odin.Library.Methods.RoomSendRpc)} {nameof(OdinRoomHandle)} is released");

            OdinError result = Odin.Library.Methods.RoomSendRpc(Handle, bytes);
            if (Utility.IsOk(result) == false)
                Utility.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomSendRpc)} in {nameof(Room.SendRpc)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());
            return result;
        }
        #endregion RPC

        #region Datagram
        /// <summary>
        /// Push the samples to all <see cref="Encoders"/> for pipeline processing and pop the result as datagrams to the server
        /// </summary>
        /// <param name="samples">Audio data</param>
        /// <param name="isSilent">flag these samples as silence</param>
        public virtual void SendAudio(float[] samples, bool isSilent = false)
        {
            foreach (var kvp in this.Encoders)
                SendAudio(samples, kvp.Value.Id, isSilent); 
        }
        /// <summary>
        /// Push the samples to the input media for pipeline processing and pop the result as datagram to the server
        /// </summary>
        /// <param name="samples">Audio data</param>
        /// <param name="mediaId">input media id</param>
        /// <param name="isSilent">flag these samples as silence</param>
        /// <returns>true on success or false</returns>
        public virtual bool SendAudio(float[] samples, ushort mediaId, bool isSilent = false) => SendAudio(samples, this.Encoders[mediaId]);
        /// <summary>
        /// Push the samples to the input media for pipeline processing and pop the result as datagram to the server
        /// </summary>
        /// <param name="samples">Audio data</param>
        /// <param name="encoder">input media</param>
        /// <returns>true on success or false</returns>
        public virtual bool SendAudio(float[] samples, MediaEncoder encoder)
        {
            if (encoder == null) return false;

            encoder.Push(samples);
            return SendEncoderAudio(encoder);
        }

        /// <summary>
        /// Pop all samples from the input media by id and send them to the server
        /// </summary>
        /// <param name="mediaId">input media id</param>
        /// <returns>true on success or false</returns>
        public virtual bool SendEncoderAudio(ushort mediaId) => SendEncoderAudio(this.Encoders[mediaId]);
        /// <summary>
        /// Pop all samples from the input media and send them to the server
        /// </summary>
        /// <param name="encoder">input media</param>
        /// <returns>true on success or false</returns>
        public virtual bool SendEncoderAudio(MediaEncoder encoder)
        {
            Utility.Assert(encoder != null, $"{nameof(SendEncoderAudio)} {nameof(MediaEncoder)} is null");

            if(encoder == null)
                return false;

            OdinError error;
            do
            {
                error = encoder.Pop(out var datagram);
                switch (error)
                {
                    case OdinError.ODIN_ERROR_SUCCESS:
                        this.SendDatagram(encoder.Id, datagram);
                        continue;
                    case OdinError.ODIN_ERROR_NO_DATA:
                        return true;
                    default:
                        return false;
                }
            } while (error == OdinError.ODIN_ERROR_SUCCESS);
            return false;
        }

        /// <summary>
        /// Send audio to the server
        /// </summary>
        /// <param name="mediaId">input media id</param>
        /// <param name="datagram">encoder datagram</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        protected virtual OdinError SendDatagram(ushort mediaId, byte[] datagram)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Odin.Library.Methods.RoomSendDatagram)} {nameof(OdinRoomHandle)} is released");

            OdinError result = Odin.Library.Methods.RoomSendDatagram(Handle, datagram);
            if (Utility.IsOk(result) == false)
                Utility.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomSendDatagram)} in {nameof(Room.SendDatagram)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());
            return result;
        }
        #endregion Datagram

        /// <summary>
        /// Send client side rpc message
        /// </summary>
        /// <remarks>
        /// Injects rpc for the client that loopback to itself
        /// </remarks>
        /// <param name="rpc">bytes</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        protected virtual OdinError SendLoopbackRpc(byte[] rpc)
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Odin.Library.Methods.RoomSendLoopbackRpc)} {nameof(OdinRoomHandle)} is released");

            OdinError result = Odin.Library.Methods.RoomSendLoopbackRpc(Handle, rpc);
            if (Utility.IsOk(result) == false)
                Utility.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomSendLoopbackRpc)} in {nameof(Room.SendLoopbackRpc)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());
            return result;
        }

        /// <summary>
        /// Close the native room. (native dispose)
        /// </summary>
        public void Close()
        {
            Utility.Assert(Handle.IsAlive, $"{nameof(Odin.Library.Methods.RoomClose)} {nameof(OdinRoomHandle)} is released");

            Odin.Library.Methods.RoomClose(Handle);
        }

        private void UnsubscribeEvents()
        {
            OnDatagram -= Room_OnDatagram;
            OnRpc -= Room_OnRPC;
            OnRoomStatusChanged -= Room_OnRoomStatusChanged;
            OnRoomJoined -= Room_OnRoomJoined;
            OnRoomLeft -= Room_OnRoomLeft;
            OnPeerJoined -= Room_OnPeerJoined;
            OnPeerLeft -= Room_OnPeerLeft;
            OnMediaStarted -= Room_OnMediaStarted;
            OnMediaStopped -= Room_OnMediaStopped;
            OnUserDataChanged -= Room_OnUserDataChanged;
            OnMessageReceived -= Room_OnMessageReceived;
        }

        private void FreeEncoders()
        {
            foreach (var encoder in Encoders)
                encoder.Value.Dispose();

            Encoders.Clear();
        }

        private void FreePeers()
        {
            foreach (var kvp in RemotePeers)
                kvp.Value.Dispose();

            RemotePeers.Clear();
        }

        private bool disposedValue;
        /// <summary>
        /// On dispose will free the room and all associated data
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    UnsubscribeEvents();
                    FreeEncoders();
                    FreePeers();
                    RpcWriter?.Dispose();
                    RpcWriter = null;
                    CryptoCipher?.Dispose();
                    CryptoCipher = null;
                    _connectionPoolHandle?.Dispose();
                    _connectionPoolHandle = null;
                    Handle?.Dispose();
                    Handle = null;
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Default deconstructor
        /// </summary>
        ~Room()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// On dispose will free the room and all associated data
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
