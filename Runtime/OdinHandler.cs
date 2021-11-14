using OdinNative.Odin;
using OdinNative.Odin.Media;
using OdinNative.Odin.Room;
using OdinNative.Unity;
using OdinNative.Unity.Audio;
using OdinNative.Unity.Events;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using static OdinNative.Core.Imports.NativeBindings;

[RequireComponent(typeof(OdinEditorConfig))]
[DisallowMultipleComponent, DefaultExecutionOrder(-100)]
public class OdinHandler : MonoBehaviour
{
    /// <summary>
    /// True if any <see cref="Room"/> is joined
    /// </summary>
    public bool HasConnections => Client.Rooms.Any(r => r.IsJoined);

    /// <summary>
    /// Unity Component that handles one Microphone where data gets routed through (n) <see cref="MediaStream"/>
    /// </summary>
    public MicrophoneReader Microphone;

    public RoomCollection Rooms => Client.Rooms;
    /// <summary>
    /// Called before an actual room join
    /// </summary>
    public RoomJoinProxy OnRoomJoin;
    /// <summary>
    /// Called after a room is joined successfully
    /// </summary>
    /// <remarks>Invokes only on success</remarks>
    public RoomJoinedProxy OnRoomJoined;
    /// <summary>
    /// Called before a room leave
    /// </summary>
    public RoomLeaveProxy OnRoomLeave;
    /// <summary>
    /// Called after a room is destroyed
    /// </summary>
    public RoomLeftProxy OnRoomLeft;

    /// <summary>
    /// Called on every Peer that joins in the same room(s)
    /// </summary>
    /// <remarks>Self is marked as Peer and this handler will trigger this invoke too</remarks>
    public PeerJoinedProxy OnPeerJoined;
    /// <summary>
    /// Called on every Peer that updates his UserData in the same room(s)
    /// </summary>
    public PeerUpdatedProxy OnPeerUpdated;
    /// <summary>
    /// Called on every Peer that left in the same room(s)
    /// </summary>
    public PeerLeftProxy OnPeerLeft;
    /// <summary>
    /// Called on every Peer that created a media in the same room(s)
    /// </summary>
    public MediaAddedProxy OnMediaAdded;
    /// <summary>
    /// Called on every Peer that closed/destroyed one of his own media in the same room(s)
    /// </summary>
    /// <remarks>Invokes before <see cref="OnDeleteMediaObject"/></remarks>
    public MediaRemovedProxy OnMediaRemoved;
    /// <summary>
    /// Called on every Peer that received message from a peer by <see cref="Room.SendMessage(ulong, byte[])"/>
    /// </summary>
    public MessageReceivedProxy OnMessageReceived;

    /// <summary>
    /// Called if this OdinHandler created a MediaStream that was requested by the MediaQueue
    /// </summary>
    /// <remarks>Invokes after <see cref="OnMediaAdded"/></remarks>
    public UnityCreatedMediaObject OnCreatedMediaObject;
    /// <summary>
    /// Called if this OdinHandler destroyed a MediaStream that was closed by a remote peer and was requested by the MediaQueue
    /// </summary>
    /// <remarks>Invokes after <see cref="OnMediaRemoved"/></remarks>
    public UnityDeleteMediaObject OnDeleteMediaObject;

    internal ConcurrentQueue<KeyValuePair<object, System.EventArgs>> EventQueue;

    /// <summary>
    /// Internal Client Wrapper instance for ODIN ffi
    /// </summary>
    internal OdinClient Client;
    internal static bool Corrupted;
    private ConcurrentQueue<KeyValuePair<Room, MediaAddedEventArgs>> MediaAddedQueue;
    private ConcurrentQueue<KeyValuePair<Room, MediaRemovedEventArgs>> MediaRemovedQueue;

    private static readonly object Lock = new object();
    private static OdinEditorConfig _config;

    internal string ClientId { get; private set; }
    internal string Identifier { get; private set; }
    /// <summary>
    /// Static reference to Global <see cref="OdinEditorConfig"/>
    /// </summary>
    /// <remarks>Is a <see cref="RequireComponent"/></remarks>
    public static OdinEditorConfig Config
    {
        get
        {
            lock (Lock)
            {
                if (_config != null)
                    return _config;

                var config = FindObjectsOfType<OdinEditorConfig>().FirstOrDefault();
                if (config == null)
                    config = Instance.gameObject.AddComponent<OdinEditorConfig>();

                return _config = config;
            }
        }
    }

