using OdinNative.Core.Imports;
using OdinNative.Unity.Events;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Room;
using OdinNative.Wrapper.Socket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity
{
    /// <summary>
    /// Wrapper class of <see cref="OdinNative.Wrapper.Socket.Socket"/> for Unity
    /// <para>
    /// This convenient class provides dispatching of events to Unity with passthrough <see cref="UnityEngine.Events.UnityEvent"/> 
    /// as well as predefined helper functions to cover default use cases.
    /// </para>
    /// </summary>
    /// <remarks>Create a custom component with <see cref="OdinNative.Wrapper.ISocket"/> or inheritance from this class and extend/override.</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity/OdinSocket/")]
    [AddComponentMenu("Odin/Instance/OdinSocket")]
    public class OdinSocket : MonoBehaviour, ISocket
    {
        /// <summary>
        /// Native socket id, 0 until the socket is registered in the room
        /// </summary>
        public ulong Id => _Socket?.Id ?? 0;
        public IPeer Parent { get; set; }
        public IRoom Room { get; set; }

        public OdinRoom RoomLink;
        public int PeerLink;
        public OdinSocketKind Kind;
        public int Label;
        public int Priority;

        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Socket.Socket.OnMessageReceived"/> redirected as Unity event
        /// </summary>
        public SocketMessageReceivedProxy OnMessageReceived;

        public OdinSocketHandle Handle => _Socket?.Handle;

        public bool IsRemote => _Socket?.IsRemote ?? false;

        private Socket _Socket;

        event OnSocketMessageReceivedDelegate ISocket.OnMessageReceived
        {
            add
            {
                if (_Socket == null) return;
                _Socket.OnMessageReceived += value;
            }

            remove
            {
                if (_Socket == null) return;
                _Socket.OnMessageReceived -= value;
            }
        }

        public OdinNative.Wrapper.Room.Room GetRoomApi() =>
            this.Room as OdinNative.Wrapper.Room.Room
            ?? (this.Room as OdinRoom)?.GetBaseRoom<OdinNative.Wrapper.Room.Room>();

        public T GetBaseSocket<T>() where T : ISocket => _Socket != null ? _Socket.GetBaseSocket<T>() : default;
        public void OnSocket(IRoom room, SocketMessageEventArgs args) => _Socket?.OnSocket(room, args);

        private ConcurrentQueue<KeyValuePair<object, EventArgs>> EventQueue;

        void Awake()
        {
            EventQueue = new ConcurrentQueue<KeyValuePair<object, EventArgs>>();

            if (OnMessageReceived == null)
                OnMessageReceived = new SocketMessageReceivedProxy();
        }

        /// <summary>
        /// Default handler that logs received socket messages
        /// </summary>
        /// <param name="sender">OdinSocket object</param>
        /// <param name="args">socket message data</param>
        public virtual void Socket_MessageReceived(object sender, SocketMessageEventArgs args)
        {
            if (isActiveAndEnabled)
            {
                OdinLog.LogDebug($"{gameObject.name} socket \"{args.Socket?.Id}\" room: {args.Room.Id}");
            }
        }

        void OnEnable()
        {
#if UNITY_WEBGL
#pragma warning disable CS0618 // Type or member is obsolete
            OdinNative.Core.Utility.Throw(new System.NotSupportedException("Unity device socket is currently not supported in WebGL"));
#pragma warning restore CS0618 // Type or member is obsolete
            this.enabled = false;
            return;
#pragma warning disable CS0162 // Unreachable code detected
#endif

            if (EventQueue == null) EventQueue = new ConcurrentQueue<KeyValuePair<object, EventArgs>>();
            EventQueue.Clear();

            // socket creation happens in Update: OnEnable runs synchronously during
            // AddComponent, i.e. before AddSocketComponent could configure kind/label
        }

        private void TryCreateSocket()
        {
            // resolve links late so a component added and configured via AddSocketComponent
            // or enabled before the room joined can catch up here
            if (Room == null)
                Room = RoomLink != null ? (IRoom)RoomLink : GetComponent<OdinRoom>();
            if (Parent == null)
                Parent = GetComponent<OdinPeer>();
            if (RoomLink == null && Room is OdinRoom odinRoom)
                RoomLink = odinRoom;
            if (Parent != null)
                PeerLink = (int)Parent.Id;

            var roomApi = GetRoomApi();
            if (roomApi == null || roomApi.IsJoined == false || Parent == null) return;

            // AddSocket keys the socket by its native id, which is also what the
            // room's native message callback uses for its lookup
            _Socket = roomApi.AddSocket(Kind, Parent.Id, Label, Priority);
            if (_Socket != null)
                _Socket.OnMessageReceived += _Socket_OnMessageReceived;
        }

        public virtual void _Socket_OnMessageReceived(object sender, IRoom room, byte[] message)
        {
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(
                this,
                new SocketMessageEventArgs()
                {
                    Socket = sender as Socket,
                    Room = room,
                    Payload = message
                }));
        }

        void Reset()
        {
            if(OnMessageReceived == null)
                OnMessageReceived = new SocketMessageReceivedProxy();

            if (RoomLink != null && Room == null)
                Room = RoomLink;
            if (RoomLink == null && Room is OdinRoom)
                RoomLink = (OdinRoom)Room;
            if (Parent != null)
                PeerLink = (int)Parent.Id;
            else PeerLink = -1;

#if UNITY_EDITOR
            if (isActiveAndEnabled)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(OnMessageReceived, Socket_MessageReceived);
            }
#endif
        }

        void Update()
        {
            // a recreated room (disable/enable cycle) leaves the socket on the old
            // wrapper room; tear it down so it gets recreated on the current one.
            // no native close here: the old room is disposed and took its sockets with it
            if (_Socket != null && ReferenceEquals(_Socket.Room, GetRoomApi()) == false)
            {
                _Socket.OnMessageReceived -= _Socket_OnMessageReceived;
                _Socket.Dispose();
                _Socket = null;
            }

            if (_Socket == null)
            {
                // the component may be enabled before the room joined, retry until then
                TryCreateSocket();
                if (_Socket == null) return;
            }

            if (EventQueue == null) return;
            while (EventQueue.TryDequeue(out KeyValuePair<object, System.EventArgs> uEvent))
            {
                if (uEvent.Value is SocketMessageEventArgs)
                    OnMessageReceived?.Invoke(this, uEvent.Value as SocketMessageEventArgs);
                else
                    OdinLog.LogError($"{nameof(OdinSocket)} Call to invoke unknown event skipped: {uEvent.Value.GetType()} from {nameof(uEvent.Key)} ({uEvent.Key.GetType()})");
            }
        }

        void OnDisable()
        {
            if (_Socket != null)
            {
                // full teardown so the next OnEnable registers a fresh socket;
                // close (reset) the native socket so it stops delivering callbacks
                _Socket.OnMessageReceived -= _Socket_OnMessageReceived;
                GetRoomApi()?.RemoveSocket(_Socket.Id, close: true);
                _Socket.Dispose();
                _Socket = null;
            }
        }

        void OnDestroy()
        {
            OnMessageReceived.RemoveAllListeners();

            _Socket?.Dispose();
            _Socket = null;
        }
    }
}