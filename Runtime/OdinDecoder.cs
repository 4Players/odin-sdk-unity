using OdinNative.Core;
using OdinNative.Unity.Audio;
using OdinNative.Unity.Events;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
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
    /// Wrapper class of <see cref="OdinNative.Wrapper.MediaDecoder"/> for Unity (requires AudioSource)
    /// <para>
    /// This convenient class provides predefined helper functions to cover default use cases where the voice chat needs to work with AudioSource, AudioClip, AudioMixer, ...
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
    /// <term><see cref="AddVi"/></term>
    /// <description>Add <see cref="OdinViComponent"/> to the current GameObject</description>
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
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity/OdinDecoder/")]
    [AddComponentMenu("Odin/Instance/OdinDecoder")]
    [RequireComponent(typeof(AudioSource))]
    public class OdinDecoder : MonoBehaviour, Wrapper.Media.IMedia
    {
        /// <summary>
        /// Actual Unity audio output component
        /// </summary>
        public AudioSource Playback;

        /// <summary>
        /// Samplerate of the decoded PCM, or <see cref="OdinRoom.OutputSampleRate"/> before a decoder is assigned.
        /// </summary>
        /// <remarks>
        /// The native decoder keeps its creation rate across output device changes. Keep the clip and
        /// read buffers at that rate; Unity resamples the clip for the current output device.
        /// </remarks>
        public int OutSampleRate => (int)(MediaDecoder?.Samplerate ?? OdinRoom.OutputSampleRate);

        /// <summary>
        /// Channel count of the decoded PCM, independent of the output device's speaker mode.
        /// </summary>
        public int OutChannels => (MediaDecoder?.Stereo ?? Room?.IsStereo ?? false) ? 2 : 1;

        /// <summary>
        /// The <see cref="OdinRoom"/> this decoder belongs to
        /// </summary>
        public OdinRoom Room;
        /// <summary>Recreate room-owned native media when Unity's output format changes.</summary>
        /// <remarks>Disabled for decoders created with an explicit format through OdinPeer.</remarks>
        public bool FollowOutputDevice = true;
        /// <summary>
        /// The peer id this decoder plays back audio for
        /// </summary>
        public uint PeerId;

        /// <summary>
        /// The peer that owns this decoder. Set by <see cref="OdinPeer.AddDecoderComponent"/> and used as the
        /// abstraction boundary — allows swapping the native wrapper for a web bridge without changing SDK components.
        /// </summary>
        public IPeer Parent { get; internal set; }

        [field: SerializeField]
        public ulong Id { get; internal set; }
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
        ///     Keeps the missing audio engine from being reported once per frame, since the frame
        ///     update retries the setup for as long as there is no clip.
        /// </summary>
        private bool _WarnedMissingAudioEngine;
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
        ///     The minimum audio buffer size. I do not recommend lowering this, because values below 20ms lead to an extreme
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
        ///     Time constant for easing the playback pitch towards its target.
        /// </summary>
        /// <remarks>
        ///     Picked so the result matches the previous per-call factor of 0.1 at Unity's default
        ///     0.02 fixed timestep: 1 - e^(-0.02 / 0.19) = 0.0999.
        /// </remarks>
        private const float PitchSmoothingTime = 0.19f;

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
        public Utility.ChannelMask ChannelMask => MediaDecoder?.ChannelMask ?? Utility.ChannelMask.None;
        /// <summary>
        /// Server side position
        /// </summary>
        /// <remarks>The native SDK defines no axis convention, positions are only used for distance based culling -
        /// all clients just have to map consistently; see <see cref="GetPosition"/> for the Unity mapping</remarks>
        public OdinPosition Position => MediaDecoder?.Position ?? new OdinPosition();

        public virtual OdinApmComponent AddApm() => AddEffect<OdinApmComponent>();
        public virtual OdinVadComponent AddVad() => AddEffect<OdinVadComponent>();
        public virtual OdinViComponent AddVi() => AddEffect<OdinViComponent>();
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

        public Wrapper.Media.CustomEffect<T> AddEffect<T>(UnityAction<OdinTArray<float>, bool, T> callback, T userData) where T : unmanaged
        {
            var pipeline = GetPipeline();
            if(pipeline == null) return null;

            return pipeline.AddCustomEffect((OdinTArray<float> audio, ref bool isSilent, T userdata) => callback(audio, isSilent, userdata), userData);
        }

        private CustomActivityEffect _activityEffect;

        public void SetDecoder(MediaDecoder decoder)
        {
            if (_activityEffect != null)
            {
                _activityEffect.OnActivityChanged -= OnActivityEffectChanged;
                GetPipeline()?.RemoveEffect(_activityEffect.Id);
                _activityEffect.Dispose();
                _activityEffect = null;
            }

            MediaDecoder = decoder;

            if (decoder != null)
            {
                _activityEffect = CustomActivityEffect.Create(decoder.Pipeline, PeerId, decoder.Id);
                if (_activityEffect != null)
                    _activityEffect.OnActivityChanged += OnActivityEffectChanged;

                if (TryGetComponent(out OdinAudioMap map) && (map.MapSelector & OdinAudioMap.MapType.Decoder) != 0)
                    decoder.ListenChannelMask = map.ChannelMask;
            }
        }

        private void OnActivityEffectChanged(bool isSilent, uint peerId, ulong mediaId)
        {
            Activity = !isSilent;
            EventQueue.Enqueue(new KeyValuePair<object, MediaActiveStateChangedEventArgs>(this, new MediaActiveStateChangedEventArgs()
            {
                Active = Activity,
                MediaId = mediaId,
                PeerId = peerId,
            }));
        }

        void Awake()
        {
            EventQueue = new ConcurrentQueue<KeyValuePair<object, MediaActiveStateChangedEventArgs>>();
            if (OnActiveStateChanged == null)
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

            if(EventQueue == null) EventQueue = new ConcurrentQueue<KeyValuePair<object, MediaActiveStateChangedEventArgs>>();
            EventQueue.Clear();

            if (Playback == null)
                Playback = GetComponent<AudioSource>();

            Playback.spatialBlend = _SpatialBlend;
            Playback.rolloffMode = _RolloffMode;
            Playback.minDistance = _MinDistance;
            Playback.maxDistance = _MaxDistance;
            if (AudioMixerGroup == null && Room != null)
                AudioMixerGroup = Room.AudioMixerGroup;
            Playback.outputAudioMixerGroup = AudioMixerGroup;
            Playback.loop = true;

            Room?.RefreshDecoderOutput(this);
            SetupPlaybackClip();

            AudioSettings.OnAudioConfigurationChanged += AudioSettings_OnAudioConfigurationChanged;

            Room?.RegisterDecoder(this, PeerId);
        }

        /// <summary>
        /// (Re-)creates <see cref="SpatialClip"/> and its dependent buffers at the current <see cref="OutSampleRate"/>.
        /// </summary>
        /// <remarks>
        /// Called from <see cref="OnEnable"/> and again from <see cref="AudioSettings_OnAudioConfigurationChanged"/>,
        /// since a procedural clip created via <see cref="AudioClip.Create"/> can end up empty or with a stale
        /// samplerate/length after the audio output (or input) device changes and Unity resets the DSP graph.
        /// </remarks>
        private void SetupPlaybackClip()
        {
            // Unity's audio engine can be switched off entirely, which projects driving FMOD or Wwise
            // commonly do. AudioClip.Create is not valid in that state: it walks an uninitialised
            // pointer inside Unity and takes the process down with an access violation. The raw
            // samplerate reported by Unity is the signal for it - OutSampleRate deliberately masks the
            // zero, because the native encoder and decoder still need a usable rate.
            if (OdinRoom.IsUnityAudioDisabled)
            {
                // a clip from before the engine went away is useless, and dropping it keeps the
                // frame update's guard from running on a dead one
                if (SpatialClip != null)
                {
                    Destroy(SpatialClip);
                    SpatialClip = null;
                }

                if (_WarnedMissingAudioEngine == false)
                {
                    _WarnedMissingAudioEngine = true;
                    OdinLog.LogError($"{nameof(OdinDecoder)} ({MediaDecoder?.Id}) cannot create a playback clip while the Unity audio engine is disabled, so this component stays silent. Drive {nameof(MediaDecoder)} yourself when playing back through FMOD or Wwise.");
                }
                return;
            }
            _WarnedMissingAudioEngine = false;

            // a previously created procedural clip is not garbage collected by Unity
            if (SpatialClip != null)
                Destroy(SpatialClip);

            int clipSamples = (int)(OutSampleRate * 3.0f * TargetBufferSize);
            if (clipSamples <= 0)
            {
                OdinLog.LogError($"{nameof(OdinDecoder)} ({MediaDecoder?.Id}) refusing to create a playback clip of {clipSamples} samples at {OutSampleRate}Hz");
                return;
            }
            // see Unity Issue 819365,1246661
            SpatialClip = AudioClip.Create("spatialClip", clipSamples, OutChannels, OutSampleRate, false);
            OdinLog.LogInfo($"AudioClip \"{SpatialClip.name}\" {clipSamples}@{OutSampleRate}Hz, {SpatialClip.length}s {SpatialClip.channels} channels {SpatialClip.samples}@{SpatialClip.frequency}Hz");
            ResetAudioClip();

            // sized like the per-FixedUpdate read in ReadOdinAudioData, so the first
            // read does not warn and reallocate
            _AudioBuffer = new float[Mathf.FloorToInt(Time.fixedUnscaledDeltaTime * OutSampleRate) * OutChannels];

            Playback.clip = SpatialClip;
            if (Playback.isPlaying == false)
                Playback.Play();

            _ClipBuffer = new float[ClipSamples * OutChannels];

            _FrameBufferEndPos = GetTargetFrameBufferEndPosition();
            _FrameBufferEndPos %= ClipSamples;
        }

        /// <summary>
        /// Recreates the playback clip when the audio device changes, since a stale/invalidated
        /// <see cref="SpatialClip"/> can end up empty (0 samples) after such a change.
        /// </summary>
        /// <param name="deviceWasChanged">true if an actual device change (not just a config reset) triggered this</param>
        private void AudioSettings_OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            if (isActiveAndEnabled == false) return;

            Room?.RefreshDecoderOutput(this);
            OdinLog.LogInfo($"{nameof(OdinDecoder)} ({MediaDecoder?.Id}) audio configuration changed, recreating playback clip");
            SetupPlaybackClip();
        }

        void Reset()
        {
            if (OnActiveStateChanged == null)
                OnActiveStateChanged = new MediaActiveStateChangedProxy();

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
        /// Get unity translated 3d vector from <see cref="OdinNative.Core.Imports.NativeBindings.OdinPosition"/>
        /// to use the Quaternion and the Matrix4x4 classes for rotating or transforming vectors and points
        /// </summary>
        /// <remarks>
        /// Odin (x, y, z) into Unity (z, x, y);
        /// </remarks>
        /// <returns>A new vector with given x, y, z components.</returns>
        public Vector3 GetPosition() => new Vector3(Position.Z, Position.X, Position.Y);

        /// <summary>
        /// UIEditor OnActiveStateChanged placeholder delegate
        /// </summary>
        /// <param name="sender"><see cref="OdinDecoder"/></param>
        /// <param name="args"><see cref="EventArgs"/></param>
        private void Media_ActiveStateChanged(object sender, MediaActiveStateChangedEventArgs args)
        {
            OdinLog.LogInfo($"Media {args.MediaId} changed activity to {args.Active}");
        }

        private void FixedUpdate()
        {
            if (_IsDestroying || MediaDecoder == null) return;

            // Self-guard a clip left empty/stale by a device change, in case the audio
            // configuration changed event above did not fire on the current platform
            if (SpatialClip == null || SpatialClip.samples <= 0
                || SpatialClip.samples * SpatialClip.channels != _ClipBuffer?.Length
                || SpatialClip.frequency != OutSampleRate || SpatialClip.channels != OutChannels)
                SetupPlaybackClip();

            // SetupPlaybackClip refuses to build a clip at an invalid samplerate. Without one there is
            // nothing to read into or hand to the AudioSource, and continuing would dereference null.
            if (SpatialClip == null || _ClipBuffer == null) return;

            // Read => buffer
            ReadOdinAudioData();
            if (isActiveAndEnabled == false || _IsDestroying || MediaDecoder == null) return;

            // Current audio buffer
            float audioBufferSize = GetFrameBufferSize();
            // Reset if we haven't received an audio frame for a certain amount of time
            CheckResetFrameBuffer(audioBufferSize);
            // We'll adjust the playback source pitch to try and keep the audio buffer size close to the target
            SetAudioSourcePitch(audioBufferSize);

            // buffer => AudioClip
            SetAudioClipData();
        }

        public virtual void ReadOdinAudioData()
        {
            if (MediaDecoder == null || MediaDecoder.IsPaused) return;

            // readBufferSize is based on the fixed unscaled delta time - we want to read "one frame" from the media stream
            int readFrames = Mathf.FloorToInt(Time.fixedUnscaledDeltaTime * OutSampleRate);
            int readBufferSize = readFrames * OutChannels;
            if (_AudioBuffer == null || _AudioBuffer.Length != readBufferSize)
            {
                OdinLog.LogWarning($"{nameof(OdinDecoder)} ({MediaDecoder?.Id}) change buffer from {_AudioBuffer?.Length ?? 0} to {readBufferSize}");
                _AudioBuffer = new float[readBufferSize];
            }

            if (MediaDecoder.Pop(ref _AudioBuffer, out bool isSilent) == false)
            {
                this.Id = MediaDecoder.Id;
                OdinLog.LogWarning($"Disable {nameof(OdinDecoder)} {this.Id} due to errors");
                this.enabled = false;

                if (AutoDestroyMediaStream)
                {
                    OdinLog.LogWarning($"Free {nameof(MediaDecoder)} {this.Id} because of {nameof(AutoDestroyMediaStream)} is set");
                    MediaDecoder.Dispose();
                    MediaDecoder = null;
                }
                if (AutoDestroyAudioSource)
                {
                    OdinLog.LogWarning($"Destroy {nameof(OdinDecoder)} {this.Id} because of {nameof(AutoDestroyAudioSource)} is set");
                    Destroy(this);
                }
                return; // do not process the stale buffer of a failed pop
            }

            // Write and advance unconditionally, silence included. The AudioSource play head moves
            // with wall clock, so skipping a write leaves the cursor permanently one frame behind,
            // and that deficit is only ever recovered by a buffer reset - which fabricates silence,
            // mid-word if the skipped stretch sat inside one.
            for (int i = 0; i < readBufferSize; i++)
            {
                int writePosition = (_FrameBufferEndPos * OutChannels + i) % _ClipBuffer.Length;
                _ClipBuffer[writePosition] = _AudioBuffer[i];
            }

            // Update the buffer end position
            _FrameBufferEndPos += readFrames;
            _FrameBufferEndPos %= ClipSamples;

            // Only actual audio counts as "the stream is alive"; silence must not, or a decoder that
            // stopped delivering would never hit MaxFrameLossTime.
            if (isSilent == false)
                LastPlaybackUpdateTime = Time.time;
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
            if (shouldResetFrameBuffer) _FrameBufferEndPos = GetTargetFrameBufferEndPosition() % ClipSamples;
        }

        /// <summary>Forward for the previous misspelled name</summary>
        [Obsolete("Renamed to " + nameof(SetAudioSourcePitch))]
        public void SetAudioSourcePtich(float audioBufferSize) => SetAudioSourcePitch(audioBufferSize);

        public virtual void SetAudioSourcePitch(float audioBufferSize)
        {
            float targetPitch = 1.0f;
            // if the audio buffer size is below the threshold, lower the pitch to allow the media stream input to catch up
            if (audioBufferSize < TargetBufferSize - TargetBufferTolerance)
                targetPitch = 1.0f - TargetSizePitchAdjustment;
            // if the audio buffer size is above the threshold, increase the pitch to allow the clip playback to catch up
            else if (audioBufferSize > TargetBufferSize + TargetBufferTolerance)
                targetPitch = 1.0f + TargetSizePitchAdjustment;

            // Ease the pitch towards the target. The factor has to follow elapsed time rather than
            // be applied once per call, otherwise a project running a 100 Hz fixed timestep corrects
            // twice as aggressively as one at Unity's default 50 Hz.
            float pitch = Playback.pitch;
            float pitchSmoothing = 1f - Mathf.Exp(-Time.fixedUnscaledDeltaTime / PitchSmoothingTime);
            pitch += (targetPitch - pitch) * pitchSmoothing;
            Playback.pitch = pitch;
        }

        public virtual void SetAudioClipData()
        {
            // clean up any already played data from the clip buffer. Otherwise the playback will loop once no new data is inserted
            int cleanUpCount = GetBufferDistance(_FrameBufferEndPos, CurrentClipPos) * OutChannels;
            for (int i = 0; i < cleanUpCount; i++)
            {
                int cleanUpIndex = (_FrameBufferEndPos * OutChannels + i) % _ClipBuffer.Length;
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
        ///     <see cref="TargetBufferSize" /> seconds
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
            SpatialClip.SetData(new float[ClipSamples * OutChannels], 0);
        }

        void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= AudioSettings_OnAudioConfigurationChanged;

            if (_IsDestroying) return;

            Playback?.Stop();
        }

        void OnDestroy()
        {
            _IsDestroying = true;
            OnActiveStateChanged.RemoveAllListeners();

            if (_activityEffect != null)
            {
                _activityEffect.OnActivityChanged -= OnActivityEffectChanged;
                GetPipeline()?.RemoveEffect(_activityEffect.Id);
                _activityEffect.Dispose();
                _activityEffect = null;
            }

            Room?.UnregisterDecoder(this);

            if (AutoDestroyAudioSource)
                Destroy(Playback);

            if (AutoDestroyMediaStream)
                MediaDecoder?.Dispose();

            // the procedural clip is not garbage collected by Unity
            if (SpatialClip != null)
            {
                Destroy(SpatialClip);
                SpatialClip = null;
            }

            MediaDecoder = null;
            _AudioBuffer = null;
        }
    }
}
