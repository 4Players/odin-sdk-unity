using OdinNative.Unity.Events;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Room;
using System.Linq;
using UnityEngine;

namespace OdinNative.Unity
{
    /// <summary>
    /// Wrapper class of <see cref="OdinNative.Wrapper.PeerEntity"/> for Unity
    /// <para>
    /// This convenient class provides dispatching of events to Unity with passthrough <see cref="UnityEngine.Events.UnityEvent"/> 
    /// as well as predefined helper functions to cover for a default usecases where the voice chat is visually and logical represented
    /// Unity gameobject that are manageable with the Unity editor.
    /// </para>
    /// Default Unity GameObject altering event callback functions:
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="Peer_MediaAdded"/></term>
    /// <description>Creates GameObject with <see cref="OdinMedia"/> component and attach media</description>
    /// </item>
    /// <item>
    /// <term><see cref="Peer_MediaRemoved"/></term>
    /// <description>Destroy GameObject with <see cref="OdinMedia"/> component</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>Create a custom component with <see cref="OdinNative.Wrapper.IPeer"/> or inheritance from this class and extend/override.</remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odinpeer/")]
    [AddComponentMenu("Odin/Instance/OdinPeer")]
    public class OdinPeer : MonoBehaviour, IPeer
    {
        public IRoom Parent { get; set; }

        /// <summary>
        /// Additional event redirect from <see cref="OdinRoom"/>
        /// </summary>
        /// <remarks>Usually invoked by <see cref="OdinRoom.MediaAddedPeerCreateComponent"/></remarks>
        public MediaAddedProxy OnMediaAdded;
        /// <summary>
        /// Additional event redirect from <see cref="OdinRoom"/>
        /// </summary>
        /// <remarks>Usually invoked by <see cref="OdinRoom.MediaRemovedPeerRemoveComponent"/></remarks>
        public MediaRemovedProxy OnMediaRemoved;

        public ulong Id { get; set; }
        public string UserId => _Peer?.UserId ?? string.Empty;
        public IUserData UserData => _Peer?.UserData ?? null;

        public PeerEntity GetBasePeer() => _Peer;
        private PeerEntity _Peer;
        public Room GetRoomApi() => Parent?.GetBaseRoom<Room>();

        void Awake()
        {
            OnMediaAdded = new MediaAddedProxy();
            OnMediaRemoved = new MediaRemovedProxy();

            this.enabled = false;
        }

        /// <summary>
        /// Event trigger to create a <see cref="OdinMedia"/> component
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">started media data</param>
        public virtual void Peer_MediaAdded(object sender, MediaAddedEventArgs args)
        {
            if (isActiveAndEnabled)
            {
                GameObject mediaObject = new GameObject(args.MediaId.ToString());
                mediaObject.transform.parent = this.gameObject.transform;
                AddMediaComponent(mediaObject, args.MediaId);
            }
        }

        /// <summary>
        /// Create a <see cref="OdinMedia"/> component
        /// </summary>
        /// <param name="containerObject">gameobject where the component will be added</param>
        /// <param name="mediaId">id of <see cref="OdinNative.Wrapper.MediaDecoder"/></param>
        /// <param name="enable">flag if the new <see cref="OdinMedia"/> component is enabled</param>
        /// <returns>created component</returns>
        public OdinMedia AddMediaComponent(GameObject containerObject, ushort mediaId, bool enable = true)
        {
            var roomApi = Parent.GetBaseRoom<Room>();
            if (roomApi == null) return null;
            return AddMediaComponent(containerObject, mediaId, roomApi.Samplerate, roomApi.Stereo, enable);
        }
        /// <summary>
        /// Create a <see cref="OdinMedia"/> component
        /// </summary>
        /// <param name="containerObject">gameobject where the component will be added</param>
        /// <param name="mediaId">id of <see cref="OdinNative.Wrapper.MediaDecoder"/></param>
        /// <param name="samplerate">decoder samplerate</param>
        /// <param name="stereo">decoder channel flag</param>
        /// <param name="enable">flag if the new <see cref="OdinMedia"/> component is enabled</param>
        /// <returns>created component</returns>
        public OdinMedia AddMediaComponent(GameObject containerObject, ushort mediaId, uint samplerate, bool stereo, bool enable = true)
        {
            if (containerObject == null)
                return null;

            var roomApi = Parent.GetBaseRoom<Room>();
            if (roomApi == null) return null;
            if (roomApi.GetOrCreateDecoder(Id, mediaId, samplerate, stereo, out var decoder) == false)
                return null;
            else
                decoder.Parent = GetBasePeer();

            OdinMedia mediaComponent = containerObject.AddComponent<OdinMedia>();
            mediaComponent.Parent = this;
            mediaComponent.Id = mediaId;
            mediaComponent.MediaDecoder = decoder;
            mediaComponent.AutoDestroyAudioSource = true;
            if(Parent is OdinRoom)
                mediaComponent.AudioMixerGroup = ((OdinRoom)Parent).AudioMixerGroup;
            mediaComponent.enabled = enable;
            return mediaComponent;
        }

