using OdinNative.Core;
using OdinNative.Core.Imports;
using OdinNative.Unity.Events;
using OdinNative.Wrapper.Room;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity
{
    /// <summary>
    /// Wrapper class of Odin connections
    /// <para>
    /// This convenient class provides dispatching of events to Unity with passthrough <see cref="UnityEngine.Events.UnityEvent"/> 
    /// to support multiple rooms on fewer connections. 
    /// </para>
    /// </summary>
    /// <remarks>The Odin room usually supports its own connection handling by default. May only be used with custom Odin room components. Do not confuse with multi-room tokens.</remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odinconnection/")]
    [DefaultExecutionOrder(-100)]
    public class OdinConnection : MonoBehaviour
    {
        /// <summary>
        /// Native connection settings
        /// </summary>
        public OdinConnectionPoolSettings Settings;

        /// <summary>
        /// Odin connection
        /// </summary>
        public OdinConnectionPoolHandle Handle { get => _connectionPoolHandle; }
        private OdinConnectionPoolHandle _connectionPoolHandle;

        private ConcurrentQueue<KeyValuePair<object, EventArgs>> EventQueue;
        /// <summary>
        /// Redirect of <see cref="OdinNative.Wrapper.Room.Room.OnDatagram"/>
        /// </summary>
        public DatagramProxy OnDatagram;
        /// <summary>
        /// Redirect of <see cref="OdinNative.Wrapper.Room.Room.OnRpc"/>
        /// </summary>
        public RpcProxy OnRpc;
        /// <summary>
        /// Custom connection userdata 
        /// </summary>
        public MarshalByRefObject EventUserdata { get; private set; } = null;
        private void Connection_Datagram(object sender, DatagramEventArgs args) => OnDatagram?.Invoke(sender, args);
        private void Connection_RPC(object sender, RpcEventArgs args) => OnRpc?.Invoke(sender, args);

        private bool _IsConsumed;

        void Awake()
        {
            EventQueue = new ConcurrentQueue<KeyValuePair<object, EventArgs>>();
        }

        void OnEnable()
        {
#if UNITY_WEBGL
#pragma warning disable CS0618 // Type or member is obsolete
            Utility.Throw(new NotSupportedException("Raw connections in WebGL are not supported"));
#pragma warning restore CS0618 // Type or member is obsolete
            this.enabled = false;
            return;
#pragma warning disable CS0162 // Unreachable code detected
#endif
            Settings = new OdinConnectionPoolSettings()
            {
                OnDatagram = OnNativeDatagramReceived,
                OnRPC = OnNativeRPCReceived,
                user_data = EventUserdata
            };

            if (_connectionPoolHandle != null) return;

            if (Odin.Library.IsInitialized == false)
                Odin.Library.Initialize();

            var connectionResult = Odin.Library.Methods.ConnectionPoolCreate(Settings, out _connectionPoolHandle);
            Utility.Assert(Utility.IsOk(connectionResult), $"{nameof(OdinConnection)} init {nameof(Odin.Library.Methods.ConnectionPoolCreate)} failed. {Utility.OdinErrorToString(connectionResult)}");
        }

        protected virtual void OnNativeDatagramReceived(ulong room_id, ushort media_id, IntPtr bytesPtr, uint bytes_length, MarshalByRefObject user_data)
        {
            Utility.Assert(EventQueue != null, $"{nameof(OdinConnection)} room {room_id} {nameof(OnNativeDatagramReceived)} connection released and got consumed: {_IsConsumed}");
            if (_IsConsumed) return;

            Utility.Assert(bytesPtr != IntPtr.Zero, $"{nameof(OdinConnection)} room {room_id} {nameof(OnNativeDatagramReceived)} datagram pointer should not be zero");
            Utility.Assert(bytes_length > 0, $"{nameof(OdinConnection)} room {room_id} {nameof(OnNativeDatagramReceived)} datagram should not be empty");

            byte[] datagramPayload = Utility.GetNativeBuffer(bytesPtr, bytes_length);

            EventQueue.Enqueue(new KeyValuePair<object, EventArgs>(this, new DatagramEventArgs()
            {
                RoomId = room_id,
                Datagram = bytesPtr,
                Payload = datagramPayload,
                Userdata = user_data
            }));
        }

        protected virtual void OnNativeRPCReceived(ulong room_id, IntPtr bytesPtr, uint bytes_length, MarshalByRefObject user_data)
        {
            Utility.Assert(EventQueue != null, $"{nameof(OdinConnection)} room {room_id} {nameof(OnNativeRPCReceived)} connection released and got consumed: {_IsConsumed}");
            if (_IsConsumed) return;

            Utility.Assert(bytesPtr != IntPtr.Zero, $"{nameof(OdinConnection)} room {room_id} {nameof(OnNativeRPCReceived)} rpc pointer should not be zero");
            Utility.Assert(bytes_length > 0, $"{nameof(OdinConnection)} room {room_id} {nameof(OnNativeRPCReceived)} rpc data should not be empty");

            byte[] rpcPayload = Utility.GetNativeBuffer(bytesPtr, bytes_length);

            EventQueue.Enqueue(new KeyValuePair<object, EventArgs>(this, new RpcEventArgs()
            {
                RoomId = room_id,
                Rpc = rpcPayload,
                Userdata = user_data
            }));
        }

        void Reset()
        {
            OnDatagram = new DatagramProxy();
            OnRpc = new RpcProxy();

            OnDatagram.AddListener(Connection_Datagram);
            OnRpc.AddListener(Connection_RPC);
        }

        void Update()
        {
            if (EventQueue == null) return;
            while (EventQueue.TryDequeue(out KeyValuePair<object, System.EventArgs> uEvent))
            {
                if (uEvent.Value is DatagramEventArgs)
                    OnDatagram?.Invoke(uEvent.Key, uEvent.Value as DatagramEventArgs);
                else if (uEvent.Value is RpcEventArgs)
                    OnRpc?.Invoke(uEvent.Key, uEvent.Value as RpcEventArgs);
                else
                    Debug.LogError($"Call to invoke unknown event skipped: {uEvent.Value.GetType()} from {nameof(uEvent.Key)} ({uEvent.Key.GetType()})");
            }
        }

        private void Free()
        {
            _IsConsumed = true;

            if (_connectionPoolHandle?.IsAlive == true)
            {
                _connectionPoolHandle?.Dispose();
                _connectionPoolHandle = null;
            }
            EventQueue.Clear();

            this.enabled = false;
        }

        void OnDisable()
        {
            Free();
        }

        void OnDestroy()
        {
            Free();
        }
    }
}
