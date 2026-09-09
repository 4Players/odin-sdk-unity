using OdinNative.Core.Imports;
using OdinNative.Unity.Events;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using OdinNative.Wrapper.Peer.Rpc;
using OdinNative.Wrapper.Room;
using OdinNative.Wrapper.Room.Rpc;
using OdinNative.Wrapper.Socket;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace OdinNative.Unity
{
    /// <summary>
    /// Wrapper class of <see cref="OdinNative.Wrapper.Room.Room"/> for Unity.
    /// <para>
    /// This convenient class provides dispatching of events to Unity with passthrough <see cref="UnityEngine.Events.UnityEvent"/>
    /// as well as predefined helper functions to cover default use cases where the voice chat is visually and logically represented by
    /// Unity gameobjects that are manageable with the Unity editor like Context menu, Inspector and/or Hierarchy window.
    /// </para>
    /// Default Unity GameObject altering event callback functions:
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="PeerJoinedCreateComponent"/></term>
    /// <description>Creates GameObject with <see cref="OdinPeer"/> component</description>
    /// </item>
    /// <item>
    /// <term><see cref="PeerLeftRemoveComponent"/></term>
    /// <description>Destroy GameObject with <see cref="OdinPeer"/> component</description>
    /// </item>
    /// <item>
    /// <term><see cref="RoomStatusState"/></term>
    /// <description>Destroy this components GameObject if the connection is closed i.e cleanup</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>Create a custom component with <see cref="OdinNative.Wrapper.IRoom"/> or inheritance from this class and extend/override.</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity/OdinRoom/")]
    [AddComponentMenu("Odin/Instance/OdinRoom")]
    //[RequireComponent(typeof(OdinConnection))]
    [DefaultExecutionOrder(-99)]
    public class OdinRoom : MonoBehaviour, IRoom
    {
        /// <summary>
        /// Odin endpoint server
        /// </summary>
        public string Gateway;
        /// <summary>
        /// Odin room token
        /// </summary>
        public string Token;
        /// <summary>
        /// Unity mixer
        /// </summary>
        public AudioMixerGroup AudioMixerGroup;
        /// <summary>
        /// Unity samplerate
        /// </summary>
        public uint Samplerate { get; set; }

        /// <summary>
        /// Fallback samplerate for setups that run without the Unity audio engine.
        /// </summary>
        public const uint DefaultSampleRate = 48000;

        /// <summary>
        /// Playback samplerate reported by Unity, or <see cref="DefaultSampleRate"/> when the
        /// Unity audio engine is disabled.
        /// </summary>
        /// <remarks>
        /// Unity reports 0 while the audio engine is disabled, which projects driving FMOD or Wwise
        /// commonly do. A zero reaches the native pipeline through encoder and decoder creation.
        /// </remarks>
        public static uint OutputSampleRate
        {
            get
            {
                int rate = AudioSettings.outputSampleRate;
                return rate > 0 ? (uint)rate : DefaultSampleRate;
            }
        }
        /// <summary>
        /// Unity channel flag
        /// </summary>
        /// <remarks>We let Unity resample/mix audio on demand and use only mono by default internally</remarks>
        public bool IsStereo { get; set; }

        /// <summary>
        /// Prefab instantiated per joining peer. Falls back to a blank GameObject when null.
        /// </summary>
        public GameObject PeerPrefab;

        /// <summary>
        /// Odin room id
        /// </summary>
        public ulong Id => _Room.Id;
        /// <summary>
        /// Odin Crypto cipher component
        /// </summary>
        /// <remarks>It is important for multiple encrypted rooms to provide each OdinRoom its own OdinCrypto instance!</remarks>
        [Tooltip("Odin room encryption. Provide each OdinRoom its own OdinCrypto instance!")]
        public OdinCrypto CryptoComponent;
        public Crypto CryptoCipher => CryptoComponent?.Crypto ?? _Room?.CryptoCipher;

        /// <summary>
        /// Automatically create default medias: an input encoder when audio is pushed via <see cref="ProxyAudio"/>
        /// and a decoder when audio arrives from a peer that has no decoder with a matching listen channel mask.
        /// </summary>
        /// <remarks>Disable to manage medias manually, e.g. one decoder per channel mask via <see cref="OdinPeer.AddDecoderComponent(GameObject, ulong, bool)"/> to separate proximity and team chat and <see cref="LinkInputMedia"/> for capture.</remarks>
        [Tooltip("Automatically create a default input encoder on pushed audio and a default decoder when audio arrives from a peer without a matching decoder. Disable to manage medias manually, e.g. one decoder per channel mask.")]
        public bool AutoCreateMedia = true;
        /// <summary>
        /// Media id used for default medias created by <see cref="AutoCreateMedia"/>
        /// </summary>
        public const ulong AutoDecoderMediaId = ulong.MaxValue;

        /// <summary>
        /// Odin connection status
        /// </summary>
        event OnRoomStatusChangedDelegate IRoom.OnRoomStatusChanged
        {
            add
            {
                if (_Room == null) return;
                _Room.OnRoomStatusChanged += value;
            }

            remove
            {
                if (_Room == null) return;
                _Room.OnRoomStatusChanged -= value;
            }
        }
        /// <summary>
        /// Odin room joined
        /// </summary>
        event OnRoomJoinedDelegate IRoom.OnRoomJoined
        {
            add
            {
                if (_Room == null) return;
                _Room.OnRoomJoined += value;
            }

            remove
            {
                if (_Room == null) return;
                _Room.OnRoomJoined -= value;
            }
        }
        /// <summary>
        /// Odin room left
        /// </summary>
        event OnRoomLeftDelegate IRoom.OnRoomLeft
        {
            add
            {
                if (_Room == null) return;
                _Room.OnRoomLeft += value;
            }

            remove
            {
                if (_Room == null) return;
                _Room.OnRoomLeft -= value;
            }
        }
        /// <summary>
        /// Odin peer joined
        /// </summary>
        event OnPeerJoinedDelegate IRoom.OnPeerJoined
        {
            add
            {
                if (_Room == null) return;
                _Room.OnPeerJoined += value;
            }

            remove
            {
                if (_Room == null) return;
                _Room.OnPeerJoined -= value;
            }
        }
        /// <summary>
        /// Odin peer left
        /// </summary>
        event OnPeerLeftDelegate IRoom.OnPeerLeft
        {
            add
            {
                if (_Room == null) return;
                _Room.OnPeerLeft += value;
            }

            remove
            {
                if (_Room == null) return;
                _Room.OnPeerLeft -= value;
            }
        }
        /// <summary>
        /// Odin peer changed userdata
        /// </summary>
        event OnPeerChangedDelegate IRoom.OnPeerChanged
        {
            add
            {
                if (_Room == null) return;
                _Room.OnPeerChanged += value;
            }

            remove
            {
                if (_Room == null) return;
                _Room.OnPeerChanged -= value;
            }
        }
        /// <summary>
        /// Odin room received message
        /// </summary>
        event OnMessageReceivedDelegate IRoom.OnMessageReceived
        {
            add
            {
                if (_Room == null) return;
                _Room.OnMessageReceived += value;
            }

            remove
            {
                if (_Room == null) return;
                _Room.OnMessageReceived -= value;
            }
        }

        /// <summary>
        /// Internal event fired when the native SDK creates a decoder for a peer
        /// </summary>
        internal event OnDecoderCreatedDelegate OnNativeDecoderCreated;
        /// <summary>
        /// Internal event fired when the native SDK removes a decoder for a peer
        /// </summary>
        internal event OnDecoderRemovedDelegate OnNativeDecoderRemoved;

        /// <summary>
        /// Odin base room
        /// </summary>
        /// <returns>wrapper room object</returns>
        public T GetBaseRoom<T>() where T : IRoom => (T)(_Room as IRoom);
        private IRoom _Room;
        OdinRoomHandle IRoom.Handle => _Room?.Handle;

        /// <summary>
        /// Default value gameObject parent or Unity root
        /// </summary>
        public object Parent => this.gameObject.transform.parent;

        public bool IsJoined => _Room?.IsJoined ?? false;
        /// <summary>
        /// The local peer for this client in this room. Null until the room is joined.
        /// </summary>
        public IPeer Self => _Room?.Self;

        bool IRoom.Stereo => IsStereo;
        string IRoom.Name => (_Room as Room)?.Name ?? string.Empty;

        /// <summary>
        /// Push audio samples to the room via a specific encoder.
        /// </summary>
        public virtual bool SendAudio(float[] samples, MediaEncoder encoder)
            => (_Room as Room)?.SendAudio(samples, encoder) ?? false;
        /// <summary>
        /// Push audio samples to the room via a specific encoder with a silence flag.
        /// </summary>
        public virtual bool SendAudio(float[] samples, MediaEncoder encoder, bool isSilent)
            => (_Room as Room)?.SendAudio(samples, encoder, isSilent) ?? false;

        bool IRoom.GetOrCreateDecoder(uint peerId, ulong mediaId, uint samplerate, bool stereo, out MediaDecoder decoder)
        {
            var r = _Room as Room;
            if (r != null) return r.GetOrCreateDecoder(peerId, mediaId, samplerate, stereo, out decoder);
            decoder = null;
            return false;
        }

        bool IRoom.RemoveDecoder(uint peerId, ulong mediaId, out MediaDecoder decoder)
        {
            var r = _Room as Room;
            if (r != null) return r.RemoveDecoder(peerId, mediaId, out decoder);
            decoder = null;
            return false;
        }

        private ConcurrentQueue<KeyValuePair<object, EventArgs>> EventQueue;
        private readonly Dictionary<uint, List<OdinDecoder>> _decoderRegistry = new Dictionary<uint, List<OdinDecoder>>();

        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Room.Room.OnRoomJoined"/> redirected as Unity event
        /// </summary>
        [Header("Events")]
        public RoomJoinedProxy OnRoomJoined;
        /// <summary>
        /// Event raised when a decoder is added for a peer, redirected as Unity event
        /// </summary>
        [FormerlySerializedAs("OnMediaAdded")]
        public DecoderAddedProxy OnDecoderAdded;
        /// <summary>
        /// Event raised when a decoder is removed for a peer, redirected as Unity event
        /// </summary>
        [FormerlySerializedAs("OnMediaRemoved")]
        public DecoderRemovedProxy OnDecoderRemoved;
        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Room.Room.OnPeerJoined"/> redirected as Unity event
        /// </summary>
        public PeerJoinedProxy OnPeerJoined;
        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Room.Room.OnPeerLeft"/> redirected as Unity event
        /// </summary>
        public PeerLeftProxy OnPeerLeft;
        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Room.Room.OnMessageReceived"/> redirected as Unity event
        /// </summary>
        public MessageReceivedProxy OnMessageReceived;
        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Room.Room.OnRoomStatusChanged"/> redirected as Unity event
        /// </summary>
        [FormerlySerializedAs("OnConnectionStateChanged")]
        public RoomStateChangedProxy OnRoomStateChanged;

        private bool IsConsumed = false;

        private void Init()
        {
            OnRoomJoined = new RoomJoinedProxy();
            OnDecoderAdded = new DecoderAddedProxy();
            OnDecoderRemoved = new DecoderRemovedProxy();
            OnPeerJoined = new PeerJoinedProxy();
            OnPeerLeft = new PeerLeftProxy();
            OnMessageReceived = new MessageReceivedProxy();
            OnRoomStateChanged = new RoomStateChangedProxy();
        }

        void Awake()
        {
            Samplerate = OutputSampleRate;
            // we use Mono for convenience setup and less samples to init encoders/decoders
            // even without a check to 'AudioSettings.speakerMode >= AudioSpeakerMode.Stereo;'
            // Unity will resample and/or upmix, downmix on AudioClip<->AudioSource
            // (on true with custom virtual channels override SetAudioClipData in OdinDecoder)
            IsStereo = false;

            EventQueue = new ConcurrentQueue<KeyValuePair<object, EventArgs>>();
        }

        void OnEnable()
        {
            Room room = new Room(Gateway, Samplerate, IsStereo);
            _Room = room;

            room.Parent = this;

            if (EventQueue == null) EventQueue = new ConcurrentQueue<KeyValuePair<object, EventArgs>>();
            EventQueue.Clear();

            room.OnRoomStatusChanged += Room_OnConnectionStatusChanged;
            room.OnRoomJoined += Room_OnRoomJoined;
            room.OnPeerJoined += Room_OnPeerJoined;
            room.OnPeerLeft += Room_OnPeerLeft;
            room.OnMessageReceived += Room_OnMessageReceived;
            room.OnDatagram += Room_OnAutoCreateDecoder;

            OnNativeDecoderCreated += ExtraOnNativeDecoderCreated;
            OnNativeDecoderRemoved += ExtraOnNativeDecoderRemoved;

            // joining is deferred to Update() so the cipher of an assigned CryptoComponent is always used
            IsConsumed = false;
        }

        void Reset()
        {
            Gateway = OdinDefaults.Server;

            Init();

#if UNITY_EDITOR
            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnPeerJoined, PeerJoinedCreateComponent);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnPeerLeft, PeerLeftRemoveComponent);

            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnDecoderAdded, DecoderAddedPeerCreateComponent);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnDecoderRemoved, DecoderRemovedPeerRemoveComponent);

            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnRoomStateChanged, RoomStatusState);
