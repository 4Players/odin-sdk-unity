using OdinNative.Core;
using OdinNative.Unity;
using OdinNative.Wrapper;
using OdinNative.Wrapper.Media;
using UnityEngine;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// VoiceActivity component for <see cref="OdinNative.Wrapper.Media.VadEffect"/>
    /// <para>
    /// This class provides configuration for the native implemented voice detection. Supports speech recognition and/or a volume threshold.
    /// </para>
    /// </summary>
    /// <remarks>Autoset <see cref="OdinNative.Core.Imports.NativeBindings.OdinVadConfig"/> for built-in VAD</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinVadComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Voice activity detection")]
    public class OdinVadComponent : MonoBehaviour, IOdinEffect
    {
        public IMedia Media { get; set; }

        public VadEffect Effect { get; private set; }
        public bool IsCreated { get; private set; } = false;
        private bool _warn = true;

        /// <summary>
        /// Indicates whether the vad setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/> is enabled
        /// </summary>
        [Header("Odin Voice Activity Detection")]
        public bool VoiceActivityEnabled = OdinDefaults.VoiceActivityDetection;
        /// <summary>
        /// Indicates the vad attack probability setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VoiceActivityAttackThreshold = OdinDefaults.VoiceActivityDetectionAttackProbability;
        /// <summary>
        /// Indicates the vad release probability setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VoiceActivityReleaseThreshold = OdinDefaults.VoiceActivityDetectionReleaseProbability;

        /// <summary>
        /// Indicates whether the gate setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/> is enabled
        /// </summary>
        [Header("Odin Volume Gate")]
        public bool VolumeGateEnabled = OdinDefaults.VolumeGate;
        /// <summary>
        /// Indicates the gate attack loudness setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VolumeGateAttackThreshold = OdinDefaults.VolumeGateAttackLoudness;
        /// <summary>
        /// Indicates the gate release loudness setting in <see cref="Core.Imports.NativeBindings.OdinSensitivityConfig"/>
        /// </summary>
        public float VolumeGateReleaseThreshold = OdinDefaults.VolumeGateReleaseLoudness;

        void Reset()
        {
            IsCreated = false;

            VoiceActivityEnabled = OdinDefaults.VoiceActivityDetection;
            VoiceActivityAttackThreshold = OdinDefaults.VoiceActivityDetectionAttackProbability;
            VoiceActivityReleaseThreshold = OdinDefaults.VoiceActivityDetectionReleaseProbability;
            VolumeGateEnabled = OdinDefaults.VolumeGate;
            VolumeGateAttackThreshold = OdinDefaults.VolumeGateAttackLoudness;
            VolumeGateReleaseThreshold = OdinDefaults.VolumeGateReleaseLoudness;
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
                OdinLog.LogInfo($"{gameObject.name} does not have a {nameof(OdinDecoder)} or {nameof(OdinEncoder)} to add {nameof(VadEffect)} for this {nameof(OdinVadComponent)}");
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
                        OdinLog.LogInfo($"{gameObject.name} {nameof(OdinVadComponent)} can not create/add {nameof(VadEffect)} without a pipeline");
                        _warn = false;
                    }
                    return;
                }

                if (pipeline.AddVadEffect(out VadEffect vad))
                {
                    Effect = vad;
                    _effectPipeline = pipeline;
                    UpdateConfig(pipeline);
                    IsCreated = true;
                    OdinLog.LogInfo($"{gameObject.name} {nameof(OdinVadComponent)} added {nameof(VadEffect)} (id {Effect.Id})");
                }
                else if(_warn)
                {
                    OdinLog.LogError($"{gameObject.name} {nameof(OdinVadComponent)} error in {nameof(MediaPipeline.AddVadEffect)}");
                }
                _warn = true;
            }
            else if (Effect != null)
            {
                // update config on change like the sibling apm/vi components
                if (Effect.VoiceActivityEnabled != VoiceActivityEnabled ||
                    Effect.VoiceActivityAttackThreshold != VoiceActivityAttackThreshold ||
                    Effect.VoiceActivityReleaseThreshold != VoiceActivityReleaseThreshold ||
                    Effect.VolumeGateEnabled != VolumeGateEnabled ||
                    Effect.VolumeGateAttackThreshold != VolumeGateAttackThreshold ||
                    Effect.VolumeGateReleaseThreshold != VolumeGateReleaseThreshold)
                    UpdateConfig();
            }
        }

        public void UpdateConfig() => UpdateConfig(Media?.GetPipeline());
        public void UpdateConfig(MediaPipeline pipeline)
        {
            OdinLog.Assert(pipeline != null, $"{nameof(OdinVadComponent)} {nameof(UpdateConfig)} {nameof(MediaPipeline)} is null");
            OdinLog.Assert(Effect != null, $"{nameof(OdinVadComponent)} {nameof(UpdateConfig)} {nameof(VadEffect)} is null");

            if (Effect == null) return;
            // the config write must target the pipeline the effect was created on
            if (ReferenceEquals(pipeline, _effectPipeline) == false) return;

            Effect.VoiceActivityEnabled = VoiceActivityEnabled;
            Effect.VoiceActivityAttackThreshold = VoiceActivityAttackThreshold;
            Effect.VoiceActivityReleaseThreshold = VoiceActivityReleaseThreshold;
            Effect.VolumeGateEnabled = VolumeGateEnabled;
            Effect.VolumeGateAttackThreshold = VolumeGateAttackThreshold;
            Effect.VolumeGateReleaseThreshold = VolumeGateReleaseThreshold;
            Effect.UpdateEffectConfig();
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

        public void ResetEffect() => ResetEffect(Media?.GetPipeline());
        public void ResetEffect(MediaPipeline pipeline)
        {
            OdinLog.Assert(pipeline != null, $"{nameof(OdinVadComponent)} {nameof(ResetEffect)} {nameof(MediaPipeline)} is null");
            if (pipeline == null) return;

            ClearEffect();

            if (pipeline.AddVadEffect(out VadEffect effect))
            {
                Effect = effect;
                _effectPipeline = pipeline;
                IsCreated = true;
                UpdateConfig(pipeline);
            }
        }

        void OnDestroy()
        {
            ClearEffect();
        }

        public T GetMedia<T>() where T : IMedia => (T)Media;
        public PipelineEffect GetEffect() => Effect;
    }
}