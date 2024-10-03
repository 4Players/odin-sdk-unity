using OdinNative.Core;
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
    /// <remarks>Build-in APM</remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odinapmcomponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Audio processing module")]
    public class OdinApmComponent : MonoBehaviour, IOdinEffect
    {
        public IMedia Media { get; set; }
        public ApmEffect Effect { get; private set; }
        public bool IsCreated { get; private set; }
        private bool _warn = true;

        public bool EchoCanceller = OdinDefaults.EchoCanceller;
        public bool HighPassFilter = OdinDefaults.HighPassFilter;
        public bool PreAmplifier = OdinDefaults.PreAmplifier;
        public Core.Imports.NativeBindings.OdinNoiseSuppression NoiseSuppression = OdinDefaults.NoiseSuppressionLevel;
        public bool TransientSuppressor = OdinDefaults.TransientSuppressor;
        public bool GainController = OdinDefaults.GainController;

        internal uint Samplerate { get; private set; }
        internal bool IsStereo { get; private set; }

        void Reset()
        {
            IsCreated = false;

            EchoCanceller = OdinDefaults.EchoCanceller;
            HighPassFilter = OdinDefaults.HighPassFilter;
            PreAmplifier = OdinDefaults.PreAmplifier;
            NoiseSuppression = OdinDefaults.NoiseSuppressionLevel;
            TransientSuppressor = OdinDefaults.TransientSuppressor;
            GainController = OdinDefaults.GainController;
        }

        void Start()
        {
            if (Media == null)
                Media = this.gameObject.GetComponent<OdinMedia>();

            if (Media == null && _warn)
            {
                Debug.Log($"{gameObject.name} does not have a {nameof(OdinMedia)} to add {nameof(ApmEffect)} for this {nameof(OdinApmComponent)}");
                return;
            }
        }

        void Update()
        {
            if (IsCreated == false)
            {
                MediaPipeline pipeline = Media?.GetPipeline();
                if (pipeline == null)
                {
                    if (_warn)
                    {
                        Debug.LogWarning($"{gameObject.name} {nameof(OdinApmComponent)} can not create/add {nameof(ApmEffect)} without a pipline");
                        _warn = false;
                    }
                    return;
                }

                if (Media is OdinMedia)
                {
                    OdinMedia media = Media as OdinMedia;
                    Samplerate = (uint)media.OutSampleRate;
                    IsStereo = !(media.OutChannels == (int)AudioSpeakerMode.Mono);
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
                    Debug.LogError($"{gameObject.name} {nameof(OdinApmComponent)} not supported type of {Media.GetType()} media for apm.");
                    return;
                }

                ApmEffect apm = pipeline.AddApmEffect(Samplerate, IsStereo, new OdinApmConfig()
                {
                    echo_canceller = EchoCanceller,
                    high_pass_filter = HighPassFilter,
                    pre_amplifier = PreAmplifier,
                    noise_suppression = NoiseSuppression,
                    transient_suppressor = TransientSuppressor,
                    gain_controller = GainController
                });

                if (apm != null)
                {
                    Effect = apm;

                    IsCreated = true;
                    if (OdinDefaults.Verbose || OdinDefaults.Debug) Debug.Log($"{gameObject.name} {nameof(OdinApmComponent)} added {nameof(ApmEffect)} (id {Effect.Id})");
                }
                else if (_warn)
                {
                    Debug.LogError($"{gameObject.name} {nameof(OdinApmComponent)} error in {nameof(MediaPipeline.AddApmEffect)}");
                }
                _warn = true;
            }
            else if (Effect != null)
            {
                // update config on change
                if (Effect.EchoCanceller != EchoCanceller ||
                    Effect.HighPassFilter != HighPassFilter ||
                    Effect.PreAmplifier != PreAmplifier ||
                    Effect.NoiseSuppressionLevel != NoiseSuppression ||
                    Effect.TransientSuppressor != TransientSuppressor ||
                    Effect.GainController != GainController)
                    UpdateConfig();
            }
        }

        public void UpdateConfig() => UpdateConfig(Media?.GetPipeline());
        public void UpdateConfig(MediaPipeline pipeline)
        {
            Utility.Assert(pipeline != null, $"{nameof(OdinApmComponent)} {nameof(UpdateConfig)} {nameof(MediaPipeline)} is null");
            Utility.Assert(Effect != null, $"{nameof(OdinApmComponent)} {nameof(UpdateConfig)} {nameof(ApmEffect)} is null");

            if (Effect == null) return;

            Effect.EchoCanceller = EchoCanceller;
            Effect.HighPassFilter = HighPassFilter;
            Effect.PreAmplifier = PreAmplifier;
            Effect.NoiseSuppressionLevel = NoiseSuppression;
            Effect.TransientSuppressor = TransientSuppressor;
            Effect.GainController = GainController;
            Effect.UpdateEffectConfig(pipeline);
        }

        public void ResetEffect() 
        {
            Utility.Assert(Effect != null, $"{nameof(OdinApmComponent)} {nameof(ResetEffect)} {nameof(ApmEffect)} is null");
            if (Effect == null) return;

            ResetEffect(Media?.GetPipeline(), Effect.Samplerate, Effect.IsStereo, Effect.Config); 
        }
        public void ResetEffect(MediaPipeline pipeline, uint samplerate, bool stereo, OdinApmConfig config)
        {
            Utility.Assert(pipeline != null, $"{nameof(OdinApmComponent)} {nameof(ResetEffect)} {nameof(MediaPipeline)} is null");

            if (Effect != null)
                pipeline.RemoveEffect(Effect.Id);

            Effect = ApmEffect.Create(pipeline, samplerate, stereo, config);
        }

        /// <summary>
        /// Send samples to Apm processing. The buffer should contain loopback audio data for calculating echo cancellation.
        /// </summary>
        /// <param name="buffer">samples</param>
        public void UpdateApmPlayback(float[] buffer) => Effect?.UpdateApmPlayback(buffer);

        void OnDestroy()
        {
            if (Media != null && Effect != null)
                Media.GetPipeline()?.RemoveEffect(Effect.Id);
        }

        public T GetMedia<T>() where T : IMedia => (T)Media;
        public PiplineEffect GetEffect() => Effect;
    }
}