#endif
        }

        /// <summary>
        /// Creates a default decoder for a peer on incoming audio if the peer has no decoder with a matching listen channel mask.
        /// </summary>
        /// <remarks>Only active if <see cref="AutoCreateMedia"/> is set. Runs on a native thread; the <see cref="OdinDecoder"/> component is created deferred on the main thread via <see cref="OnDecoderAdded"/>.</remarks>
        /// <param name="sender">Room object</param>
        /// <param name="args">datagram data</param>
        protected virtual void Room_OnAutoCreateDecoder(object sender, DatagramEventArgs args)
        {
            if (AutoCreateMedia == false) return;
            var room = _Room as Room;
            if (room == null || room.RemotePeers.TryGetValue(args.PeerId, out PeerEntity peer) == false) return;

            // an existing default decoder already handles - or with a changed mask deliberately filters - incoming audio
            if (peer.Medias.ContainsKey(AutoDecoderMediaId)) return;

            // some other decoder handles this datagram
            foreach (MediaDecoder media in peer.Medias.Values)
                if (args.ChannelMask == Core.Utility.ChannelMask.None || (media.ListenChannelMask & args.ChannelMask) != Core.Utility.ChannelMask.None)
                    return;

            MediaDecoder decoder;
            lock (_autoDecoderLock)
            {
                // lost the race against a concurrent datagram, that thread raised the event
                if (peer.Medias.ContainsKey(AutoDecoderMediaId)) return;
                if (room.GetOrCreateDecoder(args.PeerId, AutoDecoderMediaId, out decoder) == false || decoder == null) return;
                OnNativeDecoderCreated?.Invoke(room, args.PeerId, AutoDecoderMediaId);
            }
            // the wrapper routing already ran and dropped this datagram, so push it to the new decoder
            decoder.Push(args.Datagram, args.Payload.Length);
        }
        private readonly object _autoDecoderLock = new object();

        [Obsolete("Not subscribed by the component; re-raises the wrapper event and would recurse if wired to the event it re-raises")]
        protected virtual void Room_OnDatagram(object sender, DatagramEventArgs args) => (_Room as Room)?.OnDatagramReceived(args);
        [Obsolete("Not subscribed by the component; re-raises the wrapper event and would recurse if wired to the event it re-raises")]
        protected virtual void Room_OnRpc(object sender, RpcEventArgs args) => (_Room as Room)?.OnRPCReceived(args);
        [Obsolete("Not subscribed by the component; re-raises the wrapper event and would recurse if wired to the event it re-raises")]
        protected virtual void Room_OnSocket(object sender, SocketMessageEventArgs args) => (_Room as Room)?.OnSocketReceived(args);

        protected virtual void Room_OnMessageReceived(object sender, uint peerId, byte[] message)
        {
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, new MessageReceivedEventArgs() { PeerId = peerId, Data = message }));
        }

        /// <summary>
        /// Called by <see cref="Room"/> when the native SDK creates a decoder for a peer
        /// </summary>
        /// <param name="sender">Room object</param>
        /// <param name="peerId">peer that started streaming</param>
        /// <param name="mediaId">media id of the created decoder</param>
        public virtual void ExtraOnNativeDecoderCreated(object sender, uint peerId, ulong mediaId)
        {
            var room = sender as Room;
            var args = new DecoderAddedEventArgs()
            {
                PeerId = peerId,
                RoomId = room?.Id ?? 0,
                MediaId = mediaId,
            };

            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(room, args));
        }

        /// <summary>
        /// Dispatches <see cref="OnDecoderAdded"/> to the matching <see cref="OdinPeer"/>
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">decoder data</param>
        public virtual void DecoderAddedPeerCreateComponent(object sender, DecoderAddedEventArgs args)
        {
            OdinPeer peer = gameObject
                .GetComponentsInChildren<OdinPeer>(true)
                .FirstOrDefault(component => component.Id == args.PeerId);

            if (peer == null) return;
            peer.OnDecoderAdded?.Invoke(sender, args);
        }

        /// <summary>
        /// Called by <see cref="Room"/> when the native SDK removes a decoder for a peer
        /// </summary>
        /// <param name="sender">Room object</param>
        /// <param name="peerId">peer that stopped streaming</param>
        /// <param name="mediaId">media id of the removed decoder</param>
        public virtual void ExtraOnNativeDecoderRemoved(object sender, uint peerId, ulong mediaId)
        {
            var room = sender as Room;
            var args = new DecoderRemovedEventArgs()
            {
                MediaId = mediaId,
                PeerId = peerId,
            };

            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(room, args));
        }

        /// <summary>
        /// Dispatches <see cref="OnDecoderRemoved"/> to the matching <see cref="OdinPeer"/>
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">decoder data</param>
        public virtual void DecoderRemovedPeerRemoveComponent(object sender, DecoderRemovedEventArgs args)
        {
            OdinPeer peer = gameObject
                .GetComponentsInChildren<OdinPeer>(true)
                .FirstOrDefault(component => component.Id == args.PeerId);

            if (peer == null) return;

            peer.OnDecoderRemoved?.Invoke(sender, args);
        }

        protected virtual void Room_OnRoomJoined(object sender, JoinedObjectContainer args)
        {
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(
                this,
                new RoomJoinedEventArgs() { Room = sender as IRoom, customer = args.customer, own_peer_id = args.own_peer_id, room_name = args.room_name }));
        }

        protected virtual void Room_OnPeerLeft(object sender, PeerLeftObjectContainer args)
        {
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, new PeerLeftEventArgs() { PeerId = args.peer_id }));
        }

        /// <summary>
        /// Removes all child components with the same peer id
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">left peer data</param>
        public void PeerLeftRemoveComponent(object sender, PeerLeftEventArgs args)
        {
            DestroyDecodersForPeer(args.PeerId);
            foreach (OdinPeer peer in gameObject.GetComponentsInChildren<OdinPeer>(true))
                if (peer.Id == args.PeerId)
                    Destroy(peer.gameObject);
        }

        /// <summary>
        /// Remove a <see cref="OdinPeer"/> from a gameobject
        /// </summary>
        /// <param name="containerObject"></param>
        public void RemovePeerComponent(GameObject containerObject)
        {
            if (containerObject == null)
                return;

            OdinPeer peerComponent = containerObject.GetComponent<OdinPeer>();
            if (peerComponent == null) return;

            Destroy(peerComponent);
        }

        protected virtual void Room_OnPeerJoined(object sender, PeerJoinedObjectContainer args)
        {
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, new PeerJoinedEventArgs()
            {
                Room = this,
                peer_id = args.peer_id,
                user_id = args.user_id,
                user_data = new UserData(args.user_data),
            }));
        }

        /// <summary>
        /// Add a new GameObject with a new <see cref="OdinPeer"/> component. Uses <see cref="PeerPrefab"/> if set.
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">peer join data</param>
        public void PeerJoinedCreateComponent(object sender, PeerJoinedEventArgs args)
        {
            GameObject peerObject;
            if (PeerPrefab != null)
                peerObject = Instantiate(PeerPrefab, gameObject.transform);
            else
            {
                peerObject = new GameObject(args.peer_id.ToString());
                peerObject.transform.parent = gameObject.transform;
            }

            AddPeerComponent(peerObject, args.peer_id);
        }

        /// <summary>
        /// Add <see cref="OdinPeer"/> to a gameobject, detecting an existing component on a prefab
        /// </summary>
        /// <param name="containerObject">gameobject where the component will be added</param>
        /// <param name="peerId">id of <see cref="OdinNative.Wrapper.PeerEntity"/></param>
        /// <param name="enable">flag if the new <see cref="OdinPeer"/> component is enabled</param>
        /// <returns>created component</returns>
        public OdinPeer AddPeerComponent(GameObject containerObject, uint peerId, bool enable = true)
        {
            if (containerObject == null)
                return null;

            OdinPeer peerComponent = containerObject.GetComponent<OdinPeer>()
                ?? containerObject.AddComponent<OdinPeer>();
            peerComponent.Parent = this;
            peerComponent.Id = peerId;
#if UNITY_EDITOR
            // Add for Inspector UI
            UnityEditor.Events.UnityEventTools.AddPersistentListener(peerComponent.OnDecoderAdded, peerComponent.Peer_DecoderAdded);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(peerComponent.OnDecoderRemoved, peerComponent.Peer_DecoderRemoved);
#else
            peerComponent.OnDecoderAdded.AddListener(peerComponent.Peer_DecoderAdded);
            peerComponent.OnDecoderRemoved.AddListener(peerComponent.Peer_DecoderRemoved);
#endif
            peerComponent.enabled = enable;

            return peerComponent;
        }

        protected virtual void Room_OnConnectionStatusChanged(object sender, string connectionStatus)
        {
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, new RoomStateChangedEventArgs() { RoomState = connectionStatus }));
        }

        /// <summary>
        /// Check status if the room should destroy the gameobject
        /// </summary>
        /// <remarks>Any room can not recover from a <c>"Closed"</c> state and will destroy the gameobject.</remarks>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="status">new status</param>
        public virtual void RoomStatusState(object sender, RoomStateChangedEventArgs status)
        {
            if (string.IsNullOrEmpty(status?.RoomState)) return;
            string name = (sender as IRoom)?.Name ?? string.Empty;
            OdinLog.LogDebug($"{gameObject.name} room \"{name}\" new state: {status.RoomState}");
            if (status.RoomState.Equals("Closed") || status.RoomState.Equals("disconnected"))
                Destroy(gameObject);
        }

        #region serialized UnityEvent compatibility
        /// <summary>Forward for serialized UnityEvents of previous versions</summary>
        [Obsolete("Renamed to " + nameof(DecoderAddedPeerCreateComponent) + "; kept so serialized UnityEvents from previous versions keep working")]
        public void MediaAddedPeerCreateComponent(object sender, DecoderAddedEventArgs args) => DecoderAddedPeerCreateComponent(sender, args);
        /// <summary>Forward for serialized UnityEvents of previous versions</summary>
        [Obsolete("Renamed to " + nameof(DecoderRemovedPeerRemoveComponent) + "; kept so serialized UnityEvents from previous versions keep working")]
        public void MediaRemovedPeerRemoveComponent(object sender, DecoderRemovedEventArgs args) => DecoderRemovedPeerRemoveComponent(sender, args);
        /// <summary>Forward for serialized UnityEvents of previous versions</summary>
        [Obsolete("Renamed to " + nameof(RoomStatusState) + "; kept so serialized UnityEvents from previous versions keep working")]
        public void ConnectionStatusState(object sender, RoomStateChangedEventArgs status) => RoomStatusState(sender, status);
        #endregion serialized UnityEvent compatibility

        void Update()
        {
            if (string.IsNullOrEmpty(Token) == false && _Room?.IsJoined == false && IsConsumed == false)
            {
                OdinCipherHandle cipher = CryptoCipher?.Handle;
                if (_Room.Join(Token, cipher))
                    IsConsumed = true;
                else
                {
                    OdinLog.LogError($"{nameof(OdinRoom)} of \"{gameObject.name}\" can not join the room with token \"{Token}\"");
                    this.enabled = false;
                    return;
                }
            }

            if (EventQueue == null) return;
            while (EventQueue.TryDequeue(out KeyValuePair<object, System.EventArgs> uEvent))
            {
                if (uEvent.Value is RoomStateChangedEventArgs)
                    OnRoomStateChanged?.Invoke(this, uEvent.Value as RoomStateChangedEventArgs);
                //Room
                else if (uEvent.Value is RoomJoinedEventArgs)
                    OnRoomJoined?.Invoke(this, uEvent.Value as RoomJoinedEventArgs);
                //SubRoom
                else if (uEvent.Value is PeerJoinedEventArgs)
                    OnPeerJoined?.Invoke(this, uEvent.Value as PeerJoinedEventArgs);
                else if (uEvent.Value is PeerLeftEventArgs)
                    OnPeerLeft?.Invoke(this, uEvent.Value as PeerLeftEventArgs);
                else if (uEvent.Value is DecoderAddedEventArgs)
                    OnDecoderAdded?.Invoke(this, uEvent.Value as DecoderAddedEventArgs);
                else if (uEvent.Value is DecoderRemovedEventArgs)
                    OnDecoderRemoved?.Invoke(this, uEvent.Value as DecoderRemovedEventArgs);
                else if (uEvent.Value is MessageReceivedEventArgs)
                    OnMessageReceived?.Invoke(this, uEvent.Value as MessageReceivedEventArgs);
                else
                    OdinLog.LogError($"{nameof(OdinRoom)} Call to invoke unknown event skipped: {uEvent.Value.GetType()} from {nameof(uEvent.Key)} ({uEvent.Key.GetType()})");
            }
        }

        /// <summary>
        /// Room join
        /// </summary>
        /// <remarks>Calls BaseRoom Join! Use the token property instead or handle manually</remarks>
        /// <param name="token"></param>
        /// <returns>result of Join or false</returns>
        public bool Join(string token)
        {
            if (string.IsNullOrEmpty(token) || _Room == null || _Room.IsJoined) return false;

            // consume so Update() does not attempt a second join while this one is connecting
            return IsConsumed = _Room.Join(token);
        }

        /// <summary>
        /// Room join with optional encryption
        /// </summary>
        /// <remarks>Calls BaseRoom Join! Use the token property instead or handle manually</remarks>
        /// <param name="token"></param>
        /// <param name="cipher">crypto cipher</param>
        /// <returns>result of Join or false</returns>
        public bool Join(string token, OdinCipherHandle cipher)
        {
            if(string.IsNullOrEmpty(token) || _Room == null || _Room.IsJoined) return false;

            // consume so Update() does not attempt a second join while this one is connecting
            return IsConsumed = _Room.Join(token, cipher);
        }

        /// <summary>
        /// Redirects audio to all media encoders in the corresponding room.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="isSilent"></param>
        public virtual void ProxyAudio(float[] buffer, int position, bool isSilent = false)
        {
            var roomApi = _Room as Room;
            if (roomApi == null || roomApi.IsJoined == false) return;

            // convenience: create the default input media on first pushed audio
            if (AutoCreateMedia && roomApi.Encoders.IsEmpty)
                LinkInputMedia(Samplerate, IsStereo, out _);

            roomApi.SendAudio(buffer, isSilent);
        }

        /// <summary>
        /// Add a input media encoder to the corresponding room.
        /// </summary>
        /// <param name="samplerate">encoder samplerate</param>
        /// <param name="stereo">encoder channel flag</param>
        /// <param name="encoder">started encoder or null</param>
        /// <returns>true on start or false</returns>
        public virtual bool LinkInputMedia(uint samplerate, bool stereo, out MediaEncoder encoder)
        {
            encoder = null;
            var roomApi = _Room as Room;
            if (roomApi != null && roomApi.IsJoined)
            {
                if (roomApi.GetOrCreateEncoder(ulong.MaxValue, samplerate, stereo, out encoder))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a input media encoder from the corresponding room.
        /// </summary>
        /// <param name="encoder">input media</param>
        /// <returns>true on stop or false</returns>
        public virtual bool UnlinkInputMedia(MediaEncoder encoder)
        {
            bool result = false;
            if (encoder == null) return result;
            var roomApi = _Room as Room;
            if (roomApi != null && roomApi.IsJoined)
            {
                result = roomApi.GetEncoder(encoder.Id, out MediaEncoder roomEncoder);
                if (result && roomEncoder.Id == encoder.Id)
                {
                    if (roomApi.RemoveEncoder(roomEncoder.Id, out MediaEncoder tmp))
                        tmp.Dispose();
                }
            }
            return result;
        }

        /// <summary>
        /// Register an <see cref="OdinDecoder"/> with the scene-wide decoder registry for the given peer.
        /// Called automatically from <see cref="OdinDecoder.OnEnable"/>.
        /// </summary>
        /// <param name="decoder">decoder to register</param>
        /// <param name="peerId">peer this decoder belongs to</param>
        public void RegisterDecoder(OdinDecoder decoder, uint peerId)
        {
            if (!_decoderRegistry.TryGetValue(peerId, out var list))
            {
                list = new List<OdinDecoder>();
                _decoderRegistry[peerId] = list;
            }
            if (!list.Contains(decoder))
                list.Add(decoder);
        }

        /// <summary>
        /// Unregister an <see cref="OdinDecoder"/> from the scene-wide decoder registry.
        /// Called automatically from <see cref="OdinDecoder.OnDestroy"/>.
        /// </summary>
        /// <param name="decoder">decoder to unregister</param>
        public void UnregisterDecoder(OdinDecoder decoder)
        {
            foreach (var list in _decoderRegistry.Values)
                list.Remove(decoder);
        }

        /// <summary>
        /// Destroy all <see cref="OdinDecoder"/> instances registered for the given peer,
        /// regardless of where they live in the scene hierarchy.
        /// </summary>
        /// <param name="peerId">peer whose decoders should be destroyed</param>
        public void DestroyDecodersForPeer(uint peerId)
        {
            if (_decoderRegistry.TryGetValue(peerId, out var list))
            {
                foreach (var decoder in list.ToList())
                    if (decoder != null)
                        Destroy(decoder.gameObject);
                _decoderRegistry.Remove(peerId);
            }
        }

        /// <summary>
        /// Manually create a decoder for a peer and notify <see cref="OnDecoderAdded"/> listeners.
        /// </summary>
        /// <remarks>With <see cref="AutoCreateMedia"/> disabled use this or <see cref="OdinPeer.AddDecoderComponent(GameObject, ulong, bool)"/> to setup playback, e.g. on <see cref="OnPeerJoined"/>.</remarks>
        /// <param name="peerId">peer to create the decoder for</param>
        /// <returns>new <see cref="MediaDecoder"/> or null</returns>
        public MediaDecoder CreateMediaDecoder(uint peerId)
        {
            var decoder = _Room.CreateMediaDecoder(peerId);
            if (decoder != null)
                this.OnNativeDecoderCreated?.Invoke(this, peerId, decoder.Id);
            return decoder;
        }

        /// <summary>
        /// Manually remove a decoder of a peer and notify <see cref="OnDecoderRemoved"/> listeners.
        /// </summary>
        /// <param name="peerId">peer the decoder belongs to</param>
        /// <param name="mediaId">media id of the removed decoder</param>
        public void RemoveMediaDecoder(uint peerId, ulong mediaId)
        {
            this.OnNativeDecoderRemoved?.Invoke(this, peerId, mediaId);
            _Room.RemoveMediaDecoder(peerId, mediaId);
        }

        /// <summary>
        /// Generate a test token from a test key
        /// </summary>
        /// <remarks>For testing only - tokens should always come from a token-server in production, see <see cref="WebRequestToken(string, string)"/></remarks>
        /// <param name="roomId">Room name</param>
        /// <param name="userId">User name</param>
        /// <param name="lifetimeMinutes">token valid timeframe</param>
        /// <param name="testKey">optional test accesskey</param>
        /// <returns>Token or empty</returns>
        public static string GenerateTestToken(string roomId, string userId, double lifetimeMinutes = 5, string testKey = "")
        {
            OdinLog.LogWarning($"{nameof(GenerateTestToken)} of {nameof(OdinRoom)} is for testing only! Use a token-server with {nameof(WebRequestToken)}");
            if (string.IsNullOrEmpty(testKey))
            {
                testKey = OdinClient.CreateAccessKey();
                OdinLog.LogWarning($"Generated accesskey: {testKey}");
            }

            DateTime utc = DateTime.UtcNow;
            string body = $"{{ \"rid\": \"{roomId}\",\"uid\": \"{userId}\",\"nbf\": {((DateTimeOffset)utc).ToUnixTimeSeconds()}, \"exp\": {((DateTimeOffset)utc.AddMinutes(lifetimeMinutes)).ToUnixTimeSeconds()} }}";
            return OdinClient.CreateToken(testKey, body);
        }

        /// <summary>
        /// Uses UnityWebRequest with POST data as json to get a response from a token-server
        /// </summary>
        /// <remarks>Default callback will set <see cref="Token"/> with the response plain text</remarks>
        /// <param name="url">Token-Server endpoint</param>
        /// <param name="jsonPayload">Request data</param>
        /// <returns>IEnumerator for Coroutine</returns>
        public IEnumerator WebRequestToken(string url, string jsonPayload) => WebRequestToken(url, jsonPayload, (response) => this.Token = response.text);
        /// <summary>
        /// Uses UnityWebRequest with POST data as json to get a response from a token-server
        /// </summary>
        /// <param name="url">Token-Server endpoint</param>
        /// <param name="jsonPayload">Request data</param>
        /// <param name="response">Response callback</param>
        /// <returns>IEnumerator for Coroutine</returns>
        public IEnumerator WebRequestToken(string url, string jsonPayload, UnityAction<DownloadHandler> response)
        {
            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPayload));
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                yield return www.SendWebRequest();


#if UNITY_2020_3_OR_NEWER
                if (www.result != UnityWebRequest.Result.Success)
                    OdinLog.LogError(www.error);
                else
                    response?.Invoke(www.downloadHandler);
#else
                if (string.IsNullOrEmpty(www.error))
                    response?.Invoke(www.downloadHandler);
                else
                    OdinLog.LogError(www.error);
#endif
            }
        }

        void OnDisable()
        {
            if(_Room == null) return;

            _Room.OnRoomStatusChanged -= Room_OnConnectionStatusChanged;
            _Room.OnRoomJoined -= Room_OnRoomJoined;
            _Room.OnPeerJoined -= Room_OnPeerJoined;
            _Room.OnPeerLeft -= Room_OnPeerLeft;
            _Room.OnMessageReceived -= Room_OnMessageReceived;
            if (_Room is Room baseRoom)
                baseRoom.OnDatagram -= Room_OnAutoCreateDecoder;

            OnNativeDecoderCreated -= ExtraOnNativeDecoderCreated;
            OnNativeDecoderRemoved -= ExtraOnNativeDecoderRemoved;

            // OnEnable creates a new Room, so fully dispose the current one to
            // release its GCHandle and native handles even while still joining
            (_Room as Room)?.Dispose();
            _Room = null;
        }
        void OnDestroy()
        {
            (_Room as Room)?.Dispose();
            _Room = null;
        }
    }
}
