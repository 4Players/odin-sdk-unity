using OdinNative.Core;
using OdinNative.Core.Imports;
using OdinNative.Wrapper.Room;
using OdinNative.Wrapper.Socket;
using System;
using System.Linq;
using static OdinNative.Core.Utility;

namespace OdinNative.Wrapper
{
    /// <summary>
    /// Client Wrapper for ODIN ffi <c>OdinNative.Core.Imports.NativeLibraryMethods</c>
    /// </summary>
    public class OdinClient : MarshalByRefObject, IDisposable
    {
        private OdinTokenGeneratorHandle _tokenGeneratorHandle;

        /// <summary>
        /// Rooms
        /// </summary>
        internal volatile static RoomCollection _Rooms = new RoomCollection();
        /// <summary>
        /// A collection of all <see cref="Room.Room"/>
        /// </summary>
        public RoomCollection Rooms { get { return _Rooms; } }
        /// <summary>
        /// Sockets
        /// </summary>
        internal volatile static SocketCollection _Sockets = new SocketCollection();
        /// <summary>
        /// A collection of all <see cref="OdinNative.Wrapper.Socket.Socket"/>
        /// </summary>
        public SocketCollection Sockets { get { return _Sockets; } }
        /// <summary>
        /// Connection EndPoint of the client.
        /// </summary>
        public Uri EndPoint { get; private set; }
        /// <summary>
        /// Client AccessKey for all new rooms.
        /// </summary>
        public string AccessKey { get; private set; }

        public IUserData UserData { get; private set; }

        /// <summary>
        /// Creates a new instance for ODIN ffi C# Wrapper
        /// </summary>
        /// <remarks><see cref="OdinNative.Wrapper.UserData"/> is optional</remarks>
        /// <param name="server">Odin Server</param>
        /// <param name="accessKey">Odin access key</param>
        internal OdinClient(Uri server, string accessKey)
        {
            EndPoint = server;
            AccessKey = accessKey;
            UserData = new UserData();
        }

        /// <summary>
        /// Creates a new initialized instance for ODIN ffi C# Wrapper 
        /// </summary>
        /// <remarks>Will set a random accesskey; Can fail on initialize!</remarks>
        /// <param name="server">Odin Server/Gateway</param>
        /// <returns>OdinClient wrapper</returns>
        public static OdinClient Create(Uri server) => OdinClient.Create(server, string.Empty);

        /// <summary>
        /// Creates a new initialized instance for ODIN ffi C# Wrapper 
        /// </summary>
        /// <remarks>Can fail on initialize!</remarks>
        /// <param name="server">Odin Server</param>
        /// <param name="accessKey">Odin access key</param>
        /// <returns>OdinClient wrapper</returns>
        public static OdinClient Create(Uri server, string accessKey)
        {
            OdinClient client = new OdinClient(server, accessKey);
            client.Init();
            return client;
        }

