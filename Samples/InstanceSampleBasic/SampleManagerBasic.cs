using OdinNative.Unity;
using OdinNative.Unity.Audio;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using OdinNative.Wrapper.Peer;
using OdinNative.Wrapper.Room;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// This sample is to showcase a basic version of the wrapper
/// </summary>
/// <remarks>For a GameObject/Component sample checkout the 2D or 3D versions</remarks>
public class SampleManagerBasic : MonoBehaviour
{
    public Room _Room;
    public string Gateway;
    public string AccessKey;
    public string RoomName;
    public string UserId;

    public OdinMicrophoneReader _AudioInput;
    public static readonly ConcurrentQueue<Action> UnityQueue = new ConcurrentQueue<Action>();

    void Reset()
    {
        Gateway = "https://gateway.odin.4players.de";
        RoomName = "Test";
        UserId = "DummyUsername";
    }

    // Start is called before the first frame update
    void Start()
    {
        // For persistent room with scene switches
        //DontDestroyOnLoad(this.gameObject);

        if (_Room == null)
        {
            // Create a default room, there is no need to setup samplerate/channels Unity will compose audio data
            _Room = Room.Create(Gateway);

            // Do NOT use in any circumstances Unity functions in base event callbacks use a dispatch instead
            // or use MonoBehaviour versions like OdinRoom that passthrough invokes to UnityEvents
            // but this sample is to showcase a basic version; For a GameObject/Component sample checkout InstanceSample2D
            _Room.OnRoomJoined += Example_OnRoomJoined;
            _Room.OnMediaStarted += Example_OnMediaStarted;
            _Room.OnMediaStopped += Example_OnMediaStopped;
            _Room.OnMessageReceived += Example_OnMessageLog;

            // most likely use some kind of remote token request like UnityWebRequest
            // Create token local for showcase purpose only => ACCESSKEY SHOULD NOT BE CLIENT SIDE!
            DateTime utc = DateTime.UtcNow;
            _Room.Join(ExampleKey(new ExampleTokenBody()
            {
                rid = RoomName,
                uid = UserId,
                nbf = ((DateTimeOffset)utc).ToUnixTimeSeconds(),
                exp = ((DateTimeOffset)utc.AddMinutes(5)).ToUnixTimeSeconds() // 5min valid
            }.ToString(), AccessKey));
        }
    }

    private void Example_OnRoomJoined(object sender, ulong ownPeerId, string name, string customer, byte[] roomUserData, ushort[] mediaIds, ReadOnlyCollection<PeerRpc> peers)
    {
        Debug.Log($"Joined room \"{name}\" with {peers.Count} peers");

        if (_Room == null)
        {
            Debug.LogError($"Can not create encoder without a room");
            return;
        }

        // each room has a limited number of ids reserved for encoders i.e audio input / capture
        if (_Room.AvailableEncoderIds.TryDequeue(out ushort mediaId))
            CreateCapture(mediaId);
        else
            Debug.LogError($"Can not create encoder without a encoder id that is free to use");

        // add already existing medias
        foreach (var peer in _Room.RemotePeers.Values)
            foreach(var decoder in peer.Medias.Values)
                CreatePlayback(decoder, peer);
    }

    private void Example_OnMediaStarted(object sender, ulong peerId, MediaRpc media)
    {
        Debug.Log($"Peer {peerId} started media {media.Id}");

        if (_Room == null)
        {
            Debug.LogError($"Can not add media effects to pipeline without a room in {nameof(Example_OnMediaStarted)}");
            return;
        }

        // in default setup the room creates decoders intern automatically on the event
        // get the decoder corresponding to a peer
        if (_Room.RemotePeers.TryGetValue(peerId, out PeerEntity peer))
            if (peer.Medias.TryGetValue(media.Id, out MediaDecoder decoder))
                CreatePlayback(decoder, peer);
    }

    private void Example_OnMediaStopped(object sender, ulong peerId, ushort mediaId)
    {
        Debug.Log($"Peer {peerId} removed media {mediaId}");

        DispatchDestroyAudioSource($"OdinDecoder {mediaId}");
    }

    private void Example_OnMessageLog(object sender, ulong peerId, byte[] message)
    {
        Debug.Log($"Room \"{(sender as Room).Name}\" got message ({message.Length} bytes) from peer {peerId}: \"{Encoding.UTF8.GetString(message)}\"");
    }

    private void ProxyAudio(float[] buffer, int _, bool isSilent)
    {
        if (_Room == null) return;
        // send audio to all encoders in the current room
        // or pop/push audio directly on the encoder/decoder
        foreach (var kvp in _Room.Encoders)
            _Room.SendAudio(buffer, kvp.Key, isSilent);
    }

    public void CreateCapture(ushort mediaId)
    {
        if( _Room == null) return;

        // create a encoder where to send audio data to
        if (_Room.GetOrCreateEncoder(mediaId, out MediaEncoder encoder))
        {
            // created encoders have to be started with a customizable rpc call
            _Room.StartMedia(encoder);
            // set a callback for the MicrophoneReader and add sample effects to the pipeline
            LinkEncoderToMicrophone(encoder);
        }
    }

