using OdinNative.Core;
using OdinNative.Core.Imports;
using OdinNative.Utils.Json;
using OdinNative.Wrapper.Peer.Rpc;
using OdinNative.Wrapper.Room.Rpc;
using OdinNative.Wrapper.Socket;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Core.Utility;

namespace OdinNative.Wrapper.Room
{
    /// <summary>
    /// An ODIN room, which handles the underlying native opaque room handle.
    /// This abstraction provides a high-level interface for joining rooms, managing persistent state.
    /// </summary>
    public class Room : IRoom, IDisposable
    {
        /// <summary>
        /// Room default samplerate
        /// </summary>
        public uint Samplerate { get; }
        /// <summary>
        /// Room default stereo flag
        /// </summary>
        public bool Stereo { get; }
        /// <summary>
        /// Room server gateway endpoint
        /// </summary>
        public string EndPoint { get; private set; }
        OdinRoomHandle IRoom.Handle => Handle;
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
        public uint OwnPeerId { get; private set; }
        /// <summary>
        /// UserId of self
        /// </summary>
        public string UserId { get; private set; }
        /// <summary>
        /// Room name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Joining customer
        /// </summary>
        public string Customer { get; private set; }
        /// <summary>
        /// RoomStatus
        /// </summary>
        public string RoomStatus { get; private set; }
        /// <summary>
        /// Self room tags
        /// </summary>
        public IEnumerable<string> Tags { get; private set; }
        /// <summary>
        /// Latest token for new reconnects
        /// </summary>
        public string ReconnectToken { get; private set; }
        /// <summary>
        /// User data of the local peer that is sent together with the join request
        /// </summary>
        /// <remarks>Uses the same format as <see cref="UpdateUserData(string)"/> and has to be set before joining.
        /// Later changes are sent with <see cref="UpdateUserData(string)"/>; ODIN keeps the latest user data and sends it again by itself on reconnects.</remarks>
        public string InitialUserData { get; set; }
        /// <summary>
        /// IsJoined
        /// </summary>
        public bool IsJoined => RoomStatus?.Equals("Joined", StringComparison.InvariantCultureIgnoreCase) ?? false;
        /// <summary>
        /// IsClosed
        /// </summary>
        public bool IsClosed => disposedValue || (RoomStatus?.Equals("Closed", StringComparison.InvariantCultureIgnoreCase) ?? true);
        private bool _InTransition;
        /// <summary>
        /// Crypto cipher
        /// </summary>
        public Crypto CryptoCipher { get; private set; }

        /// <summary>
        /// Container of room peers
        /// </summary>
        public ConcurrentDictionary<uint, PeerEntity> RemotePeers { get; private set; }
        /// <summary>
        /// Container of room input medias
        /// </summary>
        public ConcurrentDictionary<ulong, MediaEncoder> Encoders { get; private set; }
        /// <summary>
        /// Container of room sockets
        /// </summary>
        public ConcurrentDictionary<ulong, Socket.Socket> Sockets { get; private set; }
        /// <summary>
        /// Elements of room output medias
        /// </summary>
        public IEnumerable<MediaDecoder> Decoders => RemotePeers.Values.SelectMany(p => p.Medias.Values);
        /// <summary>
        /// Room joining authentication
        /// </summary>
        public string Authentication { get; private set; }

        /// <summary>
        /// Default value <c>null</c> indicates root or not set
        /// </summary>
        public object Parent { get; set; }

        private IPeer _self;
        /// <summary>
        /// The local peer for this client in this room. Null until the room is joined.
        /// </summary>
        public IPeer Self => IsJoined ? (_self ??= new SelfPeer(this)) : null;

        /// <summary>
        /// Lightweight <see cref="IPeer"/> representing the local client in this room.
        /// Returned by <see cref="Self"/> so SDK components can reference the local peer through
        /// the same abstraction used for remote peers.
        /// </summary>
        private sealed class SelfPeer : IPeer
        {
            private readonly Room _room;
            internal SelfPeer(Room room) { _room = room; }

            public uint Id => _room.OwnPeerId;
            public string UserId => _room.UserId;
            public List<string> Tags => new List<string>(_room.Tags ?? Enumerable.Empty<string>());
            public IUserData UserData => null;
            public IRoom Parent => _room;
            /// <summary>No native peer entity exists for the local client.</summary>
            public PeerEntity GetBasePeer() => null;
            public Room GetRoomApi() => _room;
            public MediaEncoder GetEncoder() => _room.Encoders.Values.FirstOrDefault();
            public MediaDecoder GetDecoder() => null;
        }

        #region Events
#pragma warning disable CS0067 // The event is never used
        /// <summary>
        /// Call on audio data
        /// </summary>
        public event EventHandler<DatagramEventArgs> OnDatagram;
        protected internal void OnDatagramReceived(DatagramEventArgs e) => OnDatagram?.Invoke(this, e);
        /// <summary>
        /// Call on rpc data
        /// </summary>
        public event EventHandler<RpcEventArgs> OnRpc;
        protected internal void OnRPCReceived(RpcEventArgs e) => OnRpc?.Invoke(this, e);
        /// <summary>
        /// Call on socket data
        /// </summary>
        public event EventHandler<SocketMessageEventArgs> OnSocket;
        protected internal void OnSocketReceived(SocketMessageEventArgs e) => OnSocket?.Invoke(this, e);
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
        /// A remote peer left, or was removed locally when this session ended.
        /// </summary>
        public event OnPeerLeftDelegate OnPeerLeft;
        /// <summary>
        /// Odin peer changed
        /// </summary>
        public event OnPeerChangedDelegate OnPeerChanged;
        /// <summary>
        /// Odin room received message
        /// </summary>
        public event OnMessageReceivedDelegate OnMessageReceived;
        public event EventHandler<IntPtr> __OnExtraDestroyDelegate;
        #pragma warning restore CS0067 // The event is never used
        #endregion
        private OdinRoomEvents _connectionEvents;

        /// <summary>
        /// Rooms reachable from a native callback, keyed by <see cref="_callbackId"/>.
        /// </summary>
        /// <remarks>
        /// Deliberately not a GCHandle. Mono reuses the value of a freed handle, so a callback that
        /// the native side dispatched before it noticed the room was gone could resolve to whichever
        /// room happens to hold that value now, silently delivering events into a live session. Ids
        /// come from a counter that never hands out the same value twice, so a late callback finds
        /// nothing instead. The entry also keeps the room alive for as long as native code may call
        /// back into it, which is what the GCHandle used to do.
        /// </remarks>
        private static readonly ConcurrentDictionary<long, Room> _callbackRooms = new ConcurrentDictionary<long, Room>();
        private static long _lastCallbackId;
        private readonly long _callbackId;