    /// <summary>
    /// Singleton reference to this <see cref="OdinHandler"/>
    /// </summary>
    /// <remarks>Provides access to the client with a usual Unity singleton pattern and add a instance if the client is missing in the scene</remarks>
    public static OdinHandler Instance { get; private set; }

    /// <summary>
    /// Identify Odin client
    /// </summary>
    [Header("OdinClient Settings")]
    [SerializeField]
    private bool _persistent = true;
    /// <summary>
    /// Identify <see cref="GameObject"/> by Unity-Tag to attach a <see cref="PlaybackComponent"/>
    /// </summary>
    /// <remarks>Currently no effect</remarks>
    [Tooltip("Identify GameObject by Unity-Tag to attach a PlaybackComponent")]
    [SerializeField]
    public readonly string UnityAudioSourceTag = "Peer";
    /// <summary>
    /// Enable 3D Audio via preset <see cref="UserData"/>
    /// </summary>
    /// <remarks>Currently no effect</remarks>
    [SerializeField]
    public bool Use3DAudio = false;
    /// <summary>
    /// Creates <see cref="PlaybackComponent"/> on <see cref="Room_OnMediaAdded"/> events
    /// </summary>
    [SerializeField]
    public bool CreatePlayback = false;
    [SerializeField]
    public AudioMixer PlaybackAudioMixer;
    [SerializeField]
    public AudioMixerGroup PlaybackAudioMixerGroup;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        if (_persistent)
            DontDestroyOnLoad(gameObject);

        Identifier = SystemInfo.deviceUniqueIdentifier;
        ClientId = string.Join(".", Application.companyName, Application.productName, Identifier);

        MediaAddedQueue = new ConcurrentQueue<KeyValuePair<Room, MediaAddedEventArgs>>();
        MediaRemovedQueue = new ConcurrentQueue<KeyValuePair<Room, MediaRemovedEventArgs>>();

