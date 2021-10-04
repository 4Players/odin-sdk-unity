using OdinNative.Odin;
using OdinNative.Odin.Media;
using OdinNative.Odin.Room;
using OdinNative.Unity;
using OdinNative.Unity.Audio;
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

    public delegate void CreateAudioMedia(string roomName, ulong peerId, int mediaId);
    public delegate void CreateAudioMediaEx(Room room, OdinNative.Odin.Peer.Peer peer, PlaybackStream media);
    public event CreateAudioMedia OnCreateMediaObject;
    public event CreateAudioMediaEx OnCreatedMediaObject;

    public delegate void DeleteAudioMedia(int mediaId);
    public event DeleteAudioMedia OnDeleteMediaObject;

    public RoomCollection Rooms => Client.Rooms;
    /// <summary>
    /// Internal Client Wrapper instance for ODIN ffi
    /// </summary>
    internal OdinClient Client;
    internal static bool Corrupted;
    private ConcurrentQueue<KeyValuePair<Room, MediaAddedEvent>> MediaAddedQueue;
    private ConcurrentQueue<KeyValuePair<Room, MediaRemovedEvent>> MediaRemovedQueue;

    private static readonly object Lock = new object();
    private static OdinEditorConfig _config;

    internal string Identifier { get; private set; }
    /// <summary>
    /// Singleton reference to Global <see cref="OdinEditorConfig"/>
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

    private static OdinHandler _instance;
    /// <summary>
    /// Singleton reference to this <see cref="OdinHandler"/>
    /// </summary>
    /// <remarks>Provides access to the client with a usual Unity singleton pattern and add a instance if the client is missing in the scene</remarks>
    public static OdinHandler Instance
    {
        get
        {
            lock (Lock)
            {
                if(Corrupted) Debug.LogError("Native Plugin libraries in Unity corrupted!");

                if (_instance != null)
                    return _instance;

                var instances = FindObjectsOfType<OdinHandler>();
                if (instances.Length > 0)
                {
                    if (instances.Length == 1)
                        return _instance = instances[0];

                    for (var i = 1; i < instances.Length; i++)
                        Destroy(instances[i]);

                    return _instance = instances[0];
                }

                return _instance;
            }
        }
    }

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
    [Tooltip("Enable 3D Audio via preset UserData")]
    [SerializeField]
    public bool Use3DAudio = false;
    /// <summary>
    /// Creates <see cref="PlaybackComponent"/> on <see cref="Room_OnMediaAdded"/> events
    /// </summary>
    [Tooltip("Creates PlaybackComponent on OnMediaAdded events")]
    [SerializeField]
    public bool CreatePlayback = false;
    [Tooltip("Add a AudioMixerGroup to all added PlaybackSources. Ignored when empty.")]
    [SerializeField]
    public AudioMixerGroup PlaybackAudioMixerGroup;

    void Awake()
    {
        _instance = this;
        if (_persistent)
            DontDestroyOnLoad(gameObject);
        Identifier = SystemInfo.deviceUniqueIdentifier;

        MediaAddedQueue = new ConcurrentQueue<KeyValuePair<Room, MediaAddedEvent>>();
        MediaRemovedQueue = new ConcurrentQueue<KeyValuePair<Room, MediaRemovedEvent>>();

        UserData userData = new UserData(Config.UserDataText);
        if (userData.IsEmpty())
            userData = new OdinUserData().ToUserData();

        if (string.IsNullOrEmpty(Config.ApiKey))
        {
            Debug.LogError("Api-Key was not set!");
            Config.ApiKey = OdinClient.CreateApiKey();
            Debug.LogWarning("Using a generated test key!");
        }

        Client = new OdinClient(new System.Uri(Config.Server), Config.ApiKey, userData);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.quitting += () => { Client?.Shutdown(); };
#endif
    }

    void Start()
    {
        try
        {
            Client.Startup();
        }
        catch (System.DllNotFoundException e)
        {
            Corrupted = true;
            Debug.LogError("Native Plugin libraries for Unity corrupted!");
            Debug.LogException(e);
            Destroy(this);
            return;
        }

        if (Microphone == null)
            Microphone = gameObject.AddComponent<MicrophoneReader>();
    }

    public OdinUserData GetUserData()
    {
        return OdinUserData.FromUserData(Client.UserData);
    }

    /// <summary>
    /// Join or create a room by name and attach a <see cref="MicrophoneStream"/>
    /// </summary>
    /// <remarks>Configure event liseners with <see cref="Config"/></remarks>
    /// <param name="name">Room name</param>
    public async void JoinRoom(string roomName, string userId, string customerId = "", System.Action<Room> setup = null)
    {
        if (Client.Rooms[roomName] != null)
        {
            Debug.LogError($"Room {roomName} already joined!");
            return;
        }

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
                    NoiseSuppsressionLevel = cfg.NoiseSuppressionLevel,
                    TransientSuppressor = cfg.TransientSuppressor,
                });
            };

        Room room = await Client.JoinRoom(roomName, userId, customerId, setup);

        if (room == null || room.IsJoined == false)
        {
            Debug.LogError($"Room {roomName} join failed!");
            return;
        }

        Debug.Log($"Room {room.Config.Name} joined as {userId}.");
        if (room.CreateMicrophoneMedia(new OdinNative.Core.OdinMediaConfig(Microphone.SampleRate, Config.DeviceChannels)))
            Debug.Log($"MicrophoneStream added to room {roomName}.");
    }

    /// <summary>
    /// Leave and free the <see cref="Room"/> by name
    /// </summary>
    /// <param name="roomName">Room name</param>
    public async void LeaveRoom(string roomName)
    {
        if (await Client.LeaveRoom(roomName) == false)
            Debug.LogWarning($"Room {roomName} not found!");
    }

    /// <summary>
    /// Peer joins the room.
    /// </summary>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">PeerJoined Args</param>
    protected virtual void Room_OnPeerJoined(object sender, PeerJoinedEvent e)
    {
        if (Config.Verbose)
        {
            Debug.Log("Room: " + (sender as Room).Config.Name);
            Debug.Log(string.Format("User added {0} with {1}", e.Peer, e.Peer.UserData));
        }
        Room room = sender as Room;
        if (room.Self == null)
            if (e.Peer.UserData.Contains(Identifier))
                room.Self = e.Peer;
    }

    /// <summary>
    /// Peer left the room.
    /// </summary>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">PeerLeft Args</param>
    protected virtual void Room_OnPeerLeft(object sender, PeerLeftEvent e)
    {
        if (Config.Verbose)
        {
            Debug.Log("Room: " + (sender as Room).Config.Name);
            Debug.Log(string.Format("User left {0}", e.PeerId));
        }
    }

    /// <summary>
    /// Peer updated userdata.
    /// </summary>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">PeerUpdated Args</param>
    protected virtual void Room_OnPeerUpdated(object sender, PeerUpdatedEvent e)
    {
        if (Config.Verbose)
        {
            Debug.Log("Room: " + (sender as Room).Config.Name);
            Debug.Log(string.Format("User {0} updated {1}", e.PeerId, e.UserData));
        }
    }

    /// <summary>
    /// Audio/Video stream added in the room.
    /// </summary>
    /// <remarks>The remote <see cref="MediaStream"/> is always a <see cref="PlaybackStream"/> and readonly.</remarks>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">MediaAdded Args</param>
    protected virtual void Room_OnMediaAdded(object sender, MediaAddedEvent e)
    {
        if (Config.Verbose)
        {
            Debug.Log("Room: " + (sender as Room).Config.Name);
            Debug.Log(string.Format("add Media: {0} PlaybackId: {1} to Peer: {2}", e.Media, e.Media.Id, e.Peer));
        }

        // Push for unity thread
        MediaAddedQueue.Enqueue(new KeyValuePair<Room, MediaAddedEvent>(sender as Room, e));
    }

    /// <summary>
    /// Room audio/video stream is closed in the room.
    /// </summary>
    /// <remarks>Peer and Media in <see cref="MediaRemovedEvent"/> is null if the peer left before the owned Medias are removed</remarks>
    /// <param name="sender"><see cref="Room"/> object</param>
    /// <param name="e">MediaRemoved Args with MediaId</param>
    protected virtual void Room_OnMediaRemoved(object sender, MediaRemovedEvent e)
    {
        if (Config.Verbose)
        {
            Debug.Log("Room: " + (sender as Room).Config.Name);
            Debug.Log(string.Format("removed Media: {0} from Peer: {1}", e.MediaId, e.Peer));
        }

        // Push for unity thread
        MediaRemovedQueue.Enqueue(new KeyValuePair<Room, MediaRemovedEvent>(sender as Room, e));
    }

    private void FixedUpdate()
    {
        if (Corrupted) return;

        HandleNewMediaQueue();
        HandleRemoveMediaQueue();
    }

    private void HandleNewMediaQueue()
    {
        if (MediaAddedQueue.TryDequeue(out KeyValuePair<Room, MediaAddedEvent> addedEvent))
        {
            OnCreateMediaObject?.Invoke(addedEvent.Key.Config.Name, addedEvent.Value.Peer.Id, addedEvent.Value.Media.Id);

            if (CreatePlayback)
                if (Use3DAudio)
                    AssignPlaybackComponent(UnityAudioSourceTag, addedEvent);
                else
                    AssignPlaybackComponent(this.gameObject, addedEvent);
            else if(Config.Verbose)
                Debug.LogWarning($"No available consumers for playback found.");

            OnCreatedMediaObject?.Invoke(addedEvent.Key, addedEvent.Value.Peer, addedEvent.Value.Media);
        }
    }

    private PlaybackComponent AssignPlaybackComponent(string gameObjectTag, KeyValuePair<Room, MediaAddedEvent> addedEvent)
    {
        return AssignPlaybackComponent(GetPeerContainer(gameObjectTag),
            addedEvent.Key.Config.Name,
            addedEvent.Value.Peer.Id,
            addedEvent.Value.Media.Id);
    }

    private PlaybackComponent AssignPlaybackComponent(GameObject peerContainer, KeyValuePair<Room, MediaAddedEvent> addedEvent)
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
            Debug.Log($"No game objects are tagged with '{gameObjectTag}'");

        return gameObjects.LastOrDefault();
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
        if (MediaRemovedQueue.TryDequeue(out KeyValuePair<Room, MediaRemovedEvent> removedEvent))
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

    void OnDestroy()
    {
        if (Corrupted) return;

        Client?.Close();
    }

    void OnApplicationQuit()
    {
        if (Corrupted) return;

        Client.Shutdown();
        Client.Dispose();
    }
}