        /// <summary>
        /// Set before teardown starts; no callback may enter the room afterwards.
        /// </summary>
        private volatile bool _callbacksDisabled;
        /// <summary>
        /// Number of native callbacks currently executing inside this room.
        /// </summary>
        private int _activeCallbacks;
        /// <summary>
        /// Set once teardown has asked for the room's resources to be released.
        /// </summary>
        private int _releaseRequested;
        /// <summary>
        /// Guards the release against running twice.
        /// </summary>
        private int _released;
        private int _disposeStarted;
        private volatile bool _nativeClosed;
        private readonly object _closeTimerLock = new object();
        private Timer _closeTimer;
        private const int CloseTimeoutMs = 2000;
        /// <summary>
        /// Initialise dangling room
        /// </summary>
        /// <remarks>For creating an independent room use <see cref="Room.Create"/></remarks>
        /// <param name="endPoint">Gateway server</param>
        /// <param name="samplerate">sets default samplerate</param>
        /// <param name="stereo">sets default stereo flag</param>
        /// <param name="extraCallbackData">native callback function user data</param>
        public Room(string endPoint, uint samplerate, bool stereo, IntPtr extraCallbackData = default)
        {
            _handle = new OdinRoomHandle(IntPtr.Zero, false);
            _callbackId = Interlocked.Increment(ref _lastCallbackId);
            _callbackRooms[_callbackId] = this;
            _connectionEvents = new OdinRoomEvents(
                StaticOnDatagramDelegate,
                StaticOnRpcDelegate,
                StaticOnSocketDelegate,
                new IntPtr(_callbackId)
            );

            Name = string.Empty;
            RoomStatus = string.Empty;
            Authentication = string.Empty;
            CryptoCipher = null;
            Encoders = new ConcurrentDictionary<ulong, MediaEncoder>();
            RemotePeers = new ConcurrentDictionary<uint, PeerEntity>();
            Sockets = new ConcurrentDictionary<ulong, Socket.Socket>();
            Samplerate = samplerate;
            Stereo = stereo;
            EndPoint = endPoint;

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
            return new Room(endPoint, samplerate, stereo);
        }
        private void SubscribeEvents()
        {
            OnDatagram += Room_OnDatagram;
            OnRpc += Room_OnRPC;
            OnSocket += Room_OnSocket;
            OnRoomStatusChanged += Room_OnRoomStatusChanged;
            OnRoomJoined += Room_OnRoomJoined;
            OnRoomLeft += Room_OnRoomLeft;
            OnPeerJoined += Room_OnPeerJoined;
            OnPeerLeft += Room_OnPeerLeft;
            OnPeerChanged += Room_OnPeerChanged;
            OnMessageReceived += Room_OnMessageReceived;
        }

        private void OnNativeDatagramReceived(IntPtr roomHandle, in OdinDatagramProperties properties, IntPtr bytesPtr, uint bytesLength, IntPtr userData)
        {
            OdinLog.Assert(bytesPtr != IntPtr.Zero, $"{nameof(Room)} room {roomHandle} internal {nameof(Room)} {nameof(OnNativeDatagramReceived)} datagram pointer should not be zero");
            OdinLog.Assert(bytesLength > 0, $"{nameof(Room)} room {roomHandle} internal {nameof(Room)} {nameof(OnNativeDatagramReceived)} datagram should not be empty");

            byte[] datagramPayload = Utility.GetNativeBuffer(bytesPtr, bytesLength);

            if (this.Id == (ulong)roomHandle)
            {
                this.OnDatagram?.Invoke(this, new DatagramEventArgs()
                {
                    RoomId = (ulong)roomHandle,
                    PeerId = properties.peer_id,
                    ChannelMask = (ChannelMask)properties.channel_mask,
                    Datagram = bytesPtr,
                    Payload = datagramPayload,
                    Userdata = userData
                });
            }
        }

        private void OnNativeRPCReceived(IntPtr roomHandle, string json, IntPtr user_data)
        {
            OdinLog.Assert(roomHandle != IntPtr.Zero, $"{nameof(Room)} room {roomHandle} internal {nameof(Room)} {nameof(OnNativeRPCReceived)} room pointer should not be zero");
            OdinLog.Assert(string.IsNullOrEmpty(json) == false, $"{nameof(Room)} room {roomHandle} internal {nameof(Room)} {nameof(OnNativeRPCReceived)} rpc data should not be empty");

            if (this.Id == (ulong)roomHandle)
            {
                this.OnRpc?.Invoke(this, new RpcEventArgs()
                {
                    RoomId = (ulong)roomHandle,
                    Rpc = json,
                    Userdata = user_data
                });
            }
        }

        private void OnNativeSocketReceived(IntPtr socketHandle, IntPtr bytesPtr, uint bytesLength, IntPtr userData)
        {
            OdinLog.Assert(socketHandle != IntPtr.Zero, $"{nameof(Room)} room socket {socketHandle} internal {nameof(Room)} {nameof(OnNativeSocketReceived)} socket pointer should not be zero");
            OdinLog.Assert(bytesPtr != IntPtr.Zero, $"{nameof(Room)} room socket {socketHandle} internal {nameof(Room)} {nameof(OnNativeSocketReceived)} socket payload pointer should not be zero");

            byte[] socketPayload = Utility.GetNativeBuffer(bytesPtr, bytesLength);

            var socket = Sockets.GetOrAdd((ulong)socketHandle, (k) => Socket.Socket.FromNative(this, (IntPtr)k));
            this.OnSocket?.Invoke(this, new SocketMessageEventArgs()
            {
                Socket = socket,
                Room = this,
                Payload = socketPayload,
                Userdata = userData
            });
        }

        /// <summary>
        /// Resolves the room behind a native callback's user data.
        /// </summary>
        /// <remarks>
        /// The native side can still dispatch a queued event while the room is being torn down. This
        /// must never throw: an exception escaping a native-to-managed callback is undefined
        /// behaviour and was observed to leave the runtime unable to create further rooms.
        /// </remarks>
        private static bool TryEnterCallback(IntPtr userData, out Room self)
        {
            self = null;

            // A room drops out of the registry before it is torn down, and its id is never handed
            // out again, so a callback the native side dispatched too late resolves to nothing
            // rather than to whichever room came after it.
            if (_callbackRooms.TryGetValue(userData.ToInt64(), out Room room) == false) return false;
            if (room._callbacksDisabled) return false;

            Interlocked.Increment(ref room._activeCallbacks);
            // Teardown may have started between the check above and the increment. Re-reading the
            // flag afterwards is what makes the handshake with teardown airtight: either we
            // observe the flag and back out, or teardown observes our count and leaves the
            // release to us.
            if (room._callbacksDisabled)
            {
                ExitCallback(room);
                return false;
            }

            self = room;
            return true;
        }

        private static void ExitCallback(Room self)
        {
            // The last callback out is the one that gets to release the room, if teardown asked for
            // it while callbacks were still inside.
            if (Interlocked.Decrement(ref self._activeCallbacks) == 0)
            {
                try
                {
                    self.TryReleaseResources();
                }
                catch (Exception e)
                {
                    LogCallbackError(nameof(ExitCallback), e);
                }
            }
        }

        private static void LogCallbackError(string operation, Exception error)
        {
            try
            {
                OdinLog.LogError($"{operation} failed: {error}");
            }
            catch (Exception)
            {
                // A custom logger must not let an exception escape into native code either.
            }
        }

        private static void RunCleanup(Action cleanup, string operation)
        {
            try
            {
                cleanup();
            }
            catch (Exception e)
            {
                LogCallbackError(operation, e);
            }
        }

        // Each callback below repeats the enter/try/finally by hand rather than sharing a helper
        // that takes a delegate: StaticOnDatagram runs for every datagram, and a closure per call
        // would put an allocation in the audio path. The catch is not optional either - an exception
        // crossing back into native code is undefined behaviour and was observed to leave the
        // runtime unable to create further rooms.

