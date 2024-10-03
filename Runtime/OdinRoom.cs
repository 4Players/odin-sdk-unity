using OdinNative.Core.Imports;
using OdinNative.Unity.Events;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using OdinNative.Wrapper.Room;
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

namespace OdinNative.Unity
{
    /// <summary>
    /// Wrapper class of <see cref="OdinNative.Wrapper.Room.Room"/> for Unity.
    /// <para>
    /// This convenient class provides dispatching of events to Unity with passthrough <see cref="UnityEngine.Events.UnityEvent"/> 
    /// as well as predefined helper functions to cover for a default usecases where the voice chat is visually and logical represented
    /// Unity gameobject that are manageable with the Unity editor like Context menu, Inspector and/or Hierarchy window.
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
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odinroom/")]
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
        /// Unity channel flag
        /// </summary>
        /// <remarks>We let Unity resample/mix audio on demand and use only mono by default internally</remarks>
        public bool IsStereo { get; set; }

        private OdinConnection Connection;
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

        private static OdinClient _odinClient;

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
        /// Odin media started
        /// </summary>
        event OnMediaStartedDelegate IRoom.OnMediaStarted
        {
            add
            {
                if (_Room == null) return;
                _Room.OnMediaStarted += value;
            }

            remove
            {
                if (_Room == null) return;
                _Room.OnMediaStarted -= value;
            }
        }
        /// <summary>
        /// Odin media stopped
        /// </summary>
        event OnMediaStoppedDelegate IRoom.OnMediaStopped
        {
            add
            {
                if (_Room == null) return;
                _Room.OnMediaStopped += value;
            }

            remove
            {
                if (_Room == null) return;
                _Room.OnMediaStopped -= value;
            }
        }
        /// <summary>
        /// Odin peer changed userdata
        /// </summary>
        event OnUserDataChangedDelegate IRoom.OnUserDataChanged
        {
            add
            {
                if (_Room == null) return;
                _Room.OnUserDataChanged += value;
            }

            remove
            {
                if (_Room == null) return;
                _Room.OnUserDataChanged -= value;
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
        /// Odin base room
        /// </summary>
        /// <returns>wrapper room object</returns>
        public T GetBaseRoom<T>() where T : IRoom => (T)(_Room as IRoom);
        private IRoom _Room;
        /// <summary>
        /// Default value gameObject parent or Unity root
        /// </summary>
        public object Parent => this.gameObject.transform.parent;

        public bool IsJoined => _Room?.IsJoined ?? false;

        private ConcurrentQueue<KeyValuePair<object, EventArgs>> EventQueue;
        [Header("Events")]
        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Room.Room.OnRoomJoined"/> redirected as Unity event
        /// </summary>
        public RoomJoinedProxy OnRoomJoined;
        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Room.Room.OnMediaStarted"/> redirected as Unity event
        /// </summary>
        public MediaAddedProxy OnMediaAdded;
        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Room.Room.OnMediaStopped"/> redirected as Unity event
        /// </summary>
        public MediaRemovedProxy OnMediaRemoved;
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
        public RoomStateChangedProxy OnRoomStateChanged;
        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Room.Room.OnUserDataChanged"/> redirected as Unity event
        /// </summary>
        public PeerUserDataChangedProxy OnUserDataChanged;
        /// <summary>
        /// Event <see cref="OdinNative.Wrapper.Room.Room.OnRoomLeft"/> redirected as Unity event
        /// </summary>
        public RoomLeftProxy OnRoomLeft;

        private bool IsConsumed = false;

        private void Init()
        {
            OnRoomJoined = new RoomJoinedProxy();
            OnMediaAdded = new MediaAddedProxy();
            OnMediaRemoved = new MediaRemovedProxy();
            OnPeerJoined = new PeerJoinedProxy();
            OnPeerLeft = new PeerLeftProxy();
            OnUserDataChanged = new PeerUserDataChangedProxy();
            OnMessageReceived = new MessageReceivedProxy();
            OnRoomStateChanged = new RoomStateChangedProxy();
            OnRoomLeft = new RoomLeftProxy();
        }

        void Awake()
        {
            Samplerate = (uint)AudioSettings.outputSampleRate;
            // we use Mono for convenience setup and less samples to init encoders/decoders
            // even without a check to 'AudioSettings.speakerMode >= AudioSpeakerMode.Stereo;' 
            // Unity will resample and/or upmix, downmix on AudioClip<->AudioSource
            // (on true with custom virtual channels override SetAudioClipData in OdinMedia)
            IsStereo = false; 

            EventQueue = new ConcurrentQueue<KeyValuePair<object, EventArgs>>();
        }

        void OnEnable()
        {
#if UNITY_WEBGL
            var room = gameObject.GetComponent<OdinWebRoom>();
            if (room == null)
                room = gameObject.AddComponent<OdinWebRoom>();

            room.EndPoint = Gateway;
            room.InitJSBridgePlugin();
            room.SubscribeEvents(gameObject.name);
            
            _Room = room;
            room.Parent = this;

            SetupRoomCallbacks();

            if (string.IsNullOrEmpty(Token) == false)
                IsConsumed = room.Join(Token);
#endif
            if (EventQueue == null) EventQueue = new ConcurrentQueue<KeyValuePair<object, EventArgs>>();
            EventQueue.Clear();
        }

        private void SetupRoomCallbacks()
        {
            if(null != _Room)
            {
                _Room.OnRoomJoined += Room_OnRoomJoined;
                _Room.OnRoomStatusChanged += Room_OnConnectionStatusChanged;
                _Room.OnPeerJoined += Room_OnPeerJoined;
                _Room.OnPeerLeft += Room_OnPeerLeft;
                _Room.OnMediaStarted += Room_OnMediaStarted;
                _Room.OnMediaStopped += Room_OnMediaStopped;
                _Room.OnMessageReceived += Room_OnMessageReceived;
                _Room.OnUserDataChanged += Room_OnUserDataChanged;
                _Room.OnRoomLeft += Room_OnRoomLeft;
            }
        }

        


        void Reset()
        {
            Gateway = OdinDefaults.Server;
            
            Init();

#if UNITY_EDITOR
#if !UNITY_WEBGL
            if (Connection == null)
            {
                Connection = GetComponent<OdinConnection>();
                if (Connection != null)
                {
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(Connection.OnDatagram, Room_OnDatagram);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(Connection.OnRpc, Room_OnRpc);
                }
            }
#endif

            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnPeerJoined, PeerJoinedCreateComponent);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnPeerLeft, PeerLeftRemoveComponent);

            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnMediaAdded, MediaAddedPeerCreateComponent);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnMediaRemoved, MediaRemovedPeerRemoveComponent);

            UnityEditor.Events.UnityEventTools.AddPersistentListener(OnRoomStateChanged, RoomStatusState);