        internal void Init()
        {
            if (Odin.Library.IsInitialized == false)
                Odin.Library.Initialize();

            if (string.IsNullOrEmpty(AccessKey))
                AccessKey = CreateAccessKey();

            var generatorResult = Odin.Library.Methods.TokenGeneratorCreate(AccessKey, out _tokenGeneratorHandle);
            OdinLog.Assert(Utility.IsOk(generatorResult), $"{nameof(OdinClient)} init {nameof(Odin.Library.Methods.TokenGeneratorCreate)} failed. {Utility.OdinErrorToString(generatorResult)} {Utility.OdinLastErrorString()}");
        }
#if UNITY_64 || __MonoCS__
        [AOT.MonoPInvokeCallback(typeof(NativeLibraryMethods.OdinOnDatagramDelegate))]
#endif
        protected static void OnNativeDatagramReceived(IntPtr room_id, uint peer_id, ulong channel_mask, IntPtr bytesPtr, uint bytes_length, IntPtr user_data)
        {
            OdinLog.Assert(bytesPtr != IntPtr.Zero, $"{nameof(OdinClient)} room {room_id} {nameof(OnNativeDatagramReceived)} datagram pointer should not be zero");
            OdinLog.Assert(bytes_length > 0, $"{nameof(OdinClient)} room {room_id} {nameof(OnNativeDatagramReceived)} datagram should not be empty");

            byte[] datagramPayload = Utility.GetNativeBuffer(bytesPtr, bytes_length);

            var room = _Rooms[(ulong)room_id];
            if (room == null) return;
            room?.OnDatagramReceived(new DatagramEventArgs() {
                RoomId = (ulong)room_id,
                PeerId = peer_id,
                ChannelMask = (ChannelMask)channel_mask,
                Datagram = bytesPtr,
                Payload = datagramPayload,
                Userdata = user_data
            });
        }

#if UNITY_64 || __MonoCS__
        [AOT.MonoPInvokeCallback(typeof(NativeLibraryMethods.OdinOnRPCDelegate))]
#endif
        protected static void OnNativeRPCReceived(IntPtr room_id, string json, IntPtr user_data)
        {
            OdinLog.Assert(string.IsNullOrEmpty(json) != true, $"{nameof(OdinClient)} room {room_id} {nameof(OnNativeRPCReceived)} rpc data should not be empty");

            var room = _Rooms[(ulong)room_id];
            Utility.Test(room != null, $"{nameof(OdinClient)} no room in {nameof(OnNativeRPCReceived)} with id {room_id}");
            room?.OnRPCReceived(new RpcEventArgs() 
            {
                RoomId = (ulong)room_id,
                Rpc = json,
                Userdata = user_data
            });
        }

#if UNITY_64 || __MonoCS__
        [AOT.MonoPInvokeCallback(typeof(NativeLibraryMethods.OdinOnSocketDelegate))]
#endif
        protected static void OnNativeSocketReceived(IntPtr socket_id, IntPtr bytesPtr, uint bytes_length, IntPtr user_data)
        {
            OdinLog.Assert(bytesPtr != IntPtr.Zero, $"{nameof(OdinClient)} socket {socket_id} {nameof(OnNativeSocketReceived)} socket pointer should not be zero");

            byte[] socketPayload = Utility.GetNativeBuffer(bytesPtr, bytes_length);

            if (Utility.IsOk(Odin.Library.Methods.SocketInfo(socket_id, out NativeBindings.RawOdinSocketInfo info)))
            {
                if(Utility.Test(info.RawRoomHandle != IntPtr.Zero, $"{nameof(OdinClient)} no valid room in {nameof(OnNativeSocketReceived)} for socket {socket_id}")) return;
                var room = _Rooms[(ulong)info.RawRoomHandle];
                if (room == null) return;

                var socket = room.Sockets.GetOrAdd((ulong)socket_id, (k) => new Socket.Socket(room));
                room.OnSocketReceived(new SocketMessageEventArgs()
                {
                    Socket = socket,
                    Room = room,
                    Payload = socketPayload,
                    Userdata = user_data
                });
            }
        }

        /// <summary>
        /// Internal library reload
        /// </summary>
        /// <remarks>Consider the state of the AppDomain</remarks>
        /// <param name="init">Indicates to initialize the library again after release</param>
        protected internal void ReloadLibrary(bool init = true)
        {
            if (Odin.Library.IsInitialized)
            {
                FreeRooms();
                Odin.Library.Release();
            }
            if (init) Odin.Library.Initialize();
         
            if (Odin.Crypto.IsInitialized)
                Odin.Crypto.Release();
        }

        /// <summary>
        /// Create an example access key that can be registered on 4Players (see <see href="https://developers.4players.io/odin/"/>)
        /// </summary>
        /// <remarks>This is for testing and not intended for production. Access key should never be client side.</remarks>
        /// <returns>access key string or empty</returns>
        public static string CreateAccessKey()
        {
            string result = string.Empty;
            if (Utility.IsOk(Odin.Library.Methods.TokenGeneratorCreate(null, out OdinTokenGeneratorHandle generatorHandle)))
            {
                Odin.Library.Methods.TokenGeneratorGetAccessKey(generatorHandle, out result);
                generatorHandle.Dispose();
            }
            return result;
        }

