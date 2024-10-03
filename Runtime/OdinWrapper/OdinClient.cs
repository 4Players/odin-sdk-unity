using OdinNative.Core;
using OdinNative.Core.Imports;
using OdinNative.Wrapper.Room;
using System;
using System.Linq;

namespace OdinNative.Wrapper
{
    /// <summary>
    /// Client Wrapper for ODIN ffi <c>OdinNative.Core.OdinLibrary.NativeMethods</c>
    /// </summary>
    public class OdinClient : MarshalByRefObject, IDisposable
    {
        private NativeBindings.OdinConnectionPoolSettings _connectionPoolSettings;
        private OdinConnectionPoolHandle _connectionPoolHandle;
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
        /// Connection EndPoint. Default from OdinEditorConfig.
        /// </summary>
        public Uri EndPoint { get; private set; }
        /// <summary>
        /// Client AccessKey for all new rooms. Default from OdinHandler config.
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
            client.Init(new NativeBindings.OdinConnectionPoolSettings()
            {
                OnDatagram = OdinClient.OnNativeDatagramReceived,
                OnRPC = OdinClient.OnNativeRPCReceived,
                user_data = client,
            });
            return client;
        }

        /// <summary>
        /// Creates a new initialized instance for ODIN ffi C# Wrapper 
        /// </summary>
        /// <remarks>Can fail on initialize!</remarks>
        /// <param name="server">Odin Server</param>
        /// <param name="accessKey">Odin access key</param>
        /// <param name="settings">Custom settings</param>
        /// <returns>OdinClient wrapper</returns>
        public static OdinClient Create(Uri server, string accessKey, NativeBindings.OdinConnectionPoolSettings settings)
        {
            OdinClient client = new OdinClient(server, accessKey);
            client.Init(settings);
            return client;
        }

        internal void Init(NativeBindings.OdinConnectionPoolSettings settings)
        {
            _connectionPoolSettings = settings;

            if (Odin.Library.IsInitialized == false)
                Odin.Library.Initialize();

            var connectionResult = Odin.Library.Methods.ConnectionPoolCreate(_connectionPoolSettings, out _connectionPoolHandle);
            Utility.Assert(Utility.IsOk(connectionResult), $"{nameof(OdinClient)} init {nameof(Odin.Library.Methods.ConnectionPoolCreate)} failed. {Utility.OdinErrorToString(connectionResult)} {Utility.OdinLastErrorString()}");


            if (string.IsNullOrEmpty(AccessKey))
                AccessKey = CreateAccessKey();

            var generatorResult = Odin.Library.Methods.TokenGeneratorCreate(AccessKey, out _tokenGeneratorHandle);
            Utility.Assert(Utility.IsOk(generatorResult), $"{nameof(OdinClient)} init {nameof(Odin.Library.Methods.TokenGeneratorCreate)} failed. {Utility.OdinErrorToString(generatorResult)} {Utility.OdinLastErrorString()}");
        }

        [AOT.MonoPInvokeCallback(typeof(NativeLibraryMethods.OdinConnectionPoolOnDatagramDelegate))]
        protected static void OnNativeDatagramReceived(ulong room_id, ushort media_id, IntPtr bytesPtr, uint bytes_length, MarshalByRefObject user_data)
        {
            Utility.Assert(bytesPtr != IntPtr.Zero, $"{nameof(OdinClient)} room {room_id} {nameof(OnNativeDatagramReceived)} datagram pointer should not be zero");
            Utility.Assert(bytes_length > 0, $"{nameof(OdinClient)} room {room_id} {nameof(OnNativeDatagramReceived)} datagram should not be empty");

            byte[] datagramPayload = Utility.GetNativeBuffer(bytesPtr, bytes_length);

            var room = _Rooms[room_id];
            if (room == null) return;
            room?.OnDatagramReceived(new DatagramEventArgs() {
                RoomId = room_id,
                Datagram = bytesPtr,
                Payload = datagramPayload,
                Userdata = user_data
            });
        }

