using OdinNative.Core;
using OdinNative.Core.Imports;
using System;
using System.Text;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Wrapper.Socket
{
    /// <summary>
    /// An ODIN socket, which handles the underlying native opaque socket handle.
    /// This abstraction provides a high-level interface for sending and receiving messages to and from a peer.
    /// </summary>
    public class Socket : ISocket, IDisposable
    {
        OdinSocketHandle ISocket.Handle => Handle;
        internal OdinSocketHandle Handle { get { return _handle; } private set { _handle = value; } }
        private OdinSocketHandle _handle;

        /// <summary>
        /// Socket id
        /// </summary>
        public ulong Id => IsClosed ? _Id : _Id = GetSocketId();
        private ulong _Id;

        public OdinSocketInfo Info { get; private set; } = new OdinSocketInfo() { RoomHandle = new OdinRoomHandle(IntPtr.Zero, false) };
        /// <summary>
        /// Socket label
        /// </summary>
        public int Label => Info.Label;
        /// <summary>
        /// Socket kind (reliable or unreliable)
        /// </summary>
        public OdinSocketKind Kind => Info.Kind;
        /// <summary>
        /// Id of the remote peer on the other end of this socket
        /// </summary>
        public uint RemotePeerId => Info.RemotePeerId;
        /// <summary>
        /// Socket ownership check
        /// </summary>
        public bool IsRemote => Info.IsInbound;
        /// <summary>
        /// IsClosed
        /// </summary>
        public bool IsClosed => disposedValue || _NativeClosed;
        /// <summary>
        /// True while the native socket handle can still be used, e.g. to send
        /// </summary>
        public bool IsAlive => !IsClosed && (_handle?.IsAlive ?? false);
        private bool _NativeClosed = false;

        /// <summary>
        /// Default value <c>null</c> indicates root or not set
        /// </summary>
        public IPeer Parent { get; set; }
        public IRoom Room { get; set; }

        #region Events
        /// <summary>
        /// Forward on socket data
        /// </summary>
        public virtual void OnSocket(IRoom room, SocketMessageEventArgs args)
        {
            this.OnMessageReceived.Invoke(this, room, args.Payload);
        }
  
        /// <summary>
        /// Odin socket received message
        /// </summary>
        public event OnSocketMessageReceivedDelegate OnMessageReceived;
        #endregion

        /// <summary>
        /// Initialise dangling socket
        /// </summary>
        /// <remarks>For creating an independent socket use <see cref="Socket.Create"/></remarks>
        /// <param name="room">room</param>
        public Socket(IRoom room)
        {
            _handle = new OdinSocketHandle(IntPtr.Zero, false);
            Room = room;
            SubscribeEvents();
        }

        /// <summary>
        /// Initialise and opens a socket
        /// </summary>
        /// <param name="room">Room to open the socket in</param>
        /// <param name="kind">Type of socket to open</param>
        /// <param name="targetPeerId">remote peer id</param>
        /// <param name="label">socket name</param>
        /// <param name="priority">socket priority</param>
        /// <returns>Socket object</returns>
        public static Socket Create(IRoom room, OdinSocketKind kind, uint targetPeerId, int label, int priority)
        {
            Socket socket = new Socket(room);
            if (socket.Open(kind, targetPeerId, label, priority) == false)
            {
                // an unopened socket must not escape, callers would register it under id 0
                socket.Dispose();
                return null;
            }
            // pull the native info right away so Kind, Label and RemotePeerId are valid for callers
            socket.GetSocketInfo();
            return socket;
        }

        /// <summary>
        /// Wraps a native socket handle that was opened by a remote peer (inbound socket)
        /// </summary>
        /// <remarks>The native room owns the socket, the handle is never reset or freed by this wrapper</remarks>
        /// <param name="room">Room the socket belongs to</param>
        /// <param name="socketHandle">native socket pointer received in the socket callback</param>
        /// <returns>Socket object with populated <see cref="Info"/></returns>
        internal static Socket FromNative(IRoom room, IntPtr socketHandle)
        {
            Socket socket = new Socket(room);
            socket._handle = new OdinSocketHandle(socketHandle, false);

            if (Utility.IsOk(Odin.Library.Methods.SocketInfo(socketHandle, out RawOdinSocketInfo raw)))
            {
                socket.Info = new OdinSocketInfo()
                {
                    RoomHandle = new OdinRoomHandle(raw.RawRoomHandle, false),
                    Kind = raw.Kind,
                    IsInbound = raw.IsInbound,
                    RemotePeerId = raw.RemotePeerId,
                    Label = raw.Label,
                    Priority = raw.Priority,
                    UnsentBytes = raw.UnsentBytes,
                };
            }
            return socket;
        }

        /// <summary>
        /// This will always return itself
        /// </summary>
        public T GetBaseSocket<T>() where T : ISocket
        {
            return (T)GetBaseSocket();
        }
        private ISocket GetBaseSocket()
        {
            return this;
        }

        /// <summary>
        /// Refresh the native socket handle and pull socket info
        /// </summary>
        /// <param name="peerId">default (-1) uses the last remote peer id</param>
        /// <returns>true on new handle</returns>
        public bool RefreshHandle(int peerId = -1)
        {
            OdinLog.Assert(Room != null, $"{nameof(ISocket)} socket {_handle} internal {nameof(Socket)} {nameof(RefreshHandle)} room parent should not be null");

            if (Open(this.Info.Kind, (peerId < 0 ? this.Info.RemotePeerId : (uint)peerId), this.Info.Label, this.Info.Priority))
            {
                GetSocketInfo();
                return true;
            }

            return false;
        }

        private void SubscribeEvents()
        {
            OnMessageReceived += Socket_OnMessageReceived;
        }

        public virtual void Socket_OnMessageReceived(object sender, IRoom room, byte[] message)
        {
            OdinLog.Assert(message != null, $"{nameof(ISocket)} socket {_handle} internal {nameof(Socket)} {nameof(Socket_OnMessageReceived)} message is null");
        }

        public bool Open(OdinSocketKind kind, uint targetPeerId, int label, int priority)
        {
            OdinLog.Assert(Room != null, $"{nameof(ISocket)} socket {_handle} internal {nameof(Socket)} {nameof(Open)} room parent should not be null");
            OdinLog.Assert(targetPeerId >= 0, $"{nameof(OdinClient)} socket {_handle} {nameof(Open)} target remote peer id should not be negative");
            if (Room == null || Room.Handle == IntPtr.Zero) return false;

            return Utility.IsOk(Odin.Library.Methods.SocketCreate(Room.Handle, kind, targetPeerId, label, priority, out _handle));
        }

        /// <summary>
        /// Retrieves the socket id
        /// </summary>
        /// <remarks>handle as id representation</remarks>
        /// <returns>socket id</returns>
        public ulong GetSocketId()
        {
            OdinLog.Assert(Handle?.IsAlive == true, $"{nameof(GetSocketId)} {nameof(OdinSocketHandle)} is released");
            return (ulong)(IntPtr)Handle;
        }

        public OdinSocketInfo GetSocketInfo()
        {
            OdinLog.Assert(Handle?.IsAlive == true, $"{nameof(GetSocketInfo)} {nameof(OdinSocketHandle)} is released");
            var info = this.Info;
            var result = Odin.Library.Methods.SocketInfo(Handle, ref info);
            this.Info = info;

            if (Utility.IsOk(result) == false)
                OdinLog.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.SocketInfo)} in {nameof(Socket.GetSocketInfo)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());

            return info;
        }

        /// <summary>
        /// Sends an utf8 string message to the peer
        /// </summary>
        /// <param name="message">text message</param>
        /// <returns>OdinError</returns>
        public OdinError Send(string message) => Send(Encoding.UTF8.GetBytes(message));
        /// <summary>
        /// Sends an arbitrary message to the peer
        /// </summary>
        /// <param name="message">arbitrary message</param>
        /// <returns>OdinError</returns>
        public OdinError Send(byte[] message)
        {
            OdinLog.Assert(Handle?.IsAlive == true, $"{nameof(Odin.Library.Methods.SocketSend)} {nameof(OdinSocketHandle)} is released");

            OdinError result = Odin.Library.Methods.SocketSend(_handle, message);
            if (Utility.IsOk(result) == false)
                OdinLog.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.SocketSend)} in {nameof(Socket.Send)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());

            return result;
        }

        /// <summary>
        /// Sends a slice of a buffer to the peer without an intermediate copy
        /// </summary>
        /// <param name="message">buffer slice to send</param>
        /// <returns>OdinError</returns>
        public OdinError Send(ArraySegment<byte> message)
        {
            OdinLog.Assert(Handle?.IsAlive == true, $"{nameof(Odin.Library.Methods.SocketSend)} {nameof(OdinSocketHandle)} is released");

            OdinError result = Odin.Library.Methods.SocketSend(_handle, message.Array, message.Offset, message.Count);
            if (Utility.IsOk(result) == false)
                OdinLog.Assert(message: new OdinException(result, $"{nameof(Odin.Library.Methods.SocketSend)} in {nameof(Socket.Send)} failed (handle {Handle.IsAlive}): {Utility.OdinLastErrorString()} (code {result})").ToString());

            return result;
        }

        /// <summary>
        /// Close the native socket. (native dispose)
        /// Therefore local socket can not send.
        /// </summary>
        public void Reset()
        {
            OdinLog.Assert(Handle.IsAlive, $"{nameof(Odin.Library.Methods.SocketReset)} {nameof(OdinSocketHandle)} is released");

            Odin.Library.Methods.SocketReset(Handle);
        }

        private void UnsubscribeEvents()
        {
            OnMessageReceived -= Socket_OnMessageReceived;
        }

        private bool disposedValue;
        /// <summary>
        /// On dispose will close the socket
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    UnsubscribeEvents();
                    Handle?.Dispose();
                    Handle = null;
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~Socket()
        {
            Dispose(disposing: false);
        }

        /// <summary>
        /// On dispose will free the socket and all associated data
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
