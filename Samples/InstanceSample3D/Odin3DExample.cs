using OdinNative.Unity.Audio;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Room;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OdinNative.Unity.Samples
{
    public class Odin3DExample : MonoBehaviour
    {
        public string AccessKey;
        public string RoomName;
        public GameObject RoomPrefab;
        public OdinRoom[] RoomsObjects;
        public GameObject PeersPrefab;
        public ConcurrentDictionary<ulong, GameObject> PeersObjects = new ConcurrentDictionary<ulong, GameObject>();
        private Color LastCubeColor;
        private UserData TestSelfUserdata;
        private OdinRoom _Room;
        private uint UnitySamplerate;
        private bool UnityIsStereo;
        [Space(10)]
        public bool AddExampleCustomComponent = false;

        /// <summary>
        /// Get Unity <see cref="AudioSettings"/> for playback to set the correct decoder. <seealso href="https://docs.unity3d.com/ScriptReference/AudioSettings.html"/>
        /// </summary>
        /// <remarks>we recommend to check the <see href="https://docs.unity3d.com/ScriptReference/AudioSettings-driverCapabilities.html">driverCapabilities</see> on input for encoder and output for decoder</remarks>
        private void Awake()
        {
            UnitySamplerate = AudioSettings.outputSampleRate  == 0 ? 48000 : (uint)AudioSettings.outputSampleRate;
            UnityIsStereo = AudioSettings.speakerMode >= AudioSpeakerMode.Stereo;
        }

        /// <summary>
        /// Init with two examples on how to join a room
        /// 1. Instantiate prefab and uses event callbacks in editor UI
        /// 2. Add component and manually add event callbacks
        /// </summary>
        void Start()
        {
            TestSelfUserdata = new CustomUserDataJsonFormat().ToUserData(); // convert to serialize SystemInfo/Application

            //Set Player
            GameObject player = GameObject.FindGameObjectsWithTag("Player").FirstOrDefault();
            if (player != null)
            {
                TextMesh label = player.GetComponentInChildren<TextMesh>();
                label.text = CustomUserDataJsonFormat.FromUserData(TestSelfUserdata)?.name ?? player.name;
            }


            if (RoomPrefab)
            {
                //1. Join prefab room as child object
                GameObject prefabRoom = Instantiate(RoomPrefab, this.gameObject.transform);
                _Room = prefabRoom.GetComponent<OdinRoom>();
            }
            else
            {
                //2. Join a room as child object without prefab
                GameObject containerObject = new GameObject(RoomName);
                _Room = containerObject.AddComponent<OdinRoom>();
                _Room.gameObject.transform.parent = this.gameObject.transform;
            }
            
            RoomsObjects = new OdinRoom[] { _Room };
            // We generate a test token. Use a token-server see WebRequestToken
            _Room.Token = OdinRoom.GenerateTestToken(RoomName, player?.name ?? "Username", 5, AccessKey);

            _Room.OnRoomJoined.AddListener(Example_OnRoomJoined);
            _Room.OnPeerJoined.AddListener(Example_OnPeerJoined);
            _Room.OnPeerLeft.AddListener(Example_OnPeerLeft);
            _Room.OnMediaAdded.AddListener(Example_OnCreatedMediaObject);
            _Room.OnMediaRemoved.AddListener(Example_OnDeleteMediaObject);
            _Room.OnMessageReceived.AddListener(Example_OnMessageLog);

            SetupUnityMicrophoneInput();
        }

        /// <summary>
        /// We attach the Unity <see cref="Microphone"/> but could use an arbitary input like <see cref="OdinAudioReader"/>
        /// </summary>
        /// <remarks>This is rudimentary setup for flexibility use <see cref="Room.SendAudio"/> or manually <see cref="MediaEncoder.Push"/> and <see cref="MediaEncoder.Pop"/></remarks>
        private void SetupUnityMicrophoneInput()
        {
            // hook up the audio input data
            var mic = GetComponentInChildren<OdinMicrophoneReader>();
            // we send audio to all encoders
            mic.OnAudioData.AddListener(_Room.ProxyAudio);
        }

        /// <summary>
        /// Show how to start a capture media
        /// </summary>
        /// <remarks>this can fail if there is no reserved available media id left in this room</remarks>
        /// <param name="sender">room where the event is dispatched from</param>
        /// <param name="args">event args that contains internal room</param>
        public void Example_OnRoomJoined(object sender, RoomJoinedEventArgs args)
        {
            Debug.Log($"RoomJoined : \"{(args.Room as Room).Name}\"");

            var mic = GetComponentInChildren<OdinMicrophoneReader>();
            if (mic == null)
            {
                Debug.LogError($"Missing required {nameof(OdinMicrophoneReader)} component");
                return;
            }

            // Example to create and start an encoder with room api
            //OdinRoom room = sender as OdinRoom;
            //Room roomApi = room.GetBaseRoom();
            //if (roomApi.AvailableEncoderIds.TryDequeue(out var mediaId))
            //{
            //    if (roomApi.GetOrCreateEncoder(mediaId, (uint)mic.MicrophoneSamplerate, mic.MicrophoneChannels > 1, out MediaEncoder encoder))
            //        roomApi.StartMedia(encoder);
            //}

            // Example to create and start encoder for input 
            OdinRoom room = sender as OdinRoom;
            if (room.LinkInputMedia((uint)mic.MicrophoneSamplerate, mic.MicrophoneChannels > 1, out MediaEncoder encoder))
            {
                /* 
                 * processing the audio pipline can have many different effects and are called in order
                 * add either a buildin component or directly with the pipeline and/or build one
                 * 
                 * an effect is PiplineEffect : ICustomPiplineEffect and can work with any IMedia 
                 * i.e encoder/decoder; capture/playback
                 * 
                 * to remove a component effect just destroy the component
                 * on direct access to the pipeline with effects use AddCustomEffect/RemoveEffect
                 * (see MediaPipeline.RemoveEffect)
                */

                // add a build in voice activity detection component if available
                // look for components where the microphone reader is
                OdinVadComponent vad = mic.gameObject.GetComponent<OdinVadComponent>();
                if(vad != null)
                    vad.Media = encoder;

                // add a IOdinEffect custom component if available
                OdinVolumeBoostComponent volumeBoost = mic.gameObject.GetComponent<OdinVolumeBoostComponent>();
                if(volumeBoost != null)
                    volumeBoost.Media = encoder;
            }
            else
                Debug.LogError($"can not create an encoder and/or start media!");
        }

        /// <summary>
        /// if a peer joined we create a gameobject based on the <see cref="PeersPrefab"/> and attach a new <see cref="OdinPeer"/> component
        /// </summary>
        /// <remarks>Note that the prefab can already contain a <see cref="OdinPeer"/> component but needs to be manually configured in a script on the prefab or here</remarks>
        /// <param name="sender">room where the event is dispatched from</param>
        /// <param name="args">event args that contains native infos from the event</param>
        public void Example_OnPeerJoined(object sender, PeerJoinedEventArgs args)
        {
            Debug.Log($"PeerJoined : {args.PeerId}");

            OdinRoom room = sender as OdinRoom;
            var peerContainer = Instantiate(PeersPrefab, new Vector3(0, 0.5f, 6), Quaternion.identity);
            room.AddPeerComponent(peerContainer, args.PeerId);

            PeersObjects.TryAdd(args.PeerId, peerContainer);

            //set dummy PeerCube label
            var data = CustomUserDataJsonFormat.FromUserData(room.GetBaseRoom<Room>().RemotePeers[args.PeerId]?.UserData);
            peerContainer.GetComponentInChildren<TextMesh>().text = data == null ?
                $"Peer {args.UserId} ({args.PeerId})" :
                $"{data.name} (Peer {args.PeerId})";
        }

        /// <summary>
        /// Destroy on the gameobject or component
        /// </summary>
        /// <param name="sender">room where the event is dispatched from</param>
        /// <param name="args">event args that contains native infos from the event</param>
        public void Example_OnPeerLeft(object sender, PeerLeftEventArgs args)
        {
            Debug.Log($"PeerLeft : {args.PeerId}");

            if (PeersObjects.TryGetValue(args.PeerId, out var peerContainer))
            {
                PeersObjects.TryRemove(args.PeerId, out _);
                Destroy(peerContainer);
            }
        }

        /// <summary>
        /// redirect the event and let the <see cref="OdinPeer"/> handle the media
        /// if a peer starts a new media we can create and configure the <see cref="OdinMedia"/> component
        /// </summary>
        /// <remarks>we use the helper function <see cref="OdinPeer.AddMediaComponent"/> either direct or with default eventhandler but a <see cref="GameObject.AddComponent{T}()"/> with a <see cref="OdinMedia"/> that needs to be manually configured works too</remarks>
        /// <param name="sender">room where the event is dispatched from</param>
        /// <param name="args">event args that contains native infos from the event</param>
        public void Example_OnCreatedMediaObject(object sender, MediaAddedEventArgs args)
        {
            if (PeersObjects.TryGetValue(args.PeerId, out var peerContainer))
            {
                SetExamplePeerLabel(sender, peerContainer, args.PeerId, args.MediaId);

                var peer = peerContainer.GetComponent<OdinPeer>();
                
                // example let peer handle event
                //peer.OnMediaAdded.Invoke(sender, args);
                //return;

                // example setup media
                GameObject mediaObject = new GameObject(args.MediaId.ToString());
                mediaObject.transform.parent = peerContainer.transform;

                // we created OdinMedia with AddMediaComponent(...,enable: false) as disabled to handle custom changes first
                var media = peer.AddMediaComponent(mediaObject, args.MediaId, UnitySamplerate, UnityIsStereo, enable: false);
                // enable positional audio for AudioSource (AudioSource.spatialBlend)
                media.SpatialBlend = 1.0f;
                // listener to handle voice activity
                media.OnActiveStateChanged.AddListener(Example_MediaActivityChanged);
                // this example playfield pane is small for default audible changes
                media.RolloffMode = AudioRolloffMode.Linear; // (AudioSource.RolloffMode)
                media.MaxDistance = 10.0f; // (AudioSource.MaxDistance)
                // after we setup enable the component
                media.enabled = true; // or SetActive
                // optionally add effects outside of Unity AudioMixer for Playback (MediaDecoder)
                //media.AddVad();
            }
        }

        /// <summary>
        /// show
        /// </summary>
        /// <param name="sender">OdinMedia component</param>
        /// <param name="args">event args that contains infos from the audio with changed activity</param>
        private void Example_MediaActivityChanged(object sender, MediaActiveStateChangedEventArgs args)
        {
            Debug.Log($"Media {args.MediaId} on peer {args.PeerId} changed activity to {args.Active}");

            OdinMedia media = sender as OdinMedia;
            Material cubeMaterial = media.GetComponentInParent<Renderer>().material;
            if (args.Active)
            {
                LastCubeColor = cubeMaterial.color;
                cubeMaterial.color = Color.green;
            }
            else
                cubeMaterial.color = LastCubeColor;
        }

        /// <summary>
        /// we only update the label - redirect the event and let the <see cref="OdinPeer"/> handle the media
        /// if a still existing peer removes his media we remove it as well
        /// <code>
        /// // simple example
        /// OdinMedia mediaComponent = peerContainer.GetComponent{OdinMedia}();
        /// Destroy(mediaComponent);
        /// </code>
        /// </summary>
        /// <remarks>we use the helper function <see cref="OdinPeer.RemoveMediaComponent"/> either direct or with default eventhandler but a <see cref="GameObject.GetComponent{T}()"/> with a <see cref="OdinMedia"/> that needs to be destroyed works too</remarks>
        /// <param name="sender">room where the event is dispatched from</param>
        /// <param name="args">event args that contains native infos from the event</param>
        public void Example_OnDeleteMediaObject(object sender, MediaRemovedEventArgs args)
        {
            if (PeersObjects.TryGetValue(args.PeerId, out var peerContainer))
            {
                if (peerContainer.gameObject == null) return; // already destroyed gameobject
                SetExamplePeerLabel(sender, peerContainer, args.PeerId);

                var peer = peerContainer.GetComponent<OdinPeer>();
                if (peer == null) return; // already destroyed component
                peer.OnMediaRemoved.Invoke(sender, args);
            }
        }

        // only for showcase
        private static void SetExamplePeerLabel(object sender, GameObject peerContainer, ulong peerid, ushort mediaid = 0)
        {
            //set dummy PeerCube label
            var data = CustomUserDataJsonFormat.FromUserData((sender as OdinRoom).GetBaseRoom<Room>().RemotePeers[peerid]?.UserData);
            if(mediaid > 0)
                peerContainer.GetComponentInChildren<TextMesh>().text = data == null ?
                    $"Peer {peerid} (Media {mediaid})" :
                    $"{data.name} (Peer {peerid} Media {mediaid})";
            else
                peerContainer.GetComponentInChildren<TextMesh>().text = data == null ?
                    $"Peer {peerid}" :
                    $"{data.name} (Peer {peerid})";
        }

        /// <summary>
        /// just log a message received from a peer, for simplicity we just convert it to string but could be anything
        /// </summary>
        /// <remarks><see cref="MessageReceivedEventArgs.Data"/> is arbitrary data send by <see cref="Room.SendMessage(string)"/></remarks>
        /// <param name="sender">room where the event is dispatched from</param>
        /// <param name="args">event args that contains native infos from the event</param>
        public void Example_OnMessageLog(object sender, MessageReceivedEventArgs args)
        {
            string message = Encoding.UTF8.GetString(args.Data);
            Debug.Log($"{nameof(Example_OnMessageLog)} from peer {args.PeerId}: \"{message}\"");
        }

        /// <summary>
        /// to leave a room just destroy the gameobject/component
        /// </summary>
        public void Example_RoomLeave()
        {
            var mic = GetComponent<OdinMicrophoneReader>();
            if(mic != null)
                mic.OnAudioData.RemoveListener(_Room.ProxyAudio);

            OdinRoom room = RoomsObjects.FirstOrDefault();
            Destroy(room);
            RoomsObjects = new OdinRoom[0];
        }

        /// <summary>
        /// example how to cleanup this example
        /// </summary>
        private void OnDestroy()
        {
            foreach (var obj in PeersObjects.Values)
                Destroy(obj);
            PeersObjects.Clear();

            foreach (var obj in RoomsObjects)
                Destroy(obj);
        }
    }
}