        [AOT.MonoPInvokeCallback(typeof(NativeLibraryMethods.OdinConnectionPoolOnRPCDelegate))]
        protected static void OnNativeRPCReceived(ulong room_id, IntPtr bytesPtr, uint bytes_length, MarshalByRefObject user_data)
        {
            Utility.Assert(bytesPtr != IntPtr.Zero, $"{nameof(OdinClient)} room {room_id} {nameof(OnNativeRPCReceived)} rpc pointer should not be zero");
            Utility.Assert(bytes_length > 0, $"{nameof(OdinClient)} room {room_id} {nameof(OnNativeRPCReceived)} rpc data should not be empty");
            byte[] rpcPayload = Utility.GetNativeBuffer(bytesPtr, bytes_length);

            var room = _Rooms[room_id];
            Utility.Test(room != null, $"{nameof(OdinClient)} no room in {nameof(OnNativeRPCReceived)} with id {room_id}");
            room?.OnRPCReceived(new RpcEventArgs() 
            {
                RoomId = room_id,
                Rpc = rpcPayload,
                Userdata = user_data
            });
        }

        /// <summary>
        /// Internal library reload
        /// </summary>
        /// <remarks>Consider the state of the AppDomain</remarks>
        /// <param name="init">Idicates to initialize the library again after release</param>
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
        /// Create a example access key that can be registered on 4Players (see <see href="https://developers.4players.io/odin/"/>)
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
        /// Create a example token to join a room. For production use a token server!
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
        /// Create a example token to join a room. For production use a token server!
        /// </summary>
        /// <remarks>This is for testing and not intended for production. Access key should never be client side.</remarks>
        /// <param name="body">token body</param>
        /// <param name="token">token string</param>
        /// <returns>true on success or false</returns>
        public bool GenerateToken(string body, out string token)
        {
            if (string.IsNullOrEmpty(body))
                Utility.Assert(message: new ArgumentNullException($"{nameof(OdinClient)} {nameof(GenerateToken)} body invalid").ToString());

            Utility.Assert(_tokenGeneratorHandle != null, $"{nameof(GenerateToken)} the {nameof(OdinTokenGeneratorHandle)} is null.");
            return Utility.IsOk(Odin.Library.Methods.TokenGeneratorSign(_tokenGeneratorHandle, body, out token));
        }

        /// <summary>
        /// Create a room for the set gateway.
        /// </summary>
        /// <remarks>This is for testing and not intended for production. Access key should never be client side.</remarks>
        /// <param name="samplerate">room default samplerate fallback</param>
        /// <param name="stereo">room default stereo flag fallback</param>
        /// <returns>Room object with a connection</returns>
        public Room.Room CreateRoom(uint samplerate, bool stereo)
        {
            Room.Room room = new Room.Room(_connectionPoolHandle, EndPoint.ToString(), samplerate, stereo);
            _Rooms.Add(room);

            return room;
        }

        /// <summary>
        /// Create and join a <see cref="Room.Room"/>
        /// </summary>
        /// <param name="token">token</param>
        /// <param name="handleRoom">true will add the room to <see cref="OdinNative.Wrapper.Room.RoomCollection"/> of the client</param>
        /// <returns>room or null</returns>
        public Room.Room JoinRoom(string token, uint samplerate, bool stereo, bool handleRoom = true)
        {
            if(Room.Room.Join(_connectionPoolHandle, EndPoint.ToString(), token, samplerate, stereo, out Room.Room room))
                if(handleRoom) _Rooms.Add(room);

            return room;
        }

        /// <summary>
        /// Create and join a <see cref="Room.Room"/>
        /// </summary>
        /// <param name="room">Returns the room object that was created</param>
        /// <param name="token">The token generated using your access key</param>
        /// <param name="roomName">The room name</param>
        /// <param name="userData">The arbitrary user data encoded as a byte array</param>
        /// <param name="positionX">X position of the peer. Used to initialize the peer position</param>
        /// <param name="positionY">Y position of the peer. Used to initialize the peer position</param>
        /// <param name="positionZ">Z position of the peer. Used to initialize the peer position</param>
        /// <param name="samplerate">The default sample rate of this room</param>
        /// <param name="stereo">The default stereo flag of this room</param>
        /// <param name="cipher">The crypto interface</param>
        /// <param name="handleRoom">If true, the odin client will handle forwarding callbacks and freeing the room automatically when the client is destroyed.</param>
        /// <returns>True if join room was successful, false otherwise</returns>
        public bool JoinRoom(out Room.Room room, string token,
            string roomName, byte[] userData, float positionX, float positionY, float positionZ, uint samplerate,
            bool stereo, OdinCipherHandle cipher = null,
            bool handleRoom = true)
        {
            room = new Room.Room(_connectionPoolHandle, EndPoint.ToString(), samplerate, stereo);
            bool bJoined = false;
            
            if(handleRoom)
            {
                _Rooms.Add(room);
            }
            bJoined = room.Join(token, roomName, userData, positionX, positionY, positionZ, cipher);
            
            if (handleRoom && !bJoined)
                _Rooms.Free(room);
           
            return bJoined;
        }

