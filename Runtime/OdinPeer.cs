using OdinNative.Core.Imports;
using OdinNative.Unity.Events;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using OdinNative.Wrapper.Room;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity
{
    /// <summary>
    /// Wrapper class of <see cref="OdinNative.Wrapper.PeerEntity"/> for Unity
    /// <para>
    /// This convenient class provides dispatching of events to Unity with passthrough <see cref="UnityEngine.Events.UnityEvent"/>
    /// as well as predefined helper functions to cover default use cases where the voice chat is visually and logically represented by
    /// Unity gameobjects that are manageable with the Unity editor.
    /// </para>
    /// Default Unity GameObject altering event callback functions:
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="Peer_DecoderAdded"/></term>
    /// <description>Creates GameObject with <see cref="OdinDecoder"/> component and attach media</description>
    /// </item>
    /// <item>
    /// <term><see cref="Peer_DecoderRemoved"/></term>
    /// <description>Destroy GameObject with <see cref="OdinDecoder"/> component</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>Create a custom component with <see cref="OdinNative.Wrapper.IPeer"/> or inheritance from this class and extend/override.</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity/OdinPeer/")]
    [AddComponentMenu("Odin/Instance/OdinPeer")]
    public class OdinPeer : MonoBehaviour, IPeer
    {
        public IRoom Parent { get; set; }

        /// <summary>
        /// Prefab instantiated per decoder.
        /// </summary>
        public GameObject DecoderPrefab;

        /// <summary>
        /// Additional event redirect from <see cref="OdinRoom"/>
        /// </summary>
        /// <remarks>Usually invoked by <see cref="OdinRoom.DecoderAddedPeerCreateComponent"/></remarks>
        [FormerlySerializedAs("OnMediaAdded")]
        public DecoderAddedProxy OnDecoderAdded;
        /// <summary>
        /// Additional event redirect from <see cref="OdinRoom"/>
        /// </summary>
        /// <remarks>Usually invoked by <see cref="OdinRoom.DecoderRemovedPeerRemoveComponent"/></remarks>
        [FormerlySerializedAs("OnMediaRemoved")]
        public DecoderRemovedProxy OnDecoderRemoved;

        public uint Id { get; set; }
        public string UserId => _Peer?.UserId ?? string.Empty;
        public IUserData UserData => _Peer?.UserData ?? null;
        public List<string> Tags => _Peer?.Tags;

        public PeerEntity GetBasePeer() => _Peer;
        private PeerEntity _Peer;
        public Room GetRoomApi() { var r = Parent as OdinRoom; return r != null ? r.GetBaseRoom<Room>() : null; }
        public MediaEncoder GetEncoder() => ((IPeer)_Peer)?.GetEncoder();
        public MediaDecoder GetDecoder() => ((IPeer)_Peer)?.GetDecoder();

        void Awake()
        {
            // keep serialized events so persistent listeners from scenes/prefabs survive
            if (OnDecoderAdded == null)
                OnDecoderAdded = new DecoderAddedProxy();
            if (OnDecoderRemoved == null)
                OnDecoderRemoved = new DecoderRemovedProxy();

            this.enabled = false;
        }

        /// <summary>
        /// Event trigger to create a <see cref="OdinDecoder"/> component
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">started media data</param>
        public virtual void Peer_DecoderAdded(object sender, DecoderAddedEventArgs args)
        {
            if (isActiveAndEnabled)
            {
                GameObject decoderObject;
                if (DecoderPrefab != null)
                    decoderObject = Instantiate(DecoderPrefab, this.gameObject.transform);
                else
                {
                    decoderObject = new GameObject(args.MediaId.ToString());
                    decoderObject.transform.parent = this.gameObject.transform;
                }
                AddDecoderComponent(decoderObject, args.MediaId);
            }
        }

        /// <summary>
        /// Create a <see cref="OdinDecoder"/> component
        /// </summary>
        /// <param name="containerObject">gameobject where the component will be added</param>
        /// <param name="mediaId">id of <see cref="OdinNative.Wrapper.MediaDecoder"/></param>
        /// <param name="enable">flag if the new <see cref="OdinDecoder"/> component is enabled</param>
        /// <returns>created component</returns>
        public OdinDecoder AddDecoderComponent(GameObject containerObject, ulong mediaId, bool enable = true)
        {
            if (Parent == null) return null;
            var component = AddDecoderComponent(containerObject, mediaId, Parent.Samplerate, Parent.Stereo, false);
            if (component != null)
            {
                component.FollowOutputDevice = true;
                component.enabled = enable;
            }
            return component;
        }

        /// <summary>
        /// Create a <see cref="OdinDecoder"/> component
        /// </summary>
        /// <param name="containerObject">gameobject where the component will be added</param>
        /// <param name="mediaId">id of <see cref="OdinNative.Wrapper.MediaDecoder"/></param>
        /// <param name="samplerate">decoder samplerate</param>
        /// <param name="stereo">decoder channel flag</param>
        /// <param name="enable">flag if the new <see cref="OdinDecoder"/> component is enabled</param>
        /// <returns>created component</returns>
        public OdinDecoder AddDecoderComponent(GameObject containerObject, ulong mediaId, uint samplerate, bool stereo, bool enable = true)
        {
            if (containerObject == null)
                return null;

            if (Parent == null) return null;
            if (Parent.GetOrCreateDecoder(Id, mediaId, samplerate, stereo, out var decoder) == false)
                return null;
            else
                decoder.Parent = GetBasePeer();

            if (!containerObject.TryGetComponent(out OdinDecoder decoderComponent))
                decoderComponent = containerObject.AddComponent<OdinDecoder>();
            decoderComponent.Room = Parent as OdinRoom;
            decoderComponent.PeerId = Id;
            decoderComponent.Parent = GetBasePeer();
            decoderComponent.Id = mediaId;
            decoderComponent.FollowOutputDevice = false;
            decoderComponent.SetDecoder(decoder);
            decoderComponent.AutoDestroyAudioSource = true;
            decoderComponent.enabled = enable;
            return decoderComponent;
        }

        /// <summary>
        /// Event trigger to remove a <see cref="OdinDecoder"/> component
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">stopped media data</param>
        public virtual void Peer_DecoderRemoved(object sender, DecoderRemovedEventArgs args)
        {
            if (isActiveAndEnabled)
                RemoveDecoderComponent(this.gameObject, args.MediaId);
        }

        #region serialized UnityEvent compatibility
        /// <summary>Forward for serialized UnityEvents of previous versions</summary>
        [Obsolete("Renamed to " + nameof(Peer_DecoderAdded) + "; kept so serialized UnityEvents from previous versions keep working")]
        public void Peer_MediaAdded(object sender, DecoderAddedEventArgs args) => Peer_DecoderAdded(sender, args);
        /// <summary>Forward for serialized UnityEvents of previous versions</summary>
        [Obsolete("Renamed to " + nameof(Peer_DecoderRemoved) + "; kept so serialized UnityEvents from previous versions keep working")]
        public void Peer_MediaRemoved(object sender, DecoderRemovedEventArgs args) => Peer_DecoderRemoved(sender, args);
        #endregion serialized UnityEvent compatibility

        /// <summary>
        /// Removes all child components with the same media id
        /// </summary>
        /// <param name="containerObject">child components</param>
        /// <param name="mediaId">decoder id</param>
        /// <param name="componentOnly">on false will destroy gameobject</param>
        /// <returns>true on removed decoder or false</returns>
        public bool RemoveDecoderComponent(GameObject containerObject, ulong mediaId, bool componentOnly = false)
        {
            if (containerObject == null)
                return false;

            OdinDecoder decoderComponent = containerObject
                .GetComponentsInChildren<OdinDecoder>()
                .FirstOrDefault(d => d.Id == mediaId);

            if (decoderComponent == null) return false;

            // use the component id, _Peer stays null when the peer was not in RemotePeers on enable
            bool result = Parent != null && Parent.RemoveDecoder(Id, mediaId, out _);
            Destroy(componentOnly ? decoderComponent : decoderComponent.gameObject);

            return result;
        }

        public OdinSocket AddSocketComponent(GameObject containerObject, OdinSocketKind kind, int label, int priority = 0)
        {
            if (containerObject == null)
                return null;

            var odinRoom = Parent as OdinRoom;
            var roomApi = odinRoom != null ? odinRoom.GetBaseRoom<Room>() : null;
            if (roomApi == null) return null;
            OdinSocket socketComponent = containerObject.AddComponent<OdinSocket>();
            socketComponent.Parent = this;
            // link the component, not the wrapper room: the socket resolves the
            // live wrapper room itself and survives a room disable/enable cycle
            socketComponent.RoomLink = odinRoom;
            socketComponent.Room = odinRoom;
            socketComponent.Kind = kind;
            socketComponent.Label = label;
            socketComponent.Priority = priority;
            return socketComponent;
        }

        public bool RemoveSocketComponent(GameObject containerObject, ulong socketId, bool componentOnly = false)
        {
            if (containerObject == null)
                return false;

            // sockets are keyed by their native id
            OdinSocket socketComponent = containerObject
                .GetComponentsInChildren<OdinSocket>()
                .FirstOrDefault(socket => socket.Id == socketId);

            if (socketComponent == null) return false;

            var odinRoom = Parent as OdinRoom;
            var roomApi = odinRoom != null ? odinRoom.GetBaseRoom<Room>() : null;
            bool result = roomApi != null && roomApi.RemoveSocket(socketId) != null;
            Destroy(componentOnly ? socketComponent : socketComponent.gameObject);

            return result;
        }

        void OnEnable()
        {
            if (Parent == null)
            {
                Parent = GetComponent<OdinRoom>();
                if (Parent == null || ((OdinRoom)Parent).isActiveAndEnabled == false)
                {
                    OdinLog.LogInfo($"No available active room for \"{gameObject.name}\" {nameof(OdinPeer)}");
                    this.enabled = false;
                    return;
                }
            }

            var roomApi = GetRoomApi();
            if (roomApi == null) return;

            if (_Peer == null)
                if (roomApi.RemotePeers.TryGetValue(Id, out _Peer))
                    _Peer.Parent = roomApi;

        }

        void Reset()
        {
#if UNITY_EDITOR
            if (isActiveAndEnabled)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(OnDecoderAdded, Peer_DecoderAdded);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(OnDecoderRemoved, Peer_DecoderRemoved);
            }
#endif
        }

        void OnDisable()
        {
            if (_Peer != null && Parent != null && Parent.IsJoined)
            {
                foreach (var media in _Peer.Medias)
                    Parent.RemoveDecoder(_Peer.Id, media.Key, out _);
            }
        }

        void OnDestroy()
        {
            OnDecoderAdded.RemoveAllListeners();
            OnDecoderRemoved.RemoveAllListeners();

            // the PeerEntity is owned and disposed by the wrapper room (peer left/room dispose),
            // destroying the component must not free it while the room still routes datagrams
            _Peer = null;
        }
    }
}