        SetupEventProxy();
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        UnityEditor.EditorApplication.quitting += OnEditorApplicationQuitting;

        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        UnityEditor.AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
#endif
    }

    void Start()
    {
        if (string.IsNullOrEmpty(Config.ClientId))
            Config.ClientId = ClientId;

        UserData userData = new UserData(Config.UserDataText);

        try
        {
            if (string.IsNullOrEmpty(Config.AccessKey))
            {
                Debug.LogError("Access-Key was not set!");
                Config.AccessKey = OdinClient.CreateAccessKey();
                Debug.LogWarning("Using a generated test key!");
            }

            Client = new OdinClient(new System.Uri(Config.Server), Config.AccessKey, userData);
            Client.Startup();
        }
        catch (System.DllNotFoundException e)
        {
            Corrupted = true;
            Debug.LogError("Native Plugin libraries for Unity not found!");
            Debug.LogException(e);
            Destroy(this.gameObject);
            return;
        }

        if (Microphone == null && Corrupted == false)
            Microphone = gameObject.AddComponent<MicrophoneReader>();
    }

    private void SetupEventProxy(bool customProxy = false)
    {
        EventQueue = new ConcurrentQueue<KeyValuePair<object, System.EventArgs>>();

        if (OnCreatedMediaObject == null) OnCreatedMediaObject = new UnityCreatedMediaObject();
        if (OnDeleteMediaObject == null) OnDeleteMediaObject = new UnityDeleteMediaObject();

        //Room
        if (OnRoomJoin == null) OnRoomJoin = new RoomJoinProxy();
        if (OnRoomJoined == null) OnRoomJoined = new RoomJoinedProxy();
        if (OnRoomLeave == null) OnRoomLeave = new RoomLeaveProxy();
        if (OnRoomLeft == null) OnRoomLeft = new RoomLeftProxy();
        //sub Room
        if (OnPeerJoined == null) OnPeerJoined = new PeerJoinedProxy();
        if (customProxy) OnPeerJoined.AddListener(new UnityEngine.Events.UnityAction<object, PeerJoinedEventArgs>(Room_OnPeerJoined));
        if (OnPeerUpdated == null) OnPeerUpdated = new PeerUpdatedProxy();
        if (customProxy) OnPeerUpdated.AddListener(new UnityEngine.Events.UnityAction<object, PeerUpdatedEventArgs>(Room_OnPeerUpdated));
        if (OnPeerLeft == null) OnPeerLeft = new PeerLeftProxy();
        if (customProxy) OnPeerLeft.AddListener(new UnityEngine.Events.UnityAction<object, PeerLeftEventArgs>(Room_OnPeerLeft));
        if (OnMediaAdded == null) OnMediaAdded = new MediaAddedProxy();
        if (customProxy) OnMediaAdded.AddListener(new UnityEngine.Events.UnityAction<object, MediaAddedEventArgs>(Room_OnMediaAdded));
        if (OnMediaRemoved == null) OnMediaRemoved = new MediaRemovedProxy();
        if (customProxy) OnMediaRemoved.AddListener(new UnityEngine.Events.UnityAction<object, MediaRemovedEventArgs>(Room_OnMediaRemoved));
        if (OnMessageReceived == null) OnMessageReceived = new MessageReceivedProxy();
        if (customProxy) OnMessageReceived.AddListener(new UnityEngine.Events.UnityAction<object, MessageReceivedEventArgs>(Room_OnMessageReceived));
    }

    public UserData GetUserData()
    {
        return Client.UserData;
    }

    /// <summary>
    /// Join or create a room by name and attach a <see cref="MicrophoneStream"/>
    /// </summary>
    /// <remarks>Configure Room-Apm i.e VadEnable, ... or Odin-Event-Listeners i.e PeerJoinedEvent, ... with <see cref="Config"/></remarks>
    /// <param name="roomName">Room name</param>
    /// <param name="userData">Override OdinClient default UserData</param>
    /// <param name="setup">Override default Room setup</param>
    public async void JoinRoom(string roomName, UserData userData = null, System.Action<Room> setup = null)
    {
        if(string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("Room name can not be empty!");
            return;
        }

        if (Client.Rooms[roomName] != null)
        {
            Debug.LogError($"Room {roomName} already joined!");
            return;
        }

        if (userData == null)
            userData = new UserData(Config.UserDataText);
        
        if (setup == null)
            setup = (r) =>
            {
                var cfg = Config;
                if (cfg.PeerJoinedEvent) r.OnPeerJoined += Room_OnPeerJoined;
                if (cfg.PeerLeftEvent) r.OnPeerLeft += Room_OnPeerLeft;
                if (cfg.PeerUpdatedEvent) r.OnPeerUpdated += Room_OnPeerUpdated;
                if (cfg.MediaAddedEvent) r.OnMediaAdded += Room_OnMediaAdded;
                if (cfg.MediaRemovedEvent) r.OnMediaRemoved += Room_OnMediaRemoved;

                r.SetApmConfig(new OdinNative.Core.OdinRoomConfig()
                {
                    VadEnable = cfg.VadEnable,
                    EchoCanceller = cfg.EchoCanceller,
                    HighPassFilter = cfg.HighPassFilter,
                    PreAmplifier = cfg.PreAmplifier,
                    OdinNoiseSuppressionLevel = cfg.NoiseSuppressionLevel,
                    TransientSuppressor = cfg.TransientSuppressor,
                });

                EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(
                    this,
                    new RoomJoinEventArgs() { Room = r }));
            };

        Room room = await Client.JoinRoom(roomName, Config.ClientId, userData, setup);

        if (room == null || room.IsJoined == false)
        {
            Debug.LogError($"Odin {Config.ClientId}: Room {roomName} join failed!");
            return;
        }
        Debug.Log($"Odin {Config.ClientId}: Room {room.Config.Name} joined.");

        if (room.CreateMicrophoneMedia(new OdinNative.Core.OdinMediaConfig(Microphone.SampleRate, Config.DeviceChannels)))
            Debug.Log($"MicrophoneStream added to room {roomName}.");

        EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(
            this, 
            new RoomJoinedEventArgs() { Room = room }));
    }

    /// <summary>
    /// Leave and free the <see cref="Room"/> by name
    /// </summary>
    /// <param name="roomName">Room name</param>
    public async void LeaveRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("Room name can not be empty!");
            return;
        }

        EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(
            this, 
            new RoomLeaveEventArgs() { Room = Rooms[roomName] } ));

        if (CreatePlayback && Use3DAudio == false)
        {
            var playbacks = FindObjectsOfType<PlaybackComponent>()
                .Where(p => p.RoomName == roomName);

            foreach (PlaybackComponent playback in playbacks)
                DestroyImmediate(playback);
        }

        if (await Client.LeaveRoom(roomName) == false)
            Debug.LogWarning($"Room {roomName} not found!");
        else
        {
            EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(
                this,
                new RoomLeftEventArgs() { RoomName = roomName }));
        }
    }

    /// <summary>
    /// Peer joins the room.
    /// </summary>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">PeerJoined Args</param>
    protected virtual void Room_OnPeerJoined(object sender, PeerJoinedEventArgs e)
    {
        if (Config.Verbose)
            Debug.Log($"Odin Room \"{(sender as Room).Config.Name}\": User added {e.Peer} with {e.Peer.UserData}");

        EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, e));
    }

    /// <summary>
    /// Peer left the room.
    /// </summary>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">PeerLeft Args</param>
    protected virtual void Room_OnPeerLeft(object sender, PeerLeftEventArgs e)
    {
        if (Config.Verbose)
            Debug.Log($"Odin Room \"{(sender as Room).Config.Name}\": User left {e.PeerId}");

        EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, e));
    }

    /// <summary>
    /// Peer updated userdata.
    /// </summary>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">PeerUpdated Args</param>
    protected virtual void Room_OnPeerUpdated(object sender, PeerUpdatedEventArgs e)
    {
        if (Config.Verbose)
            Debug.Log($"Odin Room \"{(sender as Room).Config.Name}\": User {e.PeerId} updated {e.UserData}");

        EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, e));
    }

    /// <summary>
    /// Audio/Video stream added in the room.
    /// </summary>
    /// <remarks>The remote <see cref="MediaStream"/> is always a <see cref="PlaybackStream"/> and readonly.</remarks>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">MediaAdded Args</param>
    protected virtual void Room_OnMediaAdded(object sender, MediaAddedEventArgs e)
    {
        if (Config.Verbose)
            Debug.Log($"Odin Room \"{(sender as Room).Config.Name}\": add Media: {e.Media} PlaybackId: {e.Media.Id} to Peer: {e.Peer}");

        // Push for unity thread
        MediaAddedQueue.Enqueue(new KeyValuePair<Room, MediaAddedEventArgs>(sender as Room, e));
        EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, e));
    }

    /// <summary>
    /// Room audio/video stream is closed in the room.
    /// </summary>
    /// <remarks>Peer and Media in <see cref="MediaRemovedEventArgs"/> is null if the peer left before the owned Medias are removed</remarks>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">MediaRemoved Args with MediaId</param>
    protected virtual void Room_OnMediaRemoved(object sender, MediaRemovedEventArgs e)
    {
        if (Config.Verbose)
            Debug.Log($"Odin Room \"{(sender as Room).Config.Name}\": removed Media: {e.MediaId} from Peer: {e.Peer}");

        // Push for unity thread
        MediaRemovedQueue.Enqueue(new KeyValuePair<Room, MediaRemovedEventArgs>(sender as Room, e));
        EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, e));
    }

    /// <summary>
    /// Room audio/video stream is closed in the room.
    /// </summary>
    /// <remarks>Peer and Media in <see cref="MediaRemovedEventArgs"/> is null if the peer left before the owned Medias are removed</remarks>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">MediaRemoved Args with MediaId</param>
    protected virtual void Room_OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        if (Config.Verbose)
            Debug.Log($"Odin Room \"{(sender as Room).Config.Name}\": received message from Peer: {e.PeerId}");

        EventQueue.Enqueue(new KeyValuePair<object, System.EventArgs>(sender, e));
    }

    void FixedUpdate()
    {
        if (Corrupted) return;

        HandleNewMediaQueue();
        HandleRemoveMediaQueue();
        HandleEventQueue();
    }

    private void HandleEventQueue()
    {
        while (EventQueue.TryDequeue(out KeyValuePair<object, System.EventArgs> uEvent))
        {
            //Room
            if (uEvent.Value is RoomJoinEventArgs)
                OnRoomJoin?.Invoke(uEvent.Value as RoomJoinEventArgs);
            else if (uEvent.Value is RoomJoinedEventArgs)
                OnRoomJoined?.Invoke(uEvent.Value as RoomJoinedEventArgs);
            else if (uEvent.Value is RoomLeaveEventArgs)
                OnRoomLeave?.Invoke(uEvent.Value as RoomLeaveEventArgs);
            else if (uEvent.Value is RoomLeftEventArgs)
                OnRoomLeft?.Invoke(uEvent.Value as RoomLeftEventArgs);
            //SubRoom
            else if (uEvent.Value is PeerJoinedEventArgs)
                OnPeerJoined?.Invoke(uEvent.Key, uEvent.Value as PeerJoinedEventArgs);
            else if (uEvent.Value is PeerUpdatedEventArgs)
                OnPeerUpdated?.Invoke(uEvent.Key, uEvent.Value as PeerUpdatedEventArgs);
            else if (uEvent.Value is PeerLeftEventArgs)
                OnPeerLeft?.Invoke(uEvent.Key, uEvent.Value as PeerLeftEventArgs);
            else if (uEvent.Value is MediaAddedEventArgs)
                OnMediaAdded?.Invoke(uEvent.Key, uEvent.Value as MediaAddedEventArgs);
            else if (uEvent.Value is MediaRemovedEventArgs)
                OnMediaRemoved?.Invoke(uEvent.Key, uEvent.Value as MediaRemovedEventArgs);
            else if (uEvent.Value is MessageReceivedEventArgs)
                OnMessageReceived?.Invoke(uEvent.Key, uEvent.Value as MessageReceivedEventArgs);
            else
                Debug.LogError($"Call to invoke unknown event skipped: {uEvent.Value.GetType()} from {nameof(uEvent.Key)} ({uEvent.Key.GetType()})");
        }
    }

    private void HandleNewMediaQueue()
    {
        if (MediaAddedQueue.TryDequeue(out KeyValuePair<Room, MediaAddedEventArgs> addedEvent))
        {

            if (CreatePlayback)
                if (Use3DAudio)
                {
                    Debug.LogWarning("Create Playback and 3D Audio enabled at the same time has currently limit support and uses FindGameObjectsWithTag with UnityAudioSourceTag tagged gameobjects.");
                    AssignPlaybackComponent(UnityAudioSourceTag, addedEvent);
                }
                else if (addedEvent.Key.OwnId != addedEvent.Value.PeerId)
                    AssignPlaybackComponent(this.gameObject, addedEvent);
            else if (Config.Verbose)
                Debug.LogWarning($"No available consumers for playback found.");

            OnCreatedMediaObject?.Invoke(addedEvent.Key.Config.Name, addedEvent.Value.Peer.Id, addedEvent.Value.Media.Id);
        }
    }

    private PlaybackComponent AssignPlaybackComponent(string gameObjectTag, KeyValuePair<Room, MediaAddedEventArgs> addedEvent)
    {
        return AssignPlaybackComponent(GetPeerContainer(gameObjectTag),
            addedEvent.Key.Config.Name,
            addedEvent.Value.Peer.Id,
            addedEvent.Value.Media.Id);
    }

    private PlaybackComponent AssignPlaybackComponent(GameObject peerContainer, KeyValuePair<Room, MediaAddedEventArgs> addedEvent)
    {
        return AssignPlaybackComponent(peerContainer,
            addedEvent.Key.Config.Name,
            addedEvent.Value.Peer.Id,
            addedEvent.Value.Media.Id);
    }

    public PlaybackComponent AssignPlaybackComponent(string gameObjectTag, string roomName, ulong peerId, int mediaId, bool autoDestroySource = true)
    {
        return AssignPlaybackComponent(GetPeerContainer(gameObjectTag),
            roomName,
            peerId,
            mediaId,
            autoDestroySource);
    }

    private GameObject GetPeerContainer(string gameObjectTag)
    {
        GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(gameObjectTag);
        if (gameObjects.Length == 0)
            Debug.LogWarning($"No game objects are tagged with '{gameObjectTag}'");
        var free = gameObjects.Where(g => g.GetComponent<PlaybackComponent>() == null);
        if (free.Count() == 0)
            Debug.LogWarning($"No game objects are free with '{gameObjectTag}' tag ({free.Count()}/{gameObjects.Length})");

        return free.FirstOrDefault();
    }

    public PlaybackComponent AssignPlaybackComponent(GameObject peerContainer, string roomName, ulong peerId, int mediaId, bool autoDestroySource = true)
    {
        if (peerContainer == null)
        {
            Debug.LogError("Can not add PlaybackComponent to null");
            return null;
        }

        var playback = peerContainer.AddComponent<PlaybackComponent>();
        playback.AutoDestroyAudioSource = autoDestroySource; // We create and destroy the audiosource
        playback.RoomName = roomName;
        playback.PeerId = peerId;
        playback.MediaId = mediaId;
        if (PlaybackAudioMixerGroup != null)
            playback.PlaybackSource.outputAudioMixerGroup = PlaybackAudioMixerGroup;
        Debug.Log($"Playback created on {peerContainer.name} for Room {playback.RoomName} Peer {playback.PeerId} Media {playback.MediaId}");

        return playback;
    }

    private void HandleRemoveMediaQueue()
    {
        if (MediaRemovedQueue.TryDequeue(out KeyValuePair<Room, MediaRemovedEventArgs> removedEvent))
        {
            OnDeleteMediaObject?.Invoke(removedEvent.Value.MediaId);

            if (CreatePlayback && Use3DAudio == false)
            {
                var playbacks = gameObject.GetComponents<PlaybackComponent>();
                var playback = playbacks.FirstOrDefault(p => p.MediaId == removedEvent.Value.MediaId);

                if (playback == null)
                    Debug.LogWarning($"No Playback for stream id {removedEvent.Value.MediaId} found to destroy!");
                else if (removedEvent.Value == null)
                    Debug.LogWarning($"No Media for stream id {removedEvent.Value.MediaId} found!");
                else
                    Destroy(playback);
            }
        }
    }

    /// <summary>
    /// The attached <see cref="MicrophoneStream"/> used by <see cref="MicrophoneReader"/>
    /// </summary>
    /// <param name="roomName">Room name</param>
    /// <param name="config"><see cref="MicrophoneStream"/> config</param>
    /// <returns><see cref="MicrophoneStream"/> or null if there is no room</returns>
    internal MicrophoneStream GetOrCreateMicrophoneStream(string roomName, OdinNative.Core.OdinMediaConfig config = null)
    {
        if (string.IsNullOrEmpty(roomName)) return null;
        var room = Client.Rooms[roomName];
        if (room == null) return null;

        if (room.MicrophoneMedia == null)
            return room.MicrophoneMedia = new MicrophoneStream(config ?? new OdinNative.Core.OdinMediaConfig(Config.DeviceSampleRate, Config.DeviceChannels));
        else
            return room.MicrophoneMedia;
    }

    public void OnBeforeAssemblyReload()
    {
        Client.ReloadLibrary(false);
        Debug.LogException(new System.NotSupportedException("Odin currently not supports reloading while in Playmode!"));
        Corrupted = true;
    }

    public void OnAfterAssemblyReload()
    {
        Corrupted = true;
        Awake();
        Start();

        Debug.LogWarning("Odin instance lost! Please, restart the application.");
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
        UnityEditor.EditorApplication.quitting -= OnEditorApplicationQuitting;

        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
#endif
    }

    private void EditorApplication_playModeStateChanged(UnityEditor.PlayModeStateChange stateChange)
    {
        if (stateChange.HasFlag(UnityEditor.PlayModeStateChange.ExitingPlayMode))
            Client.ReloadLibrary(false);
    }

    private void OnEditorApplicationQuitting()
    {
        Client?.Shutdown();
    }

    void OnDestroy()
    {
        if (Corrupted) return;

        Client?.Close();
    }

    void OnApplicationQuit()
    {
        if (Corrupted) return;

        Client?.Dispose();
    }
}