        /// <summary>
        /// Releases the room's resources once no native callback is inside it any more.
        /// </summary>
        /// <remarks>
        /// Called from teardown and from the last callback to leave; whichever finds the room empty
        /// does the work, and the interlocked flag keeps it to exactly one. Releasing behind a
        /// running callback is what has to be avoided: it could still add sockets or peers after
        /// FreePeers, or raise events on a half released room. Waiting for it instead is not an
        /// option, since teardown usually runs on Unity's main thread during OnDestroy.
        /// </remarks>
        private void TryReleaseResources()
        {
            if (Volatile.Read(ref _releaseRequested) == 0) return;
            if (Volatile.Read(ref _activeCallbacks) > 0) return;
            if (Interlocked.CompareExchange(ref _released, 1, 0) != 0) return;

            disposedValue = true;

            // Attempt every release even when a resource's Dispose override throws. Cleanup can
            // run in a native callback's finally block, so errors must stay on the managed side.
            RunCleanup(FreeEncoders, nameof(FreeEncoders));
            RunCleanup(FreePeers, nameof(FreePeers));
            RunCleanup(UnsubscribeEvents, nameof(UnsubscribeEvents));
            RunCleanup(() => CryptoCipher?.Dispose(), nameof(CryptoCipher));
            CryptoCipher = null;

            RunCleanup(() => Handle?.Dispose(), nameof(Handle));
            Handle = null;
        }

        private void RequestRelease()
        {
            _callbacksDisabled = true;
            _callbackRooms.TryRemove(_callbackId, out _);
            Interlocked.Exchange(ref _releaseRequested, 1);
            lock (_closeTimerLock)
            {
                _closeTimer?.Dispose();
                _closeTimer = null;
            }

            TryReleaseResources();
        }

        private void OnCloseTimeout(object state)
        {
            // The timer runs independently of Unity Update, including after OnDisable/OnDestroy.
            RunCleanup(RequestRelease, nameof(OnCloseTimeout));
        }

        // native keeps the function pointers for the lifetime of the room; static
        // references prevent the marshaling thunks from being garbage collected
        private static readonly NativeLibraryMethods.OdinOnDatagramDelegate StaticOnDatagramDelegate = StaticOnDatagram;
        private static readonly NativeLibraryMethods.OdinOnRPCDelegate StaticOnRpcDelegate = StaticOnRpc;
        private static readonly NativeLibraryMethods.OdinOnSocketDelegate StaticOnSocketDelegate = StaticOnSocket;

#if UNITY_64 || __MonoCS__
        [AOT.MonoPInvokeCallback(typeof(NativeLibraryMethods.OdinOnDatagramDelegate))]
#endif
        private static void StaticOnDatagram(IntPtr room, ref OdinDatagramProperties properties, IntPtr bytes, uint bytesLength, IntPtr userData)
        {
            if (TryEnterCallback(userData, out Room self) == false) return;

            try
            {
                self.OnNativeDatagramReceived(room, in properties, bytes, bytesLength, userData);
            }
            catch (Exception e)
            {
                LogCallbackError(nameof(StaticOnDatagram), e);
            }
            finally
            {
                ExitCallback(self);
            }
        }

#if UNITY_64 || __MonoCS__
        [AOT.MonoPInvokeCallback(typeof(NativeLibraryMethods.OdinOnRPCDelegate))]
#endif
        private static void StaticOnRpc(IntPtr room, string json, IntPtr userData)
        {
            if (TryEnterCallback(userData, out Room self) == false) return;

            try
            {
                self.OnNativeRPCReceived(room, json, userData);
            }
            catch (Exception e)
            {
                LogCallbackError(nameof(StaticOnRpc), e);
            }
            finally
            {
                ExitCallback(self);
            }
        }

#if UNITY_64 || __MonoCS__
        [AOT.MonoPInvokeCallback(typeof(NativeLibraryMethods.OdinOnSocketDelegate))]
#endif
        private static void StaticOnSocket(IntPtr socket, IntPtr bytes, uint bytesLength, IntPtr userData)
        {
            if (TryEnterCallback(userData, out Room self) == false) return;

            try
            {
                self.OnNativeSocketReceived(socket, bytes, bytesLength, userData);
            }
            catch (Exception e)
            {
                LogCallbackError(nameof(StaticOnSocket), e);
            }
            finally
            {
                ExitCallback(self);
            }
        }

        private void Room_OnPeerJoined(object sender, PeerJoinedObjectContainer args)
        {
            RemotePeers.TryAdd(args.peer_id, new PeerEntity(args.peer_id)
            {
                UserId = args.user_id,
                Tags = args.tags,
                UserData = new UserData(args.user_data),
                AudioParameters = args.audio_parameters,
                VideoParameters = args.video_parameters,
            });

            ApplyListenChannelMasks();
        }

        /// <summary>
        /// Remove and dispose peer of <see cref="RemotePeers"/>
        /// </summary>
        protected virtual void Room_OnPeerLeft(object sender, PeerLeftObjectContainer args)
        {
            if (RemotePeers.TryRemove(args.peer_id, out PeerEntity peer))
                peer?.Dispose();

            lock (_ListenChannelMasksLock)
                _ListenChannelMaskOverrides.Remove(args.peer_id);
        }

        /// <summary>
        /// Add new created decoder to <see cref="RemotePeers"/> by id
        /// returns decoder or null if there is no peer
        /// </summary>
        public virtual MediaDecoder CreateMediaDecoder(uint peerId)
        {
            if (RemotePeers.TryGetValue(peerId, out PeerEntity peer))
            {
                var decoder = MediaDecoder.Create(Samplerate, Stereo);
                if (decoder == null) return null;
                decoder.Parent = peer;
                peer?.Medias.TryAdd(decoder.Id, decoder);
                return decoder;
            }

            return null;
        }

        /// <summary>
        /// Remove and dispose decoder of <see cref="RemotePeers"/> by media id
        /// </summary>
        public virtual void RemoveMediaDecoder(uint peerId, ulong mediaId)
        {
            if (this.RemoveDecoder(peerId, mediaId, out MediaDecoder decoder))
                decoder?.Dispose();
        }

        /// <summary>
        /// Set userdata of <see cref="RemotePeers"/> by id
        /// </summary>
        protected virtual void Room_OnPeerChanged(object sender, PeerChangedObjectContainer args)
        {
            if (RemotePeers.TryGetValue(args.peer_id, out PeerEntity peer))
                peer?.SetUserData(Encoding.UTF8.GetBytes(args.user_data));
        }

        /// <summary>
        /// Log message in Debug 
        /// </summary>
        protected virtual void Room_OnMessageReceived(object sender, uint peerId, byte[] message)
        {
            OdinLog.LogInfo($"{nameof(Room_OnMessageReceived)}(id {Id}) peer {peerId} message {message.Length}");
            OdinLog.LogDebug($"\t{Encoding.UTF8.GetString(message)}");
        }

