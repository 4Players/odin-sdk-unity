using OdinNative.Core;
using OdinNative.Unity.Audio;
using OdinNative.Unity.Events;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using OdinNative.Wrapper.Room;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity
{
    /// <summary>
    /// Freestanding encoder component that captures audio from an <see cref="AudioProvider"/> and sends it to an <see cref="OdinRoom"/>.
    /// <para>
    /// Place this anywhere in the scene. Assign <see cref="Room"/> and optionally an <see cref="AudioProvider"/>
    /// (<see cref="OdinMicrophoneReader"/> or <see cref="OdinAudioReader"/>). Audio can also be pushed manually
    /// via <see cref="PushAudio"/> wired through an Inspector UnityEvent or called directly.
    /// </para>
    /// Default behaviour:
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="CreateEncoder"/></term>
    /// <description>Called automatically when the room is joined</description>
    /// </item>
    /// <item>
    /// <term><see cref="PushAudio"/></term>
    /// <description>Subscribed to <see cref="AudioProvider"/> <c>OnAudioData</c> when set</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>Create a custom component with <see cref="OdinNative.Wrapper.Media.IMedia"/> or inheritance from this class and extend/override.</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity/OdinEncoder/")]
    [AddComponentMenu("Odin/Instance/OdinEncoder")]
    public class OdinEncoder : MonoBehaviour, Wrapper.Media.IMedia
    {
        /// <summary>
        /// The <see cref="OdinRoom"/> this encoder sends audio to
        /// </summary>
        public OdinRoom Room;
        /// <summary>
        /// Optional audio source: <see cref="OdinMicrophoneReader"/> or <see cref="OdinAudioReader"/>.
        /// When set, <see cref="PushAudio"/> is subscribed to its <c>OnAudioData</c> event automatically in <see cref="OnEnable"/>.
        /// </summary>
        public MonoBehaviour AudioProvider;

        /// <summary>
        /// Input PCM samplerate. Known audio providers supply their actual clip format; zero defaults to OdinRoom.OutputSampleRate.
        /// </summary>
        public uint Samplerate;
        /// <summary>
        /// Encoder stereo flag
        /// </summary>
        public bool Stereo;

        [field: SerializeField]
        public ulong Id { get; internal set; }
        /// <summary>
        /// The owned <see cref="MediaEncoder"/>
        /// </summary>
        public MediaEncoder Encoder { get; private set; }
        /// <summary>
        /// Flag to dispose of <see cref="Encoder"/> on destroy
        /// </summary>
        public bool AutoDestroyMediaStream = true;

        public MediaPipeline GetPipeline() => Encoder?.Pipeline ?? null;
        public Utility.ChannelMask ChannelMask => Encoder?.ChannelMask ?? Utility.ChannelMask.None;
        public OdinPosition Position => default;
        /// <summary>
        /// The local peer this encoder sends audio as. Set when the encoder is linked to a room.
        /// </summary>
        public IPeer Parent { get; internal set; }

        void Awake()
        {
            if (Samplerate == 0) Samplerate = OdinRoom.OutputSampleRate;
        }

        void OnEnable()
        {
            if (AudioProvider is OdinMicrophoneReader mic)
            {
                mic.OnAudioData.AddListener(PushAudio);
            }
            else if (AudioProvider is OdinAudioReader reader)
                reader.OnAudioData.AddListener(PushAudio);

            if (Room != null)
            {
                Room.OnRoomJoined.AddListener(OnRoomJoined_CreateEncoder);
                if (Room.IsJoined)
                    CreateEncoder();
            }
        }

        void Reset()
        {
            AutoDestroyMediaStream = true;
        }

        /// <summary>
        /// Called when the linked <see cref="OdinRoom"/> fires its joined event
        /// </summary>
        /// <param name="sender">OdinRoom object</param>
        /// <param name="args">room joined data</param>
        private void OnRoomJoined_CreateEncoder(object sender, RoomJoinedEventArgs args)
        {
            CreateEncoder();
        }

        /// <summary>
        /// Create and start the <see cref="MediaEncoder"/> in the linked room.
        /// Called automatically when the room joins; safe to call manually after join.
        /// </summary>
        public void CreateEncoder()
        {
            if (Room == null || !Room.IsJoined) return;
            if (!ResolveInputFormat()) return;
            if (Encoder != null && Encoder.IsAlive && Encoder.Samplerate == Samplerate && Encoder.Stereo == Stereo) return;
            var old = Encoder;

            if (Room.LinkInputMedia(Samplerate, Stereo, out var encoder))
            {
                if (old != null)
                {
                    encoder.SetChannels(old.ChannelMask);
                    if (old.ChannelMask != Utility.ChannelMask.None)
                        encoder.SetPosition(old.ChannelMask, old.Position);
                }
                Encoder = encoder;
                if (old != null) Room.UnlinkInputMedia(old);
                Id = encoder.Id;
                Parent = Room?.Self;

                if (old == null && TryGetComponent(out OdinAudioMap map) && (map.MapSelector & OdinAudioMap.MapType.Encoder) != 0)
                    Encoder.SetChannels(map.ChannelMask);
            }
            else
                OdinLog.LogError($"{nameof(OdinEncoder)} on \"{gameObject.name}\" could not create encoder in room");
        }

        private bool ResolveInputFormat()
        {
            int channels = Stereo ? 2 : 1;
            if (AudioProvider is OdinMicrophoneReader mic)
            {
                Samplerate = (uint)mic.MicrophoneSamplerate;
                channels = mic.MicrophoneChannels;
            }
            else if (AudioProvider is OdinAudioReader reader)
            {
                if (reader.InputClip == null) return false;
                Samplerate = (uint)reader.InputClip.frequency;
                channels = reader.InputClip.channels;
            }
            if (channels != 1 && channels != 2)
            {
                OdinLog.LogError($"{nameof(OdinEncoder)} needs mono or stereo PCM; downmix the {channels}-channel source first.");
                return false;
            }
            Stereo = channels == 2;
            return Samplerate > 0;
        }

        /// <summary>
        /// Push audio samples into the encoder pipeline and send to the server.
        /// Wire this to an <see cref="AudioProvider"/>'s <c>OnAudioData</c> event or call it directly.
        /// </summary>
        /// <param name="buffer">audio samples</param>
        /// <param name="position">read position in the source buffer</param>
        /// <param name="isSilent">silence flag</param>
        public virtual void PushAudio(float[] buffer, int position, bool isSilent)
        {
            if (Room == null || Room.IsJoined == false) return;
            // A microphone restart or clip replacement can change the input format. Resolve it
            // immediately before the first new buffer, not from the output-device notification.
            if (!ResolveInputFormat()) return;
            CreateEncoder();
            if (Encoder == null || !Encoder.IsAlive || Encoder.Samplerate != Samplerate || Encoder.Stereo != Stereo) return;
            Room.SendAudio(buffer, Encoder, isSilent);
        }

        public T AddEffect<T>() where T : MonoBehaviour, IOdinEffect
        {
            T effectComponent = gameObject.AddComponent<T>();
            effectComponent.Media = this;
            return effectComponent;
        }

        void OnDisable()
        {
            if (AudioProvider is OdinMicrophoneReader mic)
                mic.OnAudioData.RemoveListener(PushAudio);
            else if (AudioProvider is OdinAudioReader reader)
                reader.OnAudioData.RemoveListener(PushAudio);

            if (Room != null)
            {
                Room.OnRoomJoined.RemoveListener(OnRoomJoined_CreateEncoder);
                if (Encoder != null)
                    Room.UnlinkInputMedia(Encoder);
            }

            Encoder = null;
        }

        void OnDestroy()
        {
            if (AutoDestroyMediaStream && Encoder != null)
                Encoder.Dispose();
            Encoder = null;
        }

        public T GetMedia<T>() where T : IMedia => (T)(IMedia)this;
        public Wrapper.Media.PipelineEffect GetEffect() => null;
    }
}
