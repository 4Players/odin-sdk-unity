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
            MediaPipeline pipeline = Media?.GetPipeline();
            // a rebuilt media (room recreation) brings a new pipeline and the old effect died with it
            if (IsCreated && ReferenceEquals(pipeline, _effectPipeline) == false)
                ClearEffect();

            if (IsCreated == false)
            {
                if (pipeline == null)
                {
                    if (_warn)
                    {
                        // no pipeline is a regular state, e.g. an encoder before the room is joined
                        OdinLog.LogInfo($"{gameObject.name} {nameof(OdinApmComponent)} can not create/add {nameof(ApmEffect)} without a pipeline");
                        _warn = false;
                    }
                    return;
                }

                if (Media is OdinDecoder)
                {
                    OdinDecoder media = Media as OdinDecoder;
                    Samplerate = (uint)media.OutSampleRate;
                    IsStereo = !(media.OutChannels == (int)AudioSpeakerMode.Mono);
                }
                else if (Media is OdinEncoder)
                {
                    OdinEncoder media = Media as OdinEncoder;
                    Samplerate = media.Samplerate;
                    IsStereo = media.Stereo;
                }
                else if (Media is MediaDecoder)
                {
                    MediaDecoder decoder = Media as MediaDecoder;
                    Samplerate = decoder.Samplerate;
                    IsStereo = decoder.Stereo;
                }
                else if (Media is MediaEncoder)
                {
                    MediaEncoder encoder = Media as MediaEncoder;
                    Samplerate = encoder.Samplerate;
                    IsStereo = encoder.Stereo;
                }
                else
                {
                    OdinLog.LogError($"{gameObject.name} {nameof(OdinApmComponent)} not supported type of {Media.GetType()} media for apm.");
                    return;
                }

                ApmEffect apm = pipeline.AddApmEffect(Samplerate, IsStereo, new OdinApmConfig()
                {
                    echo_canceller = EchoCanceller,
                    high_pass_filter = HighPassFilter,
                    transient_suppressor = TransientSuppressor,
                    noise_suppression = NoiseSuppressionLevel,
                    gain_controller_version = GainControllerVersion
                });

                if (apm != null)
                {
                    Effect = apm;

                    IsCreated = true;
                    _effectPipeline = pipeline;
                    OdinLog.LogInfo($"{gameObject.name} {nameof(OdinApmComponent)} added {nameof(ApmEffect)} (id {Effect.Id})");
                }
                else if (_warn)
                {
                    OdinLog.LogError($"{gameObject.name} {nameof(OdinApmComponent)} error in {nameof(MediaPipeline.AddApmEffect)}");
                }
                _warn = true;
            }
            else if (Effect != null)
            {
                // update config on change
                if (Effect.EchoCanceller != EchoCanceller ||
                    Effect.HighPassFilter != HighPassFilter ||
                    Effect.TransientSuppressor != TransientSuppressor ||
                    Effect.NoiseSuppressionLevel != NoiseSuppressionLevel ||
                    Effect.GainControllerVersion != GainControllerVersion)
                    UpdateConfig();
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

            ResetEffect(Media?.GetPipeline(), Effect.Samplerate, Effect.IsStereo, Effect.Config); 
        }
        public void ResetEffect(MediaPipeline pipeline, uint samplerate, bool stereo, OdinApmConfig config)
        {
            OdinLog.Assert(pipeline != null, $"{nameof(OdinApmComponent)} {nameof(ResetEffect)} {nameof(MediaPipeline)} is null");
            if (pipeline == null) return;

            ClearEffect();

            Effect = ApmEffect.Create(pipeline, samplerate, stereo, config);
            if (Effect != null)
            {
                _effectPipeline = pipeline;
                IsCreated = true;
            }
        }

        /// <summary>
        /// Send samples to Apm processing. The buffer should contain loopback audio data for calculating echo cancellation.
        /// </summary>
        /// <param name="buffer">samples</param>
        /// <param name="delay">delay</param>
        public void UpdateApmPlayback(float[] buffer, ulong delay = 100) => Effect?.UpdateApmPlayback(buffer, delay);

        void OnDestroy()
        {
            ClearEffect();
        }

        public T GetMedia<T>() where T : IMedia => (T)Media;
        public PipelineEffect GetEffect() => Effect;
    }
}