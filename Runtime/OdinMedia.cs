using OdinNative.Unity.Audio;
using OdinNative.Unity.Events;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using OdinNative.Wrapper.Room;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity
{
    /// <summary>
    /// Wrapper class of <see cref="OdinNative.Wrapper.MediaDecoder"/> for Unity (require AudioSource)
    /// <para>
    /// This convenient class provides predefined helper functions to cover for a default usecases where the voice chat needs to work with AudioSource, AudioClip, AudioMixer, ...
    /// </para>
    /// Default Unity GameObject altering functions:
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="AddApm"/></term>
    /// <description>Add <see cref="OdinApmComponent"/> to the current GameObject</description>
    /// </item>
    /// <item>
    /// <term><see cref="AddVad"/></term>
    /// <description>Add <see cref="OdinVadComponent"/> to the current GameObject</description>
    /// </item>
    /// <item>
    /// <term><see cref="AddVolumeBoost"/></term>
    /// <description>Add <see cref="OdinVolumeBoostComponent"/> to the current GameObject</description>
    /// </item>
    /// <item>
    /// <term><see cref="AddMute"/></term>
    /// <description>Add <see cref="OdinMuteAudioComponent"/> to the current GameObject</description>
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>Create a custom component with <see cref="OdinNative.Wrapper.Media.IMedia"/> or inheritance from this class and extend/override.</remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odinmedia/")]
    [AddComponentMenu("Odin/Instance/OdinMedia")]
    [RequireComponent(typeof(AudioSource))]
    public class OdinMedia : MonoBehaviour, Wrapper.Media.IMedia
    {
        /// <summary>
        /// Actual Unity audio output component
        /// </summary>
        public AudioSource Playback;

        public int OutSampleRate => AudioSettings.outputSampleRate;

        /// <summary>
        /// Gets the current speaker mode. Default is 2 channel stereo.
        /// </summary>
        public int OutChannels => (int)(AudioSettings.speakerMode >= AudioSpeakerMode.Stereo ? AudioSpeakerMode.Stereo : AudioSpeakerMode.Mono);

        public IPeer Parent { get; set; }

        [field: SerializeField]
        public ushort Id { get; internal set; }
        /// <summary>
        /// Media reference
        /// </summary>
        public MediaDecoder MediaDecoder { get; set; }
        private float[] _AudioBuffer;
        /// <summary>
        /// Unity mixer
        /// </summary>
        public AudioMixerGroup AudioMixerGroup;
        /// <summary>
        /// Property of AudioSource
        /// </summary>
        public float SpatialBlend
        {
            get => Playback?.spatialBlend ?? _SpatialBlend;
            set
            {
                _SpatialBlend = value;
                if (Playback != null)
                    Playback.spatialBlend = value;
            }
        }
        private float _SpatialBlend = 0f; // Unity default
        /// <summary>
        /// Property of AudioSource
        /// </summary>
        public AudioRolloffMode RolloffMode
        {
            get => Playback?.rolloffMode ?? _RolloffMode;
            set
            {
                _RolloffMode = value;
                if (Playback != null)
                    Playback.rolloffMode = value;
            }
        }
        private AudioRolloffMode _RolloffMode = AudioRolloffMode.Logarithmic; // Unity default
        /// <summary>
        /// Property of AudioSource
        /// </summary>
        public float MinDistance
        {
            get => Playback?.minDistance ?? _MinDistance;
            set
            {
                _MinDistance = value;
                if (Playback != null)
                    Playback.minDistance = value;
            }
        }
        private float _MinDistance = 1f; // Unity default
        /// <summary>
        /// Property of AudioSource
        /// </summary>
        public float MaxDistance
        {
            get => Playback?.maxDistance ?? _MaxDistance;
            set
            {
                _MaxDistance = value;
                if (Playback != null)
                    Playback.maxDistance = value;
            }
        }
        private float _MaxDistance = 500.0f; // Unity default

        private ConcurrentQueue<KeyValuePair<object, MediaActiveStateChangedEventArgs>> EventQueue;
        /// <summary>
        /// Media activity state flag
        /// </summary>
        public bool Activity { get; private set; }
        /// <summary>
        /// Trigger on <see cref="Activity"/> changed
        /// </summary>
        public MediaActiveStateChangedProxy OnActiveStateChanged;

        private AudioClip SpatialClip;
        /// <summary>
        ///     Represents the audio clip buffer used for Unity Playback. The Spatial Clip Data is set to this data every frame.
        ///     Could potentially also be filled asynchronously, if implementation is changed to async.
        /// </summary>
        private float[] _ClipBuffer;
        /// <summary>
        ///     The end position of the buffered stream audio frames inside the Spatial Audio Clip. We use this to append
        ///     a new Audio Frame from the Media Stream.
        /// </summary>
        private int _FrameBufferEndPos;
        /// <summary>
        ///     THe minimum audio buffer size. I do not recommend lowering this, because values below 20ms lead to an extreme
        ///     amount of noise.
        /// </summary>
        private const float MinBufferSize = 0.02f;

        /// <summary>
        ///     The target audio buffer size in seconds.
        /// </summary>
        private const float TargetBufferSize = 0.1f;

        /// <summary>
        ///     The maximum audio buffer size - if we go above this, reset the audio buffer. Will lead to a bit of noise, but
        ///     reset the audio lag.
        /// </summary>
        private const float MaxBufferSize = 2f * TargetBufferSize;

        /// <summary>
        ///     The maximum divergence in seconds from the <see cref="TargetBufferSize" /> before starting to adjust the pitch.
        /// </summary>
        private const float TargetBufferTolerance = 0.015f;

        /// <summary>
        ///     The maximum pitch change available to move the audio buffer size back towards the <see cref="TargetBufferSize" />.
        /// </summary>
        private const float TargetSizePitchAdjustment = 0.025f;

        /// <summary>
        ///     The maximum amount of zero frames in seconds we wait before resetting the current audio buffer. Uses
        ///     the <see cref="LastPlaybackUpdateTime" /> to determine if we have hit this value.
        /// </summary>
        private const float MaxFrameLossTime = 0.2f;


        /// <summary>
        ///     The last time we read an ODIN audio frame into the output buffer.
        /// </summary>
        private float LastPlaybackUpdateTime;

        /// <summary>
        ///     Number of Samples in the <see cref="SpatialClip" /> used for playback.
        /// </summary>
        private int ClipSamples => SpatialClip.samples;

        /// <summary>
        ///     The position in samples of the current playback audio source. Used to determine the current size of the
        ///     audio buffer.
        /// </summary>
        private int CurrentClipPos => Playback.timeSamples;
        /// <summary>
        /// Flag for destroy <see cref="Playback"/> linked AudioSource
        /// </summary>
        public bool AutoDestroyAudioSource;
        /// <summary>
        /// Flag for dispose of <see cref="MediaDecoder"/>
        /// </summary>
        /// <remarks>This should only be set for unhandled or bound decoders</remarks>
        public bool AutoDestroyMediaStream;
        private bool _IsDestroying;

        public MediaPipeline GetPipeline() => MediaDecoder?.Pipeline ?? null;

        public virtual OdinApmComponent AddApm() => AddEffect<OdinApmComponent>();
        public virtual OdinVadComponent AddVad() => AddEffect<OdinVadComponent>();
        public virtual OdinVolumeBoostComponent AddVolumeBoost() => AddEffect<OdinVolumeBoostComponent>();
        public virtual OdinMuteAudioComponent AddMute() => AddEffect<OdinMuteAudioComponent>();

        public T AddEffect<T>() where T : MonoBehaviour, IOdinEffect
        {
            T effectComponent = gameObject.AddComponent<T>();
            effectComponent.Media = this;
            return effectComponent;
        }

        public T AddEffect<T>(T effect) where T : MonoBehaviour, IOdinEffect
        {
            IMedia media = effect.GetMedia<IMedia>();
            if (media == null) return null;
            T component = gameObject.AddComponent<T>();
            if (component == null) return null;
            component.Media = media;
            return component;
        }

        public Wrapper.Media.CustomEffect<T> AddEffect<T>(UnityAction<OdinArrayf, bool, T> callback, T userData) where T : unmanaged
        {
            var pipeline = GetPipeline();
            if(pipeline == null) return null;

            return pipeline.AddCustomEffect((OdinArrayf audio, ref bool isSilent, T userdata) => callback(audio, isSilent, userdata), userData);
        } 

        public void SetDecoder(MediaDecoder decoder) => MediaDecoder = decoder;

        void Awake()
        {
            EventQueue = new ConcurrentQueue<KeyValuePair<object, MediaActiveStateChangedEventArgs>>();
            OnActiveStateChanged = new MediaActiveStateChangedProxy();

            this.enabled = false;
        }

        void OnEnable()
        {
#if UNITY_WEBGL
#pragma warning disable CS0618 // Type or member is obsolete
            OdinNative.Core.Utility.Throw(new NotSupportedException("Positional audio is currently not supported in WebGL"));
#pragma warning restore CS0618 // Type or member is obsolete
            this.enabled = false;
            return;
#pragma warning disable CS0162 // Unreachable code detected
#endif

            if (Parent == null)
            {
                Parent = GetComponent<OdinPeer>();
                if (Parent == null || ((OdinPeer)Parent).isActiveAndEnabled == false)
                {
                    Debug.LogWarning($"No available active peer for \"{gameObject.name}\" {nameof(OdinMedia)}");
                    this.enabled = false;
                    return;
                }
            }

            if(EventQueue == null) EventQueue = new ConcurrentQueue<KeyValuePair<object, MediaActiveStateChangedEventArgs>>();
            EventQueue.Clear();

            if (Playback == null)
                Playback = GetComponent<AudioSource>();

            int clipSamples = (int)(OutSampleRate * 3.0f * TargetBufferSize);
            // see Unity Issue 819365,1246661
            SpatialClip = AudioClip.Create("spatialClip", clipSamples, 1, OutSampleRate, false);
            if (OdinDefaults.Debug) Debug.Log($"AudioClip \"{SpatialClip.name}\" {clipSamples}@{OutSampleRate}Hz, {SpatialClip.length}s {SpatialClip.channels} channels {SpatialClip.samples}@{SpatialClip.frequency}Hz");
            ResetAudioClip();

            _AudioBuffer = new float[SpatialClip.samples];

            Playback.clip = SpatialClip;
            Playback.spatialBlend = _SpatialBlend;
            Playback.rolloffMode = _RolloffMode;
            Playback.minDistance = _MinDistance;
            Playback.maxDistance = _MaxDistance;
            if (AudioMixerGroup == null && Parent?.Parent is OdinRoom)
                AudioMixerGroup = ((OdinRoom)Parent?.Parent).AudioMixerGroup;
            Playback.outputAudioMixerGroup = AudioMixerGroup;
            Playback.loop = true;

            if (Playback.isPlaying == false)
                Playback.Play();

            _ClipBuffer = new float[ClipSamples];

            _FrameBufferEndPos = GetTargetFrameBufferEndPosition();
            _FrameBufferEndPos %= ClipSamples;
        }

        void Reset()
        {
            AutoDestroyAudioSource = true;
            AutoDestroyMediaStream = true;
            Activity = false;

#if UNITY_EDITOR
            if (isActiveAndEnabled)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(OnActiveStateChanged, Media_ActiveStateChanged);
            }
#endif
        }

        /// <summary>
        /// UIEditor OnActiveStateChanged placeholder delegate
        /// </summary>
        /// <param name="sender"><see cref="OdinMedia"/></param>
        /// <param name="args"><see cref="EventArgs"/></param>
        private void Media_ActiveStateChanged(object sender, MediaActiveStateChangedEventArgs args)
        {
            Debug.Log($"Media {args.MediaId} changed activity to {args.Active}");
        }

        private void FixedUpdate()
        {
            if (_IsDestroying || MediaDecoder == null) return;

            // Read => buffer
            ReadOdinAudioData();

            // Current audio buffer
            float audioBufferSize = GetFrameBufferSize();
            // Reset if we haven't received an audio frame for a certain amount of time
            CheckResetFrameBuffer(audioBufferSize);
            // We'll adjust the playback source pitch to try and keep the audio buffer size close to the target
            SetAudioSourcePtich(audioBufferSize);

            // buffer => AudioClip
            SetAudioClipData();
        }

        public virtual void ReadOdinAudioData()
        {
            if (MediaDecoder.IsPaused) return;

            if (!MediaDecoder.Handle.IsAlive) return;

            // readBufferSize is based on the fixed unscaled delta time - we want to read "one frame" from the media stream
            int readBufferSize = Mathf.FloorToInt(Time.fixedUnscaledDeltaTime * OutSampleRate);
            if (_AudioBuffer == null || _AudioBuffer.Length != readBufferSize)
            {
                if (OdinDefaults.Debug) Debug.LogWarning($"{nameof(OdinMedia)} ({MediaDecoder?.Id}) change buffer from {_AudioBuffer?.Length ?? 0} to {readBufferSize}");
                _AudioBuffer = new float[readBufferSize];
            }

            if (MediaDecoder.Pop(ref _AudioBuffer, out bool isSilent) == false)
            {
                this.Id = MediaDecoder.Id;
                if (OdinDefaults.Verbose || OdinDefaults.Debug) Debug.LogWarning($"Disable {nameof(OdinMedia)} {this.Id} due to errors");
                this.enabled = false;

                if (AutoDestroyMediaStream)
                {
                    if (OdinDefaults.Debug) Debug.LogWarning($"Free {nameof(MediaDecoder)} {this.Id} because of {nameof(AutoDestroyMediaStream)} is set");
                    MediaDecoder.Dispose();
                    MediaDecoder = null;
                }
                if (AutoDestroyAudioSource)
                {
                    if (OdinDefaults.Debug) Debug.LogWarning($"Destroy {nameof(OdinMedia)} {this.Id} because of {nameof(AutoDestroyAudioSource)} is set");
                    Destroy(this);
                }
            }

            // dispatch MediaActiveStateChanged event to Unity on IsSilent/Activity toggle change
            if (Activity != !isSilent)
            {
                Activity = !isSilent;
                EventQueue.Enqueue(new KeyValuePair<object, MediaActiveStateChangedEventArgs>(this, new MediaActiveStateChangedEventArgs()
                {
                    Active = Activity,
                    MediaId = MediaDecoder.Id,
                    PeerId = Parent?.Id ?? 0,
                }));
            }

            // Only read the data, if there is data in the _AudioBuffer
            if (isSilent == false)
            {
                // write the data into the _ClipBuffer.
                for (int i = 0; i < readBufferSize; i++)
                {
                    int writePosition = _FrameBufferEndPos + i;
                    writePosition %= ClipSamples;
                    _ClipBuffer[writePosition] = _AudioBuffer[i];
                }

                // Update the buffer end position
                _FrameBufferEndPos += readBufferSize;
                _FrameBufferEndPos %= ClipSamples;
                // Update the last time we wrote into the playback clip buffer
                LastPlaybackUpdateTime = Time.time;
            }
        }

        public virtual float GetFrameBufferSize()
        {
            int distanceToClipStart = GetBufferDistance(CurrentClipPos, _FrameBufferEndPos);
            // The size / duration of the current audio buffer.
            return (float)distanceToClipStart / OutSampleRate;
        }

        public virtual void CheckResetFrameBuffer(float audioBufferSize)
        {
            // Reset the frame buffering, if we haven't received an audio frame for a certain amount of time
            bool shouldResetFrameBuffer = Time.time - LastPlaybackUpdateTime > MaxFrameLossTime;
            shouldResetFrameBuffer |=
                audioBufferSize <
                MinBufferSize; // This is a fixed value - anything below this will lead to audio issues
            shouldResetFrameBuffer |= audioBufferSize > MaxBufferSize;
            if (shouldResetFrameBuffer) _FrameBufferEndPos = GetTargetFrameBufferEndPosition();
        }

        public virtual void SetAudioSourcePtich(float audioBufferSize)
        {
            float targetPitch = 1.0f;
            // if the audio buffer size is below the threshold, lower the pitch to allow the media stream input to catch up
            if (audioBufferSize < TargetBufferSize - TargetBufferTolerance)
                targetPitch = 1.0f - TargetSizePitchAdjustment;
            // if the audio buffer size is above the threshold, increase the pitch to allow the clip playback to catch up
            else if (audioBufferSize > TargetBufferSize + TargetBufferTolerance)
                targetPitch = 1.0f + TargetSizePitchAdjustment;

            // Interpolate the pitch over a few frames to avoid sudden pitch jumps.
            float pitch = Playback.pitch;
            pitch += (targetPitch - pitch) * 0.1f;
            Playback.pitch = pitch;
        }

        public virtual void SetAudioClipData()
        {
            // clean up any already played data from the clip buffer. Otherwise the playback will loop once no new data is inserted
            int cleanUpCount = GetBufferDistance(_FrameBufferEndPos, CurrentClipPos);
            for (int i = 0; i < cleanUpCount; i++)
            {
                int cleanUpIndex = (_FrameBufferEndPos + i) % ClipSamples;
                _ClipBuffer[cleanUpIndex] = 0.0f;
            }

            // insert the read data into the spatial clip.
            SpatialClip.SetData(_ClipBuffer, 0);
        }

        private void Update()
        {
            while (EventQueue.TryDequeue(out var newActivity))
                OnActiveStateChanged?.Invoke(this, newActivity.Value);
        }

        /// <summary>
        ///     Returns the targeted frame buffer end position in time samples. The End position is located
        ///     <see cref="TargetBufferSize" /> ms
        ///     in front of the current playback clip position.
        /// </summary>
        /// <returns>The targeted frame buffer end position in time samples</returns>
        private int GetTargetFrameBufferEndPosition()
        {
            return (int)(CurrentClipPos + TargetBufferSize * OutSampleRate);
        }

        /// <summary>
        ///     The distance (in time samples) between two time samples on the current playback clip.
        /// </summary>
        /// <param name="a">First time sample</param>
        /// <param name="b">Second time sample</param>
        /// <returns>Distance (in time samples) between two time samples</returns>
        private int GetBufferDistance(int a, int b)
        {
            int result = b - a;
            if (result < 0)
                result += ClipSamples;
            return result;
        }

        /// <summary>
        ///     Resets the data in the <see cref="SpatialClip" />.
        /// </summary>
        private void ResetAudioClip()
        {
            SpatialClip.SetData(new float[ClipSamples], 0);
        }

        void OnDisable()
        {
            if (_IsDestroying) return;

            Playback?.Stop();
        }

        void OnDestroy()
        {
            _IsDestroying = true;
            OnActiveStateChanged.RemoveAllListeners();

            if (AutoDestroyAudioSource)
                Destroy(Playback);

            if (AutoDestroyMediaStream)
                MediaDecoder?.Dispose();

            MediaDecoder = null;
            _AudioBuffer = null;
        }
    }
}