#endif
        }

#if !UNITY_WEBGL
        protected virtual void Room_OnDatagram(object sender, DatagramEventArgs args) => (_Room as Room)?.OnDatagramReceived(args);
        protected virtual void Room_OnRpc(object sender, RpcEventArgs args) => (_Room as Room)?.OnRPCReceived(args);
#endif

        private void Room_OnRoomLeft(object sender, string reason)
        {
            if (sender is Room room)
            {
                RoomLeftEventArgs roomLeftEventArgs = new RoomLeftEventArgs() { RoomId = room.Id, Reason = reason};
                EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, roomLeftEventArgs));
            }
        }
        
        protected virtual void Room_OnMessageReceived(object sender, ulong peerId, byte[] message)
        {
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, new MessageReceivedEventArgs() { PeerId = peerId, Data = message }));
        }
        
        private void Room_OnUserDataChanged(object sender, ulong peerid, byte[] userdata)
        {
            var userDataChangedEventArgs = new PeerUserDataChangedEventArgs() { PeerId = peerid, UserData = new UserData(userdata) };
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, userDataChangedEventArgs));
        }

        protected virtual void Room_OnMediaStarted(object sender, ulong peerId, MediaRpc media)
        {
            var room = sender as Room;
            var args = new MediaAddedEventArgs()
            {
                MediaId = media.Id,
                PeerId = peerId,
                RoomId = room.Id,
                MediaUId = media.Properties?.uId ?? new Guid().ToString(),
            };

            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(room, args));
        }

        protected virtual void MediaAddedPeerCreateComponent(object sender, MediaAddedEventArgs args)
        {
            OdinPeer peer = gameObject
                .GetComponentsInChildren<OdinPeer>(true)
                .FirstOrDefault(component => component.Id == args.PeerId);

            if (peer == null) return;
            peer.OnMediaAdded?.Invoke(sender, args);
        }

        protected virtual void Room_OnMediaStopped(object sender, ulong peerId, ushort mediaId)
        {
            var room = sender as Room;
            var args = new MediaRemovedEventArgs()
            {
                MediaId = mediaId,
                PeerId = peerId,
            };

            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(room, args));
        }

        protected virtual void MediaRemovedPeerRemoveComponent(object sender, MediaRemovedEventArgs args)
        {
            OdinPeer peer = gameObject
                .GetComponentsInChildren<OdinPeer>(true)
                .FirstOrDefault(component => component.Id == args.PeerId);

            if (peer == null) return;

            //peer.RemoveMediaComponent(peer.gameObject, args.MediaId); // peer handling is optional
            peer.OnMediaRemoved?.Invoke(sender, args);
        }

        protected virtual void Room_OnRoomJoined(object sender, ulong ownPeerId, string name, string customer, byte[] roomUserData, ushort[] mediaIds, System.Collections.ObjectModel.ReadOnlyCollection<OdinNative.Wrapper.Peer.PeerRpc> peers)
        {
#if UNITY_WEBGL
            var room = sender as OdinWebRoom;
#else
            var room = sender as Room;
#endif
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(
                this,
                new RoomJoinedEventArgs() { Room = room, Customer = customer }));

            foreach (var peer in peers)
            {
                EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(room, new PeerJoinedEventArgs()
                {
                    PeerId = peer.Id,
                    UserId = peer.UserId,
                    UserData = new UserData(peer.UserData),
                    Medias = peer.Medias.ToArray(),
                }));

                foreach (var media in peer.Medias)
                {
                    EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(room, new MediaAddedEventArgs()
                    {
                        MediaId = media.Id,
                        PeerId = peer.Id,
                        RoomId = room.Id,
                        MediaUId = media.Properties?.uId ?? new Guid().ToString(),
                    }));
                }
            }
        }

        protected virtual void Room_OnPeerLeft(object sender, ulong peerId)
        {
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, new PeerLeftEventArgs() { PeerId = peerId }));
        }
        

        /// <summary>
        /// Removes all child components with the same peer id
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">left peer data</param>
        public void PeerLeftRemoveComponent(object sender, PeerLeftEventArgs args)
        {
#if !UNITY_WEBGL
            foreach (OdinPeer peer in gameObject.GetComponentsInChildren<OdinPeer>(true))
                if (peer.Id == args.PeerId)
                    Destroy(peer.gameObject);
#else
            foreach (GameObject container in gameObject.GetComponentsInChildren<GameObject>().Where(o => o.name == args.PeerId.ToString()))
                    Destroy(container.gameObject);
#endif
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

        protected virtual void Room_OnPeerJoined(object sender, ulong peerId, string userId, byte[] userData, MediaRpc[] medias)
        {
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, new PeerJoinedEventArgs()
            {
                PeerId = peerId,
                UserId = userId,
                UserData = new UserData(userData),
                Medias = medias
            }));
        }

        /// <summary>
        /// Add a new GameObject with a new <see cref="OdinPeer"/> component
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">peer join data</param>
        public void PeerJoinedCreateComponent(object sender, PeerJoinedEventArgs args)
        {
            GameObject peerObject = new GameObject(args.PeerId.ToString());
            peerObject.transform.parent = gameObject.transform;

#if !UNITY_WEBGL
            AddPeerComponent(peerObject, args.PeerId);
#endif
        }

        /// <summary>
        /// Add <see cref="OdinPeer"/> to a gameobject
        /// </summary>
        /// <param name="containerObject">gameobject where the component will be added</param>
        /// <param name="peerId">id of <see cref="OdinNative.Wrapper.PeerEntity"/></param>
        /// <param name="enable">flag if the new <see cref="OdinPeer"/> component is enabled</param>
        /// <returns>created component</returns>
        public OdinPeer AddPeerComponent(GameObject containerObject, ulong peerId, bool enable = true)
        {
            if (containerObject == null)
                return null;

            OdinPeer peerComponent = containerObject.AddComponent<OdinPeer>();
            peerComponent.Parent = this;
            peerComponent.Id = peerId;
#if UNITY_EDITOR
            // Add for Inspector UI
            UnityEditor.Events.UnityEventTools.AddPersistentListener(peerComponent.OnMediaAdded, peerComponent.Peer_MediaAdded);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(peerComponent.OnMediaRemoved, peerComponent.Peer_MediaRemoved);
#else
            peerComponent.OnMediaAdded.AddListener(peerComponent.Peer_MediaAdded);
            peerComponent.OnMediaRemoved.AddListener(peerComponent.Peer_MediaRemoved);
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

#if UNITY_WEBGL
            var room = (sender as OdinRoom).GetBaseRoom<OdinWebRoom>();
#else
            var room = (sender as OdinRoom).GetBaseRoom<Room>();
#endif
            if (OdinDefaults.Debug) Debug.Log($"#DBG {gameObject.name} room \"{room.Name}\" new state: {status.RoomState}");
            if (status.RoomState.Equals("Closed") || status.RoomState.Equals("disconnected"))
                Destroy(gameObject);
        }

        void Update()
        {
            if (string.IsNullOrEmpty(Token) == false && !IsJoined && IsConsumed == false)
            {
                OdinCipherHandle cipher = CryptoCipher?.Handle;
                if (Join(Token, cipher))
                    IsConsumed = true;
                else
                {
                    Debug.LogError($"{nameof(OdinRoom)} of \"{gameObject.name}\" can not join the room with token \"{Token}\"");
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
                else if (uEvent.Value is RoomLeftEventArgs)
                    OnRoomLeft?.Invoke(uEvent.Value as RoomLeftEventArgs);
                //SubRoom
                else if (uEvent.Value is PeerJoinedEventArgs)
                    OnPeerJoined?.Invoke(this, uEvent.Value as PeerJoinedEventArgs);
                else if (uEvent.Value is PeerLeftEventArgs)
                    OnPeerLeft?.Invoke(this, uEvent.Value as PeerLeftEventArgs);
                else if (uEvent.Value is PeerUserDataChangedEventArgs)
                    OnUserDataChanged?.Invoke(this, uEvent.Value as PeerUserDataChangedEventArgs);
                else if (uEvent.Value is MediaAddedEventArgs)
                    OnMediaAdded?.Invoke(this, uEvent.Value as MediaAddedEventArgs);
                else if (uEvent.Value is MediaRemovedEventArgs)
                    OnMediaRemoved?.Invoke(this, uEvent.Value as MediaRemovedEventArgs);
                else if (uEvent.Value is MessageReceivedEventArgs)
                    OnMessageReceived?.Invoke(this, uEvent.Value as MessageReceivedEventArgs);
                else
                    Debug.LogError($"Call to invoke unknown event skipped: {uEvent.Value.GetType()} from {nameof(uEvent.Key)} ({uEvent.Key.GetType()})");
            }
        }

        /// <summary>
        /// Room join
        /// </summary>
        /// <remarks>Calls BaseRoom Join! Use the token property instead or handle manualy</remarks>
        /// <param name="token"></param>
        /// <returns>result of Join or false</returns>
        public bool Join(string token)
        {
            return Join(token, null);
        }

        /// <summary>
        /// Room join with optional encryption
        /// </summary>
        /// <remarks>Calls BaseRoom Join! Use the token property instead or handle manualy</remarks>
        /// <param name="token"></param>
        /// <param name="cipher">crypto cipher</param>
        /// <returns>result of Join or false</returns>
        public bool Join(string token, OdinCipherHandle cipher)
        {
            if (string.IsNullOrEmpty(token) || IsJoined) return false;

            bool bJoinedSuccess = false;
            
            #if UNITY_WEBGL
            if (null != _Room)
            {
                bJoinedSuccess = _Room.Join(token, cipher);
            }
            #else
            _odinClient ??= OdinClient.Create(new Uri(Gateway));
            if (null != _odinClient)
            {
                bJoinedSuccess = _odinClient.JoinRoom(out Room room, token, null, null, 0, 0, 0,
                    Samplerate, IsStereo, cipher);
                _Room = room;
                room.Parent = this;
                SetupRoomCallbacks();
            }
            #endif

            return bJoinedSuccess;
        }
        

        /// <summary>
        /// Redirects audio to all media encoders in the corresponding room.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="position"></param>
        /// <param name="isSilent"></param>
        public virtual void ProxyAudio(float[] buffer, int position, bool isSilent = false)
        {
#if UNITY_WEBGL
            var roomApi = GetBaseRoom<OdinWebRoom>();
            if (roomApi != null && roomApi.IsJoined)
                roomApi.ProxyAudio(buffer, position, isSilent);
#else
            var roomApi = GetBaseRoom<Room>();
            if (roomApi != null && roomApi.IsJoined)
                roomApi.SendAudio(buffer, isSilent); // push to encoder
#endif
        }

#if UNITY_WEBGL
        /// <summary>
        /// Creates and add a input media to the corresponding room. 
        /// </summary>
        /// <param name="settings">VAD and APM settings</param>
        /// <returns>always true</returns>
        public virtual bool LinkInputMedia(OdinWebRoom.CaptureParamsData settings)
        {
            var roomApi = GetBaseRoom<OdinWebRoom>();
            roomApi.CreateCapture(settings);
            return true;
        }
#endif
        /// <summary>
        /// Add a input media encoder to the corresponding room. 
        /// </summary>
        /// <remarks>Result with a OdinWebRoom will always be true after a call to LinkCaptureMedia even with a null encoder</remarks>
        /// <param name="samplerate">encoder samplerate</param>
        /// <param name="stereo">encoder channel flag</param>
        /// <param name="encoder">started encoder or null</param>
        /// <returns>true on start or false</returns>
        public virtual bool LinkInputMedia(uint samplerate, bool stereo, out MediaEncoder encoder)
        {
            encoder = null;
#if UNITY_WEBGL
            if (_Room is OdinWebRoom) return LinkInputMedia(OdinWebRoom.CaptureParamsData.Default());
#endif
            var roomApi = GetBaseRoom<Room>();
            if (roomApi != null && roomApi.IsJoined && roomApi.AvailableEncoderIds.TryDequeue(out var mediaId))
            {
                if (roomApi.GetOrCreateEncoder(mediaId, samplerate, stereo, out encoder))
                    return roomApi.StartMedia(encoder).Result.Error == string.Empty; // Send "StartMedia" rpc
            }
            return false;
        }

#if UNITY_WEBGL
        /// <summary>
        /// Remove a input media from the corresponding room.
        /// </summary>
        /// <returns>true or false</returns>
        public virtual bool UnlinkInputMedia()
        {
            var roomApi = GetBaseRoom<OdinWebRoom>();
            if (roomApi != null && roomApi.IsJoined)
            {
                roomApi.UnlinkCaptureMedia();
                return true;
            }
            return false;
        }
#endif
        /// <summary>
        /// Remove a input media encoder from the corresponding room.
        /// </summary>
        /// <param name="encoder">input media</param>
        /// <param name="free">flag if the freed up encoder id will be available for the room again</param>
        /// <returns>true on stop or false</returns>
        public virtual bool UnlinkInputMedia(MediaEncoder encoder, bool free = true)
        {
            bool result = false;
            if (encoder == null) return result;
#if UNITY_WEBGL
            if (_Room is OdinWebRoom) return UnlinkInputMedia();
#endif
            var roomApi = GetBaseRoom<Room>();
            if (roomApi != null && roomApi.IsJoined)
            {
                // Send "StopMedia" rpc
                result = roomApi.StopMedia(encoder).Result.Error == string.Empty;

                // check for internal room encoder
                if (result && roomApi.GetEncoder(encoder.Id, out MediaEncoder roomEncoder) && roomEncoder.Id == encoder.Id)
                {
                    // clean up internal room encoder if not already destroyed
                    if (roomApi.RemoveEncoder(roomEncoder.Id, out MediaEncoder tmp) && free)
                    {
                        ushort freeId = tmp.Id;
                        tmp.Dispose();
                        // give back mediaId that is free again
                        if (roomApi.AvailableEncoderIds.Contains(freeId) == false)
                            roomApi.AvailableEncoderIds.Enqueue(freeId); 
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Start a stopped remote output decoder
        /// </summary>
        /// <param name="media">output wrapper</param>
        /// <returns>true on start or false</returns>
        public virtual bool ResumeOutputMedia(OdinMedia media) => ResumeOutputMedia(media.MediaDecoder);
        /// <summary>
        /// Start a stopped remote output decoder
        /// </summary>
        /// <param name="decoder">output</param>
        /// <returns>true on start or false</returns>
        public virtual bool ResumeOutputMedia(MediaDecoder decoder) 
        {
#if UNITY_WEBGL
            if (_Room is OdinWebRoom) return ResumeOutputMedia(decoder.MediaProperties.uId);
#endif
            var roomApi = GetBaseRoom<Room>();
            if (roomApi != null && roomApi.IsJoined)
                return roomApi.ResumeMedia(decoder).Result.Error == string.Empty; // Send "ResumeMedia" rpc

            return false;
        }

#if UNITY_WEBGL
        /// <summary>
        /// Start remote output
        /// </summary>
        /// <param name="uid">output media id</param>
        /// <returns>true or false</returns>
        public virtual bool ResumeOutputMedia(string uid)
        {
            var roomApi = GetBaseRoom<OdinWebRoom>();
            if (roomApi != null && roomApi.IsJoined)
            {
                roomApi.StartPlaybackMedia(uid);
                return true;
            }

            return false;
        }
#endif

        /// <summary>
        /// Stop a started remote output decoder
        /// </summary>
        /// <param name="media">output wrapper</param>
        /// <returns>true on stop or false</returns>
        public virtual bool PauseOutputMedia(OdinMedia media) => PauseOutputMedia(media.MediaDecoder);
        /// <summary>
        /// Stop a started remote output decoder
        /// </summary>
        /// <param name="decoder">output</param>
        /// <returns>true on stop or false</returns>
        public virtual bool PauseOutputMedia(MediaDecoder decoder)
        {
#if UNITY_WEBGL
            if (_Room is OdinWebRoom) return PauseOutputMedia(decoder.MediaProperties.uId);
#endif
            var roomApi = GetBaseRoom<Room>();
            if (roomApi != null && roomApi.IsJoined)
                return roomApi.PauseMedia(decoder).Result.Error == string.Empty; // Send "PauseMedia" rpc

            return false;
        }

#if UNITY_WEBGL
        /// <summary>
        /// Start remote output
        /// </summary>
        /// <param name="uid">output media id</param>
        /// <returns>true or false</returns>
        public virtual bool PauseOutputMedia(string uid)
        {
            var roomApi = GetBaseRoom<OdinWebRoom>();
            if (roomApi != null && roomApi.IsJoined)
            {
                roomApi.StopPlaybackMedia(uid);
                return true;
            }

            return false;
        }
#endif

        /// <summary>
        /// Generate a test token from a test key
        /// </summary>
        /// <remarks>This is a editor function is only available</remarks>
        /// <param name="roomId">Room name</param>
        /// <param name="userId">User name</param>
        /// <param name="lifetimeMinutes">token valid timeframe</param>
        /// <param name="testKey">optional test accesskey</param>
        /// <returns>Token or empty</returns>
        public static string GenerateTestToken(string roomId, string userId, double lifetimeMinutes = 5, string testKey = "")
        {
#if UNITY_WEBGL
            Debug.LogWarning($"{nameof(GenerateTestToken)} of {nameof(OdinRoom)} is for testing only! Use a token-server with a valid customerId");
            return System.Threading.Tasks.Task.Run(() => OdinWebRoom.GetToken(roomId, userId, "", "https://app-server.odin.4players.io/v1/token")).GetAwaiter().GetResult();
#else
            Debug.LogWarning($"{nameof(GenerateTestToken)} of {nameof(OdinRoom)} is for testing only! Use a token-server with {nameof(WebRequestToken)}");
            if (string.IsNullOrEmpty(testKey))
            {
                testKey = OdinClient.CreateAccessKey();
                Debug.LogWarning($"Generated accesskey: {testKey}");
            }

            DateTime utc = DateTime.UtcNow;
            string body = $"{{ \"rid\": \"{roomId}\",\"uid\": \"{userId}\",\"nbf\": {((DateTimeOffset)utc).ToUnixTimeSeconds()}, \"exp\": {((DateTimeOffset)utc.AddMinutes(lifetimeMinutes)).ToUnixTimeSeconds()}, \"customer\":\"d\" }}";
            return OdinClient.CreateToken(testKey, body);
#endif
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
                    Debug.LogError(www.error);
                else
                    response?.Invoke(www.downloadHandler);
#else
                if (string.IsNullOrEmpty(www.error))
                    response?.Invoke(www.downloadHandler);
                else
                    Debug.LogError(www.error);
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
            _Room.OnMediaStarted -= Room_OnMediaStarted;
            _Room.OnMediaStopped -= Room_OnMediaStopped;
            _Room.OnMessageReceived -= Room_OnMessageReceived;
            _Room.OnUserDataChanged -= Room_OnUserDataChanged;
            _Room.OnRoomLeft -= Room_OnRoomLeft;

#if !UNITY_WEBGL
            if (_Room.IsJoined && _Room is Room)
                (_Room as Room).Close();
#endif
        }
        void OnDestroy()
        {
            if(_Room is Room)
                (_Room as Room)?.Dispose();

            _Room = null;
        }
    }
}