        /// <summary>
        /// Create an example token to join a room. For production use a token server!
        /// </summary>
        /// <remarks>This is for testing and not intended for production. Access key should never be client side.</remarks>
        /// <param name="accesskey">Key to generate a token from</param>
        /// <param name="body">token body</param>
        /// <returns>token string or empty</returns>
        public static string CreateToken(string accesskey, string body)
        {
            string result = string.Empty;
            if (Utility.IsOk(Odin.Library.Methods.TokenGeneratorCreate(accesskey, out OdinTokenGeneratorHandle generatorHandle)))
            {
                Odin.Library.Methods.TokenGeneratorSign(generatorHandle, body, out result);
                generatorHandle.Dispose();
            }
            return result;
        }

        /// <summary>
        /// Create an example token to join a room. For production use a token server!
        /// </summary>
        /// <remarks>This is for testing and not intended for production. Access key should never be client side.</remarks>
        /// <param name="body">token body</param>
        /// <param name="token">token string</param>
        /// <returns>true on success or false</returns>
        public bool GenerateToken(string body, out string token)
        {
            if (string.IsNullOrEmpty(body))
                OdinLog.Assert(message: new ArgumentNullException($"{nameof(OdinClient)} {nameof(GenerateToken)} body invalid").ToString());

            OdinLog.Assert(_tokenGeneratorHandle != null, $"{nameof(GenerateToken)} the {nameof(OdinTokenGeneratorHandle)} is null.");
            return Utility.IsOk(Odin.Library.Methods.TokenGeneratorSign(_tokenGeneratorHandle, body, out token));
        }

        /// <summary>
        /// Create a room for the set gateway.
        /// </summary>
        /// <param name="samplerate">room default samplerate fallback</param>
        /// <param name="stereo">room default stereo flag fallback</param>
        /// <returns>Room object with a connection</returns>
        public Room.Room CreateRoom(uint samplerate, bool stereo)
        {
            Room.Room room = new Room.Room(EndPoint.ToString(), samplerate, stereo);
            _Rooms.Add(room);

            return room;
        }

        /// <summary>
        /// Close a room by Id
        /// </summary>
        /// <param name="roomId">room id</param>
        /// <returns>true on success or false</returns>
        public bool CloseRoom(ulong roomId) => Rooms.Close(roomId);
        /// <summary>
        /// Close a room
        /// </summary>
        /// <param name="room">room to close</param>
        /// <returns>true on success or false</returns>
        public bool CloseRoom(Room.Room room) => Rooms.Close(room);
        /// <summary>
        /// Close and remove a room by Id
        /// </summary>
        /// <param name="roomId">room id</param>
        /// <returns>true on success or false</returns>
        public bool FreeRoom(ulong roomId) => Rooms.Free(roomId);
        /// <summary>
        /// Close and remove a room
        /// </summary>
        /// <param name="room">room</param>
        /// <returns>true on success or false</returns>
        public bool FreeRoom(Room.Room room) => Rooms.Free(room);

        /// <summary>
        /// Send a message to all rooms with the default encoding UTF8
        /// </summary>
        /// <param name="message">UTF8 string</param>
        public void BroadcastSendMessage(string message)
        {
            foreach (var room in Rooms.Where(r => r.IsJoined))
                room?.SendMessage(message);
        }

        /// <summary>
        /// Completely closes all <see cref="Room.Room"/> associated.
        /// </summary>
        public void CloseRooms()
        {
            if (Rooms == null) return;

            foreach(var room in Rooms)
                room?.Close();
        }

        /// <summary>
        /// Free all <see cref="Room.Room"/> associated.
        /// </summary>
        /// <remarks>Should only be called in Loading-Screens or Scene transitions</remarks>
        public void FreeRooms()
        {
            if (Rooms == null) return;

            try { Rooms.FreeAll(); }
            catch { /* nop */ }
        }

        private bool disposedValue;
        /// <summary>
        /// On dispose will free all <see cref="OdinNative.Wrapper.Room.Room"/> associated
        /// </summary>
        /// <param name="disposing">Indicates to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _Rooms?.FreeAll();
                    _tokenGeneratorHandle?.Dispose();
                    _tokenGeneratorHandle = null;
                    //ReloadLibrary(false);
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~OdinClient()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// On dispose will free all <see cref="OdinNative.Wrapper.Room.Room"/> associated
        /// </summary>
        /// <remarks>Override dispose if multiple <see cref="OdinClient"/> are needed</remarks>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