    private void LinkEncoderToMicrophone(MediaEncoder encoder)
    {
        UnityQueue.Enqueue(() =>
        {
            // this sample does not set PersistentListener direct or by prefab
            if (_AudioInput && _AudioInput.OnAudioData?.GetPersistentEventCount() <= 0 && _Room != null)
                _AudioInput.OnAudioData.AddListener(ProxyAudio);

            OdinMicrophoneReader microphone = GetComponent<OdinMicrophoneReader>();
            if (microphone != null)
            {
                // optionally add effects to the encoder (Input/Capture)

                // add voice activity detection
                OdinVadComponent vadComponent = microphone.gameObject.AddComponent<OdinVadComponent>();
                vadComponent.Media = encoder;
                // add a microphone boost
                OdinVolumeBoostComponent volumeBoostComponent = microphone.gameObject.AddComponent<OdinVolumeBoostComponent>();
                volumeBoostComponent.Media = encoder;
                // add a microphone mute
                OdinMuteAudioComponent muteComponent = microphone.gameObject.AddComponent<OdinMuteAudioComponent>();
                muteComponent.Media = encoder;
            }
        });
    }

    /// <summary>
    /// Add show how
    /// </summary>
    public void CreatePlayback(MediaDecoder decoder, PeerEntity peer)
    {
        // EXAMPLE optionally add INTERNAL effects to the decoder like VAD (Output/Playback)
        MediaPipeline pipeline = decoder.GetPipeline();
        if (pipeline.AddVadEffect(out _))
            Debug.Log($"added {nameof(VadEffect)} to \"OdinDecoder {decoder.Id}\" of peer {peer.Id}");

        // Odin uses Unity to play the audio
        DispatchCreateAudioSource(decoder, peer);
    }

    /// <summary>
    /// Add OdinMedia that handles <see cref="AudioSource"/> and copy data from Odin to <see cref="AudioClip"/>
    /// </summary>
    /// <remarks>optionally <see cref="MediaDecoder.Pop"/> samples can be used with <see cref="AudioClip.SetData"/></remarks>
    private void DispatchCreateAudioSource(MediaDecoder decoder, PeerEntity peer)
    {
        UnityQueue.Enqueue(() =>
        {
            GameObject container = new GameObject($"OdinDecoder {decoder.Id}");
            container.transform.parent = transform;
            OdinMedia mediaComponent = container.AddComponent<OdinMedia>();
            mediaComponent.MediaDecoder = decoder; // set the decoder to copy data from
            mediaComponent.Parent = peer; // the use of OdinMedia requires a parent else it is optional
            mediaComponent.enabled = true;
            mediaComponent.OnActiveStateChanged.AddListener(Example_OnActiveStateChanged);

            // optionally add interal effects wrapped with Unity to the decoder (Output/Playback)
            // for audio pipeline manipulation

            // add a playback volume boost
            OdinVolumeBoostComponent volumeBoostComponent = container.AddComponent<OdinVolumeBoostComponent>();
            volumeBoostComponent.Media = mediaComponent;
            volumeBoostComponent.Scale = 1.0f; // set to none boost value
            // add a playback mute toggle component
            OdinMuteAudioComponent muteComponent = container.AddComponent<OdinMuteAudioComponent>();
            muteComponent.Media = mediaComponent;
            /* see other Effects or build one with CustomEffect (PipelineEffect) or with Unity helper class OdinCustomEffectUnityComponentBase */
        });
    }

    private void Example_OnActiveStateChanged(object sender, MediaActiveStateChangedEventArgs args)
    {
        OdinMedia unityMediaComponent = sender as OdinMedia;
        Debug.Log($"\"{unityMediaComponent.name}\" Activity from peer {args.PeerId} on media {args.MediaId} is {args.Active}");
    }

    /// <summary>
    /// Disposing objects is necessary to prevent memory leaks
    /// </summary>
    /// <remarks>To cleaup Unity versions of Odin components just destroy on gameObject to automatically dispose them <see cref="OnDestroy"/></remarks>
    private void DispatchDestroyAudioSource(string gameObjectName)
    {
        UnityQueue.Enqueue(() =>
        {
            OdinMedia mediaComponent = this.gameObject
                .GetComponentsInChildren<OdinMedia>()
                .FirstOrDefault(component => component.name == gameObjectName);

            if (mediaComponent != null)
                Destroy(mediaComponent.gameObject);
        });
    }

    private void Update()
    {
        if (UnityQueue.IsEmpty == false)
            while (UnityQueue.TryDequeue(out var action))
                action?.Invoke();
    }

    private void OnDestroy()
    {
        if( _Room != null )
        {
            _Room.Dispose();
            _Room = null;
        }
    }

    #region ExampleToken
    [Serializable]
    class ExampleTokenBody
    {
        public string rid;
        public string uid;
        public long nbf;
        public long exp;

        public override string ToString() => JsonUtility.ToJson(this);
    }

    private string ExampleKey(string body, string accesskey = "")
    {
        Debug.LogAssertion("The access key should never be used client side and is for showcase purpose only!");

        if (string.IsNullOrEmpty(accesskey))
        {
            string currentKey = OdinClient.CreateAccessKey();
            Debug.LogWarning($"Generated example key: \"{currentKey}\"");
            return OdinClient.CreateToken(currentKey, body);
        }
        else
        {
            Debug.LogWarning($"Using example key: \"{accesskey}\"");
            return OdinClient.CreateToken(accesskey, body);
        }
    }
    #endregion ExampleToken
}