        /// <summary>
        /// Set <see cref="RemotePeers"/> for bookkeeping and AvailableEncoderIds for encoders
        /// </summary>
        protected virtual void Room_OnRoomJoined(object sender, JoinedObjectContainer args)
        {
            OwnPeerId = args.own_peer_id;
            Name = args.room_name;

            //RemotePeers = new ConcurrentDictionary<uint, PeerEntity>(peers.Select(rpc =>
            //        new PeerEntity(rpc.Id)
            //        {
            //            UserId = rpc.UserId,
            //            UserData = new UserData(rpc.UserData),
            //            Medias = new ConcurrentDictionary<ulong, MediaDecoder>(rpc.Medias.Select(m =>
            //            {
            //                var decoder = MediaDecoder.Create(rpc.Id, Samplerate, Stereo);
            //                decoder.IsPaused = m.Paused;
            //                return new KeyValuePair<ulong, MediaDecoder>(decoder.Id, decoder);
            //            }))
            //        })
            //        .ToDictionary(kvp => kvp.Id));
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
            if (RoomStatus.Equals("Joining", StringComparison.InvariantCultureIgnoreCase))
                _InTransition = true;
            else if (IsJoined || IsClosed)
            {
                if (IsJoined && _InTransition && Sockets.Count > 0)
                {
                    foreach (var localSocket in Sockets.Where(s => s.Value.IsRemote == false).Select(s => s.Value))
                        localSocket.RefreshHandle();
                }

                _InTransition = false;
            }

            if (_InTransition && string.IsNullOrEmpty(RoomStatus))
                OdinLog.LogWarning("Auto reconnect from hot-reload currently not supported!");
        }

        /// <summary>
        /// Default impl will push a datagram to the <see cref="MediaDecoder"/>s of the sending peer whose
        /// <see cref="MediaDecoder.ListenChannelMask"/> overlaps the datagram's channel mask.
        /// </summary>
        /// <remarks>The room will drop a datagram if there is no matching peer or decoder to push</remarks>
        /// <param name="sender">Room object</param>
        /// <param name="args">Datagram event arguments</param>
        public virtual void Room_OnDatagram(object sender, DatagramEventArgs args)
        {
            OdinLog.Assert(args.Datagram != IntPtr.Zero, $"{nameof(DatagramEventArgs)} {nameof(Room_OnDatagram)} datagram pointer should not be zero");
            OdinLog.LogDebug($"{nameof(Room_OnDatagram)}(id {Id}) room datagramSize {args.Payload.Length}");

            if (RemotePeers.TryGetValue(args.PeerId, out PeerEntity peer) == false)
            {
                OdinLog.LogDebug($"{nameof(Room_OnDatagram)}(id {Id}) dropped datagram of unknown peer {args.PeerId}");
                return;
            }

            foreach (MediaDecoder decoder in peer.Medias.Values)
            {
                if (args.ChannelMask == ChannelMask.None || (decoder.ListenChannelMask & args.ChannelMask) != ChannelMask.None)
                    decoder.Push(args.Datagram, args.Payload.Length);
            }
        }

        /// <summary>
        /// Default impl will process all rpc packets.
        /// </summary>
        /// <remarks>Client side processing of <see cref="OdinNative.Utils.Json.JSONParser">json</see></remarks>
        /// <param name="sender">Room object</param>
        /// <param name="args">RPC event arguments</param>
        public virtual void Room_OnRPC(object sender, RpcEventArgs args)
        {
            OdinLog.LogDebug($"{nameof(Room_OnRPC)}(id {Id}) rpcSize {args.Rpc.Length}\n{args.Rpc}");
            ProcessJsonRpc(args.Rpc);
        }

        /// <summary>
        /// Default impl will forward all socket messages.
        /// </summary>
        /// <remarks>The room will drop socket messages if the remote peer had a reconnect and the socket is not recreated yet</remarks>
        /// <param name="sender">Room object</param>
        /// <param name="args">RPC event arguments</param>
        public virtual void Room_OnSocket(object sender, SocketMessageEventArgs args)
        {
            OdinLog.LogDebug($"{nameof(Room_OnSocket)}(id {Id}) messageSize {args.Payload?.Length}");
            args.Socket.OnSocket(this, args);
        }

        #region ParseRpc
        protected virtual void ProcessJsonRpc(string json)
        {
            if (string.IsNullOrEmpty(json)) return; // NOP

            ReadOnlyDictionary<string, object> root = new ReadOnlyDictionary<string, object>((Dictionary<string, object>)JSONParser.FromJson<object>(json)); // no interface support
            foreach (var kvp in root)
            {
                switch (kvp.Key)
                {
                    #region RoomRpc
                    case RoomStatusChangedObject.EVENTNAME:
                        RoomStatusChangedObject roomStatusChangedObjectRoot = JSONParser.FromJson<RoomStatusChangedObject>(json);
                        RoomStatusChangedRpc(roomStatusChangedObjectRoot.Values);
                        break;
                    case NewReconnectTokenObject.EVENTNAME:
                        NewReconnectTokenObject newReconnectTokenObjectRoot = JSONParser.FromJson<NewReconnectTokenObject>(json);
                        NewReconnectTokenRpc(newReconnectTokenObjectRoot.Values);
                        break;
                    case JoinedObject.EVENTNAME:
                        JoinedObject joinedObjectRoot = JSONParser.FromJson<JoinedObject>(json);
                        JoinedRpc(joinedObjectRoot.Values);
                        break;
                    #endregion RoomRpc

                    #region PeerRpc
                    case ChangeSelfObject.EVENTNAME:
                        ChangeSelfObject changeSelfObjectRoot = JSONParser.FromJson<ChangeSelfObject>(json);
                        ChangeSelfRpc(changeSelfObjectRoot.Values);
                        break;
                    case PeerJoinedObject.EVENTNAME:
                        PeerJoinedObject peerJoinedObjectRoot = JSONParser.FromJson<PeerJoinedObject>(json);
                        PeerJoinedRpc(peerJoinedObjectRoot.Values);
                        break;
                    case PeerLeftObject.EVENTNAME:
                        PeerLeftObject peerLeftObjectRoot = JSONParser.FromJson<PeerLeftObject>(json);
                        PeerLeftRpc(peerLeftObjectRoot.Values);
                        break;
                    case PeerChangedObject.EVENTNAME:
                        PeerChangedObject peerChangedObjectRoot = JSONParser.FromJson<PeerChangedObject>(json);
                        PeerChangedRpc(peerChangedObjectRoot.Values);
                        break;
                    case MessageReceivedObject.EVENTNAME:
                        MessageReceivedObject messageReceivedObjectRoot = JSONParser.FromJson<MessageReceivedObject>(json);
                        MessageReceivedRpc(messageReceivedObjectRoot.Values);
                        break;
                    #endregion PeerRpc

                    default:
                        OdinLog.Assert(false, $"{nameof(ProcessJsonRpc)} type \"{kvp.Key}\" NotImplemented");
#pragma warning disable CS0618 // Type or member is obsolete
                        OdinLog.Throw(new NotImplementedException(json), OdinLog.LogVerbosity == OdinLog.VerbosityLevel.Verbose);
#pragma warning restore CS0618 // Type or member is obsolete
                        break;
                }
            }
        }

        private void RoomStatusChangedRpc(RoomStatusChangedObjectContainer values)
        {
            if (string.Equals(values.status, "Closed", StringComparison.OrdinalIgnoreCase))
                _nativeClosed = true;

            try
            {
                // The next join sends a fresh peer snapshot. Retire the old session before its
                // status event, so consumers remove old decoders before seeing any new joins.
                if (_nativeClosed || string.Equals(values.status, "Joining", StringComparison.OrdinalIgnoreCase))
                    FreePeers();
                OnRoomStatusChanged?.Invoke(this, string.IsNullOrEmpty(values.status) ? "unknown" : values.status);
            }
            finally
            {
                if (_nativeClosed && Volatile.Read(ref _disposeStarted) != 0)
                    RequestRelease();
            }
        }

