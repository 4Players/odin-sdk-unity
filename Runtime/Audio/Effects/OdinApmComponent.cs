using OdinNative.Core;
using OdinNative.Unity;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// AudioProcessingModule component for <see cref="OdinNative.Wrapper.Media.ApmEffect"/>
    /// <para>
    /// This class provides configuration for the native implemented audio processing. 
    /// The supported processor/filter are applicable to <see cref="OdinNative.Wrapper.Media.IMedia"/> which must be set.
    /// </para>
    /// </summary>
    /// <remarks>Built-in APM</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinApmComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Audio processing module")]
    public class OdinApmComponent : MonoBehaviour, IOdinEffect
    {
        public IMedia Media { get; set; }
        public ApmEffect Effect { get; private set; }
        public bool IsCreated { get; private set; }
        private bool _warn = true;

        public bool EchoCanceller = OdinDefaults.EchoCanceller;
        public bool HighPassFilter = OdinDefaults.HighPassFilter;
        public bool TransientSuppressor = OdinDefaults.TransientSuppressor;
        public Core.Imports.NativeBindings.OdinNoiseSuppressionLevel NoiseSuppressionLevel = OdinDefaults.NoiseSuppressionLevel;
        public Core.Imports.NativeBindings.OdinGainControllerVersion GainControllerVersion = OdinDefaults.GainControllerVersion;

        /// <summary>Reverse-stream sample rate. Zero follows Unity's current mixer format.</summary>
        /// <remarks>For FMOD/Wwise or SDK pipeline taps, set the actual format before feeding playback.
        /// These settings describe the reverse stream, never the microphone/capture format.
        /// Surround output must be downmixed to mono or stereo before it is submitted.</remarks>
        public uint PlaybackSampleRate;
        /// <summary>Reverse-stream layout when PlaybackSampleRate is nonzero.</summary>
        public bool PlaybackStereo = true;

        internal uint Samplerate { get; private set; }
        internal bool IsStereo { get; private set; }

        void Reset()
        {
            IsCreated = false;

            EchoCanceller = OdinDefaults.EchoCanceller;
            HighPassFilter = OdinDefaults.HighPassFilter;
            NoiseSuppressionLevel = OdinDefaults.NoiseSuppressionLevel;
            TransientSuppressor = OdinDefaults.TransientSuppressor;
            GainControllerVersion = OdinDefaults.GainControllerVersion;
        }

        void Start()
        {
            if (Media == null)
            {
                if (this.gameObject.TryGetComponent(out OdinDecoder decoder))
                    Media = decoder;
                else if (this.gameObject.TryGetComponent(out OdinEncoder encoder))
                    Media = encoder;
            }

            if (Media == null && _warn)
            {
                OdinLog.LogInfo($"{gameObject.name} does not have a {nameof(OdinDecoder)} or {nameof(OdinEncoder)} to add {nameof(ApmEffect)} for this {nameof(OdinApmComponent)}");
                return;
            }
        }

        // pipeline instance the effect was created on; stable per media lifetime since the
        // wrapper caches it at media creation, so a rebuild yields a new instance (no ABA)
        private MediaPipeline _effectPipeline;

        void Update()
        {
            EnsureEffect();
            if (Effect != null && (Effect.EchoCanceller != EchoCanceller ||
                Effect.HighPassFilter != HighPassFilter ||
                Effect.TransientSuppressor != TransientSuppressor ||
                Effect.NoiseSuppressionLevel != NoiseSuppressionLevel ||
                Effect.GainControllerVersion != GainControllerVersion))
                UpdateConfig();
        }

        private OdinApmConfig CurrentConfig() => new OdinApmConfig
        {
            echo_canceller = EchoCanceller,
            high_pass_filter = HighPassFilter,
            transient_suppressor = TransientSuppressor,
            noise_suppression = NoiseSuppressionLevel,
            gain_controller_version = GainControllerVersion
        };

        // Called on the Unity thread. Capture PCM has already been converted to the SDK's
        // internal format; only the independent reverse ingress needs configuring here.
        private void EnsureEffect()
        {
            MediaPipeline pipeline = Media?.GetPipeline();
            if (!ReferenceEquals(pipeline, _effectPipeline)) ClearEffect();
            if (pipeline?.Handle?.IsAlive != true) return;

            uint rate = PlaybackSampleRate;
            bool stereo = PlaybackStereo;
            if (rate == 0)
            {
                var config = AudioSettings.GetConfiguration();
                rate = config.sampleRate > 0 ? (uint)config.sampleRate : OdinRoom.DefaultSampleRate;
                stereo = config.speakerMode >= AudioSpeakerMode.Stereo;
            }
            if (Effect != null && Effect.Samplerate == rate && Effect.IsStereo == stereo) return;
            RecreateEffect(pipeline, rate, stereo, CurrentConfig());
        }

        /// <summary>Configure the actual reverse-stream format on the Unity thread before feeding it.</summary>
        public void ConfigurePlayback(uint samplerate, bool stereo)
        {
            if (samplerate == 0) throw new System.ArgumentOutOfRangeException(nameof(samplerate));
            PlaybackSampleRate = samplerate;
            PlaybackStereo = stereo;
            EnsureEffect();
        }

        private void RecreateEffect(MediaPipeline pipeline, uint samplerate, bool stereo, OdinApmConfig config)
        {
            uint index = pipeline.GetNextIndex();
            if (Effect != null && ReferenceEquals(pipeline, _effectPipeline))
                Effect.GetEffectIndex(out index);
            ClearEffect();
            Effect = pipeline.InsertApmEffect(index, samplerate, stereo);
            if (Effect != null)
            {
                Effect.SetApmConfig(config);
                Samplerate = samplerate;
                IsStereo = stereo;
                _effectPipeline = pipeline;
                IsCreated = true;
            }
        }

        public void UpdateConfig() => UpdateConfig(Media?.GetPipeline());
        public void UpdateConfig(MediaPipeline pipeline)
        {
            OdinLog.Assert(pipeline != null, $"{nameof(OdinApmComponent)} {nameof(UpdateConfig)} {nameof(MediaPipeline)} is null");
            OdinLog.Assert(Effect != null, $"{nameof(OdinApmComponent)} {nameof(UpdateConfig)} {nameof(ApmEffect)} is null");

            if (Effect == null) return;
            // the config write must target the pipeline the effect was created on
            if (ReferenceEquals(pipeline, _effectPipeline) == false) return;

            Effect.EchoCanceller = EchoCanceller;
            Effect.HighPassFilter = HighPassFilter;
            Effect.TransientSuppressor = TransientSuppressor;
            Effect.NoiseSuppressionLevel = NoiseSuppressionLevel;
            Effect.GainControllerVersion = GainControllerVersion;
            Effect.UpdateEffectConfig(pipeline);
        }

        private void ClearEffect()
        {
            // remove from the pipeline the effect actually lives on, if it is still alive
            if (Effect != null && _effectPipeline?.Handle?.IsAlive == true)
                _effectPipeline.RemoveEffect(Effect.Id);

            Effect = null;
            IsCreated = false;
            _effectPipeline = null;
        }

        public void ResetEffect() 
        {
            OdinLog.Assert(Effect != null, $"{nameof(OdinApmComponent)} {nameof(ResetEffect)} {nameof(ApmEffect)} is null");
            if (Effect == null) return;

            var pipeline = Media?.GetPipeline();
            if (pipeline?.Handle?.IsAlive == true)
                RecreateEffect(pipeline, Effect.Samplerate, Effect.IsStereo, Effect.Config);
        }
        public void ResetEffect(MediaPipeline pipeline, uint samplerate, bool stereo, OdinApmConfig config)
        {
            OdinLog.Assert(pipeline != null, $"{nameof(OdinApmComponent)} {nameof(ResetEffect)} {nameof(MediaPipeline)} is null");
            if (pipeline == null) return;

            PlaybackSampleRate = samplerate;
            PlaybackStereo = stereo;
            EchoCanceller = config.echo_canceller;
            HighPassFilter = config.high_pass_filter;
            TransientSuppressor = config.transient_suppressor;
            NoiseSuppressionLevel = config.noise_suppression;
            GainControllerVersion = config.gain_controller_version;
            RecreateEffect(pipeline, samplerate, stereo, config);
        }

        /// <summary>
        /// Send samples to Apm processing. The buffer should contain loopback audio data for calculating echo cancellation.
        /// </summary>
        /// <param name="buffer">samples</param>
        /// <param name="delay">delay</param>
        public void UpdateApmPlayback(float[] buffer, ulong delay = 100) => Effect?.UpdateApmPlayback(buffer, delay);

        /// <summary>Feed reverse PCM with an explicit format on the Unity thread.</summary>
        /// <remarks>Use one consistently formatted playback reference per APM instance.</remarks>
        public void UpdateApmPlayback(float[] buffer, uint samplerate, bool stereo, ulong delay = 100)
        {
            ConfigurePlayback(samplerate, stereo);
            UpdateApmPlayback(buffer, delay);
        }

        void OnDestroy()
        {
            ClearEffect();
        }

        public T GetMedia<T>() where T : IMedia => (T)Media;
        public PipelineEffect GetEffect() => Effect;
    }
}