        /// <summary>
        /// Event trigger to remove a <see cref="OdinMedia"/> component
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">stopped media data</param>
        public virtual void Peer_MediaRemoved(object sender, MediaRemovedEventArgs args)
        {
            if (isActiveAndEnabled)
                RemoveMediaComponent(this.gameObject, args.MediaId);
        }

        /// <summary>
        /// Removes all child components with the same media id
        /// </summary>
        /// <param name="containerObject">child components</param>
        /// <param name="mediaId">decoder id</param>
        /// <param name="componentOnly">on false will destroy gameobject</param>
        /// <returns>true on removed decoder or false</returns>
        public bool RemoveMediaComponent(GameObject containerObject, ushort mediaId, bool componentOnly = false)
        {
            if (containerObject == null)
                return false;

            OdinMedia mediaComponent = containerObject
                .GetComponentsInChildren<OdinMedia>()
                .FirstOrDefault(media => media.Id == mediaId);

            if(mediaComponent == null) return false;

            var roomApi = Parent.GetBaseRoom<Room>();
            bool result = roomApi.RemoveDecoder(_Peer.Id, mediaId, out _);
            Destroy(componentOnly ? mediaComponent : mediaComponent.gameObject);

            return result;
        }

        void OnEnable()
        {
#if UNITY_WEBGL
#pragma warning disable CS0618 // Type or member is obsolete
            OdinNative.Core.Utility.Throw(new System.NotSupportedException("Unity device encoder/decoder is currently not supported in WebGL"));
#pragma warning restore CS0618 // Type or member is obsolete
            this.enabled = false;
            return;
#pragma warning disable CS0162 // Unreachable code detected
#endif

            if (Parent == null)
            {
                Parent = GetComponent<OdinRoom>();
                if (Parent == null || ((OdinRoom)Parent).isActiveAndEnabled == false)
                {
                    Debug.LogWarning($"No available active room for \"{gameObject.name}\" {nameof(OdinPeer)}");
                    this.enabled = false;
                    return;
                }
            }

            var roomApi = GetRoomApi();
            if (roomApi == null) return;

            if (_Peer == null)
                if(roomApi.RemotePeers.TryGetValue(Id, out _Peer))
                    _Peer.Parent = roomApi;

        }

        void Reset()
        {
#if UNITY_EDITOR
            if (isActiveAndEnabled)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(OnMediaAdded, Peer_MediaAdded);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(OnMediaRemoved, Peer_MediaRemoved);
            }
#endif
        }

        void OnDisable()
        {
            if (_Peer != null)
            {
                var roomApi = GetRoomApi();
                if (roomApi == null) return;

                if (roomApi.IsJoined)
                {
                    foreach (var media in _Peer.Medias)
                        roomApi.StopMedia(media.Key);
                }
            }
        }

        void OnDestroy()
        {
            OnMediaAdded.RemoveAllListeners();
            OnMediaRemoved.RemoveAllListeners();

            _Peer?.Dispose();
            _Peer = null;
        }
    }
}