        private void NewReconnectTokenRpc(NewReconnectTokenObjectContainer values)
        {
            this.ReconnectToken = values.token;
        }

        private void JoinedRpc(JoinedObjectContainer values)
        {
            this.OwnPeerId = values.own_peer_id;
            this.Name = values.room_name;
            this.Customer = values.customer;
            OnRoomJoined?.Invoke(this, values);
        }

        private void MessageReceivedRpc(MessageReceivedObjectContainer values)
        {
            OnMessageReceived?.Invoke(this, values.sender_peer_id, values.GetPayload());
        }

        private void PeerLeftRpc(PeerLeftObjectContainer values)
        {
            if (!RemotePeers.TryRemove(values.peer_id, out PeerEntity peer)) return;
            RunCleanup(() => peer.Dispose(), nameof(PeerLeftRpc));
            lock (_ListenChannelMasksLock)
                _ListenChannelMaskOverrides.Remove(values.peer_id);

            var handlers = OnPeerLeft;
            if (handlers == null) return;
            foreach (OnPeerLeftDelegate handler in handlers.GetInvocationList())
                RunCleanup(() => handler(this, values), nameof(OnPeerLeft));
        }

        private void ChangeSelfRpc(ChangeSelfObjectContainer values)
        {
            this.UserId = string.IsNullOrEmpty(values.user_id) ? this.UserId : values.user_id;
            if (values.tags != null)
                this.Tags = values.tags;
        }

        private void PeerJoinedRpc(PeerJoinedObjectContainer values)
        {
            if (Volatile.Read(ref _disposeStarted) != 0) return;
            OnPeerJoined?.Invoke(this, values);
        }

        private void PeerChangedRpc(PeerChangedObjectContainer values)
        {
            OnPeerChanged?.Invoke(this, values);
        }
        #endregion ParseRpc
        /// <summary>
        /// Create and join a Room
        /// </summary>
        /// <param name="endPoint">Gateway server</param>
        /// <param name="auth">json authentication string</param>
        /// <param name="samplerate">sets default samplerate</param>
        /// <param name="stereo">sets default stereo flag</param>
        /// <param name="cipher">optional crypto or null</param>
        /// <param name="room">Initialized room</param>
        /// <returns>true on successfully request join or false</returns>
        public static bool Join(string endPoint, string auth, uint samplerate, bool stereo, OdinCipherHandle cipher, out Room room)
        {
            room = Create(endPoint, samplerate, stereo);
            return room.Create(auth, cipher);
        }

        /// <summary>
        /// Create and join a Room
        /// </summary>
        /// <param name="endPoint">Gateway server</param>
        /// <param name="token">join token</param>
        /// <param name="roomName">room name</param>
        /// <param name="samplerate">sets default samplerate</param>
        /// <param name="stereo">sets default stereo flag</param>
        /// <param name="cipher">optional crypto or null</param>
        /// <param name="room">Initialized room</param>
        /// <returns>true on successfully request join or false</returns>
        public static bool Join(string endPoint, string token, string roomName, uint samplerate, bool stereo, OdinCipherHandle cipher, out Room room) => 
            Join(endPoint, 
            JSONWriter.ToJson(new RoomCreateObject { token = token, room_Id = roomName }), 
            samplerate, 
            stereo, 
            cipher, 
            out room);

        /// <summary>
        /// Join a room
        /// </summary>
        /// <param name="token">token for json authentication string</param>
        /// <remarks>Sends <see cref="InitialUserData"/> with the join request if set</remarks>
        /// <param name="roomName">room name for json authentication string</param>
        /// <param name="cipher">optional crypto</param>
        /// <returns>true on successfully request join or false</returns>
        public bool Join(string token, string roomName, OdinCipherHandle cipher = null) => Join(
            new RoomCreateObject 
            { 
                token = token, 
                room_Id = roomName,
                user_data = GetInitialUserData()
            }, cipher);
        internal bool Join(RoomCreateObject auth, OdinCipherHandle cipher = null) => this.Create(JSONWriter.ToJson(auth), cipher);

        /// <summary>
        /// Join a room
        /// </summary>
        /// <remarks>Sends <see cref="InitialUserData"/> with the join request if set</remarks>
        /// <param name="token">token for json authentication string</param>
        /// <param name="cipher">optional crypto</param>
        /// <returns>true on successfully request join or false</returns>
        public bool Join(string token, OdinCipherHandle cipher = null) => this.Join(new RoomCreateObject
        {
            token = token,
            user_data = GetInitialUserData()
        }, cipher);

        // an empty value is left out of the join request instead of joining with empty user data
        private string GetInitialUserData() => string.IsNullOrEmpty(InitialUserData) ? null : InitialUserData;

        /// <summary>
        /// Join a room with a json authentication string
        /// </summary>
        /// <remarks>Always false if the room is already connected. Use a new room object!</remarks>
        /// <param name="authentication">Join token</param>
        /// <param name="cipher">optional crypto cipher handle or null</param>
        /// <returns>true on successfully request join or false</returns>
        protected bool Create(string authentication, OdinCipherHandle cipher = null)
        {
            OdinLog.Assert(string.IsNullOrEmpty(EndPoint.ToString()) == false, $"{nameof(Odin.Library.Methods.RoomCreate)} {nameof(EndPoint)} IsNullOrEmpty");
            OdinLog.Assert(string.IsNullOrEmpty(authentication) == false, $"{nameof(Odin.Library.Methods.RoomCreate)} {nameof(authentication)} IsNullOrEmpty");

            if (IsJoined || Volatile.Read(ref _disposeStarted) != 0)
                return false;

            this.Authentication = authentication;
#if DEBUG
            OdinLog.LogDebug(this.Authentication);
#endif

            if (cipher != null && cipher.IsAlive)
                this.CryptoCipher = Crypto.Create(cipher);

            var result = Odin.Library.Methods.RoomCreate(EndPoint.ToString(), authentication, ref _connectionEvents, cipher, out _handle);
            bool ret = Utility.IsOk(result);
            if (ret == false)
                OdinLog.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomCreate)} in {nameof(Room.Join)} failed (invalid {Handle.IsInvalid}): {Utility.OdinLastErrorString()} (code {result})").ToString());