        /// <summary>
        /// Create and join a <see cref="Room.Room"/>
        /// </summary>
        /// <param name="token">token</param>
        /// <param name="roomName">room alias</param>
        /// <param name="userData">initial userdata</param>
        /// <param name="positionX">server culling position X</param>
        /// <param name="positionY">server culling position Y</param>
        /// <param name="handleRoom">true will add the room to <see cref="OdinNative.Wrapper.Room.RoomCollection"/> of the client</param>
        /// <returns>room or null</returns>
        public Room.Room JoinRoom(string token, string roomName, byte[] userData, float positionX, float positionY, float positionZ, uint samplerate, bool stereo, bool handleRoom = true)
        {
            if (Room.Room.Join(_connectionPoolHandle, EndPoint.ToString(), token, roomName, userData, positionX, positionY, positionZ, samplerate, stereo, out Room.Room room))
                if(handleRoom) _Rooms.Add(room);

            return room;
        }

        /// <summary>
        /// Join a object of base <see cref="Room.Room"/>
        /// </summary>
        /// <param name="room">base type <see cref="Room.Room"/></param>
        /// <param name="token"></param>
        /// <remarks>If the join fails the room is freed and removed from the client</remarks>
        /// <returns></returns>
        public bool Join(Room.Room room, string token)
        {
            if(room == null) return false;

            var r = _Rooms[room.GetHashCode()];
            if (r == null)
            {
                if (room.Join(token))
                {
                    _Rooms.Add(room);
                    return true;
                }
                else
                    return false;
            }

            if (r.Join(token))
                return true;
            else
                _Rooms.Free(r);

            return false;
        }

        /// <summary>
        /// Join a object of base <see cref="Room.Room"/>
        /// </summary>
        /// <param name="room">base type <see cref="Room.Room"/></param>
        /// <param name="token">token</param>
        /// <param name="roomName">room alias</param>
        /// <param name="userData">initial userdata</param>
        /// <param name="positionX">server culling position X</param>
        /// <param name="positionY">server culling position Y</param>
        /// <remarks>If the join fails the room is freed and removed from the client</remarks>
        /// <returns>room or null</returns>
        public bool Join(Room.Room room, string token, string roomName, byte[] userData, float positionX, float positionY)
        {
            if (room == null) return false;

            var r = _Rooms[room.GetHashCode()];
            if (r == null)
            {
                if (room.Join(token, roomName, userData, positionX, positionY))
                {
                    _Rooms.Add(room);
                    return true;
                }
                else
                    return false;
            }

            if (r.Join(token, roomName, userData, positionX, positionY))
                return true;
            else
                _Rooms.Free(r);

            return false;
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
        /// <param name="roomId">room</param>
        /// <returns>true on success or false</returns>
        public bool FreeRoom(Room.Room room) => Rooms.Free(room);

        /// <summary>
        /// Send a message to all rooms with the default encoding UTF8
        /// </summary>
        /// <param name="message">UTF8 string</param>
        public void BroadcastSendMessage(string message)
        {
            foreach (var room in Rooms.Where(r => r.IsJoined))
                room?.SendMessage(Native.Encoding.GetBytes(message));
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
        /// <remarks>Should only be called in Loading-Screens or Scene transissions</remarks>
        public void FreeRooms()
        {
            if (Rooms == null) return;

            try { Rooms.FreeAll(); }
            catch { /* nop */ }
        }

        private bool disposedValue;
        /// <summary>
        /// On dispose will free all <see cref="OdinNative.Wrapper.Room.Room"/> and <see cref="OdinNative.Core.Imports.NativeMethods.Shutdown"/>
        /// </summary>
        /// <param name="disposing">Indicates to dispose the library</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _Rooms?.FreeAll();
                    _tokenGeneratorHandle?.Dispose();
                    _tokenGeneratorHandle = null;
                    _connectionPoolHandle?.Dispose();
                    _connectionPoolHandle = null;
                    //ReloadLibrary(false);
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Default deconstructor
        /// </summary>
        ~OdinClient()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// On dispose will free all <see cref="OdinNative.Wrapper.Room.Room"/> and <see cref="OdinNative.Core.Imports.NativeMethods.Shutdown"/>
        /// </summary>
        /// <remarks>Override dispose if muliple <see cref="OdinClient"/> are needed</remarks>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