            return ret;
        }

        /// <summary>
        /// Retrieves the room id
        /// </summary>
        /// <remarks>handle as id representation</remarks>
        /// <returns>room id</returns>
        public ulong GetRoomId()
        {
            OdinLog.Assert(Handle?.IsAlive == true, $"{nameof(GetRoomId)} {nameof(OdinRoomHandle)} is released");
            return (ulong)(IntPtr)Handle;
        }

        /// <summary>
        /// Retrieves the room name
        /// </summary>
        /// <remarks>Updates <see cref="Room.Name"/> on success by default</remarks>
        /// <param name="update">update this room name on true</param>
        /// <returns>room name</returns>
        public string GetRoomName(bool update = true)
        {
            OdinLog.Assert(Handle?.IsAlive == true, $"{nameof(Odin.Library.Methods.RoomGetName)} {nameof(OdinRoomHandle)} is released");

            OdinError result = Odin.Library.Methods.RoomGetName(Handle, out string name);
            
            if (Utility.IsOk(result) == false)
                OdinLog.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomGetName)} in {nameof(Room.GetRoomName)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());
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
            OdinLog.Assert(Handle?.IsAlive == true, $"{nameof(Odin.Library.Methods.RoomResendUserData)} {nameof(OdinRoomHandle)} is released");

            OdinError result = Odin.Library.Methods.RoomResendUserData(Handle);

            if (Utility.IsOk(result) == false)
                OdinLog.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomResendUserData)} in {nameof(Room.ResendUserData)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());
            return result;
        }

        /// <summary>
        /// Get an encoder from <see cref="Encoders"/> by id
        /// </summary>
        /// <param name="mediaId">id of an input media</param>
        /// <param name="encoder">input object</param>
        /// <returns>true on encoder found or false</returns>
        public bool GetEncoder(ulong mediaId, out MediaEncoder encoder)
        {
            return Encoders.TryGetValue(mediaId, out encoder);
        }
        /// <summary>
        /// Get an encoder from <see cref="Encoders"/> by id. If the encoder is not found create a new one that will be added to <see cref="Encoders"/>.
        /// </summary>
        /// <remarks>Will use the default samplerate and stereo flag set by the current room</remarks>
        /// <param name="encoder">input object</param>
        /// <returns>true or false on error</returns>
        public bool GetOrCreateEncoder(out MediaEncoder encoder) => GetOrCreateEncoder(ulong.MaxValue, Samplerate, Stereo, out encoder);
        /// <summary>
        /// Get an encoder from <see cref="Encoders"/> by id. If the encoder is not found create a new one that will be added to <see cref="Encoders"/>.
        /// </summary>
        /// <remarks>Will use the default samplerate and stereo flag set by the current room</remarks>
        /// <param name="mediaId">id of an input media</param>
        /// <param name="encoder">input object</param>
        /// <returns>true or false on error</returns>
        public bool GetOrCreateEncoder(ulong mediaId, out MediaEncoder encoder) => GetOrCreateEncoder(mediaId, Samplerate, Stereo, out encoder);
        /// <summary>
        /// Get an encoder from <see cref="Encoders"/> by id. If the encoder is not found create a new one that will be added to <see cref="Encoders"/>.
        /// </summary>
        /// <param name="mediaId">id of an input media</param>
        /// <param name="samplerate">custom samplerate</param>
        /// <param name="stereo">custom stereo flag</param>
        /// <param name="encoder">input object</param>
        /// <returns>true or false on error</returns>
        public bool GetOrCreateEncoder(ulong mediaId, uint samplerate, bool stereo, out MediaEncoder encoder)
        {
            if (Encoders.TryGetValue(mediaId, out encoder))
                return true;

            encoder = CreateEncoder(samplerate, stereo);
            return encoder != null;
        }
        /// <summary>
        /// Create a new input media that will be added to <see cref="Encoders"/>
        /// </summary>
        /// <remarks>Will use the default samplerate and stereo flag set by the current room</remarks>
        /// <returns>input media</returns>
        public MediaEncoder CreateEncoder() => CreateEncoder(Samplerate, Stereo);
        /// <summary>
        /// Create a new input media that will be added to <see cref="Encoders"/>
        /// </summary>
        /// <param name="samplerate">custom samplerate</param>
        /// <param name="stereo">custom stereo flag</param>
        /// <param name="peerId">binding peer id</param>
        /// <remarks>Peer id 0 will rewrite header on datagram send for connected room peer id</remarks>
        /// <returns>input media</returns>
        public MediaEncoder CreateEncoder(uint samplerate, bool stereo, uint peerId = 0)
        {
            MediaEncoder encoder = MediaEncoder.Create(peerId, samplerate, stereo);
            if (encoder == null) return null;
            Encoders.TryAdd(encoder.Id, encoder);
            return encoder;
        }
        /// <summary>
        /// Removes the input media from <see cref="Encoders"/>
        /// </summary>
        /// <param name="mediaId">id of an input media</param>
        /// <param name="encoder">input media that was removed</param>
        /// <returns>true on encoder found or false</returns>
        public bool RemoveEncoder(ulong mediaId, out MediaEncoder encoder) => Encoders.TryRemove(mediaId, out encoder);

        /// <summary>
        /// Get a decoder from <see cref="PeerEntity.Medias"/> of <see cref="RemotePeers"/> by id
        /// </summary>
        /// <param name="peerId">id of peer</param>
        /// <param name="mediaId">id of output media</param>
        /// <param name="decoder">output object or null</param>
        /// <returns>true on decoder found or false</returns>
        public bool GetDecoder(uint peerId, ulong mediaId, out MediaDecoder decoder)
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
        public bool GetOrCreateDecoder(uint peerId, ulong mediaId, out MediaDecoder decoder) => GetOrCreateDecoder(peerId, mediaId, Samplerate, Stereo, out decoder);
        /// <summary>
        /// Get a decoder from <see cref="PeerEntity.Medias"/> of <see cref="RemotePeers"/> by id. If the decoder is not found create a new one that will be added to the Peer
        /// </summary>
        /// <param name="peerId">id of peer</param>
        /// <param name="mediaId">id of output media</param>
        /// <param name="samplerate">custom samplerate</param>
        /// <param name="stereo">custom stereo flag</param>
        /// <param name="decoder">output object or null</param>
        /// <returns>true on decoder found or false</returns>
        public bool GetOrCreateDecoder(uint peerId, ulong mediaId, uint samplerate, bool stereo, out MediaDecoder decoder)
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
        /// <returns>output media</returns>
        public MediaDecoder CreateDecoder(uint peerId) => CreateDecoder(peerId, Samplerate, Stereo);
        /// <summary>
        /// Create a new output media that will be added to <see cref="PeerEntity.Medias"/>
        /// </summary>
        /// <remarks>Will use the default samplerate and stereo flag set by the current room</remarks>
        /// <param name="peerId">id of peer</param>
        /// <param name="samplerate">custom samplerate</param>
        /// <param name="stereo">custom stereo flag</param>
        /// <returns>output media</returns>
        public MediaDecoder CreateDecoder(uint peerId, uint samplerate, bool stereo)
        {
            RemotePeers.TryGetValue(peerId, out var peer);
            return peer?.CreateDecoder(samplerate, stereo) ?? null;
        }
        /// <summary>
        /// Removes an output media from a remote peer.
        /// </summary>
        /// <param name="peerId">id of peer</param>
        /// <param name="mediaId">id of an output media</param>
        /// <param name="decoder">output media that was removed</param>
        /// <returns>true on decoder found or false</returns>
        public bool RemoveDecoder(uint peerId, ulong mediaId, out MediaDecoder decoder)
        {
            // the peer may already be gone when a decoder removal is processed deferred
            if (RemotePeers.TryGetValue(peerId, out PeerEntity peer))
                return peer.RemoveDecoder(mediaId, out decoder);
            decoder = null;
            return false;
        }

        #region RPC
        private readonly object _ListenChannelMasksLock = new object();
        private readonly object _ListenChannelMasksSendLock = new object();
        private readonly Dictionary<uint, ChannelMask> _ListenChannelMaskOverrides = new Dictionary<uint, ChannelMask>();
        private ChannelMask _DefaultListenChannelMask = ChannelMask.All;
        private bool _ListenChannelMaskCustomized = false;

        /// <summary>
        /// Sets the channel mask used when listening to peers that do not have a per-peer override set
        /// via <see cref="SetListenChannelMaskForPeer"/>. Applies immediately to every currently known peer
        /// and is automatically (re-)applied to peers that join afterward.
        /// </summary>
        /// <param name="mask">channel mask to listen to for all peers without an override</param>
        /// <returns>true on success or false</returns>
        public virtual bool SetListenChannelMask(ChannelMask mask)
        {
            lock (_ListenChannelMasksLock)
            {
                _DefaultListenChannelMask = mask;
                _ListenChannelMaskCustomized = true;
            }
            return ApplyListenChannelMasks();
        }

        /// <summary>
        /// Overrides the channel mask used when listening to a specific peer, taking precedence over
        /// the default set via <see cref="SetListenChannelMask"/>.
        /// </summary>
        /// <param name="peerId">peer to override the listen channel mask for</param>
        /// <param name="mask">channel mask to listen to for this peer</param>
        /// <returns>true on success or false</returns>
        public virtual bool SetListenChannelMaskForPeer(uint peerId, ChannelMask mask)
        {
            lock (_ListenChannelMasksLock)
            {
                _ListenChannelMaskOverrides[peerId] = mask;
                _ListenChannelMaskCustomized = true;
            }
            return ApplyListenChannelMasks();
        }

        /// <summary>
        /// Removes a previously set per-peer listen channel mask override, falling back to the default
        /// set via <see cref="SetListenChannelMask"/> for that peer.
        /// </summary>
        /// <param name="peerId">peer to remove the override for</param>
        /// <returns>true on success or false</returns>
        public virtual bool ClearListenChannelMaskForPeer(uint peerId)
        {
            lock (_ListenChannelMasksLock)
            {
                if (_ListenChannelMaskOverrides.Remove(peerId) == false)
                    return true;
            }
            return ApplyListenChannelMasks();
        }

        /// <summary>
        /// Rebuilds and (re-)sends the listen channel mask for every known peer in <see cref="RemotePeers"/>,
        /// using per-peer overrides where set and the default listen channel mask otherwise.
        /// </summary>
        private bool ApplyListenChannelMasks()
        {
            // the send lock serializes snapshot+send so concurrent setters cannot reach the
            // server in reversed order; native callbacks only ever take the brief state lock,
            // so holding the send lock across the native call cannot deadlock the dispatch thread
            lock (_ListenChannelMasksSendLock)
            {
                Dictionary<uint, ulong> masks;
                lock (_ListenChannelMasksLock)
                {
                    if (_ListenChannelMaskCustomized == false || RemotePeers.IsEmpty)
                        return true;

                    masks = new Dictionary<uint, ulong>();
                    foreach (uint peerId in RemotePeers.Keys)
                    {
                        ChannelMask mask = _ListenChannelMaskOverrides.TryGetValue(peerId, out ChannelMask overrideMask) ? overrideMask : _DefaultListenChannelMask;
                        masks[peerId] = (ulong)mask;
                    }
                }

                return SetChannelMasks(masks, reset: true);
            }
        }

        /// <summary>
        /// Sends a <c>"SetChannelMasks"</c> request to the server to control which channels should be
        /// received from the specified peers.
        /// </summary>
        /// <param name="masks">peer id to channel mask mapping</param>
        /// <param name="reset">when true, replaces the server-side subscription instead of merging into it</param>
        /// <returns>true on success or false</returns>
        public virtual bool SetChannelMasks(IDictionary<uint, ulong> masks, bool reset)
        {
            // mask must serialize as a bare JSON number: grid parses Call::SetChannelMasks with
            // serde_json and ChannelMask(u64) rejects a quoted string, dropping the whole RPC
            object[] pairs = masks.Select(kvp => new object[] { kvp.Key, kvp.Value }).ToArray();
            return SendRpc(new { SetChannelMasks = new { masks = pairs, reset = reset } });
        }

        /// <summary>
        /// Update arbitrary userdata of self (note: <see cref="UserData"/>)
        /// </summary>
        /// <param name="userData">arbitrary data</param>
        public virtual bool UpdateUserData(IUserData userData) => UpdateUserData(userData.ToJson());
        /// <summary>
        /// Update userdata of self (note: <see cref="UpdateUserData(IUserData)"/>)
        /// </summary>
        /// <param name="json">json data</param>
        public virtual bool UpdateUserData(string json)
        {
            return SendRpc(new { ChangeSelf = new { user_data = json } });
        }

        /// <summary>
        /// Send a <c>"SendMessage"</c> rpc to the server to broadcast the message to all peers in the room.
        /// </summary>
        /// <remarks>Peers receive it as <see cref="OnMessageReceived"/> with the UTF8 bytes of the string</remarks>
        /// <param name="message">UTF8 string</param>
        public virtual bool SendMessage(string message)
        {
            return SendMessage(Encoding.UTF8.GetBytes(message ?? string.Empty));
        }

        /// <summary>
        /// Send a <c>"SendMessage"</c> rpc to the server with arbitrary bytes.
        /// </summary>
        /// <remarks>
        /// Messages are delivered reliably and in order over the signaling channel and are not affected by
        /// channel masks or positions. Peers receive them as <see cref="OnMessageReceived"/>.
        /// </remarks>
        /// <param name="message">arbitrary data</param>
        /// <param name="peerIds">receiving peers or null to broadcast to all peers in the room</param>
        /// <returns>true on success or false</returns>
        public virtual bool SendMessage(byte[] message, IEnumerable<uint> peerIds = null)
        {
            if (message == null) return false;

            // peer_ids is optional on the wire: leave it out entirely for a broadcast
            var payload = new Dictionary<string, object> { { "message", message } };
            if (peerIds != null)
                payload["peer_ids"] = peerIds.ToArray();

            return SendRpc(new Dictionary<string, object> { { "SendMessage", payload } });
        }

        /// <summary>
        /// Send rpc to the room as json representation
        /// </summary>
        /// <typeparam name="T">type</typeparam>
        /// <param name="obj">data to serialize</param>
        public virtual bool SendRpc<T>(T obj)
        {
            if (obj == null) return false;

            return Utility.IsOk(SendRpc(JSONWriter.ToJson(obj)));
        }

        /// <summary>
        /// Send raw rpc to the room
        /// </summary>
        /// <param name="rpc">json representation string</param>
        public OdinError SendRpc(string rpc)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(Odin.Library.Methods.RoomSendRpc)} {nameof(OdinRoomHandle)} is released");

            OdinError result = Odin.Library.Methods.RoomSendRpc(Handle, rpc);
            if (Utility.IsOk(result) == false)
                OdinLog.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomSendRpc)} in {nameof(Room.SendRpc)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());
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
        public virtual bool SendAudio(float[] samples, ulong mediaId, bool isSilent = false)
            => this.Encoders.TryGetValue(mediaId, out MediaEncoder encoder) && SendAudio(samples, encoder, isSilent);
        /// <summary>
        /// Push the samples to the input media for pipeline processing and pop the result as datagram to the server
        /// </summary>
        /// <param name="samples">Audio data</param>
        /// <param name="encoder">input media</param>
        /// <returns>true on success or false</returns>
        public virtual bool SendAudio(float[] samples, MediaEncoder encoder) => SendAudio(samples, encoder, false);
        /// <summary>
        /// Push the samples to the input media for pipeline processing and pop the result as datagram to the server
        /// </summary>
        /// <remarks>On <paramref name="isSilent"/> the samples are zeroed but still pushed, so the
        /// pipeline time advances and activity events update while no audio content is transmitted</remarks>
        /// <param name="samples">Audio data</param>
        /// <param name="encoder">input media</param>
        /// <param name="isSilent">flag these samples as silence</param>
        /// <returns>true on success or false</returns>
        public virtual bool SendAudio(float[] samples, MediaEncoder encoder, bool isSilent)
        {
            if (encoder == null) return false;

            if (isSilent)
                Array.Clear(samples, 0, samples.Length);
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
            if (encoder == null)
                OdinLog.LogError($"{nameof(SendEncoderAudio)} {nameof(MediaEncoder)} is null");

            if(encoder == null)
                return false;

            // Preserve the byte[] virtual send hook for derived rooms.
            if (GetType() == typeof(Room))
            {
                byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(1920);
                try
                {
                    while (true)
                    {
                        var result = encoder.Pop(buffer, out uint count);
                        if (result == OdinError.ODIN_ERROR_NO_DATA) return true;
                        if (result != OdinError.ODIN_ERROR_SUCCESS) return false;
                        if (SendDatagram(buffer, count) != OdinError.ODIN_ERROR_SUCCESS) return false;
                    }
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            OdinError error;
            do
            {
                error = encoder.Pop(out var datagram);
                switch (error)
                {
                    case OdinError.ODIN_ERROR_SUCCESS:
                        this.SendDatagram(datagram);
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
        /// <param name="datagram">encoder datagram</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        protected virtual OdinError SendDatagram(byte[] datagram)
            => SendDatagram(datagram, (uint)datagram.Length);

        private OdinError SendDatagram(byte[] datagram, uint count)
        {
            if (OdinDefaults.DEBUG) OdinLog.Assert(Handle.IsAlive, $"{nameof(Odin.Library.Methods.RoomSendDatagram)} {nameof(OdinRoomHandle)} is released", silent: true);

            OdinError result = Odin.Library.Methods.RoomSendDatagram(Handle, datagram, count);
            if (Utility.IsOk(result) == false)
                OdinLog.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomSendDatagram)} in {nameof(Room.SendDatagram)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());
            return result;
        }
        #endregion Datagram

        public virtual Socket.Socket AddSocket(OdinSocketKind type, uint peerId, int label = 0, int priority = 0)
        {
            var socket = Socket.Socket.Create(this, type, peerId, label, priority);
            
            if (socket != null)
                this.Sockets.TryAdd(socket.Id, socket);

            return socket;
        }
        public virtual Socket.Socket GetSocket(ulong Id)
        {
            this.Sockets.TryGetValue(Id, out Socket.Socket socket);
            return socket;
        }
        [Obsolete("Custom uint keys never matched the native socket callback lookup; use " + nameof(AddSocket) + " and keep the returned socket - sockets are keyed by their native id, so this compatibility shim opens a new socket per call")]
        public virtual Socket.Socket GetOrAddSocket(uint Id, OdinSocketKind type, uint peerId, int label = 0, int priority = 0)
            => GetSocket(Id) ?? AddSocket(type, peerId, label, priority);
        public virtual Socket.Socket RemoveSocket(ulong Id, bool close = false)
        {
            this.Sockets.TryRemove(Id, out Socket.Socket socket);
            // reset actually closes the native socket, otherwise it keeps delivering
            // callbacks and OnNativeSocketReceived would re-add a placeholder entry
            if (close && socket?.Handle?.IsAlive == true)
                socket.Reset();
            return socket;
        }

        /// <summary>
        /// Send client side rpc message
        /// </summary>
        /// <remarks>
        /// Injects rpc for the client that loopback to itself
        /// </remarks>
        /// <param name="rpc">json string</param>
        /// <returns><see cref="OdinNative.Core.Imports.NativeBindings.OdinError.ODIN_ERROR_SUCCESS"/> or error</returns>
        protected virtual OdinError SendLoopbackRpc(string rpc)
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(Odin.Library.Methods.RoomSendLoopbackRpc)} {nameof(OdinRoomHandle)} is released");

            OdinError result = Odin.Library.Methods.RoomSendLoopbackRpc(Handle, rpc);
            if (Utility.IsOk(result) == false)
                OdinLog.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.RoomSendLoopbackRpc)} in {nameof(Room.SendLoopbackRpc)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());
            return result;
        }

        private bool _closed;

        /// <summary>
        /// Close the native room. (native dispose)
        /// </summary>
        /// <remarks>
        /// Makes our own peer leave the room on the server. Repeated calls are ignored, since both a
        /// server side leave and <see cref="Dispose()"/> reach this.
        /// </remarks>
        public void Close()
        {
            if (_closed) return;

            OdinLog.Assert(Handle.IsAlive, $"{nameof(Odin.Library.Methods.RoomClose)} {nameof(OdinRoomHandle)} is released");

            _closed = true;
            Odin.Library.Methods.RoomClose(Handle);
        }

        private void UnsubscribeEvents()
        {
            OnDatagram -= Room_OnDatagram;
            OnRpc -= Room_OnRPC;
            OnSocket -= Room_OnSocket;
            OnRoomStatusChanged -= Room_OnRoomStatusChanged;
            OnRoomJoined -= Room_OnRoomJoined;
            OnRoomLeft -= Room_OnRoomLeft;
            OnPeerJoined -= Room_OnPeerJoined;
            OnPeerLeft -= Room_OnPeerLeft;
            OnPeerChanged -= Room_OnPeerChanged;
            OnMessageReceived -= Room_OnMessageReceived;
        }

        private void FreeEncoders()
        {
            foreach (var encoder in Encoders)
                RunCleanup(() => encoder.Value.Dispose(), nameof(FreeEncoders));

            Encoders.Clear();
        }

        private void FreePeers()
        {
            foreach (uint peerId in RemotePeers.Keys)
                PeerLeftRpc(new PeerLeftObjectContainer { peer_id = peerId });
        }

        private bool disposedValue;
        /// <summary>
        /// On dispose will free the room and all associated data
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            // Also protect the Close call itself: a synchronous status callback or the timeout
            // must not free its handle before the native call returns.
            Interlocked.Increment(ref _activeCallbacks);
            try
            {
                if (Interlocked.CompareExchange(ref _disposeStarted, 1, 0) != 0) return;

                if (_nativeClosed || Handle == null || !Handle.IsAlive)
                {
                    RequestRelease();
                    return;
                }

                // Existing native versions already emit Closed. Keep their callbacks alive until
                // that event arrives, with a bounded fallback if the worker cannot finish.
                lock (_closeTimerLock)
                {
                    if (Volatile.Read(ref _releaseRequested) == 0)
                    {
                        _closeTimer = new Timer(OnCloseTimeout, null, Timeout.Infinite, Timeout.Infinite);
                        _closeTimer.Change(CloseTimeoutMs, Timeout.Infinite);
                    }
                }

                Close();
            }
            catch (Exception e)
            {
                LogCallbackError(nameof(Dispose), e);
                RequestRelease();
            }
            finally
            {
                ExitCallback(this);
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~Room()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// Requests close, then frees resources after Closed or a timeout and after active callbacks